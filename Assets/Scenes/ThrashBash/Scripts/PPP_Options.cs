
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PPP_Options : UdonSharpBehaviour
{
    [SerializeField] public float sync_impulse = 3.0f;
    [NonSerialized] public float sync_timer = 0.0f;
    [NonSerialized] public bool should_sync = false;
    [SerializeField] public GameController gameController;
    [NonSerialized] public GameObject harmTester;
    [SerializeField] public UnityEngine.UI.Toggle ui_hurtboxtoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_colorblindtoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_wristtoggle_l;
    [SerializeField] public UnityEngine.UI.Toggle ui_wristtoggle_r;
    [SerializeField] public UnityEngine.UI.Toggle ui_spectatortoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_particletoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_haptictoggle;
    [SerializeField] public UnityEngine.UI.Slider ui_uiscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiseparationslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uistretchslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiotherscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiharmscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uimusicslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uisoundslider;
    [SerializeField] public TMP_Text ui_uiscaletext;
    [SerializeField] public TMP_Text ui_uiseparationtext;
    [SerializeField] public TMP_Text ui_uistretchtext;
    [SerializeField] public TMP_Text ui_uiotherscaletext;
    [SerializeField] public TMP_Text ui_uiharmscaletext;
    [SerializeField] public TMP_Text ui_uimusictext;
    [SerializeField] public TMP_Text ui_uisoundtext;
    [NonSerialized] public bool hurtbox_show = true;
    [NonSerialized] public bool colorblind = false;
    [NonSerialized] public bool haptics_on = true;
    [NonSerialized] public bool intend_to_be_spectator = false;
    [NonSerialized] public int ui_wrist = 0; // 0 = None, 1 = Left, 2 = Right
    [NonSerialized] public float ui_scale = 1.0f;
    [NonSerialized] public float ui_separation = 300.0f;
    [NonSerialized] public float ui_stretch = 1.0f;
    [NonSerialized] public float ui_harm_scale = 1.0f;
    [NonSerialized] public float ui_other_scale = 1.0f;
    [NonSerialized] public bool waiting_on_playerdata = true;
    [NonSerialized] public float music_volume = 1.0f;
    [NonSerialized] public float sound_volume = 1.0f;
    [NonSerialized] public bool particles_on = true;
    [NonSerialized] public bool lock_wrist = false;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        harmTester = gameController.GetChildTransformByName(transform, "HarmTester").gameObject;
    }

    private void Update()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        else
        {
            ui_spectatortoggle.interactable = !(gameController.round_state == (int)round_state_name.Queued || gameController.round_state == (int)round_state_name.Loading || gameController.round_state == (int)round_state_name.Ready);
        }

        if (sync_timer < sync_impulse)
        {
            sync_timer += Time.deltaTime;
        }
        else
        {
            sync_timer = 0.0f;
            if (should_sync) { SyncData(); }
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
        UpdateUISeparation();
        UpdateUIStretch();
        UpdateUIWrist();
        UpdateSpectatorIntent();
        UpdateHurtbox();
        UpdateColorblind();
        UpdateParticles();
        UpdateMusicVolume();
        UpdateSoundVolume();
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal)
        {
            bool should_refresh = false;
            for (int i = 0; i < infos.Length; i++)
            {
                if (!PlayerData.HasKey(player, "UIScale"))
                {
                    ui_uiscaleslider.value = 10.0f;
                    UpdateUIScale();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UIVertical"))
                {
                    ui_uiseparationslider.value = 10.0f;
                    UpdateUISeparation();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UIStretch"))
                {
                    ui_uistretchslider.value = 10.0f;
                    UpdateUIStretch();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UIOtherScale"))
                {
                    ui_uiotherscaleslider.value = 10.0f;
                    UpdateUIOtherScale();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UIHarmScale"))
                {
                    ui_uiharmscaleslider.value = 10.0f;
                    UpdateUIHarmScale();
                    continue;
                }
                if (!PlayerData.HasKey(player, "MusicVolume"))
                {
                    ui_uimusicslider.value = 10.0f;
                    UpdateMusicVolume();
                    continue;
                }
                if (!PlayerData.HasKey(player, "SoundVolume"))
                {
                    ui_uisoundslider.value = 10.0f;
                    UpdateSoundVolume();
                    continue;
                }
                if (!PlayerData.HasKey(player, "Colorblind"))
                {
                    ui_colorblindtoggle.isOn = false;
                    UpdateColorblind();
                    continue;
                }
                if (!PlayerData.HasKey(player, "HurtboxShow"))
                {
                    ui_hurtboxtoggle.isOn = true;
                    UpdateHurtbox();
                    continue;
                }
                if (!PlayerData.HasKey(player, "ParticleShow"))
                {
                    if (gameController != null && gameController.flag_for_mobile_vr != null)
                    {
                        if (!gameController.flag_for_mobile_vr.activeInHierarchy) { ui_particletoggle.isOn = true; }
                        else
                        {
                            ui_particletoggle.isOn = false;
                            ui_particletoggle.interactable = false;
                        }
                    }
                    UpdateParticles();
                    continue;
                }
                if (!PlayerData.HasKey(player, "HapticsOn"))
                {
                    if (Networking.LocalPlayer.IsUserInVR()) { ui_haptictoggle.isOn = true; }
                    else
                    {
                        ui_haptictoggle.isOn = false;
                        ui_haptictoggle.interactable = false;
                    }
                    UpdateHaptics();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UIWrist"))
                {
                    ui_wristtoggle_l.isOn = false;
                    ui_wristtoggle_r.isOn = false;
                    UpdateUIWrist();
                    continue;
                }
                if (infos[i].State == PlayerData.State.Restored) //|| infos[i].State == PlayerData.State.Changed
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
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIVertical", out out_UISeparation))
        {
            ui_uiseparationslider.value = out_UISeparation;
        }
        else
        {
            ui_uiseparationslider.value = 10.0f;
            UpdateUISeparation();
        }

        float out_UIStretch;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIStretch", out out_UIStretch))
        {
            ui_uistretchslider.value = out_UIStretch;
        }
        else
        {
            ui_uistretchslider.value = 10.0f;
            UpdateUIStretch();
        }

        float out_UIOtherScale;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIOtherScale", out out_UIOtherScale))
        {
            ui_uiotherscaleslider.value = out_UIOtherScale;
        }
        else
        {
            ui_uiotherscaleslider.value = 10.0f;
            UpdateUIOtherScale();
        }

        float out_UIHarmScale;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIHarmScale", out out_UIHarmScale))
        {
            ui_uiharmscaleslider.value = out_UIHarmScale;
        }
        else
        {
            ui_uiharmscaleslider.value = 10.0f;
            UpdateUIHarmScale();
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

        bool out_Colorblind;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "Colorblind", out out_Colorblind))
        {
            ui_colorblindtoggle.isOn = out_Colorblind;
        }
        else
        {
            ui_colorblindtoggle.isOn = false;
            UpdateColorblind();
        }

        bool out_HurtboxShow;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "HurtboxShow", out out_HurtboxShow))
        {
            ui_hurtboxtoggle.isOn = out_HurtboxShow;
        }
        else
        {
            ui_hurtboxtoggle.isOn = true;
            UpdateHurtbox();
        }

        bool out_ParticleShow;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "ParticleShow", out out_ParticleShow))
        {
            if (!gameController.flag_for_mobile_vr.activeInHierarchy) { ui_particletoggle.isOn = out_ParticleShow; }
            else
            {
                ui_particletoggle.isOn = false;
                ui_particletoggle.interactable = false;
            }
        }
        else
        {
            if (gameController != null && gameController.flag_for_mobile_vr != null)
            {
                if (!gameController.flag_for_mobile_vr.activeInHierarchy) { ui_particletoggle.isOn = true; }
                else
                {
                    ui_particletoggle.isOn = false;
                    ui_particletoggle.interactable = false;
                }
            }
            UpdateParticles();
        }

        if (Networking.LocalPlayer.IsUserInVR())
        {
            int out_UIWrist;
            if (PlayerData.TryGetInt(Networking.LocalPlayer, "UIWrist", out out_UIWrist))
            {
                ui_wristtoggle_l.isOn = out_UIWrist == 1;
                ui_wristtoggle_r.isOn = out_UIWrist == 2;
            }
            else
            {
                ui_wristtoggle_l.isOn = false;
                ui_wristtoggle_r.isOn = false;
                UpdateUIWrist();
            }

            bool out_UIHaptics;
            if (PlayerData.TryGetBool(Networking.LocalPlayer, "HapticsOn", out out_UIHaptics))
            {
                ui_haptictoggle.isOn = out_UIHaptics;
            }
            else
            {
                ui_haptictoggle.isOn = true;
                UpdateHaptics();
            }
        }
        else
        {
            ui_wristtoggle_l.isOn = false;
            ui_wristtoggle_r.isOn = false;
            ui_haptictoggle.isOn = false;
            ui_wristtoggle_l.interactable = false;
            ui_wristtoggle_r.interactable = false;
            ui_haptictoggle.interactable = false;
        }

    }

    public void SyncData()
    {
        if (waiting_on_playerdata) { return; }
        PlayerData.SetFloat("UIScale", ui_uiscaleslider.value);
        PlayerData.SetFloat("UIVertical", ui_uiseparationslider.value);
        PlayerData.SetFloat("UIStretch", ui_uistretchslider.value);
        PlayerData.SetFloat("UIOtherScale", ui_uiotherscaleslider.value);
        PlayerData.SetFloat("UIHarmScale", ui_uiharmscaleslider.value);
        PlayerData.SetFloat("MusicVolume", ui_uimusicslider.value);
        PlayerData.SetFloat("SoundVolume", ui_uisoundslider.value);
        PlayerData.SetBool("Colorblind", ui_colorblindtoggle.isOn);
        PlayerData.SetBool("HurtboxShow", ui_hurtboxtoggle.isOn);
        if (!gameController.flag_for_mobile_vr.activeInHierarchy) { PlayerData.SetBool("ParticleShow", ui_particletoggle.isOn); }
        if (Networking.LocalPlayer.IsUserInVR()) { PlayerData.SetBool("HapticsOn", ui_haptictoggle.isOn); }
        if (Networking.LocalPlayer.IsUserInVR()) { PlayerData.SetInt("UIWrist", ui_wrist); }
        should_sync = false;
        sync_timer = 0.0f;
    }


    public void UpdateUIScale()
    {
        ui_scale = ui_uiscaleslider.value / 10.0f;
        ShowDemoUI();
        ui_uiscaletext.text = "UI Scale: " + (ui_scale * 100.0f) + "%";
        if ((ui_scale * 10.0f) <= ui_uiscaleslider.minValue) { ui_uiscaletext.color = Color.red; }
        else { ui_uiscaletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUISeparation()
    {
        ui_separation = ui_uiseparationslider.value / 10.0f;
        ShowDemoUI();
        ui_uiseparationtext.text = "UI Vertical: " + (ui_separation * 100.0f) + "%";
        if ((ui_separation * 10.0f) <= ui_uiseparationslider.minValue) { ui_uiseparationtext.color = Color.red; }
        else { ui_uiseparationtext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIStretch()
    {
        ui_stretch = ui_uistretchslider.value / 10.0f;
        ShowDemoUI();
        ui_uistretchtext.text = "UI Horizontal: " + (ui_stretch * 100.0f) + "%";
        if ((ui_stretch * 10.0f) <= ui_uistretchslider.minValue) { ui_uistretchtext.color = Color.red; }
        else { ui_uistretchtext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }


    public void UpdateUIOtherScale()
    {
        ui_other_scale = ui_uiotherscaleslider.value / 10.0f;
        ShowDemoUI();
        ui_uiotherscaletext.text = "Overhead UI Scale: " + (ui_other_scale * 100.0f) + "%";
        if ((ui_other_scale * 10.0f) <= ui_uiotherscaleslider.minValue) { ui_uiotherscaletext.color = Color.red; }
        else { ui_uiotherscaletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }


    public void UpdateUIHarmScale()
    {
        ui_harm_scale = ui_uiharmscaleslider.value / 10.0f;
        ShowDemoUI();
        ui_uiharmscaletext.text = "Damage Display UI Scale: " + (ui_harm_scale * 100.0f) + "%";
        if ((ui_harm_scale * 10.0f) <= ui_uiharmscaleslider.minValue) { ui_uiharmscaletext.color = Color.red; }
        else { ui_uiharmscaletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateMusicVolume()
    {
        music_volume = ui_uimusicslider.value / 10.0f;

        ui_uimusictext.text = "Music Volume: " + (music_volume * 100.0f) + "%";
        if ((music_volume * 10.0f) <= ui_uimusicslider.minValue) { ui_uimusictext.color = Color.red; }
        else { ui_uimusictext.color = Color.white; }

        gameController.snd_game_music_source.volume = gameController.music_volume_default * music_volume;

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateSoundVolume()
    {
        sound_volume = ui_uisoundslider.value / 10.0f;

        ui_uisoundtext.text = "Sound Volume: " + (sound_volume * 100.0f) + "%";
        if ((sound_volume * 10.0f) <= ui_uisoundslider.minValue) { ui_uisoundtext.color = Color.red; }
        else { ui_uisoundtext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateColorblind()
    {
        colorblind = ui_colorblindtoggle.isOn;
        gameController.RefreshSetupUI();
        ShowDemoUI();

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateHurtbox()
    {
        hurtbox_show = ui_hurtboxtoggle.isOn;
        gameController.RefreshSetupUI();

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateParticles()
    {
        particles_on = ui_particletoggle.isOn;
        gameController.RefreshSetupUI();
        ShowDemoUI();
        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;

    }

    public void UpdateHaptics()
    {
        haptics_on = ui_haptictoggle.isOn;
        gameController.RefreshSetupUI();

        if (haptics_on && gameController.local_plyAttr != null) { gameController.local_plyAttr.TryHapticEvent((int)game_sfx_name.ENUM_LENGTH); }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIWrist()
    {
        if (ui_wristtoggle_l.isOn)
        {
            ui_wrist = 1;
        }
        else if (ui_wristtoggle_r.isOn)
        {
            ui_wrist = 2;
        }
        else
        {
            ui_wrist = 0;
        }

        gameController.RefreshSetupUI();
        lock_wrist = false;
        ShowDemoUI();

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIWristL()
    {
        if (!lock_wrist)
        {
            if (ui_wristtoggle_l.isOn) { ui_wristtoggle_r.isOn = false; } // We don't want to do inverse because we want both to be able to be off
            UpdateUIWrist();
        }
    }

    public void UpdateUIWristR()
    {
        if (!lock_wrist)
        {
            if (ui_wristtoggle_r.isOn) { ui_wristtoggle_l.isOn = false; } // We don't want to do inverse because we want both to be able to be off
            UpdateUIWrist();
        }
    }

    public void UpdateSpectatorIntent()
    {
        intend_to_be_spectator = ui_spectatortoggle.isOn;
        gameController.RefreshSetupUI();
        gameController.LocalDeclareSpectatorIntent(intend_to_be_spectator);

    }

    public void ShowDemoUI()
    {
        if (gameController != null && gameController.local_uiplytoself != null)
        {
            if (!gameController.local_uiplytoself.ui_show_intro_text)
            {
                gameController.local_uiplytoself.ui_demo_timer = 0.0f;
                gameController.local_uiplytoself.ui_demo_enabled = true;
                gameController.local_uiplytoself.TestHarmNumber();
                if (gameController.local_plyweapon != null)
                {
                    gameController.local_plyweapon.PlayHapticEvent((int)game_sfx_name.HitSend);
                }
            }
        }
    }


    // Spectator does not require persistence
    /*public void SpectateGame()
    {
        intend_to_be_spectator = ui_spectatortoggle.isOn;
        if (intend_to_be_spectator)
        {
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, -1, false);
        }
        else
        {
            if (intend_to_be_spectator)
            {
                gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, -3, false);
            }
        }
        gameController.RefreshSetupUI();
    }*/
}
