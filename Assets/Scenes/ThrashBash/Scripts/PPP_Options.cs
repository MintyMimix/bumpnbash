
using System;
using System.Diagnostics.PerformanceData;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
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
    [NonSerialized] public Vector3 canvas_pos_init;
    [NonSerialized] public Quaternion canvas_rot_init;
    [SerializeField] public TMP_Text tutorial_text;
    [SerializeField] public PPP_Pickup ppp_pickup;
    [SerializeField] public UnityEngine.UI.Button close_button;
    [SerializeField] public UnityEngine.UI.Toggle ui_hurtboxtoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_hitboxtoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_colorblind_toggle;
    [SerializeField] public TMP_Dropdown ui_colorblind_dropdown;
    [SerializeField] public TMP_Text ui_colorblind_dropdown_caption;
    [SerializeField] public string[] ui_colorblind_dropdown_names;
    [SerializeField] public GameObject template_colorblind_flag;
    [SerializeField] public UnityEngine.UI.GridLayoutGroup ui_colorblind_grid;
    [SerializeField] public Sprite ui_flag_base_sprite;
    [NonSerialized] public GameObject[] colorblind_flags;
    [SerializeField] public UnityEngine.UI.Toggle ui_wristtoggle_n;
    [SerializeField] public UnityEngine.UI.Toggle ui_wristtoggle_l;
    [SerializeField] public UnityEngine.UI.Toggle ui_wristtoggle_r;
    [SerializeField] public UnityEngine.UI.Toggle ui_spectatortoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_particletoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_haptictoggle;
    [SerializeField] public UnityEngine.UI.Slider ui_uiscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiseparationslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uistretchslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uidistanceslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiotherscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiharmscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uimusicslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uisoundslider;
    [SerializeField] public TMP_Text ui_uiscaletext;
    [SerializeField] public TMP_Text ui_uiseparationtext;
    [SerializeField] public TMP_Text ui_uistretchtext;
    [SerializeField] public TMP_Text ui_uidistancetext;
    [SerializeField] public TMP_Text ui_uiotherscaletext;
    [SerializeField] public TMP_Text ui_uiharmscaletext;
    [SerializeField] public TMP_Text ui_uimusictext;
    [SerializeField] public TMP_Text ui_uisoundtext;
    [NonSerialized] public bool hitbox_show = true;
    [NonSerialized] public bool hurtbox_show = true;
    [NonSerialized] public bool colorblind = false;
    [NonSerialized] public int colorblind_choice = 0;
    [NonSerialized] public bool haptics_on = true;
    [NonSerialized] public bool intend_to_be_spectator = false;
    [NonSerialized] public int force_weapon_hand = 0; // 0 = None, 1 = Left, 2 = Right
    [NonSerialized] public int ui_wrist = 0; // 0 = None, 1 = Left, 2 = Right
    [NonSerialized] public float ui_scale = 1.0f;
    [NonSerialized] public float ui_separation = 300.0f;
    [NonSerialized] public float ui_stretch = 1.0f;
    [NonSerialized] public float ui_distance = 1.0f;
    [NonSerialized] public float ui_harm_scale = 1.0f;
    [NonSerialized] public float ui_other_scale = 1.0f;
    [NonSerialized] public bool waiting_on_playerdata = true;
    [NonSerialized] public float music_volume = 1.0f;
    [NonSerialized] public float sound_volume = 1.0f;
    [NonSerialized] public bool particles_on = true;
    [NonSerialized] public bool lock_wrist = false;

    [NonSerialized] public bool render_in_front = false;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        Transform harmtester_transform = GlobalHelperFunctions.GetChildTransformByName(transform, "HarmTester");
        if (harmtester_transform != null) { harmTester = harmtester_transform.gameObject; }

        // Make sure our placeholder newline character is replaced with the real version (inspector doesn't interpret it correctly when manually typed)
        for (int i = 0; i < ui_colorblind_dropdown_names.Length; i++)
        {
            ui_colorblind_dropdown_names[i] = ui_colorblind_dropdown_names[i].Replace("#", "  ");
        }
        ui_colorblind_dropdown.ClearOptions();
        ui_colorblind_dropdown.AddOptions(ui_colorblind_dropdown_names);

        canvas_pos_init = transform.position;
        canvas_rot_init = transform.rotation;
        close_button.gameObject.SetActive(false);
        if (gameController != null && gameController.ui_ppp_reset_pos_button != null) { gameController.ui_ppp_reset_pos_button.interactable = false; }
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
            bool allow_round_state = !(gameController.round_state == (int)round_state_name.Queued || gameController.round_state == (int)round_state_name.Loading || gameController.round_state == (int)round_state_name.Ready);
            bool allow_ply_team = false;
            bool allow_ply_state = false;
            if (gameController.local_plyAttr != null) 
            { 
                allow_ply_team = gameController.local_plyAttr.ply_team < 0;
                allow_ply_state = gameController.local_plyAttr.ply_state == (int)player_state_name.Spectator || gameController.local_plyAttr.ply_state == (int)player_state_name.Dead || gameController.local_plyAttr.ply_state == (int)player_state_name.Inactive || gameController.local_plyAttr.ply_state == (int)player_state_name.Joined;
            }
            ui_spectatortoggle.interactable = allow_ply_state && (allow_round_state || allow_ply_team);
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

        if (canvas_pos_init == Vector3.zero)
        {
            canvas_pos_init = transform.position;
            canvas_rot_init = transform.rotation;
        }
    }

    /*public override void PostLateUpdate()
    {
        if (render_in_front)
        {
            transform.position = Networking.LocalPlayer.GetPosition() + Vector3.forward;
        }
    }*/

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.isLocal) { return; }
        //gameController.local_ppp_options = this;
        RefreshAllOptions();
        waiting_on_playerdata = false;

        string tutorial_method = "pushing your E or F key";
        if (Networking.LocalPlayer.IsUserInVR()) { tutorial_method = "grabbing behind your head"; }
        tutorial_text.text = tutorial_text.text.Replace("$METHOD", tutorial_method);
    }

    public void RefreshAllOptions()
    {
        RefreshComponents();
        UpdateUIScale();
        UpdateUISeparation();
        UpdateUIStretch();
        UpdateUIDistance();
        UpdateUIWrist();
        UpdateSpectatorIntent();
        UpdateHitbox();
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
                if (!PlayerData.HasKey(player, "UIDistance"))
                {
                    ui_uidistanceslider.value = 10.0f;
                    UpdateUIDistance();
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
                if (!PlayerData.HasKey(player, "ColorblindSprite"))
                {
                    ui_colorblind_toggle.isOn = false;
                    UpdateColorblind();
                    continue;
                }
                if (!PlayerData.HasKey(player, "ColorblindChoice"))
                {
                    ui_colorblind_dropdown.value = 0;
                    UpdateColorblind();
                    continue;
                }
                if (!PlayerData.HasKey(player, "HitboxShow"))
                {
                    ui_hitboxtoggle.isOn = true;
                    UpdateHitbox();
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
                    ui_wristtoggle_n.isOn = true;
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
        else if (!Networking.IsOwner(gameObject) && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        else if (Networking.IsOwner(gameObject) && !gameObject.activeInHierarchy)
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

        float out_UIDistance;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIDistance", out out_UIDistance))
        {
            ui_uidistanceslider.value = out_UIDistance;
        }
        else
        {
            ui_uidistanceslider.value = 10.0f;
            UpdateUIDistance();
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

        bool out_Colorblind_sprite;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "ColorblindSprite", out out_Colorblind_sprite))
        {
            ui_colorblind_toggle.isOn = out_Colorblind_sprite;
        }
        else
        {
            ui_colorblind_toggle.isOn = false;
            UpdateColorblind();
        }

        int out_Colorblind_choice;
        if (PlayerData.TryGetInt(Networking.LocalPlayer, "ColorblindChoice", out out_Colorblind_choice))
        {
            ui_colorblind_dropdown.value = out_Colorblind_choice;
        }
        else
        {
            ui_colorblind_dropdown.value = out_Colorblind_choice;
            UpdateColorblind();
        }

        bool out_HitboxShow;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "HitboxShow", out out_HitboxShow))
        {
            ui_hitboxtoggle.isOn = out_HitboxShow;
        }
        else
        {
            ui_hitboxtoggle.isOn = true;
            UpdateHitbox();
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
                ui_wristtoggle_n.isOn = out_UIWrist == 0;
                ui_wristtoggle_l.isOn = out_UIWrist == 1;
                ui_wristtoggle_r.isOn = out_UIWrist == 2;
            }
            else
            {
                ui_wristtoggle_n.isOn = true;
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
            ui_wristtoggle_n.isOn = true;
            ui_wristtoggle_l.isOn = false;
            ui_wristtoggle_r.isOn = false;
            ui_haptictoggle.isOn = false;
            ui_wristtoggle_n.interactable = false;
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
        PlayerData.SetFloat("UIDistance", ui_uidistanceslider.value);
        PlayerData.SetFloat("UIOtherScale", ui_uiotherscaleslider.value);
        PlayerData.SetFloat("UIHarmScale", ui_uiharmscaleslider.value);
        PlayerData.SetFloat("MusicVolume", ui_uimusicslider.value);
        PlayerData.SetFloat("SoundVolume", ui_uisoundslider.value);
        PlayerData.SetBool("ColorblindSprite", ui_colorblind_toggle.isOn);
        PlayerData.SetInt("ColorblindChoice", ui_colorblind_dropdown.value);
        PlayerData.SetBool("HitboxShow", ui_hitboxtoggle.isOn);
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

    public void UpdateUIDistance()
    {
        ui_distance = ui_uidistanceslider.value / 10.0f;
        ShowDemoUI();
        ui_uidistancetext.text = "UI Distance: " + (ui_distance * 100.0f) + "%";
        if ((ui_distance * 10.0f) <= ui_uidistanceslider.minValue) { ui_uidistancetext.color = Color.red; }
        else { ui_uidistancetext.color = Color.white; }

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
        colorblind_choice = ui_colorblind_dropdown.value;
        colorblind = ui_colorblind_toggle.isOn;
        gameController.SetColorOptions(colorblind_choice);
        SetColorblindFlagColors();
        gameController.RefreshSetupUI();
        //ShowDemoUI();

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateHitbox()
    {
        hitbox_show = ui_hitboxtoggle.isOn;
        gameController.RefreshSetupUI();

        if (gameController.ply_object_plyhitbox != null && gameController.ply_object_plyhitbox.Length > gameController.ply_owners_cnt)
        {
            for (int i = 0; i < gameController.ply_owners_cnt; i++)
            {
                if (gameController.ply_object_plyhitbox[i] == null) { continue; }
                gameController.ply_object_plyhitbox[i].ToggleMaterial(hitbox_show);
            }
        }

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

    public void UpdateUIWristNone()
    {
        if (!lock_wrist)
        {
            if (ui_wristtoggle_n.isOn) 
            { 
                ui_wristtoggle_l.isOn = false;
                ui_wristtoggle_r.isOn = false;
            } 
            UpdateUIWrist();
        }
    }

    public void UpdateUIWristL()
    {
        if (!lock_wrist)
        {
            if (ui_wristtoggle_l.isOn) 
            {
                ui_wristtoggle_n.isOn = false;
                ui_wristtoggle_r.isOn = false;
            }
            UpdateUIWrist();
        }
    }

    public void UpdateUIWristR()
    {
        if (!lock_wrist)
        {
            if (ui_wristtoggle_r.isOn) 
            {
                ui_wristtoggle_n.isOn = false;
                ui_wristtoggle_l.isOn = false;
            } 
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

    public void ResetPPPCanvas()
    {
        render_in_front = false;
        transform.position = canvas_pos_init;
        transform.rotation = canvas_rot_init;
        close_button.gameObject.SetActive(false);
        if (gameController != null && gameController.ui_ppp_reset_pos_button != null) { gameController.ui_ppp_reset_pos_button.interactable = false; }
    }

    public void PushPPPCanvas()
    {
        render_in_front = true;
        float heightUI = 0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        Vector3 posFinal = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (plyForward * 6.5f * heightUI);
        transform.SetPositionAndRotation(
            posFinal
            , Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation
        );
        close_button.gameObject.SetActive(true);
        if (gameController != null && gameController.ui_ppp_reset_pos_button != null) { gameController.ui_ppp_reset_pos_button.interactable = true; }

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

    public void ColorblindTemplateInit()
    {
        // Setup the colorblind flag objects for demonstration
        colorblind_flags = new GameObject[gameController.team_colors_base.Length];
        for (int j = 0; j < gameController.team_colors_base.Length; j++)
        {
            colorblind_flags[j] = Instantiate(template_colorblind_flag, ui_colorblind_grid.transform);
            colorblind_flags[j].transform.GetChild(1).GetComponent<TMP_Text>().text = gameController.team_names[j].Split(' ')[0];
        }
        SetColorblindFlagColors();
        // After making the copies, set the template to be inactive
        template_colorblind_flag.SetActive(false);
    }

    public void SetColorblindFlagColors()
    {
        ui_colorblind_dropdown_caption.text = ui_colorblind_dropdown_caption.text.Replace("  ", "\n");

        if (colorblind_flags == null) { return; }
        for (int j = 0; j < colorblind_flags.Length; j++)
        {
            colorblind_flags[j].GetComponent<UnityEngine.UI.Image>().color = gameController.team_colors[j];
            colorblind_flags[j].transform.GetChild(1).GetComponent<TMP_Text>().color = gameController.team_colors_bright[j];
            if (colorblind)
            {
                colorblind_flags[j].GetComponent<UnityEngine.UI.Image>().sprite = gameController.team_sprites[j];
                colorblind_flags[j].transform.GetChild(0).gameObject.SetActive(false);
            }
            else
            {
                colorblind_flags[j].GetComponent<UnityEngine.UI.Image>().sprite = ui_flag_base_sprite;
                colorblind_flags[j].transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }
}
