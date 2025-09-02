
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UIHarmNumber : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public TMP_Text ui_text;
    [SerializeField] public int display_value = 0;
    [SerializeField] public float duration = 2.5f;
    [SerializeField] public float fade_at_pct = 0.80f;
    [SerializeField] public float lower_at_pct = 0.40f;
    [SerializeField] public float offset = 0.1f;

    [NonSerialized] public int global_index = -1;
    [NonSerialized] public int ref_index = -1;

    [NonSerialized] public float timer = 0.0f;
    [NonSerialized] public Vector3 origin = Vector3.zero;
    //[NonSerialized] public float ply_init_distance = 0.0f;
    //[NonSerialized] public Vector3 scale_init = Vector3.zero;
    //[NonSerialized] public float scale_ppp = 1.0f;
    //[NonSerialized] public Vector3 scale_init_stored = Vector3.zero;

    [NonSerialized] public bool isOn = false;
    [NonSerialized] public GameObject ui_parent;
    [NonSerialized] public int target_id = -1;
    [NonSerialized] public bool waiting_for_destruction = false;

    public void StartTimer()
    {
        //scale_init = transform.localScale;
        if (duration > 0.0f && ui_text != null) { isOn = true; } //&& ui_parent != null && internal_id >= 0
        else { ResetDisplay(); }
        //else { Destroy(gameObject); }
    }

    public void ResetDisplay()
    {
        duration = 0.0f;
        display_value = 0;
        isOn = false;
        gameObject.SetActive(false);
    }

    public void Update()
    {
        // Below only occurs if active
        if (!isOn || !gameObject.activeInHierarchy) { return; }

        // Handle timer
        if (timer < duration) { timer += Time.deltaTime; }
        else if (ui_parent != null && !waiting_for_destruction) { ui_parent.GetComponent<UIPlyToSelf>().ReleaseHarmNumber(target_id, gameObject); waiting_for_destruction = true; } // timer = 0.0f;
        // else if (ui_parent != null && waiting_for_destruction) { Destroy(gameObject); }
        else if (ui_parent == null) { Destroy(gameObject); }

        // Handle alpha
        Color color = ui_text.color;
        float fade_at_time = (duration * fade_at_pct);
        if (timer < fade_at_time) { color.a = 1.0f; }
        else { color.a = 1.0f - ((timer - fade_at_time) / (duration - fade_at_time)); }
        ui_text.color = color;

        // Handle position
        float x_offset = 0.0f; float y_offset = 0.0f; float lower_at_time = duration * (fade_at_pct - lower_at_pct);
        //if (timer < lower_at_time) { y_offset = offset * (timer / lower_at_time); }
        //else { y_offset = offset - Mathf.Pow((offset * ((timer - lower_at_time) / (duration - lower_at_time))), 2); }
        x_offset = 0.5f * offset * (timer / fade_at_time);
        y_offset = -0.5f * Mathf.Pow((offset * (timer / fade_at_time)) - (offset * (lower_at_time / fade_at_time)), 2) + Mathf.Pow(offset, 2);
        transform.position = origin + new Vector3(x_offset, y_offset, 0.0f);

        // Handle billboard
        transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

        // Handle scale
        Vector3 initScale = gameController.local_uiplytoself.PTSHarmNumberTemplate.transform.localScale;
        Vector3 ratio_desired = Vector3.zero;
        if (gameController != null && gameController.local_uiplytoself != null)
        {
            // scale of template / distance to template
            Vector3 calc_ui_pos = Vector3.zero;
            float ply_scale = 1.0f;
            if (gameController != null && gameController.local_plyAttr != null)
            {
                ply_scale = gameController.local_plyAttr.ply_scale;
            }
            calc_ui_pos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position +
                (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * (0.5f * (1 / ply_scale) * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f)));
            ratio_desired =
                initScale
                /
                Mathf.Abs(Vector3.Distance(Networking.LocalPlayer.GetPosition(), calc_ui_pos));
        }
        transform.localScale = (1.0f/200.0f) * ratio_desired * Mathf.Abs(Vector3.Distance(Networking.LocalPlayer.GetPosition(), origin));
        if (gameController.local_ppp_options != null) { transform.localScale *= gameController.local_ppp_options.ui_harm_scale; }
    }

    public void UpdateValue(int in_value, bool add_value = true)
    {
        if (add_value) { display_value += in_value; }
        else { display_value = in_value; }
        ui_text.text = display_value.ToString() + "%";
        timer = 0.0f;
    }

}
