
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class PPP_Options : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public UnityEngine.UI.Toggle ui_colorblindtoggle;
    [SerializeField] public UnityEngine.UI.Slider ui_uiscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiseparationslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uimusicslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uisoundslider;
    [SerializeField] public TMP_Text ui_uiscaletext;
    [SerializeField] public TMP_Text ui_uiseparationtext;
    [SerializeField] public TMP_Text ui_uimusictext;
    [SerializeField] public TMP_Text ui_uisoundtext;
    [NonSerialized] public bool colorblind = false;
    [NonSerialized] public float ui_scale = 1.0f;
    [NonSerialized] public float ui_separation = 300.0f;
    [NonSerialized] public bool waiting_on_playerdata = true;
    [NonSerialized] public float music_volume = 1.0f;
    [NonSerialized] public float sound_volume = 1.0f;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }

    private void Update()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)  
    {
        if (!player.isLocal) { return; }
        //gameController.local_ppp_options = this;
        RefreshAllOptions();
        waiting_on_playerdata = false;
    }

    public void RefreshAllOptions()
    {
        RefreshComponents();
        UpdateUIScale();
        UpdateColorblind();
        UpdateMusicVolume();
        UpdateSoundVolume();
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal) {
            var should_refresh = false;
            for (int i = 0; i < infos.Length; i++)
            {

                if (!PlayerData.HasKey(player, "UIScale"))
                {
                    ui_uiscaleslider.value = 10.0f;
                    UpdateUIScale();
                    break;
                }
                if (!PlayerData.HasKey(player, "UISeparation"))
                {
                    ui_uiseparationslider.value = 30.0f;
                    UpdateUISeparation();
                    break;
                }
                if (!PlayerData.HasKey(player, "MusicVolume"))
                {
                    ui_uimusicslider.value = 10.0f;
                    UpdateMusicVolume();
                    break;
                }
                if (!PlayerData.HasKey(player, "SoundVolume"))
                {
                    ui_uisoundslider.value = 10.0f;
                    UpdateSoundVolume();
                    break;
                }
                if (infos[i].State == PlayerData.State.Restored || infos[i].State == PlayerData.State.Changed)
                { 
                    should_refresh = true; 
                    break; 
                }
            }
            if (should_refresh) { RefreshComponents(); }
        }
        else if (Networking.GetOwner(gameObject) != Networking.LocalPlayer && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        else if (Networking.GetOwner(gameObject) == Networking.LocalPlayer && !gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
    }

    public void RefreshComponents()
    {
        float out_UIScale;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIScale", out out_UIScale))
        {
            ui_uiscaleslider.value = out_UIScale;
        }
        else
        {
            ui_uiscaleslider.value = 10.0f;
            UpdateUIScale();
        }

        float out_UISeparation;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UISeparation", out out_UISeparation))
        {
            ui_uiseparationslider.value = out_UISeparation;
        }
        else
        {
            ui_uiseparationslider.value = 30.0f;
            UpdateUISeparation();
        }

        float out_MusicVolume;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "MusicVolume", out out_MusicVolume))
        {
            ui_uimusicslider.value = out_MusicVolume;
        }
        else
        {
            ui_uimusicslider.value = 10.0f;
            UpdateMusicVolume();
        }

        float out_SoundVolume;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "SoundVolume", out out_SoundVolume))
        {
            ui_uisoundslider.value = out_SoundVolume;
        }
        else
        {
            ui_uisoundslider.value = 10.0f;
            UpdateSoundVolume();
        }

        ui_colorblindtoggle.isOn = PlayerData.GetBool(Networking.LocalPlayer, "Colorblind");
    }

    public void UpdateUIScale()
    {
        ui_scale = ui_uiscaleslider.value / 10.0f;
        GameObject ui_plyself_obj = gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "UIPlyToSelf");
        UIPlyToSelf ui_plyself_script = null;
        if (ui_plyself_obj != null) { ui_plyself_script = ui_plyself_obj.GetComponent<UIPlyToSelf>(); }
        if (ui_plyself_obj != null && ui_plyself_script != null)
        {
            if (!ui_plyself_script.ui_show_intro_text)
            {
                ui_plyself_script.ui_demo_timer = 0.0f;
                ui_plyself_script.ui_demo_enabled = true;
            }
        }
        ui_uiscaletext.text = "UI Scale: " + (ui_scale * 100.0f) + "%";
        if ((ui_scale * 10.0f) <= ui_uiscaleslider.minValue)  { ui_uiscaletext.color = Color.red; }
        else { ui_uiscaletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        PlayerData.SetFloat("UIScale", ui_uiscaleslider.value);
    }

    public void UpdateUISeparation()
    {
        ui_separation = ui_uiseparationslider.value * 10.0f;
        GameObject ui_plyself_obj = gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "UIPlyToSelf");
        if (ui_plyself_obj != null && ui_plyself_obj.GetComponent<UIPlyToSelf>() != null)
        {
            ui_plyself_obj.GetComponent<UIPlyToSelf>().ui_demo_timer = 0.0f;
            ui_plyself_obj.GetComponent<UIPlyToSelf>().ui_demo_enabled = true;
        }
        ui_uiseparationtext.text = "UI Separation: " + (ui_separation * 1.0f) + "\n (Default: 300)";
        if ((ui_separation * 10.0f) <= ui_uiseparationslider.minValue) { ui_uiseparationtext.color = Color.red; }
        else { ui_uiseparationtext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        PlayerData.SetFloat("UISeparation", ui_uiseparationslider.value);
    }

    public void UpdateMusicVolume()
    {
        music_volume = ui_uimusicslider.value / 10.0f;

        ui_uimusictext.text = "Music Volume: " + (music_volume * 10.0f);
        if ((music_volume * 10.0f) <= ui_uimusicslider.minValue) { ui_uimusictext.color = Color.red; }
        else { ui_uimusictext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        PlayerData.SetFloat("MusicVolume", ui_uimusicslider.value);
        gameController.snd_game_music_source.volume = gameController.music_volume_default * music_volume;
    }

    public void UpdateSoundVolume()
    {
        sound_volume = ui_uisoundslider.value / 10.0f;

        ui_uisoundtext.text = "Sound Volume: " + (sound_volume * 10.0f);
        if ((sound_volume * 10.0f) <= ui_uisoundslider.minValue) { ui_uisoundtext.color = Color.red; }
        else { ui_uisoundtext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        PlayerData.SetFloat("SoundVolume", ui_uisoundslider.value);
    }

    public void UpdateColorblind()
    {
        colorblind = ui_colorblindtoggle.isOn;
        gameController.RefreshSetupUI();

        if (waiting_on_playerdata) { return; }
        PlayerData.SetBool("Colorblind", ui_colorblindtoggle.isOn);
        
    }

}
