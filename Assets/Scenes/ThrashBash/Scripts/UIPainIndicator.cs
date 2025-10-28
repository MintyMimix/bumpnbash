
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UIPainIndicator : GlobalTickReceiver
{
    [SerializeField] public RectTransform axis;
    [SerializeField] public UnityEngine.UI.Image sprite;

    [SerializeField] public float min_duration = 0.0f;
    [SerializeField] public float max_duration = 0.0f;

    [SerializeField] public float fade_at_pct = 0.35f;
    [NonSerialized] public float duration = 0.0f;
    [NonSerialized] public float timer = 0.0f;
    [NonSerialized] public bool isOn = false;
    [NonSerialized] public Vector3 pointTowards;
    public override void Start()
    {
        base.Start();
    }

    public void StartTimer()
    {
        if (duration > 0.0f && axis != null && sprite != null && pointTowards != null) { isOn = true; }
        else { Destroy(gameObject); }
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        // Below only occurs if active
        if (!isOn || !gameObject.activeInHierarchy) { return; }

        // Handle timer
        if (timer < duration) { timer += tickDeltaTime; }
        else { Destroy(gameObject); }

        // Handle alpha
        Color color = sprite.color;
        float fade_at_time = (duration * fade_at_pct);
        if (timer < fade_at_time) { color.a = 1.0f; }
        else { color.a = 1.0f - ((timer - fade_at_time) / (duration - fade_at_time)); }
        sprite.color = color;
        
    }

    public void FixedUpdate()
    {
        RotateComponent();
    }

    public void RotateComponent()
    {
        // Handle rotation
        Vector3 targetVector = (transform.position - pointTowards);
        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        float angle = Vector3.SignedAngle(plyForward.normalized, targetVector.normalized, Vector3.up);
        axis.localEulerAngles = new Vector3(0, 0, -angle + 180);
    }

}
