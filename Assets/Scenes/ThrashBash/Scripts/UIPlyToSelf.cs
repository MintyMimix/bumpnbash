
using System;
using System.IO.Pipes;
using System.Linq;
using TMPro;
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class UIPlyToSelf : UdonSharpBehaviour
{
    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] public RectTransform PTSCanvas;
    [SerializeField] public RectTransform[] PTSTextStack;
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
    [SerializeField] public UnityEngine.UI.Image PTSWeaponSprite;
    [SerializeField] public TMP_Text PTSWeaponText;
    [SerializeField] public UnityEngine.UI.Image PTSChargeMeterFGSprite;
    [SerializeField] public UnityEngine.UI.Image PTSChargeMeterBGSprite;

    [SerializeField] public Transform PTSPowerupPanel;
    [NonSerialized] public UnityEngine.UI.Image[] PTSPowerupSprites;

    [SerializeField] public Transform PTSCapturePanel;
    [NonSerialized] public RectTransform[] PTSCaptureSprites;
    [NonSerialized] public RectTransform[] PTSCaptureOverlays;
    [NonSerialized] public TMP_Text[] PTSCaptureTexts;

    [SerializeField] public GameObject PTSPainDirTemplate;
    [SerializeField] public GameObject PTSHarmNumberTemplate;
    [NonSerialized] public UIHarmNumber[] PTSHarmNumberList;
    [SerializeField] public GameObject PTSParticleTemplate;
    [SerializeField] public int max_particle_emitters_in_pool = 10;
    [SerializeField] public int particle_pool_iter = 0;
    [NonSerialized] public ParticleSystem[] PTSParticleList;
   
    [NonSerialized] public PlayerAttributes playerAttributes;

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
    [SerializeField] public float text_queue_limited_fade_time_percent = 0.20f; // At what % of the the duration should the text begin fading? (i.e. if duration is 5.0f, 0.20f means fade at 4.0f)
    [SerializeField] public float text_queue_limited_extend = 0.5f; // How much longer should an active message be displayed if it is not the top message?
    [NonSerialized] public float[] text_queue_limited_timers;
    [NonSerialized] public Color[] text_queue_full_colors;
    [NonSerialized] public float[] text_queue_full_durations;

    [SerializeField] public float ui_check_gamevars_impulse = 0.4f; // How often should we check for game variables (i.e. team lives, points, etc.)
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

    void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        SetRenderQueueFromParent(transform);

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
        foreach (Transform child in (Transform)PTSCapturePanel)
        {
            if (child.name.Contains("PTSCaptureSprite"))
            {
                PTSCaptureSprites[capture_index] = (RectTransform)child;
                child.gameObject.SetActive(false);
                foreach (Transform subchild in child)
                {
                    if (subchild.name.Contains("PTSCaptureOverlay")) { PTSCaptureOverlays[capture_index] = (RectTransform)subchild; }
                    if (subchild.name.Contains("PTSCaptureText")) { PTSCaptureTexts[capture_index] = subchild.GetComponent<TMP_Text>(); ; }
                }
                capture_index++;
            }
        }

        if (PTSWeaponSprite != null) { PTSWeaponSprite.gameObject.SetActive(false); }
        if (PTSChargeMeterFGSprite != null) { PTSChargeMeterFGSprite.gameObject.SetActive(false); }
        if (PTSChargeMeterBGSprite != null) { PTSChargeMeterBGSprite.gameObject.SetActive(false); }

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
            text_queue_full_durations[0] = text_queue_duration_default;
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
        AddToTextQueue(input, Color.white, text_queue_duration_default);
    }
    public void AddToTextQueue(string input, Color color)
    {
        AddToTextQueue(input, color, text_queue_duration_default);
    }

    public void UpdateGameVariables()
    {
        if (gameController.round_state != (int)round_state_name.Start)
        {
            int team_total_lives = 0;
            gameController.CheckRoundGoalProgress(out gamevars_leaderboard_arr, out gamevars_progress_arr, out gamevars_leader_name);

            gamevars_local_highest_points = 0; gamevars_local_lowest_deaths = 0; gamevars_local_teams_alive = 0; gamevars_local_players_alive = 0;
            gamevars_local_highest_team = -3; gamevars_local_lowest_team = -3; gamevars_local_highest_ply_id = -1; gamevars_local_lowest_ply_id = -1;

            gamevars_local_team_points = gameController.CheckAllTeamPoints(ref gamevars_local_highest_team, ref gamevars_local_highest_points, ref gamevars_local_highest_ply_id);
            gamevars_local_team_deaths = gameController.CheckAllTeamPoints(ref gamevars_local_lowest_team, ref gamevars_local_lowest_deaths, ref gamevars_local_lowest_ply_id, true);
            gamevars_local_team_lives = gameController.CheckAllTeamLives(ref gamevars_local_players_alive, ref gamevars_local_teams_alive);
            gameController.CheckSingleTeamLives(playerAttributes.ply_team, ref gamevars_local_team_members_alive, ref team_total_lives);
            ui_check_gamevars_timer = 0.0f;
            if (gameController.ui_round_scoreboard_canvas != null && gameController.ui_round_scoreboard_canvas.GetComponent<Scoreboard>() != null)
            {
                gameController.ui_round_scoreboard_canvas.gameObject.GetComponent<Scoreboard>().RefreshScores();
                gameController.ui_round_scoreboard_canvas.gameObject.GetComponent<Scoreboard>().RearrangeScoreboard(gamevars_leaderboard_arr);
            }
        }
    }

    public int GetGameRank(int player_id, PlayerAttributes inPlyAttr)
    {
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


    public string RankToString(int inRank)
    {
        int rank = inRank + 1;
        if (rank < 1) 
        {
            PTSPlacementText.color = Color.white;
            return "?"; 
        }
        else if (rank == 1) 
        {
            PTSPlacementText.color = new Color32(255, 225, 0, 255);
            return "1st"; 
        }
        else if (rank == 2) 
        {
            PTSPlacementText.color = new Color32(240, 248, 255, 255);
            return "2nd"; 
        }
        else if (rank == 3) 
        {
            PTSPlacementText.color = new Color32(255, 166, 78, 255);
            return "3rd"; 
        }
        else 
        {
            PTSPlacementText.color = new Color32(123, 223, 185, 255);
            return rank.ToString() + "th"; 
        }
    }

    public void ProcessTextQueue()
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
                new_queue_timers[iteration] += Time.deltaTime;
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

    private void Update()
    {
        if (owner == null && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
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

        ProcessTextQueue();

        string[] splitStr = text_queue_full_str.Split(text_queue_separator);
        for (int i = 0; i < text_queue_limited_lines; i++)
        {
            if (i < text_queue_full_colors.Length) { PTSTextStack[i].GetComponent<TMP_Text>().color = text_queue_full_colors[i]; } // Needs to happen first, because alpha is modified after
            if (i < splitStr.Length)
            {
                PTSTextStack[i].GetComponent<TMP_Text>().text = splitStr[i].ToUpper();
                float duration_modified = text_queue_full_durations[i];
                float fade_time = duration_modified - (text_queue_limited_fade_time_percent * duration_modified);
                if (text_queue_limited_timers[i] >= fade_time) { PTSTextStack[i].GetComponent<TMP_Text>().alpha = 1 - ((text_queue_limited_timers[i] - fade_time) / (duration_modified - fade_time)); }
                else { PTSTextStack[i].GetComponent<TMP_Text>().alpha = 1.0f; }
            }
            else { PTSTextStack[i].GetComponent<TMP_Text>().text = ""; }
        }

        // Tick down demo timer
        if (ui_demo_enabled && ui_demo_timer < ui_demo_duration)
        {
            //if (ui_demo_timer == 0.0f) { AddToTextQueue("Example message", Color.cyan); }
            ui_demo_timer += Time.deltaTime;
        }
        else if (ui_demo_enabled && ui_demo_timer >= ui_demo_duration)
        {
            ui_demo_enabled = false;
            if (ui_show_intro_text) {
                //AddToTextQueue("This game is in development; there may be major bugs or issues!", Color.red);
                //AddToTextQueue(" -- ALPHA BUILD VERSION 0.18.4 --", Color.white);
                //AddToTextQueue("Step in the square to join the game!", Color.white);
                if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.RefreshAllOptions(); }
                if (gameController != null && Networking.GetOwner(gameController.gameObject) == Networking.LocalPlayer) { gameController.ResetGameOptionsToDefault(false); }
            }
            ui_show_intro_text = false;
            ui_demo_timer = 0.0f;
        }


        // Tick down gamevars update timer
        if (ui_check_gamevars_timer < ui_check_gamevars_impulse)
        {
            ui_check_gamevars_timer += Time.deltaTime;
        }
        else
        {
            UpdateGameVariables();
            //gameController.RefreshSetupUI(); // Don't actually do this; only good for debugging but is redundant and lag spiking otherwise
        }

        // Sort out better without all the debug
        if (playerAttributes == null)
        {
            if (gameController != null && owner != null) { playerAttributes = gameController.FindPlayerAttributes(owner); }
            else if (owner == null) { TransferOwner(Networking.GetOwner(gameObject)); }
            return;
        }

        bool round_ready = gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Queued || gameController.round_state == (int)round_state_name.Loading || gameController.round_state == (int)round_state_name.Over;
        if (ui_demo_enabled && !ui_show_intro_text) { PTSTopPanel.SetActive(true); }
        else if (playerAttributes.ply_training) { PTSTopPanel.SetActive(true); }
        else if ((round_ready || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator || playerAttributes.ply_team < 0) && PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(false); }
        else if (!(round_ready || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator || playerAttributes.ply_team < 0) && !PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(true); }

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
                //if (scaleAdd < 0.05f) { gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitReceive], gameController.snd_ready_sfx_clips, (int)ready_sfx_name.TimerTick, 0.5f); }
                PTSTimerTransform.localScale = new Vector3(1.0f + scaleAdd, 1.0f + scaleAdd, 1.0f + scaleAdd);
            }
            else if (TimerValue < 30.0) { PTSTimer.color = new Color(1.0f, 0.6f, 0.4f, 1.0f); }
            else { PTSTimer.color = Color.white; }
        }
        PTSTimer.text = TimerText;


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

        var AttackVal = Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var AttackText = AttackVal + "x";
        if (gameController.round_state == (int)round_state_name.Start && !playerAttributes.ply_training) { AttackText = ""; }
        if (AttackVal > gameController.plysettings_atk) { PTSAttack.color = new Color32(60, 255, 60, 255); }
        else if (AttackVal < gameController.plysettings_atk) { PTSAttack.color = new Color32(255, 60, 60, 255); }
        else { PTSAttack.color = new Color32(255, 255, 255, 255); }
        PTSAttack.text = AttackText;

        var DefenseVal = Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var DefenseText = DefenseVal + "x";
        if (gameController.round_state == (int)round_state_name.Start && !playerAttributes.ply_training) { DefenseText = ""; }
        if (DefenseVal > gameController.plysettings_def) { PTSDefense.color = new Color32(60, 255, 60, 255); }
        else if (DefenseVal < gameController.plysettings_def) { PTSDefense.color = new Color32(255, 60, 60, 255); }
        else { PTSDefense.color = new Color32(255, 255, 255, 255); }
        PTSDefense.text = DefenseText;

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
            string rank_str = RankToString(local_rank);
            // If we are in Survival, display # of players alive on your team
            if (gameController.option_gamemode == (int)gamemode_name.Survival)
            {
                PlacementText = rank_str;
                FlagText = "\n\n" + members_alive.ToString() + " Alive";
            }
            // If this is Clash, display the leader's point count
            else if (gameController.option_gamemode == (int)gamemode_name.Clash)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n1st:\n" + gamevars_progress_arr[0].ToString() + " KO";
                    if (gamevars_progress_arr[0] != 1) { FlagText += "s"; }
                }
            }
            // If we are in Boss Bash, display your team's KOs
            else if (gameController.option_gamemode == (int)gamemode_name.BossBash)
            {
                FlagText = total_points.ToString() + " KO";
                if (total_points != 1) { FlagText += "s"; }
                PTSTeamPoleImage.enabled = false;
                PTSTeamFlagImage.sprite = PTSPointsSprite;
                PTSTeamCBSpriteImage.sprite = PTSPointsSprite;
            }
            // If we are in Infection, display the Survivor's players alive count
            else if (gameController.option_gamemode == (int)gamemode_name.Infection)
            {
                FlagText = Mathf.RoundToInt(gamevars_local_team_lives[0] / 2).ToString() + " Alive";
            }
            // To-do: If this is King of the Hill, display the total capture time remaining
            else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n1st:\n" + Mathf.RoundToInt(gameController.option_gm_goal - gamevars_progress_arr[0]).ToString();
                }
            }
            // If we are in Fitting In, display the leader's death count
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n1st:\n" + (-gamevars_progress_arr[0]).ToString() + " Falls";
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
                if (gamevars_local_team_points != null && gamevars_local_team_points.Length > 1) { LivesText = gamevars_local_team_points[1].ToString(); }
                PTSLives.color = Color.white;
                PTSLivesImage.color = Color.white;
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            // If this is King of the Hill, display the total capture time remaining
            else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                float timeLeft = gameController.option_gm_goal - playerAttributes.ply_points;
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
            // If this is Fitting In, display the number of deaths
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                if (gameController.option_teamplay && gamevars_local_team_deaths != null && gamevars_local_team_deaths.Length > playerAttributes.ply_team && playerAttributes.ply_team >= 0) 
                { 
                    LivesText = Mathf.RoundToInt(playerAttributes.ply_deaths).ToString() + "\n" + Mathf.RoundToInt(gamevars_local_team_members_alive).ToString() + " Alive"; 
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


        // Handle powerup sprites
        if (playerAttributes.powerups_active != null)
        {
            var powerup_len = (int)Mathf.Min(PTSPowerupSprites.Length, playerAttributes.powerups_active.Length);
            for (int i = 0; i < PTSPowerupSprites.Length; i++)
            {
                PTSPowerupSprites[i].transform.gameObject.SetActive(false);
                PTSPowerupSprites[i].GetComponentInChildren<TMP_Text>().text = "";

                if (i < powerup_len)
                {
                    if (playerAttributes.powerups_active[i] == null) { continue; }
                    var powerup = playerAttributes.powerups_active[i].GetComponent<ItemPowerup>();
                    if (powerup == null) { continue; }
                    PTSPowerupSprites[i].transform.gameObject.SetActive(true);
                    PTSPowerupSprites[i].sprite = powerup.powerup_sprites[powerup.powerup_type];

                    TMP_Text PTSPowerupSpriteText = PTSPowerupSprites[i].GetComponentInChildren<TMP_Text>();
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

            }
        }

        // Handle weapon stats
        PlayerWeapon plyweapon = null;
        if (gameController != null) { plyweapon = gameController.local_plyweapon; }
        if (plyweapon != null && PTSWeaponSprite != null && PTSWeaponText != null && plyweapon.weapon_type != plyweapon.weapon_type_default)
        {
            string weaponTxt = "";
            PTSWeaponText.color = Color.white;
            if (plyweapon.weapon_temp_ammo > -1)
            {
                weaponTxt += plyweapon.weapon_temp_ammo.ToString();
                if (plyweapon.weapon_temp_ammo < 3) { PTSWeaponText.color = new Color(1.0f, 0.8f, 0.4f, 1.0f); }
            }
            if (plyweapon.weapon_temp_duration > -1)
            {
                if (weaponTxt.Length > 0) { weaponTxt += " (^)"; }
                else { weaponTxt = "^"; }
                float weapon_time_left = plyweapon.weapon_temp_duration - plyweapon.weapon_temp_timer;
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

            PTSWeaponText.text = weaponTxt;
            if (!PTSWeaponSprite.gameObject.activeInHierarchy) { PTSWeaponSprite.gameObject.SetActive(true); }

            if (PTSChargeMeterFGSprite != null && PTSChargeMeterBGSprite != null)
            {
                PTSChargeMeterFGSprite.gameObject.SetActive(plyweapon.weapon_is_charging);
                PTSChargeMeterBGSprite.gameObject.SetActive(plyweapon.weapon_is_charging);
                float offsetMax = PTSChargeMeterBGSprite.rectTransform.rect.width;
                float offsetPct = 0.0f;
                if (plyweapon.weapon_charge_duration > 0.0f) { offsetPct = System.Convert.ToSingle(plyweapon.weapon_charge_timer / plyweapon.weapon_charge_duration); }
                PTSChargeMeterFGSprite.rectTransform.offsetMax = new Vector2(-offsetMax + (offsetMax * offsetPct), PTSChargeMeterFGSprite.rectTransform.offsetMax.y);
            }
        }
        else
        {
            if (PTSWeaponText != null) { PTSWeaponText.text = ""; }
            if (PTSWeaponSprite != null) { PTSWeaponSprite.gameObject.SetActive(false); }
            if (PTSChargeMeterFGSprite != null) { PTSChargeMeterFGSprite.gameObject.SetActive(false); }
            if (PTSChargeMeterBGSprite != null) { PTSChargeMeterBGSprite.gameObject.SetActive(false); }
        }

        // Handle capture zones
        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) && gameController.option_gamemode == (int)gamemode_name.KingOfTheHill && gameController.map_selected >= 0 && gameController.mapscript_list != null && gameController.map_selected < gameController.mapscript_list.Length && gameController.mapscript_list[gameController.map_selected].map_capturezones != null)
        {
            PTSCapturePanel.gameObject.SetActive(true);
            byte koth_iter = 0;
            for (int i = 0; i < PTSCaptureSprites.Length; i++)
            {
                PTSCaptureSprites[i].transform.gameObject.SetActive(false);
                PTSCaptureTexts[i].text = "";
                if (i >= gameController.mapscript_list[gameController.map_selected].map_capturezones.Length) { break; }
                if (gameController.mapscript_list[gameController.map_selected].map_capturezones.Length > PTSCaptureSprites.Length) { UnityEngine.Debug.LogWarning("There are more capture zones (" + gameController.mapscript_list[gameController.map_selected].map_capturezones.Length + ") than sprites available to draw (" + PTSCaptureSprites.Length + ")!"); }

                CaptureZone capturezone = gameController.mapscript_list[gameController.map_selected].map_capturezones[i];
                if (capturezone == null || capturezone.gameObject == null || !capturezone.gameObject.activeInHierarchy) { continue; }
                PTSCaptureSprites[koth_iter].transform.gameObject.SetActive(true);

                // Display first three letters of holder's name
                int hold_index = 0;
                if (capturezone.dict_points_keys_arr != null && capturezone.dict_points_keys_arr.Length > 0) { hold_index = gameController.DictIndexFromKey(capturezone.hold_id, capturezone.dict_points_keys_arr); }
                string hold_text = ""; Color hold_color = Color.white;
                if (!capturezone.is_locked && hold_index >= 0 && hold_index < capturezone.dict_points_keys_arr.Length)
                {
                    if (gameController.option_teamplay) 
                    { 
                        hold_text = gameController.team_names[capturezone.hold_id];
                        hold_color = gameController.team_colors[capturezone.hold_id];
                        PTSCaptureTexts[i].color = gameController.team_colors_bright[capturezone.hold_id];
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
                                PTSCaptureTexts[i].color = gameController.team_colors_bright[0];
                            }
                            else 
                            {
                                hold_color = gameController.team_colors[1];
                                PTSCaptureTexts[i].color = gameController.team_colors_bright[1];
                            }
                        }
                    }
                    hold_text = hold_text.Substring(0, Mathf.Min(hold_text.Length, 3));
                    PTSCaptureTexts[koth_iter].color = Color.white;
                }
                else if (capturezone.is_locked)
                {
                    hold_color = Color.gray;
                    PTSCaptureTexts[koth_iter].color = Color.gray;
                    hold_text = "X";
                }
                else
                {
                    hold_color = Color.white;
                    PTSCaptureTexts[koth_iter].color = Color.white;
                    hold_text = "O";
                }
                hold_color.a = 0.8f;
                PTSCaptureSprites[koth_iter].GetComponent<UnityEngine.UI.Image>().color = hold_color;
                PTSCaptureTexts[koth_iter].text = hold_text;

                // Display contest progress as an overlay
                int contest_index = -1;
                if (capturezone.dict_points_keys_arr != null && capturezone.dict_points_keys_arr.Length > 0) { contest_index = gameController.DictIndexFromKey(capturezone.contest_id, capturezone.dict_points_keys_arr); }
                 Color contest_color = Color.white;
                if (contest_index >= 0 && contest_index < capturezone.dict_points_keys_arr.Length)
                {
                    PTSCaptureOverlays[koth_iter].offsetMax = new Vector2(PTSCaptureOverlays[koth_iter].offsetMax.x, Mathf.Lerp(-PTSCaptureSprites[i].sizeDelta.y, 0, capturezone.contest_progress / gameController.option_gm_config_a));
                    if (gameController.option_teamplay)
                    {
                        contest_color = gameController.team_colors[capturezone.contest_id];
                    }
                    else
                    {
                        VRCPlayerApi contest_ply = VRCPlayerApi.GetPlayerById(capturezone.contest_id);
                        if (contest_ply != null)
                        {
                            if (contest_ply.playerId == Networking.LocalPlayer.playerId)
                            {
                                contest_color = gameController.team_colors[0];
                            }
                            else
                            {
                                contest_color = gameController.team_colors[2];
                            }
                        }
                    }
                }
                else 
                { 
                    PTSCaptureOverlays[koth_iter].offsetMax = new Vector2(PTSCaptureOverlays[koth_iter].offsetMax.x, -PTSCaptureSprites[i].sizeDelta.y);
                    contest_color = Color.white;
                }
                contest_color.a = 0.8f;
                PTSCaptureOverlays[koth_iter].GetComponent<UnityEngine.UI.Image>().color = contest_color;

                koth_iter++;

            }
        }
        else { PTSCapturePanel.gameObject.SetActive(false); }

    }

    public override void PostLateUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        SetUIForward();
    }

    public Vector3 SetUIForward()
    {
        var heightUI = 0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        var scaleUI = 1.0f;
        int useWrist = 0;
        if (gameController != null && gameController.local_ppp_options != null)
        {
            PPP_Options ppp_options = gameController.local_ppp_options;
            useWrist = ppp_options.ui_wrist;

            for (int i = 0; i < text_queue_limited_lines; i++)
            {
                PTSTextStack[i].gameObject.SetActive(useWrist > 0);
            }

            scaleUI *= (ppp_options.ui_scale);
            //PTSCanvas.sizeDelta = new Vector2(ppp_options.ui_separation * (5.0f / 3.0f), ppp_options.ui_separation);
            PTSCanvas.sizeDelta = new Vector2(500, 300);
            float x_separation = (PTSTimerTransform.localPosition.x - PTSTeamTransform.localPosition.x) / 2.0f;
            PTSLivesTransform.localPosition = new Vector3(
                x_separation * ppp_options.ui_stretch //150
                , PTSLivesTransform.localPosition.y
                , PTSLivesTransform.localPosition.z
                );
            PTSDamageTransform.localPosition = new Vector3(
                -x_separation * ppp_options.ui_stretch //150
                , PTSDamageTransform.localPosition.y
                , PTSDamageTransform.localPosition.z
                );
            PTSInvulTransform.localPosition = PTSDamageTransform.localPosition;
            if (useWrist == 0) { PTSCanvas.sizeDelta = new Vector2(500 * ppp_options.ui_stretch, 300 * ppp_options.ui_separation); }
            else { PTSCanvas.sizeDelta = new Vector2(500 * ppp_options.ui_stretch, 300); }
            PTSPainDirTemplate.transform.GetChild(0).localPosition = new Vector3(0.0f, 86.0f * ppp_options.ui_separation, 0.0f);

            ((RectTransform)PTSTextStack[0].parent).sizeDelta = new Vector2(
                ((RectTransform)PTSTextStack[0].parent).sizeDelta.x
                , text_queue_limited_lines * (PTSCanvas.sizeDelta.y / 10.0f)
                );

            for (int i = 0; i < text_queue_limited_lines; i++)
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

            }

            
            //ppp_options.ui_separation * (5.0f / 3.0f)
            //ppp_options.ui_scale
        }

        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        /*float plyMagInForward = Vector3.Dot(Networking.LocalPlayer.GetVelocity(), plyForward);
        Vector3 velAdd = Vector3.zero;

        if (playerAttributes != null && plyMagInForward > 0)
        {
            velAdd = 0.0095f * plyMagInForward * plyForward;
            if (playerAttributes.ply_scale < 1.0f && Networking.LocalPlayer.IsUserInVR()) { velAdd /= (playerAttributes.ply_scale / 0.9f); }
            if (useWrist > 0) { velAdd *= 2.0f; } // When wrist hud is active, we want to increase the tracking speed
        }*/

        Vector3 posOut = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (plyForward * heightUI);
        Vector3 posFinal = posOut; //+ velAdd;
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * heightUI * scaleUI;
        transform.SetPositionAndRotation(
            posFinal
            , Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation
            );

        if (useWrist > 0)
        {
            Vector3 wrist_pos;
            Quaternion wrist_rot;
            float offset_height = 0.10f * playerAttributes.ply_scale;
            if (gameController != null && gameController.local_ppp_options != null) { offset_height *= gameController.local_ppp_options.ui_separation; }
            Vector3 offset_pos = new Vector3(0.0f, offset_height, 0.0f);
            Quaternion offset_rot = Quaternion.Euler(180.0f, -55.0f, 0.0f);
            if (useWrist == 1) 
            { 
                wrist_pos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                wrist_rot = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
            }
            else 
            { 
                wrist_pos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                wrist_rot = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                offset_rot = Quaternion.Euler(0.0f, 55.0f, 0.0f);
            }

            transform.SetPositionAndRotation(
                wrist_pos + (wrist_rot * offset_rot * offset_pos) 
                , wrist_rot * offset_rot
                );

            transform.localScale *= 0.33f;
        }

        return posOut;
    }

    public void ShowPainIndicator(float damage, Vector3 point_towards)
    {
        GameObject indicator_obj = Instantiate(PTSPainDirTemplate, transform);
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
            GameObject harm_obj = Instantiate(PTSHarmNumberTemplate, transform);
            harm_obj.transform.SetParent(null);
            UIHarmNumber harm_script = harm_obj.GetComponent<UIHarmNumber>();
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
        else
        {
            PTSHarmNumberList[internal_id].origin = origin_point;
            PTSHarmNumberList[internal_id].ui_text.color = defender_color;
            PTSHarmNumberList[internal_id].UpdateValue(Mathf.RoundToInt(damage));
        }

        StartEmitParticle(damage, origin_point);
    }

    public void ReleaseHarmNumber(int target_id, GameObject inHarmNumberObj)
    {
        // Search the array for the target_id, if the array exists
        int internal_id = -1;
        if (PTSHarmNumberList == null || PTSHarmNumberList.Length == 0)
        {
            if (inHarmNumberObj != null) { Destroy(inHarmNumberObj); }
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
            // Otherwise, remove from the array first before doing so
            else
            {
                UIHarmNumber[] tempHarmNumberList = new UIHarmNumber[PTSHarmNumberList.Length - 1];
                for (int i = 0; i < PTSHarmNumberList.Length; i++)
                {
                    if (i < internal_id) { tempHarmNumberList[i] = PTSHarmNumberList[i]; }
                    else if (i > internal_id) { tempHarmNumberList[i - 1] = PTSHarmNumberList[i]; }
                }
                PTSHarmNumberList = tempHarmNumberList;
                Destroy(inHarmNumberObj);
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

            ShowHarmNumber(0, 10, harmTester.transform.position);

            PPP_Options ppp_options = gameController.local_ppp_options;
            GameObject harmTesterUI = harmTester.transform.GetChild(0).gameObject;
            HarmTesterUI harmtester_script = harmTesterUI.GetComponent<HarmTesterUI>();
            float scaleOtherUI = ((0.0f + ppp_options.ui_other_scale) / 1.0f);
            float posOtherUI = ((1.0f + ppp_options.ui_other_scale) / 2.0f);
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

}
