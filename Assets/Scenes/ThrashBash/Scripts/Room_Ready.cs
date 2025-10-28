
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Room_Ready : GlobalTickReceiver
{
    [SerializeField] public GameController gameController;
    [SerializeField] public Collider trigger_player;
    [SerializeField] public UnityEngine.UI.Button WarningButton;
    [SerializeField] public UnityEngine.UI.Button LangConfirmButton;
    [SerializeField] public UnityEngine.UI.Button ColorblindConfirmButton;
    [SerializeField] public UnityEngine.UI.Button StreamerButtonYes;
    [SerializeField] public UnityEngine.UI.Button StreamerButtonNo;
    [SerializeField] public GameObject WarningCanvas;
    [SerializeField] public GameObject warning_panel_main;
    [SerializeField] public GameObject warning_panel_lang;
    [SerializeField] public GameObject warning_panel_streamer;
    [SerializeField] public GameObject warning_panel_colorblindness;
    [SerializeField] public GameObject monitor_obj;
    [SerializeField] public UnityEngine.UI.Toggle[] ui_language_toggles;
    [SerializeField] public Renderer zone_marker_renderer;
    [NonSerialized] public bool warning_acknowledged = false;
    [NonSerialized] public byte warning_stage = 2; // 0 = streamer, 1 = language, 2 = motion sickness, 3 = colorblindness
    [NonSerialized] public bool updating_language = false;

    [SerializeField] public UnityEngine.UI.Toggle ui_colorblind_toggle;
    [SerializeField] public TMP_Dropdown ui_colorblind_dropdown;
    [SerializeField] public TMP_Text ui_colorblind_dropdown_caption;
    [SerializeField] public GameObject template_colorblind_flag;
    [SerializeField] public UnityEngine.UI.GridLayoutGroup ui_colorblind_grid;
    [NonSerialized] public GameObject[] colorblind_flags;

    // Why we use BOTH plyAttr.ply_team and GetGlobalTeam(): one is local, the other is networked. If the network is busy, we do not want to flood them with requests!

    /*private void Update()
    {
        if (WarningCanvas != null && WarningCanvas.activeInHierarchy)
        {
            Vector3 pos = WarningPanel.transform.position;
            pos.x = Networking.LocalPlayer.GetPosition().x;
            WarningPanel.transform.position = pos;
            pos = WarningPanel.transform.localPosition;
            float widthAdjust = ((RectTransform)WarningCanvas.transform).rect.width / 2.0f;
            widthAdjust -= ((RectTransform)WarningPanel.transform).rect.width / 2.0f;
            pos.x = Mathf.Clamp(pos.x, -widthAdjust, widthAdjust);
            WarningPanel.transform.localPosition = pos;
        }
    }*/

    public override void Start()
    {
        base.Start();
        ZoneMarkerHighlight(false);
    }

    public override void OnSlowTick(float tickDeltaTime)
    {
        if (!warning_acknowledged)
        {
            if (!WarningButton.interactable && gameController != null && gameController.local_plyAttr != null && gameController.ui_initialized) 
            { 
                WarningButton.interactable = true;
                LangConfirmButton.interactable = true;
                StreamerButtonYes.interactable = true;
                StreamerButtonNo.interactable = true;
                SetWarningStage(0);
            }
            else if (WarningButton.interactable && gameController == null || gameController.local_plyAttr == null || !gameController.ui_initialized) 
            { 
                WarningButton.interactable = false;
                LangConfirmButton.interactable = false;
                StreamerButtonYes.interactable = false;
                StreamerButtonNo.interactable = false;
                ZoneMarkerHighlight(false);
            }
        }
    }

    public void ZoneMarkerHighlight(bool toggle)
    {
        if (toggle) 
        {
            int team_id = -4;
            if (gameController != null) 
            {
                if (gameController.local_plyAttr != null) { team_id = gameController.local_plyAttr.ply_team; }
                else { team_id = gameController.GetGlobalTeam(Networking.LocalPlayer.playerId); }
            }
            if (gameController.option_teamplay && team_id >= 0 && gameController.team_colors_bright != null && team_id < gameController.team_colors_bright.Length)
            {
                zone_marker_renderer.material.SetColor("_Color", gameController.team_colors_bright[team_id]);
                zone_marker_renderer.material.EnableKeyword("_EMISSION");
                zone_marker_renderer.material.SetColor("_EmissionColor", gameController.team_colors_bright[team_id]);
            }
            else if (team_id < 0 && team_id != (int)player_tracking_name.WaitingForLobby)
            {
                zone_marker_renderer.material.SetColor("_Color", new Color32(50, 50, 50, 255));
                zone_marker_renderer.material.EnableKeyword("_EMISSION");
                zone_marker_renderer.material.SetColor("_EmissionColor", new Color32(50, 50, 50, 255));
            }
            else if (team_id < 0 && team_id == (int)player_tracking_name.WaitingForLobby)
            {
                zone_marker_renderer.material.SetColor("_Color", new Color32(128, 0, 128, 255));
                zone_marker_renderer.material.EnableKeyword("_EMISSION");
                zone_marker_renderer.material.SetColor("_EmissionColor", new Color32(128, 0, 128, 255));
            }
            else
            {
                zone_marker_renderer.material.SetColor("_Color", new Color32(0, 255, 197, 255));
                zone_marker_renderer.material.EnableKeyword("_EMISSION");
                zone_marker_renderer.material.SetColor("_EmissionColor", new Color32(0, 255, 197, 255));
            }
        }
        else
        {
            zone_marker_renderer.material.SetColor("_Color", new Color32(50, 50, 50, 255));
            zone_marker_renderer.material.EnableKeyword("_EMISSION");
            zone_marker_renderer.material.SetColor("_EmissionColor", new Color32(50, 50, 50, 255));
        }
    }

    public void SetWarningStage(byte stage)
    {
        warning_stage = stage;
        warning_panel_colorblindness.SetActive(stage == 3);
        warning_panel_main.SetActive(stage == 2);
        warning_panel_lang.SetActive(stage == 1);
        warning_panel_streamer.SetActive(stage == 0);
    }

    public void ConfirmLanguage()
    {
        gameController.room_ready_script.SetWarningStage(3);
        gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.WarningPressNext);
    }

    public void ConfirmColorblindness()
    {
        gameController.room_ready_script.SetWarningStage(0);
        gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.WarningPressNext);
    }

    public void UpdateColorblind()
    {
        gameController.SetColorOptions(ui_colorblind_dropdown.value);
        gameController.local_ppp_options.ui_colorblind_dropdown.value = ui_colorblind_dropdown.value;
        gameController.local_ppp_options.ui_colorblind_toggle.isOn = ui_colorblind_toggle.isOn;
        gameController.local_ppp_options.SetColorblindFlagColors(ref ui_colorblind_dropdown_caption, ref colorblind_flags);
    }

    public void StreamerYes()
    {
        if (gameController.local_ppp_options != null) 
        {
            gameController.local_ppp_options.ui_uimusicslider.value = 0;
            gameController.local_ppp_options.UpdateMusicVolume();
        }
        gameController.room_ready_script.SetWarningStage(2);
        gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.WarningPressNext);
    }

    public void StreamerNo()
    {
        gameController.room_ready_script.SetWarningStage(2);
        gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.WarningPressNext);
    }

    public void AcknowledgeWarning()
    {
        warning_acknowledged = true;
        GetComponent<Collider>().isTrigger = true;
        WarningCanvas.SetActive(false);
        if (gameController.round_state == (int)round_state_name.Start 
            || gameController.round_state == (int)round_state_name.Over 
            || gameController.round_state == (int)round_state_name.Queued
            || gameController.round_state == (int)round_state_name.Loading) 
        { gameController.PlaySFXFromArray(gameController.snd_game_music_source, gameController.snd_ready_music_clips, -1, 1, true); }
        gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_START_0", "Stand in the square to join the game!"), Color.cyan);
        gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_START_1", "Alternatively, you can spectate by using the 'Game' Tab in the Local Options menu!"), Color.cyan);
        gameController.room_training_portal.SetActive(true);
        if (gameController.mapscript_list != null && gameController.mapscript_list.Length > 0)
        {
            foreach (ItemSpawner itemSpawner in gameController.mapscript_list[0].GetItemSpawnerFromParent(gameController.room_training.transform))
            {
                if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                itemSpawner.item_spawn_powerups_enabled = true;
                itemSpawner.item_spawn_weapons_enabled = true;
                itemSpawner.item_spawn_frequency_mul = 1.0f;
                itemSpawner.item_spawn_duration_mul = 1.0f;
                itemSpawner.SetSpawnChances();
                if (itemSpawner.item_spawn_state == (int)item_spawn_state_name.Disabled) { itemSpawner.item_spawn_state = (int)item_spawn_state_name.Spawnable; }
                else { itemSpawner.SyncSpawns(); }
            }
        }
        if (gameController.local_plyAttr != null) 
        { 
            gameController.local_plyAttr.air_thrust_enabled = true;
            if (gameController.local_plyweapon != null) { gameController.local_plyAttr.air_thrust_cooldown = gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.MegaGlove)[(int)weapon_stats_name.Cooldown] * 2.0f; }
        }
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_ready_sfx_clips, (int)ready_sfx_name.WarningPressDone);
        //gameController.room_training.SetActive(true); // Just a temporary measure so the item spawners can be setup
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            if (!warning_acknowledged) { return; }
            PlayerAttributes plyAttr = gameController.local_plyAttr;
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Inactive && !plyAttr.ply_training) { 
                plyAttr.ply_state = (int)player_state_name.Joined; 

                if (gameController.GetGlobalTeam(player.playerId) < 0 && plyAttr.ply_team < 0)
                {
                    if (gameController.round_state != (int)round_state_name.Start 
                        && gameController.GetGlobalTeam(player.playerId) != (int)player_tracking_name.WaitingForLobby
                        && plyAttr.ply_team != (int)player_tracking_name.WaitingForLobby) 
                    {
                        plyAttr.ply_team = (int)player_tracking_name.WaitingForLobby;
                        gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.JoinMidGame);
                        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, (int)player_tracking_name.WaitingForLobby, true);
                        ZoneMarkerHighlight(true);
                    }
                    else if (gameController.round_state == (int)round_state_name.Start)
                    {
                        plyAttr.ply_team = 0;
                        gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.JoinGame);
                        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, 0, true);
                        ZoneMarkerHighlight(true);
                    }
                }
            }

            if (!monitor_obj.activeInHierarchy) { monitor_obj.SetActive(true); }
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!warning_acknowledged) { return; }
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            PlayerAttributes plyAttr = gameController.local_plyAttr;
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Joined && !plyAttr.ply_training)
            {
                plyAttr.ply_state = (int)player_state_name.Inactive;

                if (gameController.round_state == (int)round_state_name.Start 
                    && gameController.GetGlobalTeam(player.playerId) != (int)player_tracking_name.Spectator 
                    && gameController.GetGlobalTeam(player.playerId) != (int)player_tracking_name.Unassigned
                    && plyAttr.ply_team != (int)player_tracking_name.Unassigned
                    && plyAttr.ply_team != (int)player_tracking_name.Unassigned
                    )
                {
                    //if (gameController.round_state == (int)round_state_name.Start) { 
                    plyAttr.ply_team = (int)player_tracking_name.Unassigned;
                    gameController.PlaySFXFromArray(gameController.snd_ready_sfx_source, gameController.snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame);
                    gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, (int)player_tracking_name.Unassigned, false);
                    ZoneMarkerHighlight(false);
                }
            }
        }

        if (monitor_obj.activeInHierarchy) { monitor_obj.SetActive(false); }
    }

    public void UpdateLangEnglish()
    {
        if (updating_language) { return; }
        UpdateLanguage((int)language_type_name.English);
        if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.ui_language_toggles[(int)language_type_name.English].isOn = true; }
    }

    public void UpdateLangFrench()
    {
        if (updating_language) { return; }
        UpdateLanguage((int)language_type_name.French);
        if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.ui_language_toggles[(int)language_type_name.French].isOn = true; }
    }

    public void UpdateLangJapanese()
    {
        if (updating_language) { return; }
        UpdateLanguage((int)language_type_name.Japanese);
        if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.ui_language_toggles[(int)language_type_name.Japanese].isOn = true; }
    }

    public void UpdateLangSpanishLatin()
    {
        if (updating_language) { return; }
        UpdateLanguage((int)language_type_name.SpanishLatin);
        if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.ui_language_toggles[(int)language_type_name.SpanishLatin].isOn = true; }
    }

    public void UpdateLangSpanishEurope()
    {
        if (updating_language) { return; }
        UpdateLanguage((int)language_type_name.SpanishEurope);
        if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.ui_language_toggles[(int)language_type_name.SpanishEurope].isOn = true; }
    }

    public void UpdateLangItalian()
    {
        if (updating_language) { return; }
        UpdateLanguage((int)language_type_name.Italian);
        if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.ui_language_toggles[(int)language_type_name.Italian].isOn = true; }
    }

    public void UpdateLanguage(int in_language_type)
    {
        if (updating_language) { return; }
        updating_language = true;
        for (int i = 0; i < ui_language_toggles.Length; i++)
        {
            if (i == in_language_type) { ui_language_toggles[i].isOn = true; }
            else { ui_language_toggles[i].isOn = false; }
        }
        updating_language = false;
    }

}
