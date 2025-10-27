
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChangeColorWithSetting : UdonSharpBehaviour
{
    [SerializeField] GameController gameController;
    [SerializeField] public int[] material_slots;
    [SerializeField] public bool use_color_index = true;
    [SerializeField] public int color_index;
    [SerializeField] public bool set_color_as_rgb_pattern = false;
    [SerializeField] public Color32 set_color;
    [SerializeField] public bool sync_set_color_with_renderer = false;
    [SerializeField] public Renderer renderer_to_sync_to;
    [SerializeField] public bool pulse_color = false;
    [SerializeField] public float pulse_speed = 4.0f;
    [SerializeField] public Renderer[] renderers_to_change;
    [NonSerialized] public Color32 cached_color;
    [NonSerialized] private float hue_angle = 0.0f;
    [NonSerialized] private float pulse_timer = 0.0f;
    [NonSerialized] private bool pulse_halfway_complete = false;
    [NonSerialized] public byte stored_alpha;

    void Start()
    {
        cached_color = Color.black;
        if (material_slots == null || material_slots.Length == 0)
        {
            material_slots = new int[1];
            material_slots[0] = 0;
        }
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        if (gameController != null && gameController.flag_for_mobile_vr.activeInHierarchy) { pulse_color = false; }
        if (pulse_color) { stored_alpha = set_color.a; }
    }

    private void Update()
    {
        if (!use_color_index && set_color_as_rgb_pattern)
        {
            if (hue_angle < 1) { hue_angle += Time.deltaTime / 20.0f; }
            else { hue_angle = 0.0f; }
            set_color = (Color32)Color.HSVToRGB(hue_angle, 0.7f, 0.7f);
        }
        else if (!use_color_index && sync_set_color_with_renderer && renderer_to_sync_to != null)
        {
            set_color = (Color32)renderer_to_sync_to.material.color;
        }

        if (pulse_color && pulse_timer < pulse_speed)
        {
            pulse_timer += Time.deltaTime;
            set_color.a = (byte)Mathf.RoundToInt(Mathf.Lerp(0, stored_alpha, pulse_halfway_complete ? pulse_timer / pulse_speed : 1 - pulse_timer / pulse_speed));
        }
        else if (pulse_color && pulse_timer >= pulse_speed)
        {
            pulse_halfway_complete = !pulse_halfway_complete;
            pulse_timer = 0.0f;
        }
    }

    private void LateUpdate()
    {
        if (renderers_to_change == null || renderers_to_change.Length <= 0) { return; }

        if (use_color_index)
        {
            if (gameController == null || gameController.team_colors == null || gameController.team_colors.Length <= color_index || gameController.team_colors_bright == null || gameController.team_colors_bright.Length <= color_index) { return; }
            if (color_index < 0)
            {
                if (cached_color.r != Color.white.r
                    || cached_color.g != Color.white.g
                    || cached_color.b != Color.white.b)
                {
                    cached_color = Color.white;
                    ChangeColors();
                }
            }
            else
            {
                if (cached_color.r != gameController.team_colors[color_index].r
                    || cached_color.g != gameController.team_colors[color_index].g
                    || cached_color.b != gameController.team_colors[color_index].b)
                {
                    cached_color = gameController.team_colors[color_index];
                    ChangeColors();
                }
            }
        }
        else
        {
            if (cached_color.r != set_color.r
                || cached_color.g != set_color.g
                || cached_color.b != set_color.b
                || cached_color.a != set_color.a)
            {
                cached_color = set_color;
                ChangeColors();
            }
        }
    }

    public void ChangeColors()
    {
        if (renderers_to_change == null || renderers_to_change.Length <= 0) { return; }
        foreach (Renderer r in renderers_to_change)
        {
            if (material_slots == null || material_slots.Length == 0)
            {
                material_slots = new int[1];
                material_slots[0] = 0;
            }

            if (material_slots != null && material_slots.Length > 0)
            {
                for (int i = 0; i < material_slots.Length; i++)
                {
                    if (r.materials == null || r.materials.Length <= 0) { break; }
                    if (material_slots[i] >= r.materials.Length) { continue; }
                    Material mat = r.materials[material_slots[i]];
                    if (use_color_index)
                    {
                        if (gameController == null || gameController.team_colors == null || gameController.team_colors.Length <= color_index || gameController.team_colors_bright == null || gameController.team_colors_bright.Length <= color_index) { return; }
                        if (color_index < 0)
                        {
                            mat.SetColor("_Color", Color.white);
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", Color.white);
                        }
                        else
                        {
                            mat.SetColor("_Color", gameController.team_colors_bright[color_index]);
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", gameController.team_colors[color_index]);
                        }
                    }
                    else
                    {
                        mat.SetColor("_Color", set_color);
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", set_color);
                    }
                }
            }


        }
    }
}
