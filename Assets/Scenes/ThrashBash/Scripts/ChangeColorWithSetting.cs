
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChangeColorWithSetting : UdonSharpBehaviour
{
    [SerializeField] GameController gameController;
    [SerializeField] public int color_index;
    [SerializeField] public Material[] materials_to_change;
    [NonSerialized] public Color32 cached_color; 

    void Start()
    {
        cached_color = Color.black;
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }

    private void LateUpdate()
    {
        if (gameController == null || gameController.team_colors == null || gameController.team_colors.Length <= color_index || gameController.team_colors_bright == null || gameController.team_colors_bright.Length <= color_index || materials_to_change == null || materials_to_change.Length <= 0) { return; }
        
        if (cached_color.r != gameController.team_colors[color_index].r
            || cached_color.g != gameController.team_colors[color_index].g
            || cached_color.b != gameController.team_colors[color_index].b)
        {
            cached_color = gameController.team_colors[color_index];
            ChangeColors();
        }
    }

    public void ChangeColors()
    {
        if (gameController == null || gameController.team_colors == null || gameController.team_colors.Length <= color_index || gameController.team_colors_bright == null || gameController.team_colors_bright.Length <= color_index || materials_to_change == null || materials_to_change.Length <= 0) { return; }
        foreach (Material mat in materials_to_change)
        {
            mat.SetColor("_Color", gameController.team_colors_bright[color_index]);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", gameController.team_colors[color_index]);
        }
    }
}
