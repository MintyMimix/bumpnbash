
using System;
using System.Diagnostics.PerformanceData;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UIScoreboardPanelTemplate : UdonSharpBehaviour
{
    // This object's purpose is to hold onto child references and local variables and nothing else
    [SerializeField] public GameController gameController;
    [SerializeField] public TMP_Text name_text;
    [SerializeField] public TMP_Text points_text;
    [SerializeField] public TMP_Text deaths_text;
    [SerializeField] public TMP_Text lives_text;
    [SerializeField] public GameObject points_obj;
    [SerializeField] public GameObject deaths_obj;
    [SerializeField] public GameObject lives_obj;
    [SerializeField] public UnityEngine.UI.Image lives_image;
    [SerializeField] public UnityEngine.UI.Image points_image;
    [SerializeField] public UnityEngine.UI.Image flag_image;
    [SerializeField] public UnityEngine.UI.Image pole_image;
    [SerializeField] public UnityEngine.UI.Image cb_image;
    [SerializeField] public Sprite points_sprite;
    [SerializeField] public Sprite timer_sprite;
    [SerializeField] public Sprite lives_sprite;
    [SerializeField] public Sprite damage_sprite;

    [NonSerialized] public VRCPlayerApi player;
    [NonSerialized] public PlayerAttributes plyAttr;

    public void RefreshAttributes()
    {
        if (player == null || plyAttr == null || gameController == null) { return; }
        name_text.text = player.displayName;

        points_text.text = plyAttr.ply_points.ToString();
        deaths_text.text = plyAttr.ply_deaths.ToString();
        lives_text.text = plyAttr.ply_lives.ToString();

        if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { cb_image.enabled = true; }
        else { cb_image.enabled = false; }
        flag_image.enabled = !cb_image.enabled;
        pole_image.enabled = flag_image.enabled;
        if (plyAttr.ply_team >= 0)
        {
            if (gameController.option_teamplay)
            {
                flag_image.color = gameController.team_colors[plyAttr.ply_team];
                cb_image.sprite = gameController.team_sprites[plyAttr.ply_team];
                if (gameController.team_colors_bright != null && plyAttr.ply_team < gameController.team_colors_bright.Length) { name_text.color = gameController.team_colors_bright[plyAttr.ply_team]; }
            }
            else
            {
                flag_image.color = Color.white;
                cb_image.sprite = gameController.team_sprites[0];
                name_text.color = Color.white;
            }
        }
        cb_image.color = flag_image.color;
        points_image.color = flag_image.color;


        if (gameController.option_gamemode == (int)gamemode_name.Survival || gameController.option_gamemode == (int)gamemode_name.BossBash)
        {
            lives_obj.SetActive(true);
            if (points_image.sprite != points_sprite) { points_image.sprite = points_sprite; }
            if (gameController.option_gamemode == (int)gamemode_name.BossBash && plyAttr.ply_team == 0) 
            {
                if (lives_image.sprite != damage_sprite) { lives_image.sprite = damage_sprite; }
                lives_text.text = plyAttr.ply_damage_dealt.ToString() + "%";
            }
            else if (lives_image.sprite != lives_sprite) { lives_image.sprite = lives_sprite; }
        }
        else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill) 
        {
            lives_obj.SetActive(false);
            if (lives_image.sprite != lives_sprite) { lives_image.sprite = lives_sprite; }
            if (points_image.sprite != timer_sprite) { points_image.sprite = timer_sprite; }
            points_text.text = (gameController.option_gm_goal - plyAttr.ply_points).ToString();
        }
        else
        {
            lives_obj.SetActive(false);
            if (lives_image.sprite != lives_sprite) { lives_image.sprite = lives_sprite; }
            if (points_image.sprite != points_sprite) { points_image.sprite = points_sprite; }
        }
    }
}
