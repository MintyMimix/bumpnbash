
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UIPainIndicator : UdonSharpBehaviour
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

    public void StartTimer()
    {
        if (duration > 0.0f && axis != null && sprite != null && pointTowards != null) { isOn = true; }
        else { Destroy(gameObject); }
    }

    public void Update()
    {
        // Below only occurs if active
        if (!isOn || !gameObject.activeInHierarchy) { return; }

        // Handle timer
        if (timer < duration) { timer += Time.deltaTime; }
        else { Destroy(gameObject); }

        // Handle alpha
        Color color = sprite.color;
        float fade_at_time = (duration * fade_at_pct);
        if (timer < fade_at_time) { color.a = 1.0f; }
        else { color.a = ((timer - fade_at_time) / (duration - fade_at_time)); }
        sprite.color = color;

        // Handle rotation
        Vector3 targetDir = (transform.position - pointTowards).normalized;
        Quaternion rotateTo = Quaternion.LookRotation(targetDir, Vector3.up);
        rotateTo.x = 0; rotateTo.y = 0;
        axis.rotation = rotateTo;
    }

}
