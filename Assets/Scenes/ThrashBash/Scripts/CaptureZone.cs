
using System;
using System.IO.Pipes;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDK3.Persistence;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using static UnityEngine.UI.Image;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class CaptureZone : UdonSharpBehaviour
{

    [Tooltip("How long should this point be locked before it can be captured?")]
    [SerializeField] public float initial_lock_duration = 5.0f;
    //[Tooltip("How often should this capture point perform a Physics check for player hitboxes?")]
    //[SerializeField] public float check_players_impulse = 0.5f;
    [Tooltip("How long should the contestor be allowed to remain outside of the capture zone before their progress fades? (MUST be > check_players_impulse)")]
    [SerializeField] public float contest_pause_duration = 1.5f; //[UdonSynced] 
    [Tooltip("How often should a capture point grant points? (While there shouldn't be much reason for this to not be 1, it's good to be prepared.)")]
    [SerializeField] public float point_grant_impulse = 1.0f;
    [Tooltip("What's the minimum number of players required for this point to be active? (-1 = no requirement)")]
    [SerializeField] public int min_players = -1;

    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject captureDisplayArea;
    [SerializeField] public ChangeColorWithSetting tubeArea;
    [SerializeField] public GameObject UITeamFlagCanvas;
    [SerializeField] public UnityEngine.UI.Image UITeamLockImage;
    [SerializeField] public UnityEngine.UI.Image UITeamFlagImage;
    [SerializeField] public UnityEngine.UI.Image UITeamPoleImage;
    [SerializeField] public UnityEngine.UI.Image UITeamCBImage;
    [SerializeField] public TMP_Text UITeamText;
    [SerializeField] public UnityEngine.UI.Image UIContestMeterFG;
    [SerializeField] public UnityEngine.UI.Image UIContestMeterBG;
    [SerializeField] public TMP_Text UITimerText;

    [NonSerialized] public double last_network_time = 0.0f;
    [NonSerialized] [UdonSynced] public float contest_pause_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float initial_lock_timer = 0.0f;
    [NonSerialized] [UdonSynced] public int hold_points = 0;
    [NonSerialized] public float point_grant_timer = 0.0f;
    [NonSerialized] [UdonSynced] public bool is_locked = true;
    [NonSerialized] [UdonSynced] public int hold_id = -1;
    [NonSerialized] [UdonSynced] public int contest_id = -1;
    [NonSerialized] [UdonSynced] public float contest_progress = 0.0f;
    [NonSerialized] [UdonSynced] public bool overtime_enabled = false; // This will only be true if there is both a holder and a contestor.
    [NonSerialized] public bool overtime_vo_played = false; // Play the Overtime voiceline ONLY if we haven't already
    [NonSerialized] public bool contest_ongoing = false;
    [NonSerialized] public bool local_is_on_point = false;
    [NonSerialized] public int[] players_on_point;
    [NonSerialized] public int global_index;

    [NonSerialized] public int[] dict_points_keys_arr;
    [NonSerialized][UdonSynced] public string dict_points_keys_str = "";
    [NonSerialized] public int[] dict_points_values_arr;
    [NonSerialized][UdonSynced] public string dict_points_values_str = "";

    [NonSerialized] public float avg_network_delay = 0.0f;

    // To redo this, we'll have a global tracking array of POINTS, that will then be evaluated by a unique function in GameController for each team/player, evaluated against ALL PLAYERS / TEAMS, regardless of in-game or not (to prevent array fuckery with sorting from joining/leaving players).
    // Then, every interval, grant points to the array. Reset this interval timer every grant or capture.
    // Finally, don't grant the last point if it is contested (display "OVERTIME" instead).

    private void Start()
    {
        SetRenderOrder();
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        players_on_point = new int[0];

    }

    private void OnEnable()
    {
        SetRenderOrder();
        ResetZone();
    }

    public void ResetZone()
    {
        hold_id = -1;
        hold_points = 0;
        contest_id = -1;
        contest_progress = 0.0f;
        initial_lock_timer = 0.0f;
        contest_pause_timer = 0.0f;
        is_locked = true;
        players_on_point = new int[0];
        if (captureDisplayArea != null) { captureDisplayArea.SetActive(false); }
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        base.OnDeserialization(result);
        float network_diff = result.receiveTime - result.sendTime;
        avg_network_delay = network_diff > 0 ? (avg_network_delay + network_diff) / 2.0f : avg_network_delay;
    }

    private void Update()
    {
        HandleUI();
        
        // Networking Sync
        if (!Networking.IsOwner(gameObject))
        {
            dict_points_keys_arr = GlobalHelperFunctions.ConvertStrToIntArray(dict_points_keys_str);
            dict_points_values_arr = GlobalHelperFunctions.ConvertStrToIntArray(dict_points_values_str);
        }

        double currentNetworkTime = Networking.GetServerTimeInSeconds();
        double cache_last_network_time = last_network_time;
        float networkTimeDelta = (float)Networking.CalculateServerDeltaTime(currentNetworkTime, last_network_time);
        // Prevent negative delta times
        if (networkTimeDelta <= 0.0f)
        {
            networkTimeDelta = Time.deltaTime;
        }
        last_network_time = currentNetworkTime;

        // Impulse point granting (also used for any events which will occur every second)
        if (point_grant_timer >= point_grant_impulse)
        {
            int points_to_grant = Mathf.FloorToInt(1 + point_grant_timer - point_grant_impulse + avg_network_delay);
            //UnityEngine.Debug.Log("[KOTH_POINT_TEST] point_grant_timer = " + point_grant_timer + "; currentNetworkTime = " + currentNetworkTime + " vs last_network_time = " + cache_last_network_time + "(networkTimeDelta = " + networkTimeDelta + ")");
            point_grant_timer = 0.0f;
            if (!is_locked && dict_points_keys_arr != null && dict_points_values_arr != null)
            {
                int hold_index = GlobalHelperFunctions.DictIndexFromKey(hold_id, dict_points_keys_arr);

                if (Networking.IsOwner(gameObject))
                {
                    if (hold_index >= 0 && hold_index < dict_points_keys_arr.Length)
                    {
                        hold_points = dict_points_values_arr[hold_index];
                        if (hold_points < gameController.option_gm_goal - 1) { dict_points_values_arr[hold_index] += points_to_grant; } // Normal condition
                        else if (!overtime_enabled) { dict_points_values_arr[hold_index] += points_to_grant; } // Victory condition
                        else { } // Overtime condition
                        hold_points = dict_points_values_arr[hold_index];
                        gameController.CheckForRoundGoal();
                        UnityEngine.Debug.Log("[KOTH_TEST]: hold_points: " + hold_points + " (goal: " + gameController.option_gm_goal + ") for hold ID " + hold_id + " (overtime = " + overtime_enabled + ")");

                    }

                    //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalGrantPoints");
                    // (SFX handling used to be here as networked events, but has since been moved outside this block as local events)
                }
                if (hold_index >= 0 && hold_index < dict_points_keys_arr.Length)
                {
                    UnityEngine.Debug.Log("[KOTH_TEST]: hold_points: " + hold_points + " (goal: " + gameController.option_gm_goal + ") for hold ID " + hold_id + " (overtime = " + overtime_enabled + ")");
                }
       
                // Handle SFX locally
                if (contest_id >= 0 && contest_ongoing && hold_index >= 0 && hold_points >= gameController.option_gm_goal - 2) //&& overtime_enabled
                {
                    // Don't play anything if we are at goal and overtime is enabled; it will be annoying otherwise
                    // However, the pause timer for contesting is instant if they aren't on point!
                    if (contest_id >= 0 && contest_pause_timer > 0.0f)
                    {
                        //UnityEngine.Debug.Log("[KOTH_SFX_TEST]: Point is being contested by " + contest_id + " WHILE IN OVERTIME; play KOTH_Contest_Progress");
                        contest_pause_timer = contest_pause_duration;
                        PlayGlobalSoundEvent((int)announcement_sfx_name.KOTH_Contest_Progress, contest_id);
                    }
                }
                else if (hold_index >= 0 && hold_points >= gameController.option_gm_goal - 10)
                {
                    //UnityEngine.Debug.Log("[KOTH_SFX_TEST]: A team is about to win! Play KOTH_Victory_Near for " + hold_id);
                    PlayGlobalSoundEvent((int)announcement_sfx_name.KOTH_Victory_Near, hold_id);
                }
                else if (contest_id >= 0 && contest_ongoing)
                {
                    //UnityEngine.Debug.Log("[KOTH_SFX_TEST]: Point is being contested by " + contest_id + "; play KOTH_Contest_Progress");
                    PlayGlobalSoundEvent((int)announcement_sfx_name.KOTH_Contest_Progress, contest_id);
                } 

                // Play a voiceline when we reach the 1 second mark exactly, and playing only once
                if (!overtime_vo_played && hold_index >= 0 && Mathf.FloorToInt(hold_points) == gameController.option_gm_goal - 1 && overtime_enabled)
                {
                    gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.KOTH_Overtime);
                    overtime_vo_played = true;
                }
                else if (overtime_vo_played && Mathf.FloorToInt(hold_points) > gameController.option_gm_goal - 1 && !overtime_enabled)
                {
                    overtime_vo_played = false;
                }
            }
        }
        else
        {
            point_grant_timer += networkTimeDelta;
        }

        // Respawn duration should scale with progress
        if (gameController.local_plyAttr != null && gameController.local_plyAttr.ply_state != (int)player_state_name.Respawning)
        {
            if (
                (gameController.option_teamplay && gameController.local_plyAttr != null && hold_id == gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && hold_id == Networking.LocalPlayer.playerId)
                )
            { gameController.koth_respawn_wave_duration = gameController.plysettings_respawn_duration * 1.3f; } // *1.6f
            else if (
                (gameController.option_teamplay && gameController.local_plyAttr != null && contest_id == gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && contest_id == Networking.LocalPlayer.playerId)
                )
            { gameController.koth_respawn_wave_duration = gameController.plysettings_respawn_duration * 1.15f; } // *1.3f
            else { gameController.koth_respawn_wave_duration = gameController.plysettings_respawn_duration; }
        }

        // Handle point locking
        if (is_locked && initial_lock_timer >= initial_lock_duration)
        {
            is_locked = false;
            if (Networking.IsOwner(gameObject)) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayGlobalSoundEvent", (int)announcement_sfx_name.KOTH_Unlock, -1); }
        }
        else if (is_locked && gameController.round_state == (int)round_state_name.Ongoing)
        {
            initial_lock_timer += networkTimeDelta;
        }

        if (is_locked)
        {
            if (Networking.IsOwner(gameObject))
            {
                dict_points_keys_str = GlobalHelperFunctions.ConvertIntArrayToString(dict_points_keys_arr);
                dict_points_values_str = GlobalHelperFunctions.ConvertIntArrayToString(dict_points_values_arr);
            }
            return;
        }

        // -- Below will only occur if the point is unlocked and active --
        CheckPointContest(networkTimeDelta);

        // If the contestor exists but is not present on the point, drain contestor's progress.
        if (contest_id >= 0 && contest_progress >= 0.0f)
        {
            if (contest_pause_timer >= contest_pause_duration) { contest_progress -= networkTimeDelta; }
            else { contest_pause_timer += networkTimeDelta; }
        }

        // If the contest_progress exceeds duration, then contestor becomes the new holder; reset contestor and contest_progress.
        if (contest_progress >= gameController.option_gm_config_a)
        {
            UnityEngine.Debug.Log("[CAPTURE_TEST]: contest_progress: " + contest_progress + " >= " + gameController.option_gm_config_a + " for ID " + contest_id + " vs hold ID " + hold_id);

            // Assign a penalty if a holder lost the point within the last 5 seconds
            int hold_index = GlobalHelperFunctions.DictIndexFromKey(hold_id, dict_points_keys_arr);
            if (hold_index >= 0 && hold_index < dict_points_keys_arr.Length && (gameController.option_gm_goal - dict_points_values_arr[hold_index]) <= 5) {
                dict_points_values_arr[hold_index] = gameController.option_gm_goal - 6;
                //if (!Networking.IsOwner(gameObject)) { LocalGrantPoints(); }
                //else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalGrantPoints"); }
            }

            contest_progress = 0.0f;
            contest_pause_timer = 0.0f;
            point_grant_timer = 0.0f;
            hold_id = contest_id;
            contest_id = -1;
            
            // Play SFX based on whether it was the player's team that captured it or not
            if (Networking.IsOwner(gameObject)) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayGlobalSoundEvent", (int)announcement_sfx_name.KOTH_Capture_Team, hold_id); }


        }
        else if (contest_progress < 0.0f && contest_id >= 0)
        {
            // If the contestor exists and contesting contest_progress drops below 0.0f, set to 0.0f and nullify the contestor.
            contest_id = -1; contest_progress = 0.0f; contest_pause_timer = 0.0f;
        }

        if (Networking.IsOwner(gameObject)) {
            dict_points_keys_str = GlobalHelperFunctions.ConvertIntArrayToString(dict_points_keys_arr);
            dict_points_values_str = GlobalHelperFunctions.ConvertIntArrayToString(dict_points_values_arr);
        }

    }
    public void CheckPointContest(float deltaTime)
    {
        bool holder_on_point = false; bool contestor_on_point = false; int others_unique_count = 0; int other_id = 0;
        for (int i = 0; i < players_on_point.Length; i++)
        {
            // If all three conditions are already true, we do not need to search through the players on point any further; we have the information we need.
            if (holder_on_point && contestor_on_point && others_unique_count > 1) { break; }
            // Ignore invalid player entries
            if (players_on_point[i] < 0 || VRCPlayerApi.GetPlayerById(players_on_point[i]) == null) { continue; }
            int compare_id = players_on_point[i];
            // Make sure they are currently not in an invulnerable state
            if (gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(players_on_point[i])).ply_state == (int)player_state_name.Respawning) { continue; }
            // If we are in teamplay, use the team ID instead of the player ID
            if (gameController.option_teamplay) { compare_id = gameController.GetGlobalTeam(players_on_point[i]); }

            if (compare_id == hold_id) { holder_on_point = true; }
            else if (compare_id == contest_id) { contestor_on_point = true; }
            else if (compare_id == other_id) { others_unique_count++; }
            else { others_unique_count++; other_id = compare_id; }
        }
        //UnityEngine.Debug.Log("[CAPTURE_TEST]: Holder on point: " + holder_on_point + "; Contestor on point: " + contestor_on_point + "; Others on point: " + others_unique_count + "; other_id = " + other_id + "; Players on Point: " + gameController.ConvertIntArrayToString(players_on_point));

        // Enable overtime if there are others trying to contest the point. Overtime prevents a win, but allows holder point progress.
        overtime_enabled = (contestor_on_point || others_unique_count > 0 || contest_progress > 0.0f);
        //bool team_near_win = hold_points >= gameController.option_gm_goal - 1;
        bool team_near_win = false;
        // If the holder is on the point, pause contest progress.
        if (holder_on_point && contestor_on_point) { contest_pause_timer = 0.0f; }   
        // If no one other than the contestors are on point, give them progress.
        // We make this an if rather than an else-if because we do want to assign new contestors regardless of whether the holder is on point; all that matters is whether or not a contestor is or is not
        else if (!holder_on_point && contestor_on_point && others_unique_count == 0) { contest_progress += deltaTime; contest_pause_timer = 0.0f; }
        // If there is no contestor AND there is only one team/player on the point AND they are not the holder AND the game isn't about to end, assign the contestor to them.
        else if (others_unique_count == 1 && (contest_id < 0 || (contest_id >= 0 && !contestor_on_point && contest_progress < 0.0f)) && !team_near_win)
        {
            contest_progress = 0.01f; contest_pause_timer = 0.0f; contest_id = other_id;
            if (Networking.IsOwner(gameObject)) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayGlobalSoundEvent", (int)announcement_sfx_name.KOTH_Contest_Start_Team, contest_id); }
        }
        // Even if there are too many people on the point, we can still display a generic "Contested by multiple people" message
        else if (others_unique_count > 1 && (contest_id < 0 || (contest_id >= 0 && !contestor_on_point && contest_progress < 0.0f)) && !team_near_win)
        {
            contest_progress = 0.01f; contest_pause_timer = 0.0f; contest_id = -2;
        }
        else if (contestor_on_point)
        {
            // Even if no special condition is ongoing, if the contestor is on the point, make sure not to drain their progress
            contest_pause_timer = 0.0f;
        }

        contest_ongoing = contestor_on_point || others_unique_count > 0;
    }

    [NetworkCallable]
    public void PlayGlobalSoundEvent(int event_id, int override_id)
    {
        // Point capture SFX
        if (override_id >= 0 && event_id == (int)announcement_sfx_name.KOTH_Capture_Team)
        {
            if ((
                (gameController.option_teamplay && gameController.local_plyAttr != null && override_id == gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && override_id == Networking.LocalPlayer.playerId)
                ))
            {
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Capture_Team);
                gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.KOTH_CaptureSelf);
            }
            else if ((
                (gameController.option_teamplay && gameController.local_plyAttr != null && override_id != gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && override_id != Networking.LocalPlayer.playerId)
                ))
            {
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Capture_Other);
                gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.KOTH_CaptureOther);
            }
        }
        // Point contesting start SFX
        else if (override_id >= 0 && event_id == (int)announcement_sfx_name.KOTH_Contest_Start_Team)
        {
            if ((
                (gameController.option_teamplay && gameController.local_plyAttr != null && override_id == gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && override_id == Networking.LocalPlayer.playerId)
                ))
            {
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Contest_Start_Team);
            }
            else if ((
                (gameController.option_teamplay && gameController.local_plyAttr != null && override_id != gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && override_id != Networking.LocalPlayer.playerId)
                ))
            {
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Contest_Start_Other);
            }
        }
        // Point contesting progress SFX
        else if (override_id >= 0 && event_id == (int)announcement_sfx_name.KOTH_Contest_Progress)
        {
            //UnityEngine.Debug.Log("[KOTH_SFX_TEST]: Attempting to play KOTH_Contest_Progress with override ID " + override_id);
            if (contest_ongoing && local_is_on_point)
            {
                //UnityEngine.Debug.Log("[KOTH_SFX_TEST]: contest_ongoing && local_is_on_point; play KOTH_Contest_Progress");
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Contest_Progress, Mathf.Lerp(0.75f, 2.5f, contest_progress / gameController.option_gm_config_a));
            }
            /*
            if ((
                (gameController.option_teamplay && gameController.local_plyAttr != null && override_id == gameController.local_plyAttr.ply_team)
                || (!gameController.option_teamplay && override_id == Networking.LocalPlayer.playerId)
                ))
            {
                if (gameController.option_teamplay)
                {
                    // If in teamplay, make sure the local player is on the point and not just a teammate
                    for (int i = 0; i < players_on_point.Length; i++)
                    {
                        if (players_on_point[i] == Networking.LocalPlayer.playerId)
                        {
                            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Contest_Progress, Mathf.Lerp(0.75f, 2.5f, contest_progress / gameController.option_gm_config_a));
                            break;
                        }
                    }
                }
                else
                {
                    gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Contest_Progress, Mathf.Lerp(0.75f, 2.5f, contest_progress / gameController.option_gm_config_a));
                }
            }
            // If we are not the one holding or contesting the point and we are near end goal, locally play the victory near sound instead
            else if (hold_id >= 0 && hold_points >= gameController.option_gm_goal - 10 && !(Mathf.FloorToInt(hold_points) == gameController.option_gm_goal - 1 && overtime_enabled))
            {
                PlayGlobalSoundEvent((int)announcement_sfx_name.KOTH_Victory_Near, hold_id);
            }*/
        }
        else if (override_id >= 0 && event_id == (int)announcement_sfx_name.KOTH_Victory_Near)
        {
            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Victory_Near, Mathf.Lerp(1.0f, 1.5f, 1.0f - ((gameController.option_gm_goal - 11 - hold_points) / 10)));
        }
        else
        {
            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], event_id);
        }

    }

    public void HandleUI()
    {
        if (dict_points_keys_arr == null || dict_points_keys_arr.Length == 0 || gameController.option_gm_goal <= 0 || gameController.option_gm_config_a <= 0) { UITeamFlagCanvas.SetActive(false); return; }
        
        UITeamFlagCanvas.SetActive(true);
        if (captureDisplayArea != null) { captureDisplayArea.SetActive(true); }
        UITeamFlagCanvas.transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

        Vector3 initScale = new Vector3(0.000375f, 0.0006f, 0.000375f) * 0.5f;
        Vector3 ratio_desired = Vector3.zero;
        if (gameController != null && gameController.local_uiplytoself != null)
        {
            // scale of template / distance to template
            Vector3 calc_ui_pos = Vector3.zero;
            float ply_scale = 1.0f;
            if (gameController != null && gameController.local_plyAttr != null) {
                ply_scale = gameController.local_plyAttr.ply_scale;
            }
            calc_ui_pos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position +
                (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * (0.5f * (ply_scale) * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f)));
            ratio_desired =
                initScale
                /
                Mathf.Abs(Vector3.Distance(Networking.LocalPlayer.GetPosition(), calc_ui_pos));
        }
        // (scale_init / ply_dist_init)  = (scale_dist / current_dist)
        // scale_dist = current_dist * (scale_init / ply_dist_init)
        float scaleOtherUI = 1.0f;
        if (gameController != null && gameController.local_ppp_options != null) { scaleOtherUI = ((1.0f + gameController.local_ppp_options.ui_other_scale) / 2.0f); }
        UITeamFlagCanvas.transform.localScale = scaleOtherUI * (1.0f / 4.0f) * ratio_desired * Mathf.Abs(Vector3.Distance(Networking.LocalPlayer.GetPosition(), transform.position));
        //if (gameController.local_ppp_options != null) { UITeamFlagCanvas.transform.localScale *= gameController.local_ppp_options.ui_harm_scale; }

        UITeamLockImage.enabled = false;
        UITeamFlagImage.enabled = false;
        UITeamPoleImage.enabled = false;
        UITeamCBImage.enabled = false;

        UnityEngine.UI.Image activeImage = null;

        string displayText = "";
        string timerText = "";

        if (is_locked)
        {
            activeImage = UITeamLockImage;
            activeImage.enabled = true;
            RecolorDisplayArea(Color.gray);
            displayText = gameController.localizer.FetchText("CAPTUREZONE_HOLDER_LOCK", "(LOCKED)") + '\n';
            timerText = string.Format("{0:F1}", initial_lock_duration - initial_lock_timer);
            activeImage.color = Color.gray;
            UITeamCBImage.sprite = gameController.team_sprites[0];
            UITeamText.color = Color.gray;
            UITimerText.color = Color.gray;
            RecolorDisplayArea(Color.gray);
        }
        else
        {
            if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind)
            {
                activeImage = UITeamCBImage;
                activeImage.enabled = true;
            }
            else
            {
                activeImage = UITeamFlagImage;
                activeImage.enabled = true;
                UITeamPoleImage.enabled = true;
            }
        }
        
        if (hold_id >= 0)
        {
            int hold_index = hold_id;
            hold_index = GlobalHelperFunctions.DictIndexFromKey(hold_id, dict_points_keys_arr);
            if (gameController.option_teamplay)
            {
                activeImage.color = gameController.team_colors[hold_id];
                UITeamCBImage.sprite = gameController.team_sprites[hold_id];
                UITeamText.color = gameController.team_colors_bright[hold_id];
                UITimerText.color = gameController.team_colors_bright[hold_id];
                RecolorDisplayArea(gameController.team_colors[hold_id]);
                displayText += gameController.localizer.LocalizeTeamName(hold_id);
            }
            else
            {
                // If this is FFA, we can make the symbols and colors based on Teams 0 (self) and 1 (other)
                VRCPlayerApi hold_ply = VRCPlayerApi.GetPlayerById(hold_id);
                if (hold_ply != null) 
                {
                    if (hold_ply.playerId == Networking.LocalPlayer.playerId) 
                    {
                        activeImage.color = gameController.team_colors[0];
                        UITeamCBImage.sprite = gameController.team_sprites[0];
                        UITeamText.color = gameController.team_colors_bright[0];
                        UITimerText.color = gameController.team_colors_bright[0];
                        RecolorDisplayArea(gameController.team_colors[0]);
                    }
                    else
                    {
                        activeImage.color = gameController.team_colors[1];
                        UITeamCBImage.sprite = gameController.team_sprites[1];
                        UITeamText.color = gameController.team_colors_bright[1];
                        UITimerText.color = gameController.team_colors_bright[1];
                        RecolorDisplayArea(gameController.team_colors[1]);
                    }
                    displayText += hold_ply.displayName; 
                }
                else 
                {
                    activeImage.color = Color.white;
                    UITeamCBImage.sprite = gameController.team_sprites[3];
                    UITeamText.color = Color.white;
                    UITimerText.color = Color.white;
                    RecolorDisplayArea(Color.white);
                    displayText += gameController.localizer.FetchText("CAPTUREZONE_HOLDER_MISSING","(DISCONNECTED)"); 
                }
            }

            float timeLeft = gameController.option_gm_goal;
            //if (hold_index >= 0) 
            //{
            //    dict_points_values_arr = GlobalHelperFunctions.ConvertStrToIntArray(dict_points_values_str);
            int margin_time = Networking.IsOwner(gameObject) ? 1 : 2;
            timeLeft -= (hold_points + margin_time);
            timeLeft = Mathf.Max(0, timeLeft);
            //}
            //displayText += "\n ";
            if (timeLeft < 0.0f) { timerText = string.Format("{0:F1}", timeLeft); } 
            else { timerText = timeLeft.ToString(); }

        }
        else if (!is_locked)
        {
            activeImage.color = Color.white;
            UITeamCBImage.sprite = gameController.team_sprites[0];
            displayText += gameController.localizer.FetchText("CAPTUREZONE_HOLDER_NONE", "(OPEN)") + '\n';
            UITeamText.color = Color.white;
            UITimerText.color = Color.white;
            RecolorDisplayArea(Color.white);
        }

        if (contest_id >= 0)
        {
            displayText += '\n' + gameController.localizer.FetchText("CAPTUREZONE_CONTESTOR_HEADER_SINGLE", "[Contestor: ");
            float contest_pct = (contest_progress / gameController.option_gm_config_a);
            Color32 iColor = new Color32(255, 255, 0, 255);
            Color32 iColorB = new Color32(255, 255, 0, 255);
            if (gameController.option_teamplay)
            {
                displayText += gameController.localizer.LocalizeTeamName(contest_id);
                iColor = (Color)gameController.team_colors[contest_id];
                iColorB = (Color)gameController.team_colors_bright[contest_id];
            }
            else
            {
                VRCPlayerApi contest_ply = VRCPlayerApi.GetPlayerById(contest_id);
                if (contest_ply != null) 
                { 
                    if (contest_ply.playerId == Networking.LocalPlayer.playerId) 
                    { 
                        iColor = gameController.team_colors[0];
                        iColorB = gameController.team_colors_bright[0];
                    }
                    else
                    {
                        iColor = gameController.team_colors[2]; // Yellow to indicate contestor is someone else
                        iColorB = gameController.team_colors_bright[2];
                    }
                    displayText += contest_ply.displayName; 
                }
                else { displayText += gameController.localizer.FetchText("CAPTUREZONE_HOLDER_MISSING", "(DISCONNECTED)"); }
            }
            Color colorLerp = new Color(
                Mathf.Lerp(activeImage.color.r, ((Color)iColor).r, contest_pct)
                , Mathf.Lerp(activeImage.color.g, ((Color)iColor).g, contest_pct)
                , Mathf.Lerp(activeImage.color.b, ((Color)iColor).b, contest_pct)
                , Mathf.Lerp(activeImage.color.a, ((Color)iColor).a, contest_pct)
                );
            Color colorLerpB = new Color(
                Mathf.Lerp(UITeamText.color.r, ((Color)iColorB).r, contest_pct)
                , Mathf.Lerp(UITeamText.color.g, ((Color)iColorB).g, contest_pct)
                , Mathf.Lerp(UITeamText.color.b, ((Color)iColorB).b, contest_pct)
                , Mathf.Lerp(UITeamText.color.a, ((Color)iColorB).a, contest_pct)
                );
            activeImage.color = colorLerp;
            UITimerText.color = UITeamText.color;
            if (UIContestMeterBG != null) { UIContestMeterBG.color = UITeamText.color; }
            UITeamText.color = colorLerpB;
            if (UIContestMeterFG != null) { UIContestMeterFG.color = (Color)iColorB; }
            RecolorDisplayArea(colorLerp);
            displayText += "]\n" + Mathf.Round(contest_pct * 100.0f) + "%";
        }
        else if (contest_id == -2)
        {
            displayText += '\n' + gameController.localizer.FetchText("CAPTUREZONE_CONTESTOR_HEADER_MULTI", "[Contested by multiple ");
            if (gameController.option_teamplay) { displayText += gameController.localizer.FetchText("CAPTUREZONE_CONTESTOR_TEAM", "teams!]"); }
            else { displayText += gameController.localizer.FetchText("CAPTUREZONE_CONTESTOR_FFA", "players!]"); }
        }

        if (overtime_enabled && hold_points >= gameController.option_gm_goal - 1)
        {
            displayText = displayText + '\n' + gameController.localizer.FetchText("CAPTUREZONE_OVERTIME", "-- OVERTIME! --");
        }

        UITeamText.text = displayText;
        UITimerText.text = timerText;

        // Display the contest meter
        if (UIContestMeterFG != null && UIContestMeterBG != null)
        {
            UIContestMeterFG.gameObject.SetActive(contest_id >= 0);
            UIContestMeterBG.gameObject.SetActive(contest_id >= 0);
            float offsetMax = UIContestMeterBG.rectTransform.rect.width;
            float offsetPct = 0.0f;
            if (contest_progress > 0.0f && gameController.option_gm_config_a > 0) { offsetPct = System.Convert.ToSingle(contest_progress / gameController.option_gm_config_a); }
            UIContestMeterFG.rectTransform.offsetMax = new Vector2(-offsetMax + (offsetMax * offsetPct), UIContestMeterFG.rectTransform.offsetMax.y);
        }

        if (UIContestMeterFG == null) { UIContestMeterFG.gameObject.SetActive(false); }
        if (UIContestMeterBG == null) { UIContestMeterBG.gameObject.SetActive(false); }
    }

    public void RecolorDisplayArea(Color color)
    {
        if (captureDisplayArea == null) { return; }
        if (captureDisplayArea.GetComponent<Renderer>() != null)
        {
            captureDisplayArea.GetComponent<Renderer>().material.color = color;
            //captureDisplayArea.GetComponent<Renderer>().material.renderQueue = (int)RenderQueue.Overlay + 1;
        }

        foreach (Transform t in captureDisplayArea.GetComponentInChildren<Transform>())
        {
            if (t.GetComponent<ParticleSystem>() != null)
            {
                if (gameController != null && gameController.local_ppp_options != null) 
                {
                    var particle_emission = t.GetComponent<ParticleSystem>().emission;
                    particle_emission.enabled = gameController.local_ppp_options.particles_on; 
                }
                var particle_main = t.GetComponent<ParticleSystem>().main;
                particle_main.startColor = color;
            }
            else if (t.GetComponent<Renderer>() != null)
            {
                t.GetComponent<Renderer>().material.color = color;
            }
        }

        if (tubeArea != null) { tubeArea.set_color = color; }  
    }

    public void RecolorDisplayArea(Color32 color)
    {
        RecolorDisplayArea((Color)color);
    }

    public void SetRenderOrder()
    {

        /*foreach (Transform t in captureDisplayArea.GetComponentInChildren<Transform>())
        {
            if (t.GetComponent<Renderer>() != null)
            {
                t.GetComponent<Renderer>().material.renderQueue = (int)RenderQueue.Overlay + 1;
            }
        }*/

    }

    /*[NetworkCallable]
    public void LocalGrantPoints()
    {
        if (dict_points_keys_arr == null || dict_points_values_arr == null) { return; }
        int reward_ply_points = 0; int local_index_check = -1;
        if (gameController.option_teamplay)
        {
            if (gameController.local_plyAttr != null) { local_index_check = gameController.local_plyAttr.ply_team; }
        }
        else
        {
            if (gameController.local_plyAttr != null) { local_index_check = Networking.LocalPlayer.playerId; }
        }
        reward_ply_points = GlobalHelperFunctions.DictValueFromKey(local_index_check, dict_points_keys_arr, dict_points_values_arr);
        if (reward_ply_points >= 0) { gameController.local_plyAttr.ply_points = (ushort)reward_ply_points; }

        if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); }
    }
    */

    public void AddPlayerOnPoint(int player_id)
    {
        // Check the array for the ID, and if it does not exist, add it
        if (players_on_point == null) { players_on_point = new int[0]; }
        int[] add_players = new int[players_on_point.Length + 1];
        for (int i = 0; i < players_on_point.Length; i++)
        {
            if (players_on_point[i] == player_id) { return; } // We do not need to proceed any further if the ID is already in the array
            add_players[i] = players_on_point[i];
        }
        add_players[players_on_point.Length] = player_id;
        players_on_point = add_players;

        if (!local_is_on_point && player_id == Networking.LocalPlayer.playerId) { local_is_on_point = true; }
    }

    public void RemovePlayerOnPoint(int player_id)
    {
        // Check the array for the ID, and if it exists, remove it
        if (players_on_point == null) { players_on_point = new int[0]; return; }
        int[] remove_players;
        if (players_on_point.Length == 0) { remove_players = new int[1]; }
        else { remove_players = new int[players_on_point.Length - 1]; }
        int remove_index = -1;
        for (int i = 0; i < players_on_point.Length; i++)
        {
            if (players_on_point[i] == player_id) { remove_index = i; continue; } 

            if (remove_index < 0)
            {
                if (i == remove_players.Length) { return; } // If we're at the end of the array and no player was found, we do not need to proceed
                else { remove_players[i] = players_on_point[i]; }
            }
            else
            {
                if (i < remove_index) { remove_players[i] = players_on_point[i]; }
                else if (i > remove_index) { remove_players[i - 1] = players_on_point[i]; }
            }
        }
        players_on_point = remove_players;

        if (local_is_on_point && player_id == Networking.LocalPlayer.playerId) { local_is_on_point = false; }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy) { return; }
        VRCPlayerApi player = Networking.GetOwner(other.gameObject);
        if (player == null || other.GetComponent<PlayerHitbox>() == null) { return; }
        if (gameController != null && gameController.local_plyAttr != null && gameController.local_plyAttr.ply_state == (int)player_state_name.Respawning && player.playerId == Networking.LocalPlayer.playerId)
        {
            gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_KOTH_RESPAWN_WARNING", "Cannot interact with point while invulnerable!"), Color.gray);
        }
        
        // If we are not the master, signal to them that we entered the trigger
        if (!Networking.IsOwner(gameObject)) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SignalTriggerEnter", player.playerId); }
        // Regardless of whether or not we are the master, try manipulate the array just so we have a local copy
        AddPlayerOnPoint(player.playerId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy) { return; }
        VRCPlayerApi player = Networking.GetOwner(other.gameObject);
        if (player == null || other.GetComponent<PlayerHitbox>() == null) { return; }
        // If we are not the master, signal to them that we entered the trigger
        if (!Networking.IsOwner(gameObject)) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SignalTriggerExit", player.playerId); }
        // Regardless of whether or not we are the master, try manipulate the array just so we have a local copy
        RemovePlayerOnPoint(player.playerId);
    }

    [NetworkCallable]
    public void SignalTriggerEnter(int player_id)
    {
        // Function to signal to the master that a player has entered the trigger, even if the master's client doesn't see it yet
        AddPlayerOnPoint(player_id);

    }

    [NetworkCallable]
    public void SignalTriggerExit(int player_id)
    {
        // Function to signal to the master that a player has exited the trigger, even if the master's client doesn't see it yet
        RemovePlayerOnPoint(player_id);
    }

}
