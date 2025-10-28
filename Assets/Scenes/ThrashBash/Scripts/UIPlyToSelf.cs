
using System;
using System.IO.Pipes;
using System.Linq;
using TMPro;
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VRC.SDK3.Rendering;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class UIPlyToSelf : GlobalTickReceiver
{
    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] public RectTransform PTSCanvas;
    [SerializeField] public RectTransform PTSTextStackParent;
    [SerializeField] public RectTransform[] PTSTextStack;
    [SerializeField] public TMP_Text[] PTSTextStack_Label;
    [SerializeField] public GameObject PTSTopPanel;
    [SerializeField] public RectTransform PTSTimerTransform;
    [SerializeField] public TMP_Text PTSTimer;
    [SerializeField] public Sprite PTSTimerImage;
    [SerializeField] public TMP_Text PTSLives;
    [SerializeField] public UnityEngine.UI.Image PTSLivesImage;
    [SerializeField] public RectTransform PTSLivesTransform;
    [SerializeField] public Sprite PTSLivesSprite;
    [SerializeField] public Sprite PTSPointsSprite;
    [SerializeField] public Sprite PTSDeathsSprite;
    [SerializeField] public Sprite PTSFlagSprite;
    [SerializeField] public TMP_Text PTSDamage;
    [SerializeField] public RectTransform PTSDamageTransform;
    [SerializeField] public TMP_Text PTSRecentDamage; 
    [SerializeField] public TMP_Text PTSAttack;
    [SerializeField] public TMP_Text PTSDefense;
    [SerializeField] public TMP_Text PTSInvul;
    [SerializeField] public RectTransform PTSInvulTransform;
    [SerializeField] public GameObject PTSTeamFlag;
    [SerializeField] public UnityEngine.UI.Image PTSTeamFlagImage;
    [SerializeField] public UnityEngine.UI.Image PTSTeamPoleImage;
    [SerializeField] public TMP_Text PTSTeamText;
    [SerializeField] public RectTransform PTSTeamTransform;
    [SerializeField] public UnityEngine.UI.Image PTSTeamCBSpriteImage;
    [SerializeField] public TMP_Text PTSPlacementText;

    [SerializeField] public Transform PTSWeaponPanel;
    [SerializeField] public UnityEngine.UI.Image PTSWeaponSprite;
    [SerializeField] public TMP_Text PTSWeaponText;
    [SerializeField] public UnityEngine.UI.Image PTSSecondaryWeaponSprite;
    [SerializeField] public TMP_Text PTSSecondaryWeaponText;

    [SerializeField] public Transform PTSChargePanel;
    [SerializeField] public UnityEngine.UI.Image PTSChargeMeterFGSprite;
    [SerializeField] public UnityEngine.UI.Image PTSChargeMeterBGSprite;
    [SerializeField] public UnityEngine.UI.Image PTSSecondaryChargeMeterFGSprite;
    [SerializeField] public UnityEngine.UI.Image PTSSecondaryChargeMeterBGSprite;
    [SerializeField] public UnityEngine.UI.Image PTSAirThrustMeterFGSprite;
    [SerializeField] public UnityEngine.UI.Image PTSAirThrustMeterBGSprite;

    [SerializeField] public Transform PTSPowerupPanel;
    [NonSerialized] public UnityEngine.UI.Image[] PTSPowerupSprites;

    [SerializeField] public Transform PTSCapturePanel;
    [NonSerialized] public RectTransform[] PTSCaptureSprites;
    [NonSerialized] public RectTransform[] PTSCaptureOverlays;
    [NonSerialized] public TMP_Text[] PTSCaptureTexts;
    [NonSerialized] public TMP_Text[] PTSContestTexts;

    [SerializeField] public Transform PTSScorePanel;
    [NonSerialized] public RectTransform[] PTSScoreParents;
    [NonSerialized] public RectTransform[] PTSScoreSprites;
    [NonSerialized] public RectTransform[] PTSScorePoles;
    [NonSerialized] public TMP_Text[] PTSScoreNameTexts;
    [NonSerialized] public TMP_Text[] PTSScorePlacementTexts;
    [NonSerialized] public TMP_Text[] PTSScoreNumberTexts;

    [SerializeField] public GameObject PTSPainDirTemplate;
    [SerializeField] public GameObject PTSHarmNumberTemplate;
    [NonSerialized] public UIHarmNumber[] PTSHarmNumberList;
    [SerializeField] public GameObject PTSParticleTemplate;
    [SerializeField] public int max_particle_emitters_in_pool = 10;
    [SerializeField] public int particle_pool_iter = 0;
    [NonSerialized] public ParticleSystem[] PTSParticleList;

    [NonSerialized] public PlayerAttributes playerAttributes;
    //[NonSerialized] public UIMessagesToSelf local_uimessagestoself;

    // Fields used for demonstrating UI scale when modifying local options
    [SerializeField] public float ui_demo_duration = 5.0f;
    [NonSerialized] public float ui_demo_timer = 0.0f;
    [NonSerialized] public bool ui_demo_enabled = false;
    [NonSerialized] public bool ui_show_intro_text = true;

    [NonSerialized] public string text_queue_full_str = ""; // Queue system for local HUD messages, separated by the delineation character.
    [NonSerialized] public char text_queue_separator = '\r';
    [SerializeField] public int text_queue_full_max_lines = 24; // What is the hardcap on queued messages?
    [SerializeField] public int text_queue_limited_lines = 4; // Number of lines that will display at once from the text queue
    [SerializeField] public float text_queue_duration_default = 5.0f; // How long should an active message be displayed?
    [SerializeField] public int text_queue_duration_max_characters = 90; // How many characters should a message be for it to be double the default display time?
    [SerializeField] public float text_queue_limited_fade_time_percent = 0.20f; // At what % of the the duration should the text begin fading? (i.e. if duration is 5.0f, 0.20f means fade at 4.0f)
    [SerializeField] public float text_queue_limited_extend = 0.5f; // How much longer should an active message be displayed if it is not the top message?
    [NonSerialized] public float[] text_queue_limited_timers;
    [NonSerialized] public Color[] text_queue_full_colors;
    [NonSerialized] public float[] text_queue_full_durations;

    [SerializeField] public float ui_check_gamevars_impulse_default = 0.4f;// How often should we check for game variables (i.e. team lives, points, etc.)
    [NonSerialized] public float ui_check_gamevars_impulse = 0.4f; 
    [NonSerialized] public float ui_check_gamevars_timer = 0.0f;

    [NonSerialized] public int[] gamevars_leaderboard_arr;
    [NonSerialized] public int[] gamevars_progress_arr;
    [NonSerialized] public string gamevars_leader_name;
    [NonSerialized] public int[] gamevars_local_team_points;
    [NonSerialized] public int[] gamevars_local_team_deaths;
    [NonSerialized] public int[] gamevars_local_team_lives;
    [NonSerialized] public int gamevars_local_highest_team;
    [NonSerialized] public int gamevars_local_highest_points;
    [NonSerialized] public int gamevars_local_highest_ply_id;
    [NonSerialized] public int gamevars_local_lowest_team;
    [NonSerialized] public int gamevars_local_lowest_deaths;
    [NonSerialized] public int gamevars_local_lowest_ply_id;
    [NonSerialized] public int gamevars_local_total_lives;
    [NonSerialized] public int gamevars_local_team_members_alive;
    [NonSerialized] public byte gamevars_local_players_alive;
    [NonSerialized] public byte gamevars_local_teams_alive;
    [NonSerialized] public bool gamevars_force_refresh_on_next_tick;

    // Stored positions for inverted UI arrangement
    [SerializeField] private Vector2 stored_local_sizedelta_ptscanvas;
    [SerializeField] private Vector3 stored_local_pos_ptsairthrust;
    [SerializeField] private Vector3 stored_local_pos_ptsweaponpanel;
    [SerializeField] private Vector3 stored_local_pos_ptschargepanel;
    [SerializeField] private Vector3 stored_local_pos_ptscapturepanel;
    [SerializeField] private Vector3 stored_local_pos_ptsscorepanel;
    [SerializeField] private Vector3 stored_local_pos_ptspoweruppanel;
    [SerializeField] private Vector3 stored_local_pos_ptstoppanel;
    [SerializeField] private Vector3 stored_local_pos_ptstextstackparent;
    [SerializeField] private Vector4 stored_local_anchor_ptstextstackparent;

    // Cached stats for UI update referencing
    [NonSerialized] public float cached_scale = -1.0f;
    [NonSerialized] public float cached_atk = -1.0f;
    [NonSerialized] public float cached_def = -1.0f;
    [NonSerialized] public float damage_receive_temp = 0.0f;
    [NonSerialized] public float damage_receive_duration = 3.0f;
    [NonSerialized] public float damage_receive_timer = 0.0f;

    public override void Start()
    {
        base.Start();
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        SetRenderQueueFromParent(transform);

        ui_check_gamevars_impulse = ui_check_gamevars_impulse_default;

        stored_local_sizedelta_ptscanvas = PTSCanvas.sizeDelta;
        stored_local_pos_ptsairthrust = PTSAirThrustMeterBGSprite.transform.localPosition;
        stored_local_pos_ptsweaponpanel = PTSWeaponPanel.localPosition;
        stored_local_pos_ptschargepanel = PTSChargePanel.localPosition;
        stored_local_pos_ptscapturepanel = PTSCapturePanel.localPosition;
        stored_local_pos_ptsscorepanel = PTSScorePanel.localPosition;
        stored_local_pos_ptspoweruppanel = PTSPowerupPanel.localPosition;
        stored_local_pos_ptstoppanel = PTSTopPanel.transform.localPosition;
        stored_local_pos_ptstextstackparent = PTSTextStackParent.localPosition;
        stored_local_anchor_ptstextstackparent = new Vector4(PTSTextStackParent.anchorMin.x, PTSTextStackParent.anchorMin.y, PTSTextStackParent.anchorMax.x, PTSTextStackParent.anchorMax.y);
        /*for (int i = 0; i < text_queue_limited_lines; i++)
        {
            //PTSTextStack[i].sizeDelta = new Vector2(PTSTextStack[i].sizeDelta.x, PTSCanvas.sizeDelta.y / 10.0f);
            float size_delta = PTSCanvas.sizeDelta.y / 10.0f;
            float half_line = (text_queue_limited_lines / 2);
            if (i < half_line)
            {
                PTSTextStack[i].localPosition = new Vector3(
                    PTSTextStack[i].localPosition.x
                    , ((half_line - i) * size_delta) - (size_delta / 2)
                    , PTSTextStack[i].localPosition.z);
            }
            else
            {
                PTSTextStack[i].localPosition = new Vector3(
                    PTSTextStack[i].localPosition.x
                    , (-(i - half_line) * size_delta) - (size_delta / 2)
                    , PTSTextStack[i].localPosition.z);
            }
        }*/

        text_queue_limited_timers = new float[text_queue_limited_lines];
        for (int t = 0; t < text_queue_limited_timers.Length; t++)
        {
            text_queue_limited_timers[t] -= ((t - 1) * text_queue_limited_extend);
        }

        text_queue_full_colors = new Color[text_queue_full_max_lines + 1];
        text_queue_full_durations = new float[text_queue_full_max_lines + 1];
        for (int t = 0; t < text_queue_full_colors.Length; t++)
        {
            text_queue_full_colors[t] = Color.white;
            text_queue_full_durations[t] = text_queue_duration_default;
        }

        var item_index = 0;
        var item_size = 0;
        foreach (GameObject child in (Transform)PTSPowerupPanel)
        {
            if (child.name.Contains("PTSPowerupSprite")) { item_size++; }
        }

        PTSPowerupSprites = new UnityEngine.UI.Image[item_size];
        foreach (Transform child in (Transform)PTSPowerupPanel)
        {
            if (child.name.Contains("PTSPowerupSprite"))
            {
                PTSPowerupSprites[item_index] = child.GetComponent<UnityEngine.UI.Image>();
                child.gameObject.SetActive(false);
                item_index++;
            }
        }

        var capture_index = 0;
        var capture_size = 0;
        foreach (GameObject child in (Transform)PTSCapturePanel)
        {
            if (child == null) { continue; }
            if (child.name.Contains("PTSCaptureSprite")) { capture_size++; }
        }
        PTSCaptureSprites = new RectTransform[capture_size];
        PTSCaptureOverlays = new RectTransform[capture_size];
        PTSCaptureTexts = new TMP_Text[capture_size];
        PTSContestTexts = new TMP_Text[capture_size];

        foreach (Transform child in (Transform)PTSCapturePanel)
        {
            if (child.name.Contains("PTSCaptureSprite"))
            {
                PTSCaptureSprites[capture_index] = (RectTransform)child;
                child.gameObject.SetActive(false);
                foreach (Transform subchild in child)
                {
                    if (subchild.name.Contains("PTSCaptureOverlay")) { PTSCaptureOverlays[capture_index] = (RectTransform)subchild; }
                    if (subchild.name.Contains("PTSCaptureInnerPanel"))
                    {
                        foreach (Transform verysubchild in subchild)
                        {
                            if (verysubchild.name.Contains("PTSCaptureText")) { PTSCaptureTexts[capture_index] = verysubchild.GetComponent<TMP_Text>(); }
                            if (verysubchild.name.Contains("PTSContestText")) { PTSContestTexts[capture_index] = verysubchild.GetComponent<TMP_Text>(); }
                        }
                    }
                }
                capture_index++;
            }
        }

        var score_index = 0;
        var score_size = 0;
        foreach (GameObject child in (Transform)PTSScorePanel)
        {
            if (child == null) { continue; }
            if (child.name.Contains("UIScoreParent")) { score_size++; }
        }
        PTSScoreParents = new RectTransform[score_size];
        PTSScorePoles = new RectTransform[score_size];
        PTSScoreSprites = new RectTransform[score_size];
        PTSScoreNameTexts = new TMP_Text[score_size];
        PTSScorePlacementTexts = new TMP_Text[score_size];
        PTSScoreNumberTexts = new TMP_Text[score_size];
        foreach (Transform child in (Transform)PTSScorePanel)
        {
            if (child.name.Contains("UIScoreParent"))
            {
                PTSScoreParents[score_index] = (RectTransform)child;
                child.gameObject.SetActive(false);
                foreach (Transform subchild in child)
                {
                    if (subchild.name.Contains("UIScoreSprite")) { PTSScoreSprites[score_index] = (RectTransform)subchild; }
                    if (subchild.name.Contains("UIScorePole")) { PTSScorePoles[score_index] = (RectTransform)subchild; }
                    if (subchild.name.Contains("UIScorePlacementText")) { PTSScorePlacementTexts[score_index] = subchild.GetComponent<TMP_Text>();  }
                    if (subchild.name.Contains("UIScoreNameText")) { PTSScoreNameTexts[score_index] = subchild.GetComponent<TMP_Text>();  }
                    if (subchild.name.Contains("UIScoreNumberText")) { PTSScoreNumberTexts[score_index] = subchild.GetComponent<TMP_Text>(); }
                }
                score_index++;
            }
        }

        if (PTSWeaponSprite != null) { PTSWeaponSprite.gameObject.SetActive(false); }
        if (PTSChargeMeterFGSprite != null) { PTSChargeMeterFGSprite.gameObject.SetActive(false); }
        if (PTSChargeMeterBGSprite != null) { PTSChargeMeterBGSprite.gameObject.SetActive(false); }
        if (PTSSecondaryWeaponSprite != null) { PTSSecondaryWeaponSprite.gameObject.SetActive(false); }
        if (PTSSecondaryChargeMeterFGSprite != null) { PTSSecondaryChargeMeterFGSprite.gameObject.SetActive(false); }
        if (PTSSecondaryChargeMeterBGSprite != null) { PTSSecondaryChargeMeterBGSprite.gameObject.SetActive(false); }

        PTSHarmNumberList = new UIHarmNumber[0];

        PTSParticleList = new ParticleSystem[max_particle_emitters_in_pool];
        for (int i = 0; i < max_particle_emitters_in_pool; i++)
        {
            PTSParticleList[i] = Instantiate(PTSParticleTemplate).GetComponent<ParticleSystem>();
            PTSParticleList[i].gameObject.SetActive(false);
        }

        ui_show_intro_text = true;
        //if (ui_show_intro_text) { AddToTextQueue("Welcome!"); }
        ui_demo_enabled = true;

    }

    private void OnEnable()
    {
        SetRenderQueueFromParent(transform);
    }

    public void TransferOwner(VRCPlayerApi newOwner)
    {
        owner = newOwner;
        if (gameController != null)
        {
            playerAttributes = gameController.FindPlayerAttributes(newOwner);
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        TransferOwner(newOwner);
    }

    // Overloaded method where no color = Color.white and no timer = default
    public void AddToTextQueue(string input, Color color, float duration)
    {
        //if (text_queue_full_colors == null || text_queue_full_durations == null || text_queue_full_colors.Length == 0 || text_queue_full_durations.Length == 0) { return; }

        string[] queue_arr = text_queue_full_str.Split(text_queue_separator);
        //text_queue_limited_durations
        if (queue_arr.Length > text_queue_full_max_lines)
        {
            // If the queue is clogged, pop the next upcoming message 
            queue_arr[text_queue_limited_lines] = "";
            text_queue_full_str = String.Join(text_queue_separator, queue_arr, 0, queue_arr.Length - 1);
            for (int j = text_queue_limited_lines; j < text_queue_full_max_lines - 1; j++)
            {
                text_queue_full_colors[j] = text_queue_full_colors[j + 1];
                text_queue_full_durations[j] = text_queue_full_durations[j + 1];
            }
            text_queue_full_colors[text_queue_full_max_lines - 1] = color;
            text_queue_full_durations[text_queue_full_max_lines - 1] = duration;
        }
        else if (text_queue_full_str.Length == 0)
        {
            text_queue_full_colors[0] = color;
            text_queue_full_durations[0] = duration;
        }
        else if (text_queue_full_str.Length > 0)
        {
            text_queue_full_colors[queue_arr.Length] = color;
            text_queue_full_durations[queue_arr.Length] = duration;
        }
        //UnityEngine.Debug.Log("[COLOR_TEST] Adding " + color + " to colors at index " + queue_arr.Length);

        if (text_queue_full_str.Length > 0) { text_queue_full_str += text_queue_separator; }
        text_queue_full_str += input;
    }
    public void AddToTextQueue(string input)
    {
        float duration = Mathf.Lerp(text_queue_duration_default, text_queue_duration_default * 2.0f, input.Length / text_queue_duration_max_characters);
        AddToTextQueue(input, Color.white, duration);
    }
    public void AddToTextQueue(string input, Color color)
    {
        float duration = Mathf.Lerp(text_queue_duration_default, text_queue_duration_default * 2.0f, input.Length / text_queue_duration_max_characters);
        AddToTextQueue(input, color, duration);
    }

    public void UpdateGameVariables(bool forceRefresh = false)
    {
        if (gameController.round_state != (int)round_state_name.Start)
        {
            int team_total_lives = 0;
            gameController.CheckRoundGoalProgress(ref gamevars_leaderboard_arr, ref gamevars_progress_arr, out gamevars_leader_name);

            gamevars_local_highest_points = 0; gamevars_local_lowest_deaths = 0; gamevars_local_teams_alive = 0; gamevars_local_players_alive = 0;
            gamevars_local_highest_team = -3; gamevars_local_lowest_team = -3; gamevars_local_highest_ply_id = -1; gamevars_local_lowest_ply_id = -1;
            gamevars_local_team_points = gameController.CheckAllTeamPoints(gameController.cached_ply_in_game_dict, ref gamevars_local_highest_team, ref gamevars_local_highest_points, ref gamevars_local_highest_ply_id);
            gamevars_local_team_deaths = gameController.CheckAllTeamPoints(gameController.cached_ply_in_game_dict, ref gamevars_local_lowest_team, ref gamevars_local_lowest_deaths, ref gamevars_local_lowest_ply_id, true);
            gamevars_local_team_lives = gameController.CheckAllTeamLives(gameController.cached_ply_in_game_dict, ref gamevars_local_players_alive, ref gamevars_local_teams_alive);
            gameController.CheckSingleTeamLives(playerAttributes.ply_team, gameController.cached_ply_in_game_dict, ref gamevars_local_team_members_alive, ref team_total_lives);
            ui_check_gamevars_timer = 0.0f;
            if (gameController.ui_round_scoreboard_canvas != null && gameController.ui_round_scoreboard_canvas.GetComponent<Scoreboard>() != null || forceRefresh)
            {
                // Only refresh the scoreboard if a change has been made.
                if (forceRefresh || !GlobalHelperFunctions.ArraysEqual(gameController.cached_leaderboard_arr, gamevars_leaderboard_arr) || !GlobalHelperFunctions.ArraysEqual(gameController.cached_progress_arr, gamevars_progress_arr))
                {
                    if (gameController.room_ready_should_render) { 
                        gameController.ui_round_scoreboard_canvas.gameObject.GetComponent<Scoreboard>().RefreshScores();
                        gameController.ui_round_scoreboard_canvas.gameObject.GetComponent<Scoreboard>().RearrangeScoreboard(gamevars_leaderboard_arr);
                    }

                    // And if a change has been made, make sure to transfer those changes.
                    gameController.cached_leaderboard_arr = new int[gamevars_leaderboard_arr.Length];
                    gameController.cached_progress_arr = new int[gamevars_progress_arr.Length];
                    Array.Copy(gamevars_leaderboard_arr, gameController.cached_leaderboard_arr, gamevars_leaderboard_arr.Length);
                    Array.Copy(gamevars_progress_arr, gameController.cached_progress_arr, gamevars_progress_arr.Length);
                }
            }
        }

        gamevars_force_refresh_on_next_tick = false;
    }

    
    public int GetGameRank(int player_id, PlayerAttributes inPlyAttr)
    {
        // Player version
        if (player_id < 0 || gamevars_leaderboard_arr == null || gameController == null || inPlyAttr == null || inPlyAttr.ply_team < 0) { return -1; }
        else if (player_id == Networking.LocalPlayer.playerId && Networking.GetOwner(gameObject) != VRCPlayerApi.GetPlayerById(player_id)) { return -1; }
        int rank = -1;

        int total_lives = 0;
        if (gamevars_local_team_lives != null && inPlyAttr.ply_team >= 0 && inPlyAttr.ply_team < gamevars_local_team_lives.Length) { total_lives = gamevars_local_team_lives[inPlyAttr.ply_team]; }
        int total_points = 0;
        if (gamevars_local_team_points != null && inPlyAttr.ply_team >= 0 && inPlyAttr.ply_team < gamevars_local_team_points.Length) { total_points = gamevars_local_team_points[inPlyAttr.ply_team]; }
        int total_deaths = 0;
        if (gamevars_local_team_deaths != null && inPlyAttr.ply_team >= 0 && inPlyAttr.ply_team < gamevars_local_team_deaths.Length) { total_deaths = gamevars_local_team_deaths[inPlyAttr.ply_team]; }

        // We perform the below to get a dense rank (e.g. if points are {3, 3, 2, 1, 1, 0}, ranks should be {1, 1, 3, 4, 4, 6} instead of {1, 2, 3, 4, 5, 6})
        for (int i = 0; i < gamevars_progress_arr.Length; i++)
        {
            if (gameController.option_gamemode == (int)gamemode_name.Survival)
            {
                // If we are in Survival, check if your lives matches the iterating team/player's
                if (gameController.option_teamplay && total_lives == gamevars_progress_arr[i]) { rank = i; break; }
                else if (!gameController.option_teamplay && inPlyAttr.ply_lives == gamevars_progress_arr[i]) { rank = i; break; }
            }
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                // If we are in Fitting In, check if your deaths matches the iterating team/player's
                if (gameController.option_teamplay && total_deaths == gamevars_progress_arr[i]) { rank = i; break; }
                else if (!gameController.option_teamplay && inPlyAttr.ply_deaths == gamevars_progress_arr[i]) { rank = i; break; }
            }
            else
            {
                // Otherwise, check if your points matches the iterating team/player's
                if (gameController.option_teamplay && total_points == gamevars_progress_arr[i]) { rank = i; break; }
                else if (!gameController.option_teamplay && inPlyAttr.ply_points == gamevars_progress_arr[i]) { rank = i; break; }
            }

            // And if we still can't find anything, just find your individual rank in the array
            if (gamevars_leaderboard_arr == null) { rank = 0; break; }
            else if (i >= gamevars_leaderboard_arr.Length) { rank = gamevars_leaderboard_arr.Length; break; }
            else if (gameController.option_teamplay && gamevars_leaderboard_arr[i] == inPlyAttr.ply_team) { rank = i; break; }
            else if (!gameController.option_teamplay && gamevars_leaderboard_arr[i] == player_id) { rank = i; break; } //
        }
        return rank;
    }

    public int GetGameRank(int player_id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return -1; }
        return GetGameRank(player_id, gameController.FindPlayerAttributes(player));
    }

    public string RankToString(int inRank, TMP_Text PlacementText)
    {
        int rank = inRank + 1;
        if (rank < 1)
        {
            if (PlacementText != null) { PlacementText.color = Color.white; }
            return "?";
        }
        else if (rank == 1)
        {
            if (PlacementText != null) { PlacementText.color = new Color32(255, 225, 0, 255); }
            return gameController.localizer.FetchText("SELF_UI_RANK_1", "1st");
        }
        else if (rank == 2)
        {
            if (PlacementText != null) { PlacementText.color = new Color32(240, 248, 255, 255); }
            return gameController.localizer.FetchText("SELF_UI_RANK_2", "2nd");
        }
        else if (rank == 3) 
        {
            if (PlacementText != null) { PlacementText.color = new Color32(255, 166, 78, 255); }
            return gameController.localizer.FetchText("SELF_UI_RANK_3", "3rd"); 
        }
        else
        {
            if (PlacementText != null) { PlacementText.color = new Color32(123, 223, 185, 255); }
            return gameController.localizer.FetchText("SELF_UI_RANK_4", "$ARG0th", rank.ToString()); 
        }
    }

    public void ProcessTextQueue(float tickDeltaTime)
    {
        if (text_queue_limited_timers == null) { return; }
        // First, check the string to see if we even have 4 lines
        string[] splitStr = text_queue_full_str.Split(text_queue_separator);
        int new_queue_size = splitStr.Length;
        float[] new_queue_timers = text_queue_limited_timers;
        int iterateAmount = Mathf.Min(splitStr.Length, text_queue_limited_lines);
        int iteration = 0;
        while (iteration < iterateAmount)
        {
            if (text_queue_full_str.Length == 0) { break; }
            if (new_queue_timers[iteration] < text_queue_full_durations[iteration])
            {
                new_queue_timers[iteration] += tickDeltaTime;
            }
            else
            {
                // Shift timer entries up, and manage bonus time from queue position (+text_queue_limited_extended for each entry after 0)
                for (int j = iteration; j < iterateAmount - 1; j++)
                {
                    new_queue_timers[j] = new_queue_timers[j + 1];
                    if (new_queue_timers[j] < (text_queue_full_durations[iteration] - text_queue_limited_extend)) { new_queue_timers[j] += text_queue_limited_extend; }
                }
                new_queue_timers[iterateAmount - 1] = 0.0f - ((iterateAmount - 1) * text_queue_limited_extend); // We want to add a little bonus time for those later in the queue
                // Shift string entries up
                for (int k = iteration; k < splitStr.Length - 1; k++)
                {
                    splitStr[k] = splitStr[k + 1];
                    //UnityEngine.Debug.Log("[COLOR_TEST] Shifting " + text_queue_full_colors[k] + " at index " + k + " to " + text_queue_full_colors[k + 1]);
                    text_queue_full_colors[k] = text_queue_full_colors[k + 1];
                    text_queue_full_durations[k] = text_queue_full_durations[k + 1];
                }
                //UnityEngine.Debug.Log("[COLOR_TEST] Resetting " + text_queue_full_colors[splitStr.Length - 1] + " at index " + (splitStr.Length - 1));
                text_queue_full_colors[splitStr.Length - 1] = Color.white;
                text_queue_full_durations[splitStr.Length - 1] = text_queue_duration_default;
                splitStr[splitStr.Length - 1] = "";
                new_queue_size--;
                iteration--; // Now that entries are shifted up, we need to check again
                iterateAmount--;
            }
            iteration++;
        }
        text_queue_full_str = String.Join(text_queue_separator, splitStr, 0, new_queue_size);
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        if (owner == null && Networking.IsOwner(gameObject))
        {
            TransferOwner(Networking.LocalPlayer);
        }

        if (owner != Networking.LocalPlayer) { return; }

        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
            else { return; }
        }

        ProcessTextQueue(tickDeltaTime);

        string[] splitStr = text_queue_full_str.Split(text_queue_separator);
        for (int i = 0; i < text_queue_limited_lines; i++)
        {
            if (i < text_queue_full_colors.Length) { PTSTextStack_Label[i].color = text_queue_full_colors[i]; PTSTextStack_Label[i].gameObject.SetActive(true); } // Needs to happen first, because alpha is modified after
            if (i < splitStr.Length && splitStr[i] != "")
            {
                PTSTextStack_Label[i].gameObject.SetActive(true);
                PTSTextStack_Label[i].text = splitStr[i].ToUpper();
                float duration_modified = text_queue_full_durations[i];
                float fade_time = duration_modified - (text_queue_limited_fade_time_percent * duration_modified);
                if (text_queue_limited_timers[i] >= fade_time) {  PTSTextStack_Label[i].alpha = 1 - ((text_queue_limited_timers[i] - fade_time) / (duration_modified - fade_time)); }
                else {  PTSTextStack_Label[i].alpha = 1.0f; }
            }
            else if (ui_demo_enabled && !ui_show_intro_text) { PTSTextStack_Label[i].text = gameController.localizer.FetchText("SAMPLE_TEXT", "Sample Text"); PTSTextStack_Label[i].gameObject.SetActive(true); PTSTextStack_Label[i].alpha = 1.0f; }
            else { PTSTextStack_Label[i].text = ""; PTSTextStack_Label[i].gameObject.SetActive(false); }
        }

        // Tick down demo timer
        if (ui_demo_enabled && ui_demo_timer < ui_demo_duration)
        {
            //if (ui_demo_timer == 0.0f) { AddToTextQueue("Example message", Color.cyan); }
            ui_demo_timer += tickDeltaTime;
        }
        else if (ui_demo_enabled && ui_demo_timer >= ui_demo_duration)
        {
            ui_demo_enabled = false;
            if (ui_show_intro_text) {
                //AddToTextQueue("This game is in development; there may be major bugs or issues!", Color.red);
                //AddToTextQueue(" -- ALPHA BUILD VERSION 0.18.4 --", Color.white);
                //AddToTextQueue("Step in the square to join the game!", Color.white);
                //if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.RefreshAllOptions(); }
                if (gameController != null) 
                { 
                    gameController.ForceResetHitboxes(); 
                    if (Networking.IsOwner(gameController.gameObject)) { gameController.ResetGameOptionsToDefault(false); }
                }
            }
            ui_show_intro_text = false;
            ui_demo_timer = 0.0f;
        }

        // Dynamically adjust the gamevars update impulse
        if (gameController != null && gameController.ply_tracking_dict_keys_arr != null && gameController.ply_tracking_dict_keys_arr.Length > 0)
        {
            ui_check_gamevars_impulse = Mathf.Max(ui_check_gamevars_impulse_default, 0.08f * gameController.ply_tracking_dict_keys_arr.Length);

            // Change the debugger's value as well
            if (gameController != null && gameController.local_ppp_options != null && gameController.local_ppp_options.debuggerPanel != null
                && gameController.local_ppp_options.debuggerPanel.cached_gamevars_impulse != ui_check_gamevars_impulse
                ) 
            { 
                gameController.local_ppp_options.debuggerPanel.ui_input_gamevarsimpulse.text = ui_check_gamevars_impulse.ToString(); 
            }
        }

        // Tick down gamevars update timer
        if (ui_check_gamevars_timer < ui_check_gamevars_impulse)
        {
            ui_check_gamevars_timer += tickDeltaTime;
        }
        else
        {
            UpdateGameVariables(gamevars_force_refresh_on_next_tick);
            UI_Flag();
            UI_Lives();
            UI_Miniscore();
            //gameController.RefreshSetupUI(); // Don't actually do this; only good for debugging but is redundant and lag spiking otherwise
        }

        // Sort out better without all the debug
        if (playerAttributes == null)
        {
            if (gameController != null && owner != null) { playerAttributes = gameController.FindPlayerAttributes(owner); }
            else if (owner == null) { TransferOwner(Networking.GetOwner(gameObject)); }
            return;
        }
        else 
        {
            if (cached_scale != playerAttributes.ply_scale) { UI_Attack(); UI_Defense(); cached_scale = playerAttributes.ply_scale; }
            if (cached_atk != playerAttributes.ply_atk) { UI_Attack(); cached_atk = playerAttributes.ply_atk; }
            if (cached_def != playerAttributes.ply_def) { UI_Defense(); cached_def = playerAttributes.ply_def; }
        }

        bool round_ready = gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Queued || gameController.round_state == (int)round_state_name.Loading || gameController.round_state == (int)round_state_name.Over;
        if (ui_demo_enabled && !ui_show_intro_text) { PTSTopPanel.SetActive(true); }
        else if (playerAttributes.ply_training) { PTSTopPanel.SetActive(true); }
        else if ((round_ready || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator || playerAttributes.ply_team < 0) && PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(false); }
        else if (!(round_ready || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator || playerAttributes.ply_team < 0) && !PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(true); }
    
        // Tick down recent damage timer
        if (damage_receive_timer < damage_receive_duration)
        {
            damage_receive_timer += tickDeltaTime;
            UI_RecentDamage();
        }
        else if (damage_receive_temp != 0.0f)
        {
            damage_receive_temp = 0.0f;
            UI_RecentDamage();
        }
    
    }
    
    public void ForceUpdateAllUI()
    {
        UI_Timer();
        UI_Attack();
        UI_Defense();
        UI_Damage();
        UI_Flag();
        UI_Lives();
        UI_Powerups();
        UI_Weapons();
        UI_Capturezones();
        UI_Miniscore();
    }
    
    public void ResetCache()
    {
        cached_scale = -1.0f;
        cached_atk = -1.0f;
        cached_def = -1.0f;
    }

    public void UI_Timer()
    {
        float TimerValue = gameController.round_length - gameController.round_timer + 1.0f;
        string TimerText = Mathf.FloorToInt(TimerValue).ToString();
        PTSTimerTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        if (gameController.round_state == (int)round_state_name.Start || (gameController.round_state == (int)round_state_name.Ongoing && !gameController.round_length_enabled)) { TimerText = "--"; }
        else if (gameController.round_state == (int)round_state_name.Ready) { TimerText = Mathf.Floor(gameController.ready_length - gameController.round_timer + 1.0f).ToString(); }
        else
        {
            if (TimerValue < 10.0f)
            {
                PTSTimer.color = new Color(1.0f, 0.4f, 0.4f, 1.0f);
                float scaleAdd = ((TimerValue * 10.0f) % 5.0f) / 5.0f;
                PTSTimerTransform.localScale = new Vector3(1.0f + scaleAdd, 1.0f + scaleAdd, 1.0f + scaleAdd);
            }
            else if (TimerValue < 30.0) { PTSTimer.color = new Color(1.0f, 0.6f, 0.4f, 1.0f); }
            else { PTSTimer.color = Color.white; }
        }
        PTSTimer.text = TimerText;
    }
    
    public void UI_Damage()
    {
        var DamageText = Mathf.RoundToInt(playerAttributes.ply_dp) + "%";
        if (gameController.round_state == (int)round_state_name.Start && !playerAttributes.ply_training) { DamageText = ""; }
        PTSDamage.text = DamageText;
        PTSDamage.color = new Color(Mathf.Min(Mathf.Max(0.2f, 1.0f - ((playerAttributes.ply_dp - 100) / 100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp / 100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp / 100)), 1.0f), 1.0f);

        var InvulText = Mathf.Floor(playerAttributes.ply_respawn_duration - playerAttributes.ply_respawn_timer + 1.0f).ToString();
        if (gameController.round_state == (int)round_state_name.Start || playerAttributes.ply_state != (int)player_state_name.Respawning)
        {
            InvulText = "";
            PTSInvul.gameObject.transform.parent.gameObject.SetActive(false);
            PTSDamage.gameObject.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            PTSInvul.gameObject.transform.parent.gameObject.SetActive(true);
            PTSDamage.gameObject.transform.parent.gameObject.SetActive(false);
        }
        PTSInvul.text = InvulText;
        UI_RecentDamage();
    }

    public void UI_RecentDamage()
    {
        string ReceiveText = Mathf.RoundToInt(damage_receive_temp).ToString() + '%';
        if (damage_receive_temp > 0) 
        { 
            ReceiveText = '+' + ReceiveText;
            if (gameController != null && gameController.team_colors_bright != null && gameController.team_colors_bright.Length > 1) { PTSRecentDamage.color = gameController.team_colors_bright[1]; }
            else { PTSRecentDamage.color = Color.red; }
        }
        else
        {
            if (gameController != null && gameController.team_colors_bright != null && gameController.team_colors_bright.Length > 3) { PTSRecentDamage.color = gameController.team_colors_bright[3]; }
            else { PTSRecentDamage.color = Color.green; }
        }
        PTSRecentDamage.text = ReceiveText;
        if (damage_receive_timer < damage_receive_duration * 0.5f) { PTSRecentDamage.alpha = 1.0f; }
        else { PTSRecentDamage.alpha = Mathf.Lerp(1.0f, 0.0f, (damage_receive_timer - (damage_receive_duration * 0.5f)) / (damage_receive_duration * 0.5f)); }
    }

    public void FlashRecentDamage(float addDamage)
    {
        damage_receive_timer = 0.0f;
        // If we already have temp damage, but the sign changed, reset it completely
        if (damage_receive_temp != 0 && addDamage / damage_receive_temp < -1) { damage_receive_temp = addDamage; }
        // Otherwise, just add to the currently growing amount
        else { damage_receive_temp += addDamage; }
    }
    
    public void UI_Attack()
    {
        var AttackVal = Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var AttackText = AttackVal + "x";
        if (gameController.round_state == (int)round_state_name.Start && !playerAttributes.ply_training) { AttackText = ""; }
        if (AttackVal > gameController.plysettings_atk) { PTSAttack.color = new Color32(60, 255, 60, 255); }
        else if (AttackVal < gameController.plysettings_atk) { PTSAttack.color = new Color32(255, 60, 60, 255); }
        else { PTSAttack.color = new Color32(255, 255, 255, 255); }
        PTSAttack.text = AttackText;
    }
    
    public void UI_Defense()
    {
        var DefenseVal = Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var DefenseText = DefenseVal + "x";
        if (gameController.round_state == (int)round_state_name.Start && !playerAttributes.ply_training) { DefenseText = ""; }
        if (DefenseVal > gameController.plysettings_def) { PTSDefense.color = new Color32(60, 255, 60, 255); }
        else if (DefenseVal < gameController.plysettings_def) { PTSDefense.color = new Color32(255, 60, 60, 255); }
        else { PTSDefense.color = new Color32(255, 255, 255, 255); }
        PTSDefense.text = DefenseText;
    }
    
    public void UI_Flag()
    {
        if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { PTSTeamCBSpriteImage.enabled = true; }
        else { PTSTeamCBSpriteImage.enabled = false; }
        PTSTeamFlagImage.sprite = PTSFlagSprite;
        PTSTeamFlagImage.enabled = !PTSTeamCBSpriteImage.enabled;
        PTSTeamPoleImage.enabled = PTSTeamFlagImage.enabled;

        if (gameController.option_teamplay && playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gameController.team_colors.Length)
        {
            PTSTeamFlagImage.color = gameController.team_colors[playerAttributes.ply_team];
            PTSTeamCBSpriteImage.sprite = gameController.team_sprites[playerAttributes.ply_team];
        }
        else
        {
            PTSTeamFlagImage.color = new Color32(255, 255, 255, 255);
            PTSTeamCBSpriteImage.sprite = gameController.team_sprites[0];
        }
        PTSTeamCBSpriteImage.color = PTSTeamFlagImage.color;

        string FlagText = ""; string PlacementText = "";
        PTSTeamText.color = Color.white;
        if (gameController.round_state != (int)round_state_name.Start && gameController.team_count >= 0 && playerAttributes.ply_team >= 0 && gamevars_leaderboard_arr != null
            && gamevars_local_team_lives != null && gamevars_local_team_points != null && gamevars_local_team_deaths != null
            && gamevars_local_team_lives.Length > playerAttributes.ply_team && gamevars_local_team_points.Length > playerAttributes.ply_team && gamevars_local_team_deaths.Length > playerAttributes.ply_team)
        {

            int members_alive = 0; int team_total_lives = 0;
            if (playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gamevars_local_team_lives.Length) { members_alive = gamevars_local_team_members_alive; }
            int total_points = 0;
            if (playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gamevars_local_team_points.Length) { total_points = gamevars_local_team_points[playerAttributes.ply_team]; }
            int local_rank = GetGameRank(Networking.LocalPlayer.playerId, playerAttributes);
            string rank_str = RankToString(local_rank, PTSPlacementText);
            // If we are in Survival, display # of players alive on your team
            if (gameController.option_gamemode == (int)gamemode_name.Survival)
            {
                PlacementText = rank_str;
                FlagText = "\n\n" + gameController.localizer.FetchText("SELF_UI_TEAM_LIVESMODE_ALIVE", "$ARG0 Alive", members_alive.ToString());
            }
            // If this is Clash, display the leader's point count
            else if (gameController.option_gamemode == (int)gamemode_name.Clash)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n" + gameController.localizer.FetchText("SELF_UI_TEAM_CLASH_LEADER", "1st:\n$ARG0 KO(s)", gamevars_progress_arr[0].ToString());
                }
            }
            // If we are in Boss Bash, display your team's KOs
            else if (gameController.option_gamemode == (int)gamemode_name.BossBash)
            {
                //FlagText = "\n\n";
                // If we are the boss, we personal KOs as progress
                bool is_boss = playerAttributes != null && playerAttributes.ply_team == 1;
                if (is_boss) { FlagText += gameController.localizer.FetchText("SELF_UI_TEAM_BOSSBASH_PROGRESS_BOSS", "$ARG0 / $ARG1 KO(s)", total_points.ToString(), gameController.option_gm_goal.ToString()); }
                // And if we aren't, we want team KOs as score + our personal damage dealt
                else if (!is_boss && playerAttributes != null) { FlagText += gameController.localizer.FetchText("SELF_UI_TEAM_BOSSBASH_PROGRESS_TINY", "$ARG0 KO(s)\n($ARG1% Dealt)", total_points.ToString(), Mathf.Round(playerAttributes.ply_damage_dealt).ToString()); }
                PTSTeamPoleImage.enabled = false;
                PTSTeamFlagImage.sprite = PTSPointsSprite;
                PTSTeamCBSpriteImage.sprite = PTSPointsSprite;
            }
            // If we are in Infection, display the Survivor's players alive count
            else if (gameController.option_gamemode == (int)gamemode_name.Infection)
            {
                FlagText = gameController.localizer.FetchText("SELF_UI_TEAM_LIVESMODE_ALIVE", "$ARG0 Alive", Mathf.RoundToInt(gamevars_local_team_lives[0] / 2).ToString());
            }
            // If this is King of the Hill, display the total capture time remaining
            else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n" + gameController.localizer.FetchText("SELF_UI_TEAM_KOTH_LEADER", "1st:\n$ARG0", Mathf.RoundToInt(gameController.option_gm_goal - gamevars_progress_arr[0]).ToString());
                }
            }
            // If we are in Fitting In, display the leader's death count
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n" + gameController.localizer.FetchText("SELF_UI_TEAM_FITTINGIN_LEADER", "1st:\n$ARG0 Falls", (gamevars_progress_arr[0]).ToString());
                }
            }

            // If we are in points mode, display the team with the highest points, and highlight the text color based on who it is
            /*FlagText = Mathf.Max(gamevars_local_team_points).ToString();
            PTSTeamText.color = new Color32(
                (byte)Mathf.Min(255, (80 + gameController.team_colors[gamevars_local_highest_team].r)),
                (byte)Mathf.Min(255, (80 + gameController.team_colors[gamevars_local_highest_team].g)),
                (byte)Mathf.Min(255, (80 + gameController.team_colors[gamevars_local_highest_team].b)),
                (byte)gameController.team_colors[gamevars_local_highest_team].a);*/
        }
        PTSTeamText.text = FlagText;
        PTSPlacementText.text = PlacementText;
    }
    
    public void UI_Lives() 
    {
        var LivesText = "";
        if (gamevars_leaderboard_arr != null)
        {
            if (gameController.round_state == (int)round_state_name.Start) { LivesText = ""; }
            else if (gameController.option_gamemode == (int)gamemode_name.Survival || (playerAttributes.ply_team == 1 && gameController.option_gamemode == (int)gamemode_name.BossBash))
            {
                // If we are in survival mode or are the boss, display lives
                LivesText = Mathf.RoundToInt(playerAttributes.ply_lives).ToString();
                PTSLivesImage.sprite = PTSLivesSprite;
                float livesRatio = (float)((float)playerAttributes.ply_lives / (float)gameController.plysettings_lives);
                if (livesRatio < 1.0f) { livesRatio -= 0.5f * (float)(1.0f / (float)gameController.plysettings_lives); }
                livesRatio = Mathf.Min(Mathf.Max(0.0f, livesRatio), 1.0f);
                PTSLivesImage.color = Color.white;
                PTSLives.color = new Color(1.0f, livesRatio, livesRatio, 1.0f);
            }
            else if (gameController.option_gamemode == (int)gamemode_name.BossBash && gameController.gamemode_boss_id >= 0)
            {
                // If this is boss bash, display the boss's points as a total death counter
                var bossAttr = gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(gameController.gamemode_boss_id));
                if (bossAttr != null)
                {
                    LivesText = Mathf.RoundToInt(bossAttr.ply_points).ToString() + "/" + gameController.option_gm_goal;
                }
                else { LivesText = "?"; }
                PTSLives.color = Color.white;
                PTSLivesImage.color = Color.white;
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            else if (gameController.option_gamemode == (int)gamemode_name.Infection)
            {
                // If this is infection, display the total infections
                if (gamevars_local_team_points != null && gamevars_local_team_points.Length > 1) { LivesText = gameController.round_extra_data.ToString(); }
                PTSLives.color = Color.white;
                PTSLivesImage.color = Color.white;
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            // If this is King of the Hill, display the total capture time remaining
            else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                bool koth_is_valid = true;
                if (gameController.mapscript_list == null || gameController.map_selected < 0 || gameController.map_selected > gameController.mapscript_list.Length
                    || gameController.mapscript_list[gameController.map_selected].map_capturezones == null || gameController.mapscript_list[gameController.map_selected].map_capturezones.Length <= 0
                    || gameController.mapscript_list[gameController.map_selected].map_capturezones[0].dict_points_keys_arr == null
                    ) { koth_is_valid = false; }
                if (koth_is_valid)
                {
                    CaptureZone capturezone = gameController.mapscript_list[gameController.map_selected].map_capturezones[0];
                    float timeLeft = gameController.option_gm_goal; int koth_index = 0;
                    int margin_time = Networking.IsOwner(capturezone.gameObject) ? 0 : 1;
                    if (gameController.option_teamplay && playerAttributes.ply_team >= 0 && capturezone.dict_points_values_arr != null)
                    {
                        koth_index = GlobalHelperFunctions.DictIndexFromKey(playerAttributes.ply_team, capturezone.dict_points_keys_arr);
                        if (koth_index < capturezone.dict_points_values_arr.Length && koth_index >= 0) { timeLeft -= capturezone.dict_points_values_arr[koth_index] + margin_time; }
                    }
                    else if (!gameController.option_teamplay && playerAttributes.ply_team >= 0 && capturezone.dict_points_values_arr != null)
                    {
                        koth_index = GlobalHelperFunctions.DictIndexFromKey(Networking.LocalPlayer.playerId, capturezone.dict_points_keys_arr);
                        if (koth_index < capturezone.dict_points_values_arr.Length && koth_index >= 0) { timeLeft -= capturezone.dict_points_values_arr[koth_index] + margin_time; }
                    }
                    LivesText = timeLeft.ToString();
                    PTSLives.color = new Color(
                        Mathf.Lerp(((Color)gameController.team_colors_bright[0]).r, ((Color)gameController.team_colors_bright[1]).r, 1.0f - (timeLeft / gameController.option_gm_goal))
                        , Mathf.Lerp(((Color)gameController.team_colors_bright[0]).g, ((Color)gameController.team_colors_bright[1]).g, 1.0f - (timeLeft / gameController.option_gm_goal))
                        , Mathf.Lerp(((Color)gameController.team_colors_bright[0]).b, ((Color)gameController.team_colors_bright[1]).b, 1.0f - (timeLeft / gameController.option_gm_goal))
                        , Mathf.Lerp(((Color)gameController.team_colors_bright[0]).a, ((Color)gameController.team_colors_bright[1]).a, 1.0f - (timeLeft / gameController.option_gm_goal))
                        );
                    PTSLivesImage.color = PTSTeamFlagImage.color;
                    PTSLivesImage.sprite = PTSTimerImage;
                }
            }
            // If this is Fitting In, display the number of deaths
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                if (gameController.option_teamplay && gamevars_local_team_deaths != null && gamevars_local_team_deaths.Length > playerAttributes.ply_team && playerAttributes.ply_team >= 0) 
                {
                    LivesText = gameController.localizer.FetchText("SELF_UI_TEAM_FITTINGIN_ALIVE", "$ARG0\n$ARG1 Alive", Mathf.RoundToInt(playerAttributes.ply_deaths).ToString(), Mathf.RoundToInt(gamevars_local_team_members_alive).ToString()); 
                }
                else { LivesText = Mathf.RoundToInt(playerAttributes.ply_deaths).ToString(); }
                PTSLives.color = Color.white;
                PTSLivesImage.color = Color.white;
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            else
            {
                // Otherwise, display points
                if (gameController.option_teamplay && gamevars_local_team_points != null && gamevars_local_team_points.Length > playerAttributes.ply_team && playerAttributes.ply_team >= 0) { LivesText = Mathf.RoundToInt(gamevars_local_team_points[playerAttributes.ply_team]).ToString() + "/" + gameController.option_gm_goal; }
                else { LivesText = Mathf.RoundToInt(playerAttributes.ply_points).ToString() + "/" + gameController.option_gm_goal; }
                PTSLives.color = Color.white;
                PTSLivesImage.color = PTSTeamFlagImage.color;
                PTSLivesImage.sprite = PTSPointsSprite;
            }
        }
        PTSLives.text = LivesText;
    }
    
    public void UI_Powerups()
    {
        // Handle powerup sprites
        if (playerAttributes.powerups_active != null)
        {
            var powerup_len = (int)Mathf.Min(PTSPowerupSprites.Length, playerAttributes.powerups_active.Length);
            for (int i = 0; i < PTSPowerupSprites.Length; i++)
            {
                TMP_Text PTSPowerupSpriteText = PTSPowerupSprites[i].GetComponentInChildren<TMP_Text>();
                PTSPowerupSprites[i].transform.gameObject.SetActive(false);
                PTSPowerupSpriteText.text = "∞";

                if (i < powerup_len)
                {
                    if (playerAttributes.powerups_active[i] == null) { continue; }
                    var powerup = playerAttributes.powerups_active[i].GetComponent<ItemPowerup>();
                    if (powerup == null) { continue; }
                    PTSPowerupSprites[i].transform.gameObject.SetActive(true);
                    PTSPowerupSprites[i].sprite = powerup.powerup_sprites[powerup.powerup_type];

                    float powerup_time_left = (float)(powerup.powerup_duration - powerup.powerup_timer_network);
                    if (powerup_time_left < 10.0f)
                    {
                        PTSPowerupSpriteText.text =
                            (Mathf.Floor(powerup_time_left * 10.0f) / 10.0f).ToString().PadRight(2, '.').PadRight(3, '0');
                    }
                    else
                    {
                        PTSPowerupSpriteText.text = Mathf.Floor(powerup_time_left).ToString();
                    }
                    if (powerup_time_left <= 5.0f) { PTSPowerupSpriteText.color = new Color(1.0f, 0.4f, 0.4f, 1.0f); }
                    else { PTSPowerupSpriteText.color = Color.white; }
                    //
                }
                else if (ui_demo_enabled && !ui_show_intro_text)
                {
                    PTSPowerupSprites[i].transform.gameObject.SetActive(true);
                }
            }
        }
    }
    
    public void UI_Weapons()
    {
        // Handle weapon stats
        if (gameController != null && gameController.local_plyweapon != null && PTSWeaponSprite != null && PTSWeaponText != null && gameController.local_plyweapon.weapon_type != gameController.local_plyweapon.weapon_type_default)
        {
            string weaponTxt = "";
            PTSWeaponText.color = Color.white;
            if (gameController.local_plyweapon.weapon_temp_ammo > -1)
            {
                weaponTxt += gameController.local_plyweapon.weapon_temp_ammo.ToString();
                if (gameController.local_plyweapon.weapon_temp_ammo < 3) { PTSWeaponText.color = new Color(1.0f, 0.8f, 0.4f, 1.0f); }
            }
            if (gameController.local_plyweapon.weapon_temp_duration > -1)
            {
                if (weaponTxt.Length > 0) { weaponTxt += " (^)"; }
                else { weaponTxt = "^"; }
                float weapon_time_left = gameController.local_plyweapon.weapon_temp_duration - gameController.local_plyweapon.weapon_temp_timer;
                if (weapon_time_left < 10.0f)
                {
                    weaponTxt = weaponTxt.Replace("^", (Mathf.Floor(weapon_time_left * 10.0f) / 10.0f).ToString().PadRight(2, '.').PadRight(3, '0'));
                }
                else
                {
                    weaponTxt = weaponTxt.Replace("^", Mathf.Floor(weapon_time_left).ToString());
                }
                if (weapon_time_left <= 5.0f) { PTSWeaponText.color = new Color(1.0f, 0.4f, 0.4f, 1.0f); }
            }

            if (weaponTxt == "") { weaponTxt = "∞"; } // Only show infinity symbol if there is no other text
            PTSWeaponText.text = weaponTxt;
            if (!PTSWeaponText.gameObject.activeInHierarchy) { PTSWeaponText.gameObject.SetActive(true); }
            if (!PTSWeaponSprite.gameObject.activeInHierarchy) { PTSWeaponSprite.gameObject.SetActive(true); }
        }
        else
        {
            if (PTSWeaponText != null && PTSWeaponText.gameObject.activeInHierarchy) { PTSWeaponText.text = "";  PTSWeaponText.gameObject.SetActive(false); }
            if (PTSWeaponSprite != null && PTSWeaponSprite.gameObject.activeInHierarchy) { PTSWeaponSprite.gameObject.SetActive(false); }
        }

        UI_Secondary();
        UI_Charge();
        UI_Air_Thrust();
    }

    public void UI_Secondary()
    {
        // Handle weapon stats
        if (gameController != null && gameController.local_secondaryweapon != null && PTSSecondaryWeaponSprite != null && PTSSecondaryWeaponText != null && gameController.local_secondaryweapon.weapon_type != gameController.local_secondaryweapon.weapon_type_default && gameController.local_secondaryweapon.gameObject.activeInHierarchy)
        {
            string weaponTxt = "";
            PTSSecondaryWeaponText.color = Color.white;
            if (gameController.local_secondaryweapon.weapon_temp_ammo > -1)
            {
                weaponTxt += gameController.local_secondaryweapon.weapon_temp_ammo.ToString();
                if (gameController.local_secondaryweapon.weapon_temp_ammo < 3) { PTSSecondaryWeaponText.color = new Color(1.0f, 0.8f, 0.4f, 1.0f); }
            }
            if (gameController.local_secondaryweapon.weapon_temp_duration > -1)
            {
                if (weaponTxt.Length > 0) { weaponTxt += " (^)"; }
                else { weaponTxt = "^"; }
                float weapon_time_left = gameController.local_secondaryweapon.weapon_temp_duration - gameController.local_secondaryweapon.weapon_temp_timer;
                if (weapon_time_left < 10.0f)
                {
                    weaponTxt = weaponTxt.Replace("^", (Mathf.Floor(weapon_time_left * 10.0f) / 10.0f).ToString().PadRight(2, '.').PadRight(3, '0'));
                }
                else
                {
                    weaponTxt = weaponTxt.Replace("^", Mathf.Floor(weapon_time_left).ToString());
                }
                if (weapon_time_left <= 5.0f) { PTSSecondaryWeaponText.color = new Color(1.0f, 0.4f, 0.4f, 1.0f); }
            }

            if (weaponTxt == "") { weaponTxt = "∞"; } // Only show infinity symbol if there is no other text
            PTSSecondaryWeaponText.text = weaponTxt;
            if (!PTSSecondaryWeaponText.gameObject.activeInHierarchy) { PTSSecondaryWeaponText.gameObject.SetActive(true); }
            if (!PTSSecondaryWeaponSprite.gameObject.activeInHierarchy) { PTSSecondaryWeaponSprite.gameObject.SetActive(true); }
        }
        else
        {
            if (PTSSecondaryWeaponText != null && PTSSecondaryWeaponText.gameObject.activeInHierarchy) { PTSSecondaryWeaponText.text = ""; PTSSecondaryWeaponText.gameObject.SetActive(false); }
            if (PTSSecondaryWeaponSprite != null && PTSSecondaryWeaponSprite.gameObject.activeInHierarchy) { PTSSecondaryWeaponSprite.gameObject.SetActive(false); }
        }
        UI_Charge_Secondary();
    }

    public void UI_Charge()
    {
        if (PTSChargeMeterFGSprite != null && PTSChargeMeterBGSprite != null && gameController.local_plyweapon != null && (gameController.local_plyweapon.gameObject.activeInHierarchy || (ui_demo_enabled && !ui_show_intro_text)))
        {
            PTSChargeMeterFGSprite.gameObject.SetActive(gameController.local_plyweapon.weapon_is_charging || (gameController.local_plyweapon.use_timer < gameController.local_plyweapon.use_cooldown) || ui_demo_enabled);
            PTSChargeMeterBGSprite.gameObject.SetActive(gameController.local_plyweapon.weapon_is_charging || (gameController.local_plyweapon.use_timer < gameController.local_plyweapon.use_cooldown) || ui_demo_enabled);
            float offsetMax = PTSChargeMeterBGSprite.rectTransform.rect.width;
            float offsetPct = 0.0f;
            if (gameController.local_plyweapon.weapon_is_charging && gameController.local_plyweapon.weapon_charge_duration > 0.0f) 
            { 
                offsetPct = System.Convert.ToSingle(gameController.local_plyweapon.weapon_charge_timer / gameController.local_plyweapon.weapon_charge_duration);
                if (PTSChargeMeterFGSprite.color != gameController.COLOR_CHARGE) { PTSChargeMeterFGSprite.color = gameController.COLOR_CHARGE; }
            }
            else if (gameController.local_plyweapon.use_timer < gameController.local_plyweapon.use_cooldown && gameController.local_plyweapon.use_cooldown > 0.0f) 
            { 
                offsetPct = 1.0f - System.Convert.ToSingle(gameController.local_plyweapon.use_timer / gameController.local_plyweapon.use_cooldown);
                if (PTSChargeMeterFGSprite.color != gameController.COLOR_COOLDOWN) { PTSChargeMeterFGSprite.color = gameController.COLOR_COOLDOWN; }
            }
            PTSChargeMeterFGSprite.rectTransform.offsetMax = new Vector2(-offsetMax + (offsetMax * offsetPct), PTSChargeMeterFGSprite.rectTransform.offsetMax.y);
        }
        else
        {
            PTSChargeMeterFGSprite.gameObject.SetActive(false);
            PTSChargeMeterBGSprite.gameObject.SetActive(false);
        }
    }

    public void UI_Charge_Secondary()
    {
        if (PTSSecondaryChargeMeterFGSprite != null && PTSSecondaryChargeMeterBGSprite != null && gameController.local_secondaryweapon != null && gameController.local_secondaryweapon.gameObject.activeInHierarchy)
        {
            PTSSecondaryChargeMeterFGSprite.gameObject.SetActive(gameController.local_secondaryweapon.weapon_is_charging || (gameController.local_secondaryweapon.use_timer < gameController.local_secondaryweapon.use_cooldown));
            PTSSecondaryChargeMeterBGSprite.gameObject.SetActive(gameController.local_secondaryweapon.weapon_is_charging || (gameController.local_secondaryweapon.use_timer < gameController.local_secondaryweapon.use_cooldown));
            float offsetMax = PTSSecondaryChargeMeterBGSprite.rectTransform.rect.width;
            float offsetPct = 0.0f;
            if (gameController.local_secondaryweapon.weapon_is_charging && gameController.local_secondaryweapon.weapon_charge_duration > 0.0f) 
            { 
                offsetPct = System.Convert.ToSingle(gameController.local_secondaryweapon.weapon_charge_timer / gameController.local_secondaryweapon.weapon_charge_duration);
                if (PTSSecondaryChargeMeterFGSprite.color != gameController.COLOR_CHARGE) { PTSSecondaryChargeMeterFGSprite.color = gameController.COLOR_CHARGE; }
            }
            else if (gameController.local_secondaryweapon.use_timer < gameController.local_secondaryweapon.use_cooldown && gameController.local_secondaryweapon.use_cooldown > 0.0f) 
            { 
                offsetPct = 1.0f - System.Convert.ToSingle(gameController.local_secondaryweapon.use_timer / gameController.local_secondaryweapon.use_cooldown);
                if (PTSSecondaryChargeMeterFGSprite.color != gameController.COLOR_COOLDOWN) { PTSSecondaryChargeMeterFGSprite.color = gameController.COLOR_COOLDOWN;  }
            }
            PTSSecondaryChargeMeterFGSprite.rectTransform.offsetMax = new Vector2(-offsetMax + (offsetMax * offsetPct), PTSSecondaryChargeMeterFGSprite.rectTransform.offsetMax.y);
        }
        else
        {
            PTSSecondaryChargeMeterFGSprite.gameObject.SetActive(false);
            PTSSecondaryChargeMeterBGSprite.gameObject.SetActive(false);
        }
    }

    public void UI_Air_Thrust()
    {
        if (PTSAirThrustMeterFGSprite != null && PTSAirThrustMeterBGSprite != null && gameController.local_plyAttr != null && gameController.local_plyAttr.air_thrust_enabled)
        {
            PTSAirThrustMeterFGSprite.gameObject.SetActive(!gameController.local_plyAttr.air_thrust_ready || ui_demo_enabled);
            PTSAirThrustMeterBGSprite.gameObject.SetActive(!gameController.local_plyAttr.air_thrust_ready || ui_demo_enabled);
            float offsetMax = PTSAirThrustMeterBGSprite.rectTransform.rect.width;
            float offsetPct = 0.0f;
            if (!gameController.local_plyAttr.air_thrust_ready && gameController.local_plyAttr.air_thrust_cooldown > 0.0f)
            {
                offsetPct = 1.0f - System.Convert.ToSingle(gameController.local_plyAttr.air_thrust_timer / gameController.local_plyAttr.air_thrust_cooldown);
                if (PTSAirThrustMeterFGSprite.color != gameController.COLOR_AIRTHRUST) { PTSAirThrustMeterFGSprite.color = gameController.COLOR_AIRTHRUST; }
            }
            PTSAirThrustMeterFGSprite.rectTransform.offsetMax = new Vector2(-offsetMax + (offsetMax * offsetPct), PTSAirThrustMeterFGSprite.rectTransform.offsetMax.y);
        }
        else
        {
            PTSAirThrustMeterFGSprite.gameObject.SetActive(false);
            PTSAirThrustMeterBGSprite.gameObject.SetActive(false);
        }
    }

    public void UI_Capturezones()
    {
        // Handle capture zones
        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing)
            && gameController.option_gamemode == (int)gamemode_name.KingOfTheHill 
            && gameController.map_selected >= 0 
            && gameController.mapscript_list != null && gameController.map_selected < gameController.mapscript_list.Length 
            && gameController.mapscript_list[gameController.map_selected].map_capturezones != null
            && (playerAttributes == null || (playerAttributes != null && !playerAttributes.ply_training && (playerAttributes.ply_state == (int)player_state_name.Alive || playerAttributes.ply_state == (int)player_state_name.Respawning)))
            )
        {
            PTSCapturePanel.gameObject.SetActive(true);
            byte koth_iter = 0;
            for (int i = 0; i < PTSCaptureSprites.Length; i++)
            {
                PTSCaptureSprites[i].transform.gameObject.SetActive(false);
                PTSCaptureTexts[i].text = ""; PTSContestTexts[i].text = "";
                if (i >= gameController.mapscript_list[gameController.map_selected].map_capturezones.Length) { break; }
                if (gameController.mapscript_list[gameController.map_selected].map_capturezones.Length > PTSCaptureSprites.Length) { UnityEngine.Debug.LogWarning("There are more capture zones (" + gameController.mapscript_list[gameController.map_selected].map_capturezones.Length + ") than sprites available to draw (" + PTSCaptureSprites.Length + ")!"); }

                CaptureZone capturezone = gameController.mapscript_list[gameController.map_selected].map_capturezones[i];
                if (capturezone == null || capturezone.gameObject == null || !capturezone.gameObject.activeInHierarchy) { continue; }
                PTSCaptureSprites[koth_iter].transform.gameObject.SetActive(true);

                // Display first three letters of holder's name
                int hold_index = 0;
                if (capturezone.dict_points_keys_arr != null && capturezone.dict_points_keys_arr.Length > 0) { hold_index = GlobalHelperFunctions.DictIndexFromKey(capturezone.hold_id, capturezone.dict_points_keys_arr); }
                string hold_text = ""; Color hold_color = Color.white;
                if (!capturezone.is_locked && hold_index >= 0 && capturezone.dict_points_keys_arr != null && hold_index < capturezone.dict_points_keys_arr.Length)
                {
                    if (gameController.option_teamplay) 
                    { 
                        hold_text = gameController.localizer.FetchText("TEAM_COLOR_" + capturezone.hold_id, gameController.team_names[capturezone.hold_id].Substring(0, Mathf.Min(hold_text.Length, 3)));
                        hold_color = gameController.team_colors[capturezone.hold_id];
                        //PTSCaptureTexts[i].color = gameController.team_colors_bright[capturezone.hold_id];
                    }
                    else
                    {
                        VRCPlayerApi hold_ply = VRCPlayerApi.GetPlayerById(capturezone.hold_id);
                        if (hold_ply != null)
                        {
                            hold_text = hold_ply.displayName;
                            if (hold_ply.playerId == Networking.LocalPlayer.playerId) 
                            {
                                hold_color = gameController.team_colors[0];
                                //PTSCaptureTexts[i].color = gameController.team_colors_bright[0];
                            }
                            else 
                            {
                                hold_color = gameController.team_colors[1];
                                //PTSCaptureTexts[i].color = gameController.team_colors_bright[1];
                            }
                        }
                        hold_text = hold_text.Substring(0, Mathf.Min(hold_text.Length, 3));
                    }
                    hold_text += '\n';
                    int margin_time = Networking.IsOwner(capturezone.gameObject) ? 0 : 1;
                    float timeLeft = gameController.option_gm_goal - capturezone.dict_points_values_arr[hold_index] - margin_time;
                    timeLeft = Mathf.Max(0, timeLeft);
                    if (timeLeft < 0.0f) { hold_text += string.Format("{0:F1}", timeLeft); }
                    else { hold_text += timeLeft.ToString(); }

                    PTSCaptureTexts[koth_iter].color = Color.white;
                    PTSContestTexts[koth_iter].color = Color.white;
                }
                else if (capturezone.is_locked)
                {
                    hold_color = Color.black;
                    PTSCaptureTexts[koth_iter].color = Color.white;
                    PTSContestTexts[koth_iter].color = Color.white;
                    hold_text = "X";
                }
                else
                {
                    hold_color = Color.gray;
                    PTSCaptureTexts[koth_iter].color = Color.white;
                    PTSContestTexts[koth_iter].color = Color.white;
                    hold_text = "O";
                }
                hold_color.a = 0.8f;
                PTSCaptureSprites[koth_iter].GetComponent<UnityEngine.UI.Image>().color = hold_color;
                PTSCaptureTexts[koth_iter].text = hold_text;

                // Display contest progress as an overlay
                int contest_index = -1;
                if (capturezone.dict_points_keys_arr != null && capturezone.dict_points_keys_arr.Length > 0) { contest_index = GlobalHelperFunctions.DictIndexFromKey(capturezone.contest_id, capturezone.dict_points_keys_arr); }
                string contest_text = ""; Color contest_color = Color.white;
                if (contest_index >= 0 && capturezone.dict_points_keys_arr != null && contest_index < capturezone.dict_points_keys_arr.Length)
                {
                    contest_text += '(';
                    PTSCaptureOverlays[koth_iter].offsetMax = new Vector2(PTSCaptureOverlays[koth_iter].offsetMax.x, Mathf.Lerp(-PTSCaptureSprites[i].sizeDelta.y, 0, capturezone.contest_progress / gameController.option_gm_config_a));
                    if (gameController.option_teamplay)
                    {
                        contest_color = gameController.team_colors[capturezone.contest_id];
                        contest_text += gameController.localizer.FetchText("TEAM_COLOR_" + capturezone.contest_id, gameController.team_names[capturezone.contest_id].Substring(0, Mathf.Min(contest_text.Length, 3)));
                    }
                    else
                    {
                        VRCPlayerApi contest_ply = VRCPlayerApi.GetPlayerById(capturezone.contest_id);
                        if (contest_ply != null)
                        {
                            contest_text += contest_ply.displayName;
                            if (contest_ply.playerId == Networking.LocalPlayer.playerId)
                            {
                                contest_color = gameController.team_colors[0];
                            }
                            else
                            {
                                contest_color = gameController.team_colors[2];
                            }
                        }
                        contest_text = hold_text.Substring(0, Mathf.Min(hold_text.Length, 3));
                    }
                    contest_text += ")\n";
                    contest_text += Mathf.Round(100.0f * (capturezone.contest_progress / gameController.option_gm_config_a)).ToString() + "%";

                }
                else 
                { 
                    PTSCaptureOverlays[koth_iter].offsetMax = new Vector2(PTSCaptureOverlays[koth_iter].offsetMax.x, -PTSCaptureSprites[i].sizeDelta.y);
                    contest_color = Color.white;
                }
                contest_color.a = 0.8f;
                PTSCaptureOverlays[koth_iter].GetComponent<UnityEngine.UI.Image>().color = contest_color;
                PTSContestTexts[koth_iter].text = contest_text;

                koth_iter++;

            }
        }
        else { PTSCapturePanel.gameObject.SetActive(false); }

    }

    public void UI_Miniscore()
    {
        // Handle mini scoreboard
        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) 
            && gameController.option_gamemode != (int)gamemode_name.BossBash
            && gameController.option_gamemode != (int)gamemode_name.Infection
            && gameController.option_gamemode != (int)gamemode_name.KingOfTheHill
            && (playerAttributes == null || (playerAttributes != null && !playerAttributes.ply_training && (playerAttributes.ply_state == (int)player_state_name.Alive || playerAttributes.ply_state == (int)player_state_name.Respawning)))
            && gamevars_leaderboard_arr != null
            && gamevars_progress_arr != null
            ) 
        {
            PTSScorePanel.gameObject.SetActive(true);
            for (int i = 0; i < PTSScoreParents.Length; i++)
            {
                if (i >= gamevars_leaderboard_arr.Length || i >= gamevars_progress_arr.Length) { PTSScoreParents[i].transform.gameObject.SetActive(false); continue; }
                PTSScoreParents[i].transform.gameObject.SetActive(true);
                String name_str = "";
                Color name_color = Color.white;
                bool use_cb = false;
                int team_id = 0;
                if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { use_cb = true; }

                if (gameController.option_teamplay)
                {
                    team_id = gamevars_leaderboard_arr[i];
                    name_str = gameController.localizer.FetchText("TEAM_COLOR_" + team_id, gameController.team_names[team_id]);
                    name_color = gameController.team_colors[team_id];
                    PTSScoreNameTexts[i].color = gameController.team_colors_bright[team_id];
                    PTSScorePlacementTexts[i].text = RankToString(i, PTSScorePlacementTexts[i]);
                }
                else
                {
                    VRCPlayerApi hold_ply = VRCPlayerApi.GetPlayerById(gamevars_leaderboard_arr[i]);
                    if (hold_ply != null)
                    {
                        name_str = hold_ply.displayName;
                        team_id = hold_ply == Networking.LocalPlayer ? 0 : 1;
                        name_color = gameController.team_colors[team_id];
                        PTSScoreNameTexts[i].color = gameController.team_colors_bright[team_id];
                        PTSScorePlacementTexts[i].text = RankToString(GetGameRank(gamevars_leaderboard_arr[i]), PTSScorePlacementTexts[i]);
                    }
                }
                name_str = name_str.Substring(0, Mathf.Min(name_str.Length, 3));
                PTSScoreNameTexts[i].text = name_str;

                if (use_cb)
                {
                    PTSScorePoles[i].gameObject.SetActive(false);
                    PTSScoreSprites[i].GetComponent<UnityEngine.UI.Image>().sprite = gameController.team_sprites[team_id];
                    PTSScoreSprites[i].GetComponent<UnityEngine.UI.Image>().color = name_color;
                }
                else
                {
                    PTSScorePoles[i].gameObject.SetActive(true);
                    PTSScoreSprites[i].GetComponent<UnityEngine.UI.Image>().sprite = PTSFlagSprite;
                    PTSScoreSprites[i].GetComponent<UnityEngine.UI.Image>().color = name_color;
                }

                string scoreText = "";
                scoreText = Mathf.Round(gamevars_progress_arr[i]).ToString();
                //if (gameController.option_gamemode == (int)gamemode_name.Survival) { scoreText = gameController.localizer.FetchText("SELF_UI_TEAM_SURVIVAL_SCORE", "$ARG0 Lives", Mathf.Round(gamevars_progress_arr[i]).ToString()); }
                //else if (gameController.option_gamemode == (int)gamemode_name.Clash) { scoreText = gameController.localizer.FetchText("SELF_UI_TEAM_CLASH_SCORE", "$ARG0 KO(s)", Mathf.Round(gamevars_progress_arr[i]).ToString()); }
                //else if (gameController.option_gamemode == (int)gamemode_name.FittingIn) { scoreText = gameController.localizer.FetchText("SELF_UI_TEAM_FITTINGIN_SCORE", "$ARG0 Fall(s)", Mathf.Round(gamevars_progress_arr[i]).ToString()); }
                PTSScoreNumberTexts[i].text = scoreText;
            }
        }
        else { PTSScorePanel.gameObject.SetActive(false); }
    }

    public override void PostLateUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        SetUIForward();
    }

    public Vector3 SetUIForward()
    {
        float heightUI = 0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        float scaleUI = 1.0f * 0.5f;
        float stretchUI = 1.0f;
        float separationUI = 1.0f;
        float distanceUI = 1.0f;
        float offsetUI = 0.0f;
        float angleUI = 0.0f;
        int useWrist = 0;

        float UI_WIDTH = 750.0f;
        float UI_HEIGHT = 450.0f;
        float desktop_ratio = Networking.LocalPlayer.IsUserInVR() ? (UI_WIDTH / UI_HEIGHT) : VRCCameraSettings.ScreenCamera.Aspect;
        UI_WIDTH *= desktop_ratio;
        UI_HEIGHT = UI_WIDTH / desktop_ratio;

        //float VRSCALECONST = Networking.LocalPlayer.IsUserInVR() ? 0.9f : 1.0f;
        //scaleUI *= VRSCALECONST;
        //float VRSTRETCHCONST = Networking.LocalPlayer.IsUserInVR() ? 0.8f : 1.0f;
        //float VRSEPARATIONCONST = Networking.LocalPlayer.IsUserInVR() ? 

        if (gameController != null && gameController.local_ppp_options != null)
        {
            PPP_Options ppp_options = gameController.local_ppp_options;
            useWrist = ppp_options.ui_wrist;

            distanceUI *= ppp_options.ui_distance;
            scaleUI *= ppp_options.ui_scale;
            stretchUI *= ppp_options.ui_stretch;
            separationUI *= ppp_options.ui_separation;
            angleUI = ppp_options.ui_angle;

            // new default setting: if UI in front and default, scale = 0.9x, vertical = 0.8x, horizontal = 0.8x, distance = 1.2x, yoffset = -60, [inverted = on --> these two will be in PPP Options]
            if (useWrist == 0 && Networking.LocalPlayer.IsUserInVR()) 
            { 
                scaleUI *= 0.9f * 0.8f;
                stretchUI *= 0.8f * 0.8f;
                separationUI *= 0.8f * 0.8f;
                distanceUI *= 1.2f;
                offsetUI = (ppp_options.ui_yoffset - 6.0f) / 10.0f;
            }
            else
            {
                offsetUI = ppp_options.ui_yoffset / 10.0f;
            }

            //PTSCanvas.sizeDelta = new Vector2(ppp_options.ui_separation * (5.0f / 3.0f), ppp_options.ui_separation);
            //PTSCanvas.sizeDelta = new Vector2(500, 300);
            PTSCanvas.sizeDelta = new Vector2(UI_WIDTH, UI_HEIGHT);
            float x_separation = (PTSTimerTransform.localPosition.x - PTSTeamTransform.localPosition.x) / 2.0f;
            PTSLivesTransform.localPosition = new Vector3(
                x_separation * stretchUI //150
                , PTSLivesTransform.localPosition.y
                , PTSLivesTransform.localPosition.z
                );
            PTSDamageTransform.localPosition = new Vector3(
                -x_separation * stretchUI //150
                , PTSDamageTransform.localPosition.y
                , PTSDamageTransform.localPosition.z
                );
            PTSInvulTransform.localPosition = PTSDamageTransform.localPosition;
            if (useWrist == 0) 
            {
                PTSCanvas.sizeDelta = new Vector2(UI_WIDTH * stretchUI, UI_HEIGHT * separationUI); 
            }
            else 
            { 
                PTSCanvas.sizeDelta = new Vector2(UI_WIDTH * stretchUI, UI_HEIGHT * separationUI); 
            }
            PTSPainDirTemplate.transform.GetChild(0).localPosition = new Vector3(0.0f, 86.0f * separationUI, 0.0f);

            float textScale = ppp_options.ui_textscale >= 1.0f
                 ? Mathf.Lerp(0.111f, 0.622f, ((ppp_options.ui_textscale * 10.0f) - 10.0f) / (ppp_options.ui_uitextscaleslider.maxValue - 10.0f))
                 : Mathf.Lerp(0.0f, 0.111f, ppp_options.ui_textscale);
            PTSTextStackParent.sizeDelta = new Vector2(
                PTSTextStackParent.sizeDelta.x
                , PTSCanvas.sizeDelta.y * textScale
                );
            //
            if (ppp_options.ui_textoffset < -0.33f) 
            {
                // Anchored to bottom
                PTSTextStackParent.anchorMin = new Vector2(0.0f, 0.0f);
                PTSTextStackParent.anchorMax = new Vector2(1.0f, 0.0f);
                PTSTextStackParent.localPosition = new Vector3(
                    PTSTextStackParent.localPosition.x
                    , (stored_local_pos_ptstextstackparent.y * (ppp_options.ui_inverted ? -1 : 1)) - Mathf.LerpUnclamped(PTSCanvas.sizeDelta.y / 2.0f, 0.0f, ppp_options.ui_textoffset + 1.0f)
                    , PTSTextStackParent.localPosition.z
                );
            }
            else if (ppp_options.ui_textoffset > 0.33f)
            {
                // Anchored to top
                PTSTextStackParent.anchorMin = new Vector2(0.0f, 1.0f);
                PTSTextStackParent.anchorMax = new Vector2(1.0f, 1.0f);
                PTSTextStackParent.localPosition = new Vector3(
                    PTSTextStackParent.localPosition.x
                    , (stored_local_pos_ptstextstackparent.y * (ppp_options.ui_inverted ? -1 : 1)) - Mathf.LerpUnclamped(PTSCanvas.sizeDelta.y / 2.0f, 0.0f, ppp_options.ui_textoffset + 1.0f)
                    // - Mathf.LerpUnclamped(PTSCanvas.sizeDelta.y / 2.0f, 0.0f, ppp_options.ui_textoffset < 0.0f ? ppp_options.ui_textoffset + 1.0f : ppp_options.ui_textoffset - 1.0f),
                    , PTSTextStackParent.localPosition.z
                    // 0.0 is correct for center
                );
            }
            else
            {
                // Anchored to center
                PTSTextStackParent.anchorMin = new Vector2(0.0f, 0.5f);
                PTSTextStackParent.anchorMax = new Vector2(1.0f, 0.5f);
                PTSTextStackParent.localPosition = new Vector3(
                    PTSTextStackParent.localPosition.x
                    , (stored_local_pos_ptstextstackparent.y * (ppp_options.ui_inverted ? -1 : 1)) + Mathf.Lerp(-PTSCanvas.sizeDelta.y / 2.0f, PTSCanvas.sizeDelta.y / 2.0f, (ppp_options.ui_textoffset + 1.0f) / 2.0f)
                    , PTSTextStackParent.localPosition.z
                );
            }
            

        }


        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        Vector3 plyUp = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.up;
        /*float plyMagInForward = Vector3.Dot(Networking.LocalPlayer.GetVelocity(), plyForward);
        Vector3 velAdd = Vector3.zero;

        if (playerAttributes != null && plyMagInForward > 0)
        {
            velAdd = 0.0095f * plyMagInForward * plyForward;
            if (playerAttributes.ply_scale < 1.0f && Networking.LocalPlayer.IsUserInVR()) { velAdd /= (playerAttributes.ply_scale / 0.9f); }
            if (useWrist > 0) { velAdd *= 2.0f; } // When wrist hud is active, we want to increase the tracking speed
        }*/

        // If we are using UI in front and are on desktop, distance UI instead becomes a parameter that scales both vertical & horizontal separation rather than being another scale
        //if (useWrist == 0 && !Networking.LocalPlayer.IsUserInVR()) { distanceUI = 1.0f; } 
        // in fron, and only in front, scale seems to affect the offsetuI???
        Vector3 posOut = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (plyForward * heightUI * distanceUI) + (plyUp * offsetUI * heightUI);
        //Vector3 VROffset = new Vector3(0.0f, -0.1f, 0.0f) * GlobalHelperFunctions.BoolToInt(Networking.LocalPlayer.IsUserInVR());
        Vector3 posFinal = posOut; //+ velAdd;
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * heightUI * scaleUI;
        transform.SetPositionAndRotation(
            posFinal
            , Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Quaternion.Euler(new Vector3(angleUI, 0.0f, 0.0f))
            );

        if (useWrist > 0)
        {
            Vector3 wrist_pos;
            Quaternion wrist_rot;
            float offset_height = 0.10f * playerAttributes.ply_scale;
            //offset_height *= separationUI; 
            Quaternion offset_rot = Quaternion.Euler(180.0f, -55.0f, 0.0f);
            Vector3 offset_pos = new Vector3(0.0f, offset_height, 0.0f);
            Vector3 distance_pos = new Vector3(0.0f, offsetUI, 0.20f * (distanceUI - 1.0f));
            if (useWrist == 1) 
            { 
                wrist_pos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                wrist_rot = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
            }
            else 
            { 
                wrist_pos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                wrist_rot = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                offset_rot = Quaternion.Euler(0.0f, 120.0f, 0.0f);
            }

            transform.SetPositionAndRotation(
                wrist_pos + (wrist_rot * offset_rot * offset_pos) + (wrist_rot * offset_rot * distance_pos)
                , wrist_rot * offset_rot
                );

            transform.localScale *= 0.33f;
        }

        return posOut;
    }

    public void ShowPainIndicator(float damage, Vector3 point_towards)
    {
        GameObject indicator_obj = Instantiate(PTSPainDirTemplate, transform);
        Vector3 indicator_world_pos = indicator_obj.transform.position;
        /*if (local_uimessagestoself != null) 
        { 
            indicator_obj.transform.parent = local_uimessagestoself.gameObject.transform;
            indicator_obj.transform.position = indicator_world_pos;
        }*/
        UIPainIndicator indicator_script = indicator_obj.GetComponent<UIPainIndicator>();
        indicator_script.pointTowards = point_towards;
        indicator_script.duration = Mathf.Clamp(damage / 10.0f, indicator_script.min_duration, indicator_script.max_duration);
        indicator_script.RotateComponent();
        indicator_obj.SetActive(true);
        indicator_script.StartTimer();
    }

    public void ShowHarmNumber(int defender_id, float damage, Vector3 origin_point)
    {
        // If the HarmNumber array is empty, create a new one with the single entry being our newest
        bool createNewNumber = false; int internal_id = -1;
        if (PTSHarmNumberList == null || PTSHarmNumberList.Length == 0)
        {
            createNewNumber = true;
            PTSHarmNumberList = new UIHarmNumber[1];
            internal_id = 0;
        }
        else
        {
            // Otherwise, check if the entry already exists for the defender_id. If so, we can just update it at the index.
            for (int i = 0; i < PTSHarmNumberList.Length; i++)
            {
                if (defender_id == PTSHarmNumberList[i].target_id) { internal_id = i; break; }
            }
            // If the entry does not exist, we'll create a new one at the index = old array length
            if (internal_id == -1) 
            {
                createNewNumber = true;
                internal_id = PTSHarmNumberList.Length;
                UIHarmNumber[] tempHarmNumberList = new UIHarmNumber[internal_id + 1];
                for (int i = 0; i < internal_id; i++)
                {
                    // Make sure new array has all of the old entries
                    tempHarmNumberList[i] = PTSHarmNumberList[i];
                }
                PTSHarmNumberList = tempHarmNumberList;
            }
        }

        Color32 defender_color = new Color32(255, 153, 0, 255);
        if (gameController.option_teamplay)
        {
            int defender_team = gameController.GetGlobalTeam(defender_id);
            if (defender_team >= 0) { defender_color = gameController.team_colors_bright[defender_team]; }
        }

        if (createNewNumber)
        {
            //int internal_id = gameController.DictIndexFromKey(defender_id, gameController.ply_tracking_dict_keys_arr);
            // Food for thought: instead of creating/destroying all of these, just have 80 on standby and then assign to the players in the tracking index accordingly and activate/deactivate
            //GameObject harm_obj = Instantiate(PTSHarmNumberTemplate, transform);
            if (gameController.global_harmnumber_arr == null || gameController.global_harmnumber_arr.Length == 0)
            {
                gameController.PreallocGlobalObj((int)prealloc_obj_name.UIHarmNumber);
            }
            if (gameController.global_harmnumber_cnt >= gameController.global_harmnumber_arr.Length || gameController.global_lowest_available_harmnumber_index >= gameController.global_harmnumber_arr.Length || gameController.global_lowest_available_harmnumber_index == -1)
            {
                UnityEngine.Debug.LogWarning("Exceeded maximum harmnumbers possible!");
            }
            else
            {
                if (gameController.global_harmnumber_arr[gameController.global_lowest_available_harmnumber_index] == null)
                {
                    gameController.PreallocGlobalObj((int)prealloc_obj_name.UIHarmNumber);
                }

                GameObject harm_obj = gameController.PreallocAddSlot((int)prealloc_obj_name.UIHarmNumber);
                harm_obj.transform.SetParent(null);
                UIHarmNumber harm_script = harm_obj.GetComponent<UIHarmNumber>();
                harm_script.ResetDisplay();
                harm_script.ui_parent = gameObject;
                harm_script.UpdateValue(Mathf.RoundToInt(damage), false);
                harm_script.origin = origin_point;
                harm_script.target_id = defender_id;
                if (playerAttributes != null) { harm_script.duration = playerAttributes.combo_send_duration; }
                harm_script.ui_text.color = defender_color;
                harm_obj.SetActive(true);
                harm_script.StartTimer();
                PTSHarmNumberList[internal_id] = harm_script;
            }
        }
        else
        {
            PTSHarmNumberList[internal_id].origin = origin_point;
            PTSHarmNumberList[internal_id].ui_text.color = defender_color;
            if (playerAttributes != null) { PTSHarmNumberList[internal_id].duration = playerAttributes.combo_send_duration; }
            PTSHarmNumberList[internal_id].UpdateValue(Mathf.RoundToInt(damage));
            if (!PTSHarmNumberList[internal_id].gameObject.activeInHierarchy)
            {
                PTSHarmNumberList[internal_id].gameObject.SetActive(true);
                PTSHarmNumberList[internal_id].StartTimer();
            }
        }

        StartEmitParticle(damage, origin_point);
    }

    public void ReleaseHarmNumber(int target_id, GameObject inHarmNumberObj)
    {
        // Search the array for the target_id, if the array exists
        int internal_id = -1;
        if (PTSHarmNumberList == null || PTSHarmNumberList.Length == 0)
        {
            if (inHarmNumberObj != null) { Destroy(inHarmNumberObj); } // If we have an empty list, this is a dangling object created from some unknown source. Destory it.
            return;
        }
        else
        {
            for (int i = 0; i < PTSHarmNumberList.Length; i++)
            {
                if (target_id == PTSHarmNumberList[i].target_id) { internal_id = i; break; }
            }
            // If the entry does not exist, just destroy the object
            if (internal_id < 0) { Destroy(inHarmNumberObj); }
            // Otherwise, remove from the arrays
            else
            {
                UIHarmNumber harmNumberScript = PTSHarmNumberList[internal_id];
                UIHarmNumber[] tempHarmNumberList = new UIHarmNumber[PTSHarmNumberList.Length - 1];
                for (int i = 0; i < PTSHarmNumberList.Length; i++)
                {
                    if (i < internal_id) { tempHarmNumberList[i] = PTSHarmNumberList[i]; }
                    else if (i > internal_id) { tempHarmNumberList[i - 1] = PTSHarmNumberList[i]; }
                }
                PTSHarmNumberList = tempHarmNumberList;
                gameController.PreallocClearSlot((int)prealloc_obj_name.UIHarmNumber, harmNumberScript.global_index, ref harmNumberScript.ref_index);

                //Destroy(inHarmNumberObj);
            }
        }
    }

    public void StartEmitParticle(float damage, Vector3 origin_point)
    {
        if (gameController == null || gameController.local_ppp_options == null || !gameController.local_ppp_options.particles_on || PTSParticleList == null || PTSParticleList.Length == 0) { return; }
        if (particle_pool_iter >= PTSParticleList.Length) { particle_pool_iter = 0; }
        if (PTSParticleList[particle_pool_iter] == null) { particle_pool_iter++; return; }

        PTSParticleList[particle_pool_iter].gameObject.SetActive(false);
        var main = PTSParticleList[particle_pool_iter].main;
        if (playerAttributes != null) 
        { 
            main.duration = playerAttributes.combo_send_duration / 2.0f;
            main.startLifetime = playerAttributes.combo_send_duration / 2.0f;
        }
        main.maxParticles = Mathf.FloorToInt(damage / 2.0f);
        PTSParticleList[particle_pool_iter].gameObject.transform.position = origin_point;
        PTSParticleList[particle_pool_iter].gameObject.SetActive(true);
        particle_pool_iter++;
    }
    
    public void TestHarmNumber()
    {
        if (gameController != null && gameController.local_ppp_options != null) { 

            GameObject harmTester = gameController.local_ppp_options.harmTester;

            ShowHarmNumber(-1, 10, harmTester.transform.position);

            PPP_Options ppp_options = gameController.local_ppp_options;
            GameObject harmTesterUI = harmTester.transform.GetChild(0).gameObject;
            HarmTesterUI harmtester_script = harmTesterUI.GetComponent<HarmTesterUI>();
            float scaleOtherUI = ((0.0f + ppp_options.ui_other_scale) / 1.0f);
            float posOtherUI = ((1.5f + ppp_options.ui_other_scale) / 2.5f);
            harmTesterUI.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * scaleOtherUI;
            harmTesterUI.transform.position = harmTester.transform.position + new Vector3(0.0f, 1.2f * posOtherUI, 0.0f);
            if (playerAttributes != null) { harmtester_script.duration = playerAttributes.combo_send_duration; }
            harmtester_script.timer = 0;
            harmTesterUI.SetActive(true);
        }
        
    }

    private void SetRenderQueueFromParent(Transform parent_transform)
    {
        /*map_element_spawn[] array_working = new map_element_spawn[1000];
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            Renderer component = t.GetComponent<Renderer>();
            if (component != null)
            {
                component.material.renderQueue = (int)RenderQueue.Overlay;
            }
        }*/
    }

    public void InvertUI()
    {
        if (gameController == null || gameController.local_ppp_options == null) { return; }

        Vector2 hold_sizedelta = PTSCanvas.sizeDelta;
        PTSCanvas.sizeDelta = stored_local_sizedelta_ptscanvas;
        if (gameController.local_ppp_options.ui_inverted)
        {
            // Bottom stretch
            Vector3 working_pos = stored_local_pos_ptstoppanel;
            ((RectTransform)PTSTopPanel.transform).anchorMin = new Vector2(0, 0);
            ((RectTransform)PTSTopPanel.transform).anchorMax = new Vector2(1, 0);
            working_pos.y = -working_pos.y;
            PTSTopPanel.transform.localPosition = working_pos;

            working_pos = stored_local_pos_ptschargepanel;
            working_pos.y = -working_pos.y;
            ((RectTransform)PTSChargePanel).anchorMin = new Vector2(0, 0);
            ((RectTransform)PTSChargePanel).anchorMax = new Vector2(1, 0);
            PTSChargePanel.localPosition = working_pos;

            // Top stretch
            working_pos = stored_local_pos_ptspoweruppanel;
            ((RectTransform)PTSPowerupPanel).anchorMin = new Vector2(0, 1);
            ((RectTransform)PTSPowerupPanel).anchorMax = new Vector2(1, 1);
            working_pos.y = -working_pos.y;
            PTSPowerupPanel.localPosition = working_pos;

            /*working_pos = stored_local_pos_ptstextstackparent;
            ((RectTransform)PTSTextStackParent).anchorMin = new Vector2(0, 1);
            ((RectTransform)PTSTextStackParent).anchorMax = new Vector2(1, 1);
            working_pos.y = -working_pos.y;
            PTSTextStackParent.localPosition = working_pos;*/

            // Offset all elements by the inverse of their Y
            working_pos = stored_local_pos_ptsairthrust;
            working_pos.y = -working_pos.y;
            PTSAirThrustMeterBGSprite.transform.localPosition = working_pos;
            working_pos = stored_local_pos_ptsweaponpanel;
            working_pos.y = -working_pos.y;
            PTSWeaponPanel.localPosition = working_pos;
            working_pos = stored_local_pos_ptscapturepanel;
            working_pos.y = -working_pos.y;
            PTSCapturePanel.localPosition = working_pos;
            working_pos = stored_local_pos_ptsscorepanel;
            working_pos.y = -working_pos.y;
            PTSScorePanel.localPosition = working_pos;
            /*for (int i = 0; i < PTSTextStack.Length; i++)
            {
                // Invert the order of the text stack
                PTSTextStack[i].transform.localPosition = stored_local_pos_ptstextstack[PTSTextStack.Length - 1 - i];
            }*/
        }
        else
        {
            // Top stretch
            ((RectTransform)PTSTopPanel.transform).anchorMin = new Vector2(0, 1);
            ((RectTransform)PTSTopPanel.transform).anchorMax = new Vector2(1, 1);
            PTSTopPanel.transform.localPosition = stored_local_pos_ptstoppanel;

            ((RectTransform)PTSChargePanel).anchorMin = new Vector2(0, 1);
            ((RectTransform)PTSChargePanel).anchorMax = new Vector2(1, 1);
            PTSChargePanel.localPosition = stored_local_pos_ptschargepanel;

            // Bottom stretch
            ((RectTransform)PTSPowerupPanel).anchorMin = new Vector2(0, 0);
            ((RectTransform)PTSPowerupPanel).anchorMax = new Vector2(1, 0);
            PTSPowerupPanel.localPosition = stored_local_pos_ptspoweruppanel;

            /*((RectTransform)PTSTextStackParent).anchorMin = new Vector2(stored_local_anchor_ptstextstackparent.x, stored_local_anchor_ptstextstackparent.y);
            ((RectTransform)PTSTextStackParent).anchorMax = new Vector2(stored_local_anchor_ptstextstackparent.z, stored_local_anchor_ptstextstackparent.w);
            PTSTextStackParent.localPosition = stored_local_pos_ptstextstackparent;*/

            PTSAirThrustMeterBGSprite.transform.localPosition = stored_local_pos_ptsairthrust;
            PTSWeaponPanel.localPosition = stored_local_pos_ptsweaponpanel;
            PTSCapturePanel.localPosition = stored_local_pos_ptscapturepanel;
            PTSScorePanel.localPosition = stored_local_pos_ptsscorepanel;
            /*for (int i = 0; i < PTSTextStack.Length; i++)
            {
                PTSTextStack[i].transform.localPosition = stored_local_pos_ptstextstack[i];
            }*/
        }
        PTSCanvas.sizeDelta = hold_sizedelta;
        SetUIForward();
    }

}
