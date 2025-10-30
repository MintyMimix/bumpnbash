
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
    [SerializeField] public DebuggerPanel debuggerPanel;
    [SerializeField] public Localizer localizer;
    [SerializeField] public PPP_LocalizerContainer txt_container;
    [NonSerialized] public GameObject harmTester;
    [SerializeField] public Vector3 canvas_pos_init;
    [NonSerialized] public Quaternion canvas_rot_init;
    [SerializeField] public TMP_Text tutorial_text;
    [SerializeField] public PPP_Pickup ppp_pickup;
    [SerializeField] public UnityEngine.UI.Button close_button;
    [SerializeField] public UnityEngine.UI.Toggle ui_hurtboxtoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_hitboxtoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_colorblind_toggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_uiinvertedtoggle;
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
    [SerializeField] public UnityEngine.UI.Toggle ui_motionsicknessfloortoggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_motionsicknesscagetoggle;
    [SerializeField] public UnityEngine.UI.Slider ui_uiscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiseparationslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uistretchslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uidistanceslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiyoffsetslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiangleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uitextscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uitextoffsetslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiotherscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uiharmscaleslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uimusicslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uisoundslider;
    [SerializeField] public UnityEngine.UI.Slider ui_uivovolumelider;
    [SerializeField] public TMP_Text ui_uiscaletext;
    [SerializeField] public TMP_Text ui_uiseparationtext;
    [SerializeField] public TMP_Text ui_uistretchtext;
    [SerializeField] public TMP_Text ui_uidistancetext;
    [SerializeField] public TMP_Text ui_uiyoffsettext;
    [SerializeField] public TMP_Text ui_uiangletext;
    [SerializeField] public TMP_Text ui_uitextscaletext;
    [SerializeField] public TMP_Text ui_uitextoffsettext;
    [SerializeField] public TMP_Text ui_uiotherscaletext;
    [SerializeField] public TMP_Text ui_uiharmscaletext;
    [SerializeField] public TMP_Text ui_uimusictext;
    [SerializeField] public TMP_Text ui_uisoundtext;
    [SerializeField] public TMP_Text ui_uivovolumetext;
    [SerializeField] public TMP_Dropdown ui_uivotype_dropdown;
    [SerializeField] public TMP_Dropdown ui_musicoverride_dropdown;
    [SerializeField] public UnityEngine.UI.Toggle ui_audiolink_toggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_vo_pref_a_toggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_vo_pref_b_toggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_vo_pref_c_toggle;
    [SerializeField] public UnityEngine.UI.Toggle[] ui_language_toggles;
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
    [NonSerialized] public float ui_yoffset = 0.0f;
    [NonSerialized] public float ui_angle = 0.0f;
    [NonSerialized] public float ui_textscale = 1.0f;
    [NonSerialized] public float ui_textoffset = 0.0f;
    [NonSerialized] public float ui_distance = 1.0f;
    [NonSerialized] public float ui_harm_scale = 1.0f;
    [NonSerialized] public float ui_other_scale = 1.0f;
    [NonSerialized] public bool ui_inverted = false;
    [NonSerialized] public bool waiting_on_playerdata = true;
    [NonSerialized] public int music_override = -1;
    [NonSerialized] public float music_volume = 1.0f;
    [NonSerialized] public float sound_volume = 1.0f;
    [NonSerialized] public bool audiolink = true;
    [NonSerialized] public float voiceover_volume = 1.0f;
    [SerializeField] public int voiceover_type = 0;
    [NonSerialized] public bool vo_pref_a = true;
    [NonSerialized] public bool vo_pref_b = true;
    [NonSerialized] public bool vo_pref_c = true;
    [NonSerialized] public bool particles_on = true;
    [NonSerialized] public bool lock_wrist = false;
    [NonSerialized] public bool render_in_front = false;
    [NonSerialized] public bool motion_sickness_floor = false;
    [NonSerialized] public bool motion_sickness_cage = false;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        Transform harmtester_transform = GlobalHelperFunctions.GetChildTransformByName(transform, "HarmTester");
        if (harmtester_transform != null) { harmTester = harmtester_transform.gameObject; }

        ResetColorBlindnamesAll(true);
        ResetMusicNames(true);
        PopulateVOPacks(true);

        if (canvas_pos_init == Vector3.zero)
        {
            canvas_pos_init = new Vector3(-184.587f, 20.0f, 127.9f);
            canvas_rot_init = transform.rotation;
        }
        close_button.gameObject.SetActive(false);
        transform.SetPositionAndRotation(canvas_pos_init, canvas_rot_init);
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
            ui_spectatortoggle.interactable = allow_round_state && (allow_ply_state || allow_ply_team);
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
        RefreshAllOptions(true);
        waiting_on_playerdata = false;

        string tutorial_method = gameController.localizer.FetchText("LOCALOPTIONS_TUTORIAL_DESKTOP", "pushing your E or F key");
        if (Networking.LocalPlayer.IsUserInVR()) { tutorial_method = gameController.localizer.FetchText("LOCALOPTIONS_TUTORIAL_VR", "grabbing behind your head"); }
        tutorial_text.text = tutorial_text.text.Replace("$METHOD", tutorial_method);
    }

    public void RefreshAllOptions(bool init)
    {
        if (init) { RefreshComponents(); }
        UpdateUIScale();
        UpdateUISeparation();
        UpdateUIStretch();
        UpdateUIDistance();
        UpdateUIYOffset();
        UpdateUIAngle();
        UpdateUITextOffset();
        UpdateUITextScale();
        UpdateUIWrist();
        UpdateUIInverted();
        UpdateUIOtherScale();
        UpdateUIHarmScale();
        UpdateSpectatorIntent();
        UpdateHitbox();
        UpdateHurtbox();
        UpdateColorblind();
        UpdateParticles();
        UpdateMusicVolume();
        UpdateSoundVolume();
        UpdateAudioLink();
        UpdateMusicOverride();
        UpdateVOVolume();
        UpdateVOPreferences();
        UpdateMotionSickness();
        //UpdateLanguage();
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
                if (!PlayerData.HasKey(player, "UIYOffset"))
                {
                    ui_uiyoffsetslider.value = 0.0f;
                    UpdateUIYOffset();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UIAngle"))
                {
                    ui_uiangleslider.value = Networking.LocalPlayer.IsUserInVR() ? 30.0f : 0.0f;
                    UpdateUIAngle();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UITextScale"))
                {
                    ui_uitextscaleslider.value = 15.0f;
                    UpdateUITextScale();
                    continue;
                }
                if (!PlayerData.HasKey(player, "UITextOffset"))
                {
                    ui_uitextoffsetslider.value = Networking.LocalPlayer.IsUserInVR() ? -3.0f : -6.0f;
                    UpdateUITextOffset();
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
                if (!PlayerData.HasKey(player, "UIInverted"))
                {
                    ui_uiinvertedtoggle.isOn = Networking.LocalPlayer.IsUserInVR();
                    UpdateUIInverted();
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
                if (!PlayerData.HasKey(player, "MusicOverride"))
                {
                    ui_musicoverride_dropdown.value = 0;
                    UpdateMusicOverride();
                    continue;
                }
                if (!PlayerData.HasKey(player, "AudioLinkPref"))
                {
                    if (gameController != null && gameController.flag_for_mobile_vr != null)
                    {
                        if (!gameController.flag_for_mobile_vr.activeInHierarchy) { ui_audiolink_toggle.isOn = true; }
                        else
                        {
                            ui_audiolink_toggle.isOn = false;
                            ui_audiolink_toggle.interactable = false;
                        }
                    }
                    UpdateAudioLink();
                    continue;
                }
                if (!PlayerData.HasKey(player, "VOVolume"))
                {
                    ui_uivovolumelider.value = 10.0f;
                    UpdateVOVolume();
                    continue;
                }
                if (!PlayerData.HasKey(player, "VOPref_A"))
                {
                    ui_vo_pref_a_toggle.isOn = true;
                    UpdateVOPreferences();
                    continue;
                }
                if (!PlayerData.HasKey(player, "VOPref_B"))
                {
                    ui_vo_pref_b_toggle.isOn = true;
                    UpdateVOPreferences();
                    continue;
                }
                if (!PlayerData.HasKey(player, "VOPref_C"))
                {
                    ui_vo_pref_c_toggle.isOn = true;
                    UpdateVOPreferences();
                    continue;
                }
                if (!PlayerData.HasKey(player, "VOType"))
                {
                    ui_uivotype_dropdown.value = 0;
                    UpdateVOPreferences();
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
                if (!PlayerData.HasKey(player, "LangType"))
                {
                    UpdateLangEnglish();
                    gameController.room_ready_script.SetWarningStage(1);
                    continue;
                }
                if (!PlayerData.HasKey(player, "MotionSicknessCage"))
                {
                    ui_motionsicknesscagetoggle.isOn = false;
                    UpdateMotionSickness();
                    continue;
                }
                if (!PlayerData.HasKey(player, "MotionSicknessFloor"))
                {
                    ui_motionsicknessfloortoggle.isOn = false;
                    UpdateMotionSickness();
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

        float out_UIYOffset;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIYOffset", out out_UIYOffset))
        {
            ui_uiyoffsetslider.value = out_UIYOffset;
        }
        else
        {
            ui_uiyoffsetslider.value = 0.0f;
            UpdateUIYOffset();
        }

        float out_UIAngle;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UIAngle", out out_UIAngle))
        {
            ui_uiangleslider.value = out_UIAngle;
        }
        else
        {
            ui_uiangleslider.value = Networking.LocalPlayer.IsUserInVR() ? 30.0f : 0.0f;
            UpdateUIAngle();
        }

        float out_UITextScale;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UITextScale", out out_UITextScale))
        {
            ui_uitextscaleslider.value = out_UITextScale;
        }
        else
        {
            ui_uitextscaleslider.value = 15.0f;
            UpdateUITextScale();
        }

        float out_UITextOffset;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "UITextOffset", out out_UITextOffset))
        {
            ui_uitextoffsetslider.value = out_UITextOffset;
        }
        else
        {
            ui_uitextoffsetslider.value = Networking.LocalPlayer.IsUserInVR() ? -3.0f : -6.0f;
            UpdateUITextOffset();
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

        bool out_UIInverted;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "UIInverted", out out_UIInverted))
        {
            ui_uiinvertedtoggle.isOn = out_UIInverted;
        }
        else
        {
            ui_uiinvertedtoggle.isOn = Networking.LocalPlayer.IsUserInVR();
            UpdateUIInverted();
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

        int out_MusicOverride;
        if (PlayerData.TryGetInt(Networking.LocalPlayer, "MusicOverride", out out_MusicOverride))
        {
            ui_musicoverride_dropdown.value = out_MusicOverride;
        }
        else
        {
            ui_musicoverride_dropdown.value = 0;
            UpdateMusicOverride();
        }

        bool out_AudioLink;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "AudioLinkPref", out out_AudioLink))
        {
            if (gameController != null && gameController.flag_for_mobile_vr != null)
            {
                if (!gameController.flag_for_mobile_vr.activeInHierarchy) { ui_audiolink_toggle.isOn = out_AudioLink; }
                else
                {
                    ui_audiolink_toggle.isOn = false;
                    ui_audiolink_toggle.interactable = false;
                }
            }
        }
        else
        {
            if (gameController != null && gameController.flag_for_mobile_vr != null)
            {
                if (!gameController.flag_for_mobile_vr.activeInHierarchy) { ui_audiolink_toggle.isOn = true; }
                else
                {
                    ui_audiolink_toggle.isOn = false;
                    ui_audiolink_toggle.interactable = false;
                }
            }
            UpdateAudioLink();
        }

        float out_VOVolume;
        if (PlayerData.TryGetFloat(Networking.LocalPlayer, "VOVolume", out out_VOVolume))
        {
            ui_uivovolumelider.value = out_VOVolume;
        }
        else
        {
            ui_uivovolumelider.value = 10.0f;
            UpdateVOVolume();
        }

        bool out_VOPref_A;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "VOPref_A", out out_VOPref_A))
        {
            ui_vo_pref_a_toggle.isOn = out_VOPref_A;
        }
        else
        {
            ui_vo_pref_a_toggle.isOn = true;
            UpdateVOPreferences();
        }

        bool out_VOPref_B;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "VOPref_B", out out_VOPref_B))
        {
            ui_vo_pref_b_toggle.isOn = out_VOPref_B;
        }
        else
        {
            ui_vo_pref_b_toggle.isOn = true;
            UpdateVOPreferences();
        }

        bool out_VOPref_C;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "VOPref_C", out out_VOPref_C))
        {
            ui_vo_pref_c_toggle.isOn = out_VOPref_C;
        }
        else
        {
            ui_vo_pref_c_toggle.isOn = true;
            UpdateVOPreferences();
        }

        int out_VOType;
        if (PlayerData.TryGetInt(Networking.LocalPlayer, "VOType", out out_VOType))
        {
            ui_uivotype_dropdown.value = out_VOType;
        }
        else
        {
            ui_uivotype_dropdown.value = 0;
            UpdateVOPreferences();
        }

        int out_LanguageType;
        if (PlayerData.TryGetInt(Networking.LocalPlayer, "LangType", out out_LanguageType))
        {
            UnityEngine.Debug.Log("FOUND LANGTYPE KEY: " + out_LanguageType);
            if (out_LanguageType == (int)language_type_name.English) { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLangEnglish(); }
            else if (out_LanguageType == (int)language_type_name.French) { ui_language_toggles[(int)language_type_name.French].isOn = true; UpdateLangFrench(); }
            else if (out_LanguageType == (int)language_type_name.Japanese) { ui_language_toggles[(int)language_type_name.Japanese].isOn = true; UpdateLangJapanese(); }
            else if (out_LanguageType == (int)language_type_name.SpanishLatin) { ui_language_toggles[(int)language_type_name.SpanishLatin].isOn = true; UpdateLangSpanishLatin(); }
            else if (out_LanguageType == (int)language_type_name.SpanishEurope) { ui_language_toggles[(int)language_type_name.SpanishEurope].isOn = true; UpdateLangSpanishEurope(); }
            else if (out_LanguageType == (int)language_type_name.Italian) { ui_language_toggles[(int)language_type_name.Italian].isOn = true; UpdateLangItalian(); }

            UpdateLanguage(out_LanguageType);
        }
        else
        {
            gameController.room_ready_script.SetWarningStage(1);
            UpdateLangEnglish();
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

        bool out_MotionSicknessCage;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "MotionSicknessCage", out out_MotionSicknessCage))
        {
            ui_motionsicknesscagetoggle.isOn = out_MotionSicknessCage;
        }
        else
        {
            ui_motionsicknesscagetoggle.isOn = false;
            UpdateMotionSickness();
        }

        bool out_MotionSicknessFloor;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, "MotionSicknessFloor", out out_MotionSicknessFloor))
        {
            ui_motionsicknessfloortoggle.isOn = out_MotionSicknessFloor;
        }
        else
        {
            ui_motionsicknessfloortoggle.isOn = false;
            UpdateMotionSickness();
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
        PlayerData.SetFloat("UIYOffset", ui_uiyoffsetslider.value);
        PlayerData.SetFloat("UIAngle", ui_uiangleslider.value);
        PlayerData.SetFloat("UITextScale", ui_uitextscaleslider.value);
        PlayerData.SetFloat("UITextOffset", ui_uitextoffsetslider.value);
        PlayerData.SetFloat("UIOtherScale", ui_uiotherscaleslider.value);
        PlayerData.SetFloat("UIHarmScale", ui_uiharmscaleslider.value);
        PlayerData.SetBool("UIInverted", ui_uiinvertedtoggle.isOn);
        PlayerData.SetFloat("MusicVolume", ui_uimusicslider.value);
        PlayerData.SetFloat("SoundVolume", ui_uisoundslider.value);
        PlayerData.SetFloat("MusicOverride", ui_musicoverride_dropdown.value);
        PlayerData.SetBool("AudioLinkPref", ui_audiolink_toggle.isOn);
        PlayerData.SetFloat("VOVolume", ui_uivovolumelider.value);
        PlayerData.SetBool("VOPref_A", ui_vo_pref_a_toggle.isOn);
        PlayerData.SetBool("VOPref_B", ui_vo_pref_b_toggle.isOn);
        PlayerData.SetBool("VOPref_C", ui_vo_pref_c_toggle.isOn);
        PlayerData.SetInt("VOType", ui_uivotype_dropdown.value);
        PlayerData.SetBool("ColorblindSprite", ui_colorblind_toggle.isOn);
        PlayerData.SetInt("ColorblindChoice", ui_colorblind_dropdown.value);
        PlayerData.SetBool("HitboxShow", ui_hitboxtoggle.isOn);
        PlayerData.SetBool("HurtboxShow", ui_hurtboxtoggle.isOn);
        PlayerData.SetInt("LangType", localizer.language_type);
        PlayerData.SetBool("MotionSicknessCage", ui_motionsicknesscagetoggle.isOn);
        PlayerData.SetBool("MotionSicknessFloor", ui_motionsicknessfloortoggle.isOn);
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
        ui_uiscaletext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_SCALE", "UI Scale: $ARG0%", (ui_scale * 100.0f).ToString());
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
        ui_uiseparationtext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_SEPARATION", "UI Vertical: $ARG0%", (ui_separation * 100.0f).ToString());
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
        ui_uistretchtext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_STRETCH", "UI Horizontal: $ARG0%", (ui_stretch * 100.0f).ToString());
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
        ui_uidistancetext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_DISTANCE", "UI Distance: $ARG0%", (ui_distance * 100.0f).ToString());
        if ((ui_distance * 10.0f) <= ui_uidistanceslider.minValue) { ui_uidistancetext.color = Color.red; }
        else { ui_uidistancetext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIYOffset()
    {
        ui_yoffset = ui_uiyoffsetslider.value / 10.0f;
        ShowDemoUI();
        ui_uiyoffsettext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_YOFFSET", "UI Height Offset: $ARG0", (ui_yoffset * 10.0f).ToString());
        if (ui_yoffset > 0) { ui_uiyoffsettext.color = Color.cyan; }
        else if (ui_yoffset < 0) { ui_uiyoffsettext.color = new Color32(255, 153, 0, 255); }
        else { ui_uiyoffsettext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIAngle()
    {
        ui_angle = ui_uiangleslider.value;
        ShowDemoUI();
        ui_uiangletext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_ANGLE", "UI Angle: $ARG0°", (ui_angle).ToString());
        if (ui_angle > 0) { ui_uiangletext.color = Color.cyan; }
        else if (ui_angle < 0) { ui_uiangletext.color = new Color32(255, 153, 0, 255); }
        else { ui_uiangletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUITextScale()
    {
        ui_textscale = ui_uitextscaleslider.value / 10.0f;
        ShowDemoUI();
        ui_uitextscaletext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_TEXTSCALE", "UI Text Scale: $ARG0%", (ui_textscale * 100.0f).ToString());
        if ((ui_textscale * 10.0f) <= ui_uitextscaleslider.minValue) { ui_uitextscaletext.color = Color.red; }
        else { ui_uitextscaletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUITextOffset()
    {
        // Value range: -10 to 10, default 0
        ui_textoffset = ui_uitextoffsetslider.value / 10.0f;
        ShowDemoUI();
        ui_uitextoffsettext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_TEXTOFFSET", "UI Text Position: $ARG0%", (ui_textoffset * 100.0f).ToString());
        if (ui_textoffset > 0.5f) { ui_uitextoffsettext.color = Color.cyan; }
        else if (ui_textoffset < -0.5f) { ui_uitextoffsettext.color = new Color32(255, 153, 0, 255); }
        else { ui_uitextoffsettext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIOtherScale()
    {
        ui_other_scale = ui_uiotherscaleslider.value / 10.0f;
        ShowDemoUI();
        ui_uiotherscaletext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_OTHER", "Overhead UI Scale: $ARG0%", (ui_other_scale * 100.0f).ToString());
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
        ui_uiharmscaletext.text = gameController.localizer.FetchText("LOCALOPTIONS_UI_DAMAGE", "Damage Display UI Scale: $ARG0%", (ui_harm_scale * 100.0f).ToString());
        if ((ui_harm_scale * 10.0f) <= ui_uiharmscaleslider.minValue) { ui_uiharmscaletext.color = Color.red; }
        else { ui_uiharmscaletext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIInverted()
    {
        ui_inverted = ui_uiinvertedtoggle.isOn;
        if (gameController != null && gameController.local_uiplytoself != null) { gameController.local_uiplytoself.InvertUI(); }
        ShowDemoUI();

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }


    public void UpdateMusicVolume()
    {
        music_volume = ui_uimusicslider.value / 10.0f;

        ui_uimusictext.text = gameController.localizer.FetchText("LOCALOPTIONS_SOUND_VOLUME_MUSIC", "Music Volume: $ARG0%", (music_volume * 100.0f).ToString());
        if ((music_volume * 10.0f) <= ui_uimusicslider.minValue) 
        { 
            ui_uimusictext.color = Color.red; 
            ui_audiolink_toggle.isOn = false;
            ui_audiolink_toggle.interactable = false;
        }
        else 
        { 
            ui_uimusictext.color = Color.white;
            ui_audiolink_toggle.interactable = gameController.flag_for_mobile_vr == null || !gameController.flag_for_mobile_vr.activeInHierarchy;
        }

        gameController.snd_game_music_source.volume = gameController.music_volume_default * music_volume;

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateSoundVolume()
    {
        sound_volume = ui_uisoundslider.value / 10.0f;

        ui_uisoundtext.text = gameController.localizer.FetchText("LOCALOPTIONS_SOUND_VOLUME_SFX", "Sound Volume: $ARG0%", (sound_volume * 100.0f).ToString());
        if ((sound_volume * 10.0f) <= ui_uisoundslider.minValue) 
        { 
            ui_uisoundtext.color = Color.red;
            ui_audiolink_toggle.isOn = false;
            ui_audiolink_toggle.interactable = false;
        }
        else 
        { 
            ui_uisoundtext.color = Color.white;
            ui_audiolink_toggle.interactable = gameController.flag_for_mobile_vr == null || !gameController.flag_for_mobile_vr.activeInHierarchy;
        }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateMusicOverride()
    {
        music_override = ui_musicoverride_dropdown.value - 1;

        if (music_override >= 0)
        {
            gameController.PlaySFXFromArray(gameController.snd_game_music_source, gameController.snd_ready_music_clips, -1, 1, true);
        }
        else if (gameController.music_clip_playing != gameController.music_clip_desired)
        {
            AudioClip[] dummy_music_arr = new AudioClip[1];
            dummy_music_arr[0] = gameController.music_clip_desired;
            gameController.PlaySFXFromArray(gameController.snd_game_music_source, dummy_music_arr, 0, 1, true);
        }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateColorblind()
    {
        colorblind_choice = ui_colorblind_dropdown.value;
        colorblind = ui_colorblind_toggle.isOn;
        gameController.SetColorOptions(colorblind_choice);
        SetColorblindFlagColors(ref ui_colorblind_dropdown_caption, ref colorblind_flags);
        gameController.RefreshSetupUI();
        UpdateUIWrist(false);
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

    public void UpdateMotionSickness()
    {
        motion_sickness_cage = ui_motionsicknesscagetoggle.isOn;
        motion_sickness_floor = ui_motionsicknessfloortoggle.isOn;
        gameController.RefreshSetupUI();

        gameController.localMotionSicknessHelper.helper_capsule_transform.gameObject.SetActive(motion_sickness_cage);
        gameController.localMotionSicknessHelper.helper_cube_transform.gameObject.SetActive(motion_sickness_floor);

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateUIWrist(bool show_demo = true)
    {
        if (gameController.team_colors != null && gameController.team_colors.Length > 2)
        {
            Color.RGBToHSV(gameController.team_colors[2], out float H, out float S, out float V);
            V *= 0.5f;
            Color DimColor = Color.HSVToRGB(H, S, V);
            txt_container.PPPWristLToggle.color = DimColor;
            txt_container.PPPWristRToggle.color = DimColor;
            txt_container.PPPWristNoneToggle.color = DimColor;
        }

        if (ui_wristtoggle_l.isOn)
        {
            ui_wrist = 1;
            ui_wristtoggle_r.isOn = false;
            ui_wristtoggle_n.isOn = false;
            if (gameController.team_colors != null && gameController.team_colors.Length > 2) { txt_container.PPPWristLToggle.color = gameController.team_colors_bright[2]; }
        }
        else if (ui_wristtoggle_r.isOn)
        {
            ui_wrist = 2;
            ui_wristtoggle_l.isOn = false;
            ui_wristtoggle_n.isOn = false;
            if (gameController.team_colors != null && gameController.team_colors.Length > 2) { txt_container.PPPWristRToggle.color = gameController.team_colors_bright[2]; }

        }
        else
        {
            ui_wrist = 0;
            ui_wristtoggle_l.isOn = false;
            ui_wristtoggle_r.isOn = false;
            ui_wristtoggle_n.isOn = true;
            if (gameController.team_colors != null && gameController.team_colors.Length > 2) { txt_container.PPPWristNoneToggle.color = gameController.team_colors_bright[2]; }
        }

        gameController.RefreshSetupUI();
        lock_wrist = false;
        if (show_demo) { ShowDemoUI(); }

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

    public void UpdateAudioLink()
    {
        if (sound_volume <= 0 || music_volume <= 0) { ui_audiolink_toggle.isOn = false; }
        audiolink = ui_audiolink_toggle.isOn && sound_volume > 0 && music_volume > 0;
        gameController.audiolink_obj.SetActive(audiolink);

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateVOVolume()
    {
        voiceover_volume = ui_uivovolumelider.value / 10.0f;
        ui_uivovolumetext.text = gameController.localizer.FetchText("LOCALOPTIONS_SOUND_VOLUME_VO", "Announcer Volume: $ARG0%", (voiceover_volume * 100.0f).ToString());
        if ((voiceover_volume * 10.0f) <= ui_uivovolumelider.minValue) { ui_uivovolumetext.color = Color.red; }
        else { ui_uivovolumetext.color = Color.white; }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;

        if (gameController.local_plyAttr != null && gameController.local_plyAttr.in_ready_room && gameController.room_ready_script.warning_acknowledged)
        {
            // Play a test clip, scaled with announcer volume
            gameController.snd_voiceover_sfx_source.volume = voiceover_volume;
            gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.KOTH_Overtime);
        }
    }

    public void UpdateVOPreferences()
    {
        vo_pref_a = ui_vo_pref_a_toggle.isOn;
        vo_pref_b = ui_vo_pref_b_toggle.isOn;
        vo_pref_c = ui_vo_pref_c_toggle.isOn;

        voiceover_type = ui_uivotype_dropdown.value;
        if (gameController.voiceover_packs != null && voiceover_type < gameController.voiceover_packs.Length)
        {
            gameController.vopack_selected = gameController.voiceover_packs[voiceover_type];
        }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateLanguage(int in_language_type)
    {
        for (int i = 0; i < ui_language_toggles.Length; i++)
        {
            if (i == in_language_type) { ui_language_toggles[i].isOn = true; }
            else { ui_language_toggles[i].isOn = false; }
        }

        if (in_language_type != localizer.language_type)
        {
            localizer.language_type = in_language_type;
            localizer.SetLangDict();
            UnityEngine.Debug.Log("[LANGUAGE_TEST]: Setting language to " + in_language_type.ToString());
        }

        if (waiting_on_playerdata) { return; }
        should_sync = true;
        sync_timer = 0.0f;
    }

    public void UpdateLangEnglish()
    {
        if (ui_language_toggles[(int)language_type_name.English].isOn) { UpdateLanguage((int)language_type_name.English); }
        else if (!ui_language_toggles[(int)language_type_name.English].isOn && !ui_language_toggles[(int)language_type_name.French].isOn && !ui_language_toggles[(int)language_type_name.Japanese].isOn && !ui_language_toggles[(int)language_type_name.SpanishLatin].isOn && !ui_language_toggles[(int)language_type_name.SpanishEurope].isOn && !ui_language_toggles[(int)language_type_name.Italian].isOn) 
        { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLanguage((int)language_type_name.English); } // If NO languages are selected, set english to be on
    }

    public void UpdateLangFrench()
    {
        if (ui_language_toggles[(int)language_type_name.French].isOn) { UpdateLanguage((int)language_type_name.French); }
        else if (!ui_language_toggles[(int)language_type_name.English].isOn && !ui_language_toggles[(int)language_type_name.French].isOn && !ui_language_toggles[(int)language_type_name.Japanese].isOn && !ui_language_toggles[(int)language_type_name.SpanishLatin].isOn && !ui_language_toggles[(int)language_type_name.SpanishEurope].isOn && !ui_language_toggles[(int)language_type_name.Italian].isOn)
        { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLanguage((int)language_type_name.English); } // If NO languages are selected, set english to be on
    }

    public void UpdateLangJapanese()
    {
        if (ui_language_toggles[(int)language_type_name.Japanese].isOn) { UpdateLanguage((int)language_type_name.Japanese); }
        else if (!ui_language_toggles[(int)language_type_name.English].isOn && !ui_language_toggles[(int)language_type_name.French].isOn && !ui_language_toggles[(int)language_type_name.Japanese].isOn && !ui_language_toggles[(int)language_type_name.SpanishLatin].isOn && !ui_language_toggles[(int)language_type_name.SpanishEurope].isOn && !ui_language_toggles[(int)language_type_name.Italian].isOn)
        { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLanguage((int)language_type_name.English); } // If NO languages are selected, set english to be on
    }

    public void UpdateLangSpanishLatin()
    {
        if (ui_language_toggles[(int)language_type_name.SpanishLatin].isOn) { UpdateLanguage((int)language_type_name.SpanishLatin); }
        else if (!ui_language_toggles[(int)language_type_name.English].isOn && !ui_language_toggles[(int)language_type_name.French].isOn && !ui_language_toggles[(int)language_type_name.Japanese].isOn && !ui_language_toggles[(int)language_type_name.SpanishLatin].isOn && !ui_language_toggles[(int)language_type_name.SpanishEurope].isOn && !ui_language_toggles[(int)language_type_name.Italian].isOn)
        { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLanguage((int)language_type_name.English); } // If NO languages are selected, set english to be on
    }

    public void UpdateLangSpanishEurope()
    {
        if (ui_language_toggles[(int)language_type_name.SpanishEurope].isOn) { UpdateLanguage((int)language_type_name.SpanishEurope); }
        else if (!ui_language_toggles[(int)language_type_name.English].isOn && !ui_language_toggles[(int)language_type_name.French].isOn && !ui_language_toggles[(int)language_type_name.Japanese].isOn && !ui_language_toggles[(int)language_type_name.SpanishLatin].isOn && !ui_language_toggles[(int)language_type_name.SpanishEurope].isOn && !ui_language_toggles[(int)language_type_name.Italian].isOn)
        { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLanguage((int)language_type_name.English); } // If NO languages are selected, set english to be on
    }

    public void UpdateLangItalian()
    {
        if (ui_language_toggles[(int)language_type_name.Italian].isOn) { UpdateLanguage((int)language_type_name.Italian); }
        else if (!ui_language_toggles[(int)language_type_name.English].isOn && !ui_language_toggles[(int)language_type_name.French].isOn && !ui_language_toggles[(int)language_type_name.Japanese].isOn && !ui_language_toggles[(int)language_type_name.SpanishLatin].isOn && !ui_language_toggles[(int)language_type_name.SpanishEurope].isOn && !ui_language_toggles[(int)language_type_name.Italian].isOn)
        { ui_language_toggles[(int)language_type_name.English].isOn = true; UpdateLanguage((int)language_type_name.English); } // If NO languages are selected, set english to be on
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

    public void ResetUI()
    {
        ui_uiscaleslider.value = 10.0f;
        ui_uiseparationslider.value = 10.0f;
        ui_uistretchslider.value = 10.0f;
        ui_uidistanceslider.value = 10.0f;
        ui_uiyoffsetslider.value = 0.0f;
        ui_uiangleslider.value = Networking.LocalPlayer.IsUserInVR() ? 30.0f : 0.0f;
        ui_uitextscaleslider.value = Networking.LocalPlayer.IsUserInVR() ? 15.0f : 10.0f;
        ui_uitextoffsetslider.value = Networking.LocalPlayer.IsUserInVR() ? -3.0f : -6.0f;
        ui_uiotherscaleslider.value = 10.0f;
        ui_uiharmscaleslider.value = 10.0f;
        ui_wristtoggle_n.isOn = true;
        ui_wristtoggle_l.isOn = false;
        ui_wristtoggle_r.isOn = false;
        ui_uiinvertedtoggle.isOn = Networking.LocalPlayer.IsUserInVR();
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
    //ui_colorblind_grid
    //ui_colorblind_dropdown_caption
    //colorblind_flags
    public void ColorblindTemplateInit(ref GameObject template, ref UnityEngine.UI.GridLayoutGroup grid, ref TMP_Text caption, ref GameObject[] flags)
    {
        // Setup the colorblind flag objects for demonstration
        flags = new GameObject[gameController.team_colors_base.Length];
        for (int j = 0; j < gameController.team_colors_base.Length; j++)
        {
            flags[j] = Instantiate(template, grid.transform);
            flags[j].transform.GetChild(1).GetComponent<TMP_Text>().text = gameController.localizer.FetchText("TEAM_COLOR_" + j, gameController.localizer.LocalizeTeamName(j).Split(' ')[0]); 
        }
        SetColorblindFlagColors(ref caption, ref flags);
        // After making the copies, set the template to be inactive
        template.SetActive(false);
    }

    public void SetColorblindFlagColors(ref TMP_Text caption, ref GameObject[] flags)
    {
        caption.text = caption.text.Replace("  ", "\n");

        if (flags == null) { return; }
        for (int j = 0; j < flags.Length; j++)
        {
            flags[j].GetComponent<UnityEngine.UI.Image>().color = gameController.team_colors[j];
            flags[j].transform.GetChild(1).GetComponent<TMP_Text>().text = gameController.localizer.FetchText("TEAM_COLOR_" + j, gameController.localizer.LocalizeTeamName(j).Split(' ')[0]);
            flags[j].transform.GetChild(1).GetComponent<TMP_Text>().color = gameController.team_colors_bright[j];
            if (colorblind)
            {
                flags[j].GetComponent<UnityEngine.UI.Image>().sprite = gameController.team_sprites[j];
                flags[j].transform.GetChild(0).gameObject.SetActive(false);
            }
            else
            {
                flags[j].GetComponent<UnityEngine.UI.Image>().sprite = ui_flag_base_sprite;
                flags[j].transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

    //ui_colorblind_dropdown
    public void ResetColorBlindnamesAll(bool init)
    {
        ResetColorblindNames(ref ui_colorblind_dropdown, init);
        ResetColorblindNames(ref gameController.room_ready_script.ui_colorblind_dropdown, init);
    }

    public void ResetColorblindNames(ref TMP_Dropdown dropdown, bool init = false)
    {
        // Due to the way this is implemented, the value will be forced to 0. So, we store the original value and then reset to that value after refreshing the options.
        int stored_value = dropdown.value;
        // Make sure our placeholder newline character is replaced with the real version (inspector doesn't interpret it correctly when manually typed)

        for (int i = 0; i < ui_colorblind_dropdown_names.Length; i++)
        {
            if (!init) { ui_colorblind_dropdown_names[i] = gameController.localizer.FetchText("LOCALOPTIONS_GAME_COLORBLIND_SELECT_" + i.ToString(), ui_colorblind_dropdown_names[i]).Replace("\n", "#").Replace("#", "  "); }
            else { ui_colorblind_dropdown_names[i] = ui_colorblind_dropdown_names[i].Replace("\n", "#").Replace("#", "  "); }
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(ui_colorblind_dropdown_names);
        dropdown.value = stored_value;
    }

    public void ResetMusicNames(bool init = false)
    {
        // Due to the way this is implemented, the value will be forced to 0. So, we store the original value and then reset to that value after refreshing the options.
        int stored_value = ui_musicoverride_dropdown.value;

        ui_musicoverride_dropdown.ClearOptions();
        string[] music_override_names = new string[gameController.snd_override_music_names.Length + 1];
        if (!init) { music_override_names[0] = gameController.localizer.FetchText("MUSIC_0", "(Default)");  }
        else { music_override_names[0] = "(Default)"; }

        for (int i = 0; i < gameController.snd_override_music_names.Length; i++)
        {
            if (!init) { music_override_names[i + 1] = gameController.localizer.FetchText("MUSIC_" + (i + 1).ToString(), gameController.snd_override_music_names[i]); }
            else { music_override_names[i + 1] = gameController.snd_override_music_names[i]; }
        }
        ui_musicoverride_dropdown.AddOptions(music_override_names);
        ui_musicoverride_dropdown.value = stored_value;
    }

    public void PopulateVOPacks(bool init = false)
    {
        // Due to the way this is implemented, the value will be forced to 0. So, we store the original value and then reset to that value after refreshing the options.
        int stored_value = ui_uivotype_dropdown.value;

        ui_uivotype_dropdown.ClearOptions();
        string[] vo_names = new string[gameController.voiceover_packs.Length];

        for (int i = 0; i < gameController.voiceover_packs.Length; i++)
        {
            if (gameController.voiceover_packs[i] == null) { continue; } 
            vo_names[i] = gameController.voiceover_packs[i].vo_name;
            // To-do: make these localizer keys, localized only for Japanese
        }
        ui_uivotype_dropdown.AddOptions(vo_names);
        ui_uivotype_dropdown.value = stored_value;
    }
}
