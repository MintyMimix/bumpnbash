
using System;
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class UIPlyToSelf : UdonSharpBehaviour
{

    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] public RectTransform PTSCanvas;
    [SerializeField] public TMP_Text[] PTSTextStack;
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
    [NonSerialized] public UnityEngine.UI.Image[] PTSCaptureSprites;
    [NonSerialized] public UnityEngine.UI.Image[] PTSCaptureOverlays;
    [NonSerialized] public TMP_Text[] PTSCaptureTexts;

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
    [SerializeField] public float text_queue_limited_duration = 5.0f; // How long should an active message be displayed?
    [SerializeField] public float text_queue_limited_fade_time_percent = 0.20f; // At what % of the the duration should the text begin fading? (i.e. if duration is 5.0f, 0.20f means fade at 4.0f)
    [SerializeField] public float text_queue_limited_extend = 0.5f; // How much longer should an active message be displayed if it is not the top message?
    [NonSerialized] public float[] text_queue_limited_timers;
    [NonSerialized] public Color[] text_queue_full_colors;

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

        var item_index = 0;
        var item_size = 0;
        text_queue_limited_timers = new float[text_queue_limited_lines];
        for (int t = 0; t < text_queue_limited_timers.Length; t++)
        {
            text_queue_limited_timers[t] -= ((t - 1) * text_queue_limited_extend);
        }

        text_queue_full_colors = new Color[text_queue_full_max_lines];
        for (int t = 0; t < text_queue_full_colors.Length; t++)
        {
            text_queue_full_colors[t] = Color.white;
        }

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

        /*temp_parent = null;
        for (int i = 0; i < PTSCapturePanel.transform.childCount; i++)
        {
            var child = (RectTransform)transform.GetChild(i);
            if (child.name.Contains("PTSCapturePanel")) { temp_parent = child; break; }
        }*/
        foreach (GameObject child in (Transform)PTSCapturePanel)
        {
            if (child == null) { continue; }
            if (child.name.Contains("PTSCaptureSprite")) { item_size++; }
        }
        PTSCaptureSprites = new UnityEngine.UI.Image[item_size];
        PTSCaptureOverlays = new UnityEngine.UI.Image[item_size];
        PTSCaptureTexts = new TMP_Text[item_size];
        foreach (Transform child in (Transform)PTSCapturePanel)
        {
            if (child.name.Contains("PTSCaptureSprite"))
            {
                PTSCaptureSprites[item_index] = child.GetComponent<UnityEngine.UI.Image>();
                child.gameObject.SetActive(false);
                foreach (Transform subchild in child)
                {
                    if (subchild.name.Contains("PTSCaptureOverlay")) { PTSCaptureOverlays[item_index] = subchild.GetComponent<UnityEngine.UI.Image>(); ; }
                    if (subchild.name.Contains("PTSCaptureText")) { PTSCaptureTexts[item_index] = subchild.GetComponent<TMP_Text>(); ; }
                }
                item_index++;
            }
        }

        if (PTSWeaponSprite != null) { PTSWeaponSprite.gameObject.SetActive(false); }
        if (PTSChargeMeterFGSprite != null) { PTSChargeMeterFGSprite.gameObject.SetActive(false); }
        if (PTSChargeMeterBGSprite != null) { PTSChargeMeterBGSprite.gameObject.SetActive(false); }


        ui_show_intro_text = true;
        if (ui_show_intro_text) { AddToTextQueue("Welcome!"); }
        ui_demo_enabled = true;

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

    // Overloaded method where no color = Color.white
    public void AddToTextQueue(string input, Color color)
    {
        string[] queue_arr = text_queue_full_str.Split(text_queue_separator);
        if (queue_arr.Length > text_queue_full_max_lines)
        {
            // If the queue is clogged, pop the next upcoming message 
            queue_arr[text_queue_limited_lines] = "";
            text_queue_full_str = String.Join(text_queue_separator, queue_arr, 0, queue_arr.Length - 1);
            for (int j = text_queue_limited_lines; j < text_queue_full_max_lines - 1; j++)
            {
                text_queue_full_colors[j] = text_queue_full_colors[j + 1];
            }
            text_queue_full_colors[text_queue_full_max_lines - 1] = color;
        }
        else if (text_queue_full_str.Length == 0) { text_queue_full_colors[0] = color; }
        else if (text_queue_full_str.Length > 0) { text_queue_full_colors[queue_arr.Length] = color; }
        //UnityEngine.Debug.Log("[COLOR_TEST] Adding " + color + " to colors at index " + queue_arr.Length);

        if (text_queue_full_str.Length > 0) { text_queue_full_str += text_queue_separator; }
        text_queue_full_str += input;
    }
    public void AddToTextQueue(string input)
    {
        AddToTextQueue(input, Color.white);
    }

    public void UpdateGameVariables()
    {
        if (gameController.round_state != (int)round_state_name.Start)
        {
            int team_total_lives = 0;
            gameController.CheckRoundGoalProgress(out gamevars_leaderboard_arr, out gamevars_progress_arr, out gamevars_leader_name);
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
        if (inPlyAttr.ply_team >= 0 && inPlyAttr.ply_team < gamevars_local_team_lives.Length) { total_lives = gamevars_local_team_lives[inPlyAttr.ply_team]; }
        int total_points = 0; 
        if (inPlyAttr.ply_team >= 0 && inPlyAttr.ply_team < gamevars_local_team_points.Length) { total_points = gamevars_local_team_points[inPlyAttr.ply_team]; }
        int total_deaths = 0;
        if (inPlyAttr.ply_team >= 0 && inPlyAttr.ply_team < gamevars_local_team_deaths.Length) { total_deaths = gamevars_local_team_deaths[inPlyAttr.ply_team]; }

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
            if (gameController.option_teamplay && gamevars_leaderboard_arr[i] == inPlyAttr.ply_team) { rank = i; break; }
            else if (!gameController.option_teamplay && gamevars_leaderboard_arr[i] == player_id) { rank = i; break; }
        }
        return rank;
    }


    public string RankToString(int inRank)
    {
        int rank = inRank + 1;
        if (rank < 1) 
        {
            PTSPlacementText.color = Color.white;
            return "(Invalid)"; 
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
            if (new_queue_timers[iteration] < text_queue_limited_duration)
            {
                new_queue_timers[iteration] += Time.deltaTime;
            }
            else
            {
                // Shift timer entries up, and manage bonus time from queue position (+text_queue_limited_extended for each entry after 0)
                for (int j = iteration; j < iterateAmount - 1; j++)
                {
                    new_queue_timers[j] = new_queue_timers[j + 1];
                    if (new_queue_timers[j] < (text_queue_limited_duration - text_queue_limited_extend)) { new_queue_timers[j] += text_queue_limited_extend; }
                }
                new_queue_timers[iterateAmount - 1] = 0.0f - ((iterateAmount - 1) * text_queue_limited_extend); // We want to add a little bonus time for those later in the queue
                // Shift string entries up
                for (int k = iteration; k < splitStr.Length - 1; k++)
                {
                    splitStr[k] = splitStr[k + 1];
                    //UnityEngine.Debug.Log("[COLOR_TEST] Shifting " + text_queue_full_colors[k] + " at index " + k + " to " + text_queue_full_colors[k + 1]);
                    text_queue_full_colors[k] = text_queue_full_colors[k + 1];
                }
                //UnityEngine.Debug.Log("[COLOR_TEST] Resetting " + text_queue_full_colors[splitStr.Length - 1] + " at index " + (splitStr.Length - 1));
                text_queue_full_colors[splitStr.Length - 1] = Color.white;
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
            if (i < text_queue_full_colors.Length) { PTSTextStack[i].color = text_queue_full_colors[i]; } // Needs to happen first, because alpha is modified after
            if (i < splitStr.Length)
            {
                PTSTextStack[i].text = splitStr[i].ToUpper();
                float duration_modified = text_queue_limited_duration;
                float fade_time = duration_modified - (text_queue_limited_fade_time_percent * duration_modified);
                if (text_queue_limited_timers[i] >= fade_time) { PTSTextStack[i].alpha = 1 - ((text_queue_limited_timers[i] - fade_time) / (duration_modified - fade_time)); }
                else { PTSTextStack[i].alpha = 1.0f; }
            }
            else { PTSTextStack[i].text = ""; }
        }

        // Tick down demo timer
        if (ui_demo_enabled && ui_demo_timer < ui_demo_duration)
        {
            ui_demo_timer += Time.deltaTime;
        }
        else if (ui_demo_enabled && ui_demo_timer >= ui_demo_duration)
        {
            ui_demo_enabled = false;
            if (ui_show_intro_text) {
                AddToTextQueue("This game is in development; there may be major bugs or issues!", Color.red);
                AddToTextQueue(" -- ALPHA BUILD VERSION 0.17.1 --", Color.white);
                AddToTextQueue("Step in the square to join the game!", Color.white);
                if (gameController != null && gameController.local_ppp_options != null) { gameController.local_ppp_options.RefreshAllOptions(); }
                if (gameController != null && Networking.IsMaster) { gameController.ResetGameOptionsToDefault(false); }
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
        else if ((round_ready || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator || playerAttributes.ply_team < 0) && PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(false); }
        else if (!(round_ready || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator || playerAttributes.ply_team < 0) && !PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(true); }

        float TimerValue = gameController.round_length - gameController.round_timer + 1.0f;
        string TimerText = Mathf.FloorToInt(TimerValue).ToString();
        PTSTimerTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        if (gameController.round_state == (int)round_state_name.Start) { TimerText = ""; }
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
            else { PTSTimer.color = Color.white; }
        }
        PTSTimer.text = TimerText;
        

        var DamageText = Mathf.RoundToInt(playerAttributes.ply_dp) + "%";
        if (gameController.round_state == (int)round_state_name.Start) { DamageText = ""; }
        PTSDamage.text = DamageText;
        PTSDamage.color = new Color(Mathf.Min(Mathf.Max(0.2f, 1.0f - ((playerAttributes.ply_dp - 100) / 100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp/100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp / 100)), 1.0f), 1.0f);

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
        if (gameController.round_state == (int)round_state_name.Start) { AttackText = ""; }
        if (AttackVal > gameController.plysettings_atk) { PTSAttack.color = new Color32(60, 255, 60, 255); }
        else if (AttackVal < gameController.plysettings_atk) { PTSAttack.color = new Color32(255, 60, 60, 255); }
        else { PTSAttack.color = new Color32(255, 255, 255, 255); }
        PTSAttack.text = AttackText;

        var DefenseVal = Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var DefenseText = DefenseVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { DefenseText = ""; }
        if (DefenseVal > gameController.plysettings_def) { PTSDefense.color = new Color32(60, 255, 60, 255); }
        else if (DefenseVal < gameController.plysettings_def) { PTSDefense.color = new Color32(255, 60, 60, 255); }
        else { PTSDefense.color = new Color32(255, 255, 255, 255); }
        PTSDefense.text = DefenseText;

        if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { PTSTeamCBSpriteImage.enabled = true; }
        else { PTSTeamCBSpriteImage.enabled = false;  }
        PTSTeamFlagImage.sprite = PTSFlagSprite;
        PTSTeamFlagImage.enabled = !PTSTeamCBSpriteImage.enabled;
        PTSTeamPoleImage.enabled = PTSTeamFlagImage.enabled;

        if (playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gameController.team_colors.Length) 
        { 
            if (gameController.option_teamplay) 
            { 
                PTSTeamFlagImage.color = gameController.team_colors[playerAttributes.ply_team];
            }
            else 
            { 
                PTSTeamFlagImage.color = new Color32(255,255,255,255);
            }
            PTSTeamCBSpriteImage.sprite = gameController.team_sprites[playerAttributes.ply_team];
            PTSTeamCBSpriteImage.color = PTSTeamFlagImage.color;
        }

        string FlagText = ""; string PlacementText = "";
        PTSTeamText.color = Color.white;
        if (gameController.round_state != (int)round_state_name.Start && gameController.team_count >= 0 && playerAttributes.ply_team >= 0 && gamevars_leaderboard_arr != null
            && gamevars_local_team_lives.Length > playerAttributes.ply_team && gamevars_local_team_points.Length > playerAttributes.ply_team)
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
                FlagText = gamevars_local_team_lives[0].ToString() + " Alive";
            }
            // To-do: If this is King of the Hill, display the total capture time remaining
            else if (gameController.option_gamemode == (int)gamemode_name.ENUM_LENGTH)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n1st:\n" + Mathf.RoundToInt(gameController.option_gm_goal - (gamevars_progress_arr[0] / gameController.koth_decimal_division)).ToString() + "s";
                }
            }
            // If we are in Fitting In, display the leader's death count
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                PlacementText = rank_str;
                if (local_rank == 0) { FlagText = ""; }
                else
                {
                    FlagText = "\n\n1st:\n" + gamevars_progress_arr[0].ToString() + " Falls";
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
                if (gamevars_local_team_points.Length > 1) { LivesText = gamevars_local_team_points[1].ToString(); }
                PTSLives.color = Color.white;
                PTSLivesImage.color = Color.white;
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            // If this is King of the Hill, display the total capture time remaining
            else if (gameController.option_gamemode == (int)gamemode_name.ENUM_LENGTH)
            {
                float timeLeft;
                if (gameController.option_teamplay && playerAttributes.ply_team < gamevars_local_team_points.Length && gamevars_local_team_points.Length > 0 && playerAttributes.ply_team >= 0) { timeLeft = Mathf.RoundToInt(gameController.option_gm_goal - (float)((float)gamevars_local_team_points[playerAttributes.ply_team] / gameController.koth_decimal_division)); }
                else { timeLeft = Mathf.RoundToInt(gameController.option_gm_goal - (float)((float)playerAttributes.ply_points / gameController.koth_decimal_division)); }
                LivesText = timeLeft.ToString();
                float timeRatio = Mathf.Clamp((float)(timeLeft / (float)gameController.option_gm_goal), 0.0f, 1.0f);
                PTSLives.color = new Color(timeRatio, 1.0f, timeRatio / 1.5f, 1.0f);
                PTSLivesImage.color = PTSTeamFlagImage.color;
                PTSLivesImage.sprite = PTSTimerImage;
            }
            // If this is Fitting In, display the number of deaths
            else if (gameController.option_gamemode == (int)gamemode_name.FittingIn)
            {
                if (gameController.option_teamplay && gamevars_local_team_deaths != null && gamevars_local_team_deaths.Length > playerAttributes.ply_team && playerAttributes.ply_team >= 0) { LivesText = Mathf.RoundToInt(gamevars_local_team_deaths[playerAttributes.ply_team]).ToString(); }
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
                if (plyweapon.weapon_charge_duration > 0.0f) { offsetPct = System.Convert.ToSingle(plyweapon.weapon_charge_timer / plyweapon.weapon_charge_duration);}
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
    }

    private void FixedUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        var heightUI = 0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        var scaleUI = 1.0f;
        if (gameController != null && gameController.local_ppp_options != null) {
            PPP_Options ppp_options = gameController.local_ppp_options;
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
            PTSCanvas.sizeDelta = new Vector2(500 * ppp_options.ui_stretch, 300 * ppp_options.ui_separation);
            //ppp_options.ui_separation * (5.0f / 3.0f)
            //ppp_options.ui_scale
        }
        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        float plyMagInForward = Vector3.Dot(Networking.LocalPlayer.GetVelocity(), plyForward);
        Vector3 velAdd = Vector3.zero;

        if (playerAttributes != null && plyMagInForward > 0)
        {
            velAdd = 0.0095f * plyMagInForward * plyForward;
            if (playerAttributes.ply_scale < 1.0f && Networking.LocalPlayer.IsUserInVR()) { velAdd /= (playerAttributes.ply_scale / 0.9f); }
        }
        //if (Networking.LocalPlayer.IsUserInVR()) { velAdd *= 2.0f; }

        //if (!Networking.LocalPlayer.IsUserInVR()) { scaleUI *= 0.5f; }
        //else { scaleUI *= 0.5f; }
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * heightUI * scaleUI;
        transform.SetPositionAndRotation(
            Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (plyForward * heightUI) + velAdd
            , Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation
            );
    }

}
