
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using static UnityEngine.UI.Image;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class CaptureZone : UdonSharpBehaviour
{

    [Tooltip("How long should this point be locked before it can be captured?")]
    [SerializeField] public float initial_lock_duration = 30.0f;
    [Tooltip("How often should this capture point perform a Physics check for player hitboxes?")]
    [SerializeField] public float check_players_impulse = 0.5f;
    [Tooltip("How long should the contestor be allowed to remain outside of the capture zone before their progress fades? (MUST be > check_players_impulse)")]
    [SerializeField] [UdonSynced] public float contest_pause_duration = 4.0f;
    [Tooltip("How often should a capture point grant points? (While there shouldn't be much reason for this to not be 1, it's good to be prepared.)")]
    [SerializeField] public float point_grant_impulse = 1.0f;

    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject captureDisplayArea;
    [SerializeField] public GameObject UITeamFlagCanvas;
    [SerializeField] public UnityEngine.UI.Image UITeamLockImage;
    [SerializeField] public UnityEngine.UI.Image UITeamFlagImage;
    [SerializeField] public UnityEngine.UI.Image UITeamPoleImage;
    [SerializeField] public UnityEngine.UI.Image UITeamCBImage;
    [SerializeField] public TMP_Text UITeamText;

    [NonSerialized] [UdonSynced] public double last_network_time = 0.0f;
    [NonSerialized] [UdonSynced] public float check_players_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float contest_pause_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float initial_lock_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float hold_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float point_grant_timer = 0.0f;
    [NonSerialized] [UdonSynced] public bool is_locked = true;
    [NonSerialized] [UdonSynced] public int hold_id = -1;
    [NonSerialized] [UdonSynced] public int contest_id = -1;
    [NonSerialized] [UdonSynced] public float contest_progress = 0.0f;
    [NonSerialized] [UdonSynced] public bool overtime_enabled = false; // This will only be true if there is both a holder and a contestor.
    [NonSerialized] public int[] players_on_point;
    [NonSerialized] public int global_index;

    [NonSerialized] public int[] dict_points_keys_arr;
    [NonSerialized][UdonSynced] public string dict_points_keys_str = "";
    [NonSerialized] public int[] dict_points_values_arr;
    [NonSerialized][UdonSynced] public string dict_points_values_str = "";

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

    }

    private void OnEnable()
    {
        SetRenderOrder();
        ResetZone();
    }

    public void ResetZone()
    {
        hold_id = -1;
        hold_timer = 0.0f;
        contest_id = -1;
        contest_progress = 0.0f;
        check_players_timer = 0.0f;
        initial_lock_timer = 0.0f;
        contest_pause_timer = 0.0f;
        is_locked = true;
        if (captureDisplayArea != null) { captureDisplayArea.SetActive(false); }
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }

    private void Update()
    {
        HandleUI();

        // Networking Sync
        if (!Networking.IsMaster)
        {
            dict_points_keys_arr = gameController.ConvertStrToIntArray(dict_points_keys_str);
            dict_points_values_arr = gameController.ConvertStrToIntArray(dict_points_values_str);
        }

        // -- Master Only Below --

        if (!Networking.IsMaster) { return; }

        double currentNetworkTime = Networking.GetServerTimeInSeconds();
        float networkTimeDelta = (float)Networking.CalculateServerDeltaTime(currentNetworkTime, last_network_time);
        last_network_time = currentNetworkTime;

        // Impulse point granting (also used for any events which will occur every second)
        if (point_grant_timer >= point_grant_impulse)
        {
            point_grant_timer = 0.0f;
            if (!is_locked && dict_points_keys_arr != null && dict_points_values_arr != null)
            {
                int hold_index = gameController.DictIndexFromKey(hold_id, dict_points_keys_arr);
                if (hold_index >= 0 && hold_index < dict_points_keys_arr.Length)
                {
                    if (dict_points_values_arr[hold_index] < gameController.option_gm_goal) { dict_points_values_arr[hold_index]++; } // Normal condition
                    else if (!overtime_enabled) { dict_points_values_arr[hold_index]++; } // Victory condition
                    else { } // Overtime condition
                }
                // Play sound while contesting
                if ((
                    (gameController.option_teamplay && gameController.local_plyAttr != null && contest_id == gameController.local_plyAttr.ply_team)
                    || (!gameController.option_teamplay && contest_id == Networking.LocalPlayer.playerId)
                    ))
                {
                    gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Contest, Mathf.Lerp(0.75f, 2.5f, contest_progress / gameController.option_gm_config_a));
                }

                if (!Networking.IsMaster) { LocalGrantPoints(); }
                else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalGrantPoints"); }
            }
        }
        else
        {
            point_grant_timer += networkTimeDelta;
        }

        // Handle point locking
        if (is_locked && initial_lock_timer >= initial_lock_duration)
        {
            is_locked = false;
            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Unlock);
        }
        else if (is_locked && gameController.round_state == (int)round_state_name.Ongoing)
        {
            initial_lock_timer += networkTimeDelta;
        }
        
        if (is_locked)
        {
            dict_points_keys_str = gameController.ConvertIntArrayToString(dict_points_keys_arr);
            dict_points_values_str = gameController.ConvertIntArrayToString(dict_points_values_arr);
            return; 
        }
        
        // -- Below will only occur if the point is unlocked and active --

        // Impulse check for players on point
        if (check_players_timer >= check_players_impulse)
        {
            players_on_point = CheckPlayersOnPoint();
            CheckPointContest();
            check_players_timer = 0.0f;
        }
        else if (check_players_timer < check_players_impulse)
        {
            check_players_timer += networkTimeDelta;
        }


        // If the contestor exists but is not present on the point, drain contestor's progress.
        if (contest_id >= 0 && contest_progress >= 0.0f)
        {
            if (contest_pause_timer >= contest_pause_duration) { contest_progress -= networkTimeDelta; }
            else { contest_pause_timer += networkTimeDelta; }
        }

        // If the contest_progress exceeds duration, then contestor becomes the new holder; reset contestor and contest_progress.
        if (contest_progress >= gameController.option_gm_config_a)
        {
            hold_id = contest_id;
            contest_id = -1;
            contest_progress = 0.0f;
            contest_pause_timer = 0.0f;
            point_grant_timer = 0.0f;

            // Play SFX based on whether it was the player's team that captured it or not
            if (gameController.option_teamplay)
            {
                if (gameController.local_plyAttr != null && hold_id == gameController.local_plyAttr.ply_team)
                {
                    gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Capture_Team);
                }
                else
                {
                    gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Capture_Other);
                }
            }
            else
            {
                if (hold_id == Networking.LocalPlayer.playerId)
                {
                    gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Capture_Team);
                }
                else
                {
                    gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Announcement], gameController.snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Capture_Other);
                }
            }
        }
        else if (contest_progress < 0.0f && contest_id >= 0)
        {
            // If the contestor exists and contesting contest_progress drops below 0.0f, set to 0.0f and nullify the contestor.
            contest_id = -1; contest_progress = 0.0f; contest_pause_timer = 0.0f;
        }

        dict_points_keys_str = gameController.ConvertIntArrayToString(dict_points_keys_arr);
        dict_points_values_str = gameController.ConvertIntArrayToString(dict_points_values_arr);

        
    }

    public int[] CheckPlayersOnPoint()
    {
        LayerMask layers_to_hit = LayerMask.GetMask("PlayerHitbox", "LocalPlayerHitbox");
        Bounds BoxBounds = transform.GetComponent<Collider>().bounds;
        Vector3 capsuleHeight = new Vector3(0.0f, (BoxBounds.max.y - BoxBounds.min.y) / 2.0f, 0.0f);
        float capsuleRadius = ((BoxBounds.max.z - BoxBounds.min.z) + (BoxBounds.max.x - BoxBounds.min.x)) / 4.0f;
        Collider[] hitColliders = Physics.OverlapCapsule(transform.position - capsuleHeight, transform.position + capsuleHeight, capsuleRadius, layers_to_hit, QueryTriggerInteraction.Collide);
        //Collider[] hitColliders = Physics.OverlapBox(transform.position, new Vector3((BoxBounds.max.x - BoxBounds.min.x), (BoxBounds.max.y - BoxBounds.min.y), (BoxBounds.max.z - BoxBounds.min.z)) );

        int[] players_temp = new int[hitColliders.Length]; ushort players_count = 0;
        foreach (Collider collider in hitColliders)
        {
            if (CheckCollider(collider))
            {
                players_temp[players_count] = Networking.GetOwner(collider.gameObject).playerId;
                players_count++;
            }
        }
        int[] players = new int[players_count];
        for (int i = 0; i < players_count; i++)
        {
            players[i] = players_temp[i];
        }

        return players;
    }

    public void CheckPointContest()
    {
        bool holder_on_point = false; bool contestor_on_point = false; int others_unique_count = 0; int other_id = 0;
        for (int i = 0; i < players_on_point.Length; i++)
        {
            // If all three conditions are already true, we do not need to search through the players on point any further; we have the information we need.
            if (holder_on_point && contestor_on_point && others_unique_count > 1) { break; }
            // Ignore invalid player entries
            if (players_on_point[i] < 0 || VRCPlayerApi.GetPlayerById(players_on_point[i]) == null) { continue; }
            int compare_id = players_on_point[i];
            // If we are in teamplay, use the team ID instead of the player ID
            if (gameController.option_teamplay) { compare_id = gameController.GetGlobalTeam(players_on_point[i]); }

            if (compare_id == hold_id) { holder_on_point = true; }
            else if (compare_id == contest_id) { contestor_on_point = true; }
            else if (compare_id == other_id) { others_unique_count++; }
            else { others_unique_count++; other_id = compare_id; }
        }
        //UnityEngine.Debug.Log("[KOTH_TEST]: Holder on point: " + holder_on_point + "; Contestor on point: " + contestor_on_point + "; Others on point: " + others_unique_count + "; other_id = " + other_id + "; Players on Point: " + gameController.ConvertIntArrayToString(players_on_point));

        // If the holder is on the point, enable overtime if there are others trying to contest it. Otherwise, disable overtime.
        // Overtime prevents a win, but allows progress.
        if (holder_on_point) { overtime_enabled = (contestor_on_point || others_unique_count > 0); }
        // If no one other than the contestors are on point, give them progress.
        else if (contestor_on_point && others_unique_count == 0) { contest_progress += check_players_impulse; contest_pause_timer = 0.0f; }

        // If there is no contestor AND there is only one team/player on the point AND they are not the holder, assign the contestor to them.
        if (others_unique_count == 1 && (contest_id < 0 || (contest_id >= 0 && !contestor_on_point && contest_progress < 0.0f))) 
        { contest_id = other_id; contest_progress = 0.0f; contest_pause_timer = 0.0f; }
    }

    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy) { return false; }
        // Did we hit a hitbox?
        if (other.gameObject.GetComponent<PlayerHitbox>() != null && Networking.GetOwner(other.gameObject) != null)
        {
            return true;
        }
        return false;
    }

    public void HandleUI()
    {
        if (dict_points_keys_arr == null || dict_points_keys_arr.Length == 0 || gameController.option_gm_goal <= 0 || gameController.option_gm_config_a <= 0) { UITeamFlagCanvas.SetActive(false); return; }
        
        UITeamFlagCanvas.SetActive(true);
        if (captureDisplayArea != null) { captureDisplayArea.SetActive(true); }
        UITeamFlagCanvas.transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

        Vector3 initScale = new Vector3(0.000375f, 0.0006f, 0.000375f);
        UITeamFlagCanvas.transform.localScale = initScale * Mathf.Min(1.0f, (Mathf.Abs(Vector3.Distance(Networking.LocalPlayer.GetPosition(), transform.position)) / 2.0f)); 

        UITeamLockImage.enabled = false;
        UITeamFlagImage.enabled = false;
        UITeamPoleImage.enabled = false;
        UITeamCBImage.enabled = false;

        UnityEngine.UI.Image activeImage = null;

        string displayText = "";

        if (is_locked)
        {
            activeImage = UITeamLockImage;
            activeImage.enabled = true;
            RecolorDisplayArea(Color.gray);
            displayText = "(LOCKED)" + "\n" + (Mathf.Floor((initial_lock_duration - initial_lock_timer) * 10.0f) / 10.0f).ToString().PadRight(2, '.').PadRight(3, '0');
            activeImage.color = Color.gray;
            UITeamCBImage.sprite = gameController.team_sprites[0];
            UITeamText.color = Color.gray;
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
            hold_index = gameController.DictIndexFromKey(hold_id, dict_points_keys_arr);
            if (gameController.option_teamplay)
            {
                activeImage.color = gameController.team_colors[hold_id];
                UITeamCBImage.sprite = gameController.team_sprites[hold_id];
                UITeamText.color = gameController.team_colors_bright[hold_id];
                RecolorDisplayArea(gameController.team_colors[hold_id]);
                displayText += gameController.team_names[hold_id];
            }
            else
            {
                activeImage.color = Color.white;
                UITeamCBImage.sprite = gameController.team_sprites[0];
                UITeamText.color = Color.white;
                RecolorDisplayArea(Color.white);
                VRCPlayerApi hold_ply = VRCPlayerApi.GetPlayerById(hold_id);
                if (hold_ply != null) { displayText += hold_ply.displayName; }
                else { displayText += "(DISCONNECTED)"; }
            }

            float timeLeft = (gameController.option_gm_goal - dict_points_values_arr[hold_index]);
            displayText += "\n ";
            if (timeLeft < 0.0f) { displayText += (Mathf.Floor(timeLeft * 10.0f) / 10.0f).ToString().PadRight(2, '.').PadRight(3, '0'); }
            else { displayText += timeLeft.ToString(); }

        }
        else if (!is_locked)
        {
            activeImage.color = Color.gray;
            UITeamCBImage.sprite = gameController.team_sprites[0];
            displayText += "(OPEN)";
            UITeamText.color = Color.gray;
            RecolorDisplayArea(Color.gray);
        }

        if (contest_id >= 0)
        {
            displayText += "\n [Contestor: ";
            float contest_pct = (contest_progress / gameController.option_gm_config_a);
            Color32 iColor = new Color32(255, 255, 0, 255);
            Color32 iColorB = new Color32(255, 255, 0, 255);
            if (gameController.option_teamplay)
            {
                displayText += gameController.team_names[contest_id];
                iColor = (Color)gameController.team_colors[contest_id];
                iColorB = (Color)gameController.team_colors_bright[contest_id];
            }
            else
            {
                VRCPlayerApi contest_ply = VRCPlayerApi.GetPlayerById(contest_id);
                if (contest_ply != null) { displayText += contest_ply.displayName; }
                else { displayText += "(DISCONNECTED)"; }
            }
            Color colorLerp = new Color(
                Mathf.Lerp(activeImage.color.r, ((Color)iColor).r, contest_pct)
                , Mathf.Lerp(activeImage.color.g, ((Color)iColor).g, contest_pct)
                , Mathf.Lerp(activeImage.color.b, ((Color)iColor).b, contest_pct)
                , Mathf.Lerp(activeImage.color.a, ((Color)iColor).a, contest_pct)
                );
            Color colorLerpB = new Color(
                Mathf.Lerp(activeImage.color.r, ((Color)iColorB).r, contest_pct)
                , Mathf.Lerp(activeImage.color.g, ((Color)iColorB).g, contest_pct)
                , Mathf.Lerp(activeImage.color.b, ((Color)iColorB).b, contest_pct)
                , Mathf.Lerp(activeImage.color.a, ((Color)iColorB).a, contest_pct)
                );
            activeImage.color = colorLerp;
            UITeamText.color = colorLerpB;
            RecolorDisplayArea(colorLerp);
            displayText += " | " + Mathf.Round(contest_pct * 100.0f) + "%]";
        }
        UITeamText.text = displayText;

    }

    public void RecolorDisplayArea(Color color)
    {
        if (captureDisplayArea == null) { return; }
        if (captureDisplayArea.GetComponent<Renderer>() != null)
        {
            captureDisplayArea.GetComponent<Renderer>().material.color = color;
            captureDisplayArea.GetComponent<Renderer>().material.renderQueue = (int)RenderQueue.Overlay + 1;
        }

        foreach (Transform t in captureDisplayArea.GetComponentInChildren<Transform>())
        {
            if (t.GetComponent<Renderer>() != null) 
            { 
                t.GetComponent<Renderer>().material.color = color;
            }
        }
    }

    public void RecolorDisplayArea(Color32 color)
    {
        RecolorDisplayArea((Color)color);
    }

    public void SetRenderOrder()
    {

        foreach (Transform t in captureDisplayArea.GetComponentInChildren<Transform>())
        {
            if (t.GetComponent<Renderer>() != null)
            {
                t.GetComponent<Renderer>().material.renderQueue = (int)RenderQueue.Overlay + 1;
            }
        }

    }

    [NetworkCallable]
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
        reward_ply_points = gameController.DictValueFromKey(local_index_check, dict_points_keys_arr, dict_points_values_arr);
        if (reward_ply_points >= 0) { gameController.local_plyAttr.ply_points = (ushort)reward_ply_points; }

        if (Networking.IsMaster) { gameController.CheckForRoundGoal(); }
    }

}
