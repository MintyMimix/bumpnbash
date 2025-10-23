// File: LookAtLocalPlayer.cs
// Rotates an object to face the LOCAL player efficiently in VRChat (UdonSharp-safe).
// Optimized: throttled updates, sqr-distance checks, tiny-angle early-outs, cached rotations.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("Udon/Utility/Look At Local Player")]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LookAtLocalPlayer : UdonSharpBehaviour
{
    [Header("About & Support ❤️")]
    [TextArea(3, 8)]
    public string about =
        "💖 Thank you for using Axinovium tools! 💖\n" +
        "This script is 100% free for creators.\n\n" +
        "Support future tools:\n👉 Patreon: https://www.patreon.com/Axinovium";

    [Header("Usage Info (Read Only)")]
    [TextArea(2, 5)]
    public string info =
        "Automatically rotates this object to face the LOCAL player at runtime.\n" +
        "Use Facing Axis + Custom Rotation Offset to match your model’s front.\n" +
        "Perf tips: Flatten Y, set Max Follow Distance, and reduce Updates Per Second.";

    [Header("Facing Settings")]
    [Tooltip("Which local axis should point TOWARD the player. 0=Forward(+Z) 1=Back(-Z) 2=Up(+Y) 3=Down(-Y) 4=Right(+X) 5=Left(-X)")]
    public int facingAxis = 0;

    [Tooltip("Face AWAY from the player (adds 180° yaw).")]
    public bool reverse = false;

    [Tooltip("Extra local rotation offset (degrees) to fine-tune orientation in any direction.")]
    public Vector3 customRotationOffset = Vector3.zero;

    [Tooltip("Keep upright by ignoring vertical difference (horizontal-only facing).")]
    public bool flattenY = true;

    [Header("Target Settings")]
    [Tooltip("Track the player's head (HMD) instead of base position.")]
    public bool useHeadPosition = true;

    [Tooltip("Stop updating rotation when the (flattened) distance to the player exceeds this value (meters). 0 = no limit.")]
    public float maxFollowDistance = 0f;

    [Header("Smoothing & Update Rate")]
    [Tooltip("Rotate smoothly instead of snapping.")]
    public bool smooth = true;

    [Tooltip("Degrees per second when smoothing.")]
    public float rotationSpeed = 360f;

    [Tooltip("How many times per second to update. 0 = every frame.")]
    public float updatesPerSecond = 60f;

    [Tooltip("Skip rotation if angle change is smaller than this (degrees).")]
    public float minAngleDelta = 0.25f;

    [Header("Axis Locks")]
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;

    // ─── Internals ──────────────────────────────────────────────────────────────
    private Transform _t;
    private bool _started;

    // Post-rotation = axis map + custom offset + reverse; applied after LookRotation
    private Quaternion _postRot;

    // Throttling
    private float _nextUpdateTime;

    // Distance caching
    private float _maxFollowDistanceSqr;   // -1 = disabled
    private float _prevMaxFollowDistance;  // to detect inspector changes

    // Param change caching
    private int _prevAxis;
    private Vector3 _prevOffset;
    private bool _prevReverse;

    private void Start()
    {
        _t = transform;
        _started = true;

        RebuildPostRotation();
        CacheParams();

        // Initialize distance cache
        _prevMaxFollowDistance = maxFollowDistance;
        _maxFollowDistanceSqr = (_prevMaxFollowDistance > 0f) ? _prevMaxFollowDistance * _prevMaxFollowDistance : -1f;

        _nextUpdateTime = 0f; // run immediately
    }

    private void LateUpdate()
    {
        if (!_started || _t == null) return;

        // Throttle updates
        if (updatesPerSecond > 0f)
        {
            if (Time.time < _nextUpdateTime) return;
            _nextUpdateTime = Time.time + (1f / updatesPerSecond);
        }

        // Recompute post-rotation if user changed axis/offset/reverse
        if (facingAxis != _prevAxis || reverse != _prevReverse || !Vec3Equal(customRotationOffset, _prevOffset))
        {
            RebuildPostRotation();
            CacheParams();
        }

        // Recompute distance cache if user changed maxFollowDistance at runtime
        if (!Mathf.Approximately(maxFollowDistance, _prevMaxFollowDistance))
        {
            _prevMaxFollowDistance = maxFollowDistance;
            _maxFollowDistanceSqr = (_prevMaxFollowDistance > 0f) ? _prevMaxFollowDistance * _prevMaxFollowDistance : -1f;
        }

        VRCPlayerApi local = Networking.LocalPlayer;
        if (!Utilities.IsValid(local)) return;

        // Player position (head or base)
        Vector3 targetPos = useHeadPosition
            ? local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position
            : local.GetPosition();

        // Direction to player (optionally flattened first so distance matches facing plane)
        Vector3 dir = targetPos - _t.position;
        if (flattenY) dir.y = 0f;

        float sqrMag = dir.sqrMagnitude;
        if (sqrMag < 0.000001f) return; // too close / undefined

        // Distance limit (disabled when < 0)
        if (_maxFollowDistanceSqr >= 0f && sqrMag > _maxFollowDistanceSqr) return;

        // Desired orientation: point selected axis toward dir
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up) * _postRot;

        // Skip tiny adjustments
        if (Quaternion.Angle(_t.rotation, targetRot) < minAngleDelta) return;

        Quaternion newRot = smooth
            ? Quaternion.RotateTowards(_t.rotation, targetRot, rotationSpeed * Time.deltaTime)
            : targetRot;

        // Axis locks
        if (lockX || lockY || lockZ)
        {
            Vector3 euler = newRot.eulerAngles;
            Vector3 cur = _t.rotation.eulerAngles;
            if (lockX) euler.x = cur.x;
            if (lockY) euler.y = cur.y;
            if (lockZ) euler.z = cur.z;
            newRot = Quaternion.Euler(euler);
        }

        _t.rotation = newRot;
    }

    // Build rotation that maps +Z to chosen axis, then apply custom offset and optional reverse
    private void RebuildPostRotation()
    {
        Quaternion axisRot;
        switch (ClampAxis(facingAxis))
        {
            case 0: axisRot = Quaternion.identity; break;            // Forward +Z
            case 1: axisRot = Quaternion.Euler(0f, 180f, 0f); break; // Back -Z
            case 2: axisRot = Quaternion.Euler(-90f, 0f, 0f); break; // Up +Y
            case 3: axisRot = Quaternion.Euler(90f, 0f, 0f); break;  // Down -Y
            case 4: axisRot = Quaternion.Euler(0f, 90f, 0f); break;  // Right +X
            case 5: axisRot = Quaternion.Euler(0f, -90f, 0f); break; // Left -X
            default: axisRot = Quaternion.identity; break;
        }

        Quaternion offsetRot = Quaternion.Euler(customRotationOffset);
        Quaternion reverseRot = reverse ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

        _postRot = axisRot * offsetRot * reverseRot;
    }

    private void CacheParams()
    {
        _prevAxis = ClampAxis(facingAxis);
        _prevOffset = customRotationOffset;
        _prevReverse = reverse;
    }

    private int ClampAxis(int a)
    {
        if (a < 0) return 0;
        if (a > 5) return 5;
        return a;
    }

    private bool Vec3Equal(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.0001f &&
               Mathf.Abs(a.y - b.y) < 0.0001f &&
               Mathf.Abs(a.z - b.z) < 0.0001f;
    }
}
