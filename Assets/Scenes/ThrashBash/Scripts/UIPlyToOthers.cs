
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UIPlyToOthers : UdonSharpBehaviour
{
    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject PTOTopPanel;
    [SerializeField] public TMP_Text PTOInfo;
    [SerializeField] public TMP_Text PTOLives;
    [SerializeField] public UnityEngine.UI.Image PTOLivesImage;
    [SerializeField] public Sprite PTOLivesSprite;
    [SerializeField] public Sprite PTOPointsSprite;
    [SerializeField] public Sprite PTODeathsSprite;
    [SerializeField] public TMP_Text PTODamage;
    [SerializeField] public TMP_Text PTOAttack;
    [SerializeField] public TMP_Text PTODefense;
    [SerializeField] public TMP_Text PTOInvul;
    [SerializeField] public GameObject PTOTeamFlag;
    [SerializeField] public UnityEngine.UI.Image PTOTeamFlagImage;
    [SerializeField] public UnityEngine.UI.Image PTOTeamPoleImage;
    [SerializeField] public TMP_Text PTOTeamText;
    [SerializeField] public UnityEngine.UI.Image PTOTeamCBSpriteImage;
    [SerializeField] public GameObject PTOVictoryStar;

    [NonSerialized] public PlayerAttributes playerAttributes;
    // Cached stats for UI update referencing
    [NonSerialized] public int cached_respawn_timer_int = -1; 
    [NonSerialized] public int cached_team = -1;
    [NonSerialized] public float cached_dp = -1.0f; 
    [NonSerialized] public float cached_scale = -1.0f; 
    [NonSerialized] public float cached_atk = -1.0f;
    [NonSerialized] public float cached_def = -1.0f;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }
    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        owner = newOwner;
        playerAttributes = gameController.FindPlayerAttributes(newOwner);
    }

    private void Update()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        if (owner == Networking.LocalPlayer || owner == null || gameController.local_uiplytoself == null) { return; }

        UIPlyToSelf ref_uiplytoself = gameController.local_uiplytoself;

        // Sort out better without all the debug
        bool round_ready = gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Queued || gameController.round_state == (int)round_state_name.Loading || gameController.round_state == (int)round_state_name.Over;
        round_ready = round_ready && !playerAttributes.ply_training;
        if (round_ready && PTOTopPanel.activeInHierarchy) { PTOTopPanel.SetActive(false); }
        else if (!round_ready && !PTOTopPanel.activeInHierarchy) { PTOTopPanel.SetActive(true); }
        
        if (gameController.local_tick_timer == 0.0f) // We use 0.0f since it will never reach a point where the GameController will output at the end time for another object, so this is a shorthand for "the frame after its LocalPerTickUpdate()"
        {
            LocalPerTickUpdate();
        }
        if (gameController.local_uiplytoself != null && gameController.local_uiplytoself.ui_check_gamevars_timer == 0.0f) // Ditto for UIPlyToSelf's game variables refresh rate
        {
            UI_Lives();
        }
    }
    
    private void LocalPerTickUpdate() 
    {
        // Checked cached variables; if there is a mismatch, update the UI element accordingly
        if (playerAttributes == null) { return; }
        if (cached_team != playerAttributes.ply_team) { UI_Flag(); cached_team = playerAttributes.ply_team; }
        if (cached_scale != playerAttributes.ply_scale) {UI_Attack(); UI_Defense(); cached_scale = playerAttributes.ply_scale; }
        if (cached_atk != playerAttributes.ply_atk) { UI_Attack(); cached_atk = playerAttributes.ply_atk; }
        if (cached_def != playerAttributes.ply_def) { UI_Defense(); cached_def = playerAttributes.ply_def; }
        if (cached_dp != playerAttributes.ply_dp) { UI_Damage(); cached_dp = playerAttributes.ply_dp; }
        if (cached_respawn_timer_int != Mathf.CeilToInt(playerAttributes.ply_respawn_timer)) { UI_Damage(); cached_respawn_timer_int = Mathf.RoundToInt(playerAttributes.ply_respawn_timer); }
    }
    
    public void ResetCache()
    {
        cached_team = -1;
        cached_atk = -1;
        cached_def = -1;
        cached_dp = -1;
        cached_respawn_timer_int = -1;
        cached_scale = -1;
    }
    
    public void UI_Damage() 
    {
        var DamageText = Mathf.RoundToInt(playerAttributes.ply_dp) + "%";
        if (gameController.round_state == (int)round_state_name.Start) { DamageText = ""; }
        PTODamage.text = DamageText;
        PTODamage.color = new Color(Mathf.Min(Mathf.Max(0.2f, 1.0f - ((playerAttributes.ply_dp - 100) / 100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp / 100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp / 100)), 1.0f), 1.0f);

        var InvulText = Mathf.Floor(playerAttributes.ply_respawn_duration - playerAttributes.ply_respawn_timer + 1.0f).ToString();
        if (gameController.round_state == (int)round_state_name.Start || playerAttributes.ply_state != (int)player_state_name.Respawning)
        {
            InvulText = "";
            PTOInvul.gameObject.transform.parent.gameObject.SetActive(false);
            PTODamage.gameObject.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            PTOInvul.gameObject.transform.parent.gameObject.SetActive(true);
            PTODamage.gameObject.transform.parent.gameObject.SetActive(false);
        }
        PTOInvul.text = InvulText;
    }
    
    public void UI_Attack() 
    {
        var AttackVal = Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var AttackText = AttackVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { AttackText = ""; }
        if (AttackVal > gameController.plysettings_atk) { PTOAttack.color = new Color32(60, 255, 60, 255); }
        else if (AttackVal < gameController.plysettings_atk) { PTOAttack.color = new Color32(255, 60, 60, 255); }
        else { PTOAttack.color = new Color32(255, 255, 255, 255); }
        PTOAttack.text = AttackText;
    }
    
    public void UI_Defense()
    {
        var DefenseVal = Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var DefenseText = DefenseVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { DefenseText = ""; }
        if (DefenseVal > gameController.plysettings_def) { PTODefense.color = new Color32(60, 255, 60, 255); }
        else if (DefenseVal < gameController.plysettings_def) { PTODefense.color = new Color32(255, 60, 60, 255); }
        else { PTODefense.color = new Color32(255, 255, 255, 255); }
        PTODefense.text = DefenseText;
    }
    
    public void UI_Flag()
    {
        if (playerAttributes.ply_team < gameController.team_colors.Length) 
        {
            int team = Mathf.Max(0, playerAttributes.ply_team);
            if (gameController.option_teamplay)
            {
                PTOTeamFlagImage.color = gameController.team_colors[team];
                PTOTeamCBSpriteImage.color = PTOTeamFlagImage.color;
                PTOTeamCBSpriteImage.sprite = gameController.team_sprites[team];
                if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { PTOTeamCBSpriteImage.enabled = true; }
                else { PTOTeamCBSpriteImage.enabled = false; }
                PTOTeamFlagImage.enabled = !PTOTeamCBSpriteImage.enabled;
                PTOTeamPoleImage.enabled = PTOTeamFlagImage.enabled;
            }
            else
            {
                PTOTeamFlagImage.color = new Color32(255, 255, 255, 255);
                PTOTeamCBSpriteImage.color = PTOTeamFlagImage.color;
                PTOTeamCBSpriteImage.sprite = gameController.team_sprites[0];
                if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { PTOTeamCBSpriteImage.enabled = true; }
                else { PTOTeamCBSpriteImage.enabled = false; }
                PTOTeamFlagImage.enabled = !PTOTeamCBSpriteImage.enabled;
                PTOTeamPoleImage.enabled = PTOTeamFlagImage.enabled;
            }
        }
    }
    
    public void UI_Lives()
    {
        UIPlyToSelf ref_uiplytoself = gameController.local_uiplytoself;

        var LivesText = "";
        if (gameController.round_state == (int)round_state_name.Start) { LivesText = ""; }
        else if (gameController.option_gamemode == (int)gamemode_name.Survival || (playerAttributes.ply_team == 1 && gameController.option_gamemode == (int)gamemode_name.BossBash))
        {
            // If we are in survival mode or are the boss, display lives
            LivesText = Mathf.RoundToInt(playerAttributes.ply_lives).ToString();
            PTOLivesImage.sprite = PTOLivesSprite;
            PTOLivesImage.color = Color.white;
            float livesRatio = (float)((float)playerAttributes.ply_lives / (float)gameController.plysettings_lives);
            if (livesRatio < 1.0f) { livesRatio -= 0.5f * (float)(1.0f / (float)gameController.plysettings_lives); }
            livesRatio = Mathf.Min(Mathf.Max(0.0f, livesRatio), 1.0f);
            PTOLives.color = new Color(1.0f, livesRatio, livesRatio, 1.0f);
        }
        else if ((playerAttributes.ply_team != 1 && gameController.option_gamemode == (int)gamemode_name.BossBash) || gameController.option_gamemode == (int)gamemode_name.FittingIn)
        {
            // If this is Fitting In, display a death counter. For Boss Bash, make sure it's always a personal counter rather than a team counter.
            if (gameController.option_gamemode != (int)gamemode_name.BossBash && gameController.option_teamplay && ref_uiplytoself != null && ref_uiplytoself.gamevars_local_team_deaths.Length > playerAttributes.ply_team && playerAttributes.ply_team >= 0) { LivesText = Mathf.RoundToInt(ref_uiplytoself.gamevars_local_team_deaths[playerAttributes.ply_team]).ToString(); }
            else { LivesText = Mathf.RoundToInt(playerAttributes.ply_deaths).ToString(); }
            PTOLives.color = Color.white;
            PTOLivesImage.sprite = PTODeathsSprite;
            PTOLivesImage.color = Color.white;

            // However, if WE are the boss, make others show their personal KO count instead, so we can prioritize targets accordingly
            if (gameController.local_plyAttr != null && gameController.local_plyAttr.ply_team == 1)
            {
                LivesText = Mathf.RoundToInt(playerAttributes.ply_points).ToString(); 
                PTOLivesImage.sprite = PTOPointsSprite;
                PTOLivesImage.color = PTOTeamFlagImage.color;
            }
         }
        else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            float timeLeft = gameController.option_gm_goal - playerAttributes.ply_points;
            LivesText = timeLeft.ToString();
            PTOLives.color = new Color(
                Mathf.Lerp(((Color)gameController.team_colors_bright[0]).r, ((Color)gameController.team_colors_bright[1]).r, 1.0f - (timeLeft / gameController.option_gm_goal))
                , Mathf.Lerp(((Color)gameController.team_colors_bright[0]).g, ((Color)gameController.team_colors_bright[1]).g, 1.0f - (timeLeft / gameController.option_gm_goal))
                , Mathf.Lerp(((Color)gameController.team_colors_bright[0]).b, ((Color)gameController.team_colors_bright[1]).b, 1.0f - (timeLeft / gameController.option_gm_goal))
                , Mathf.Lerp(((Color)gameController.team_colors_bright[0]).a, ((Color)gameController.team_colors_bright[1]).a, 1.0f - (timeLeft / gameController.option_gm_goal))
                );
            PTOLivesImage.color = PTOTeamFlagImage.color;
            if (ref_uiplytoself != null) { PTOLivesImage.sprite = ref_uiplytoself.PTSTimerImage; }
        }
        else
        {
            // Otherwise, display points
            if (gameController.option_teamplay && ref_uiplytoself != null && ref_uiplytoself.gamevars_local_team_points != null && ref_uiplytoself.gamevars_local_team_points.Length > playerAttributes.ply_team && playerAttributes.ply_team >= 0) { LivesText = Mathf.RoundToInt(ref_uiplytoself.gamevars_local_team_points[playerAttributes.ply_team]).ToString(); }
            else { LivesText = Mathf.RoundToInt(playerAttributes.ply_points).ToString(); }
            PTOLivesImage.sprite = PTOPointsSprite;
            PTOLives.color = Color.white;
            PTOLivesImage.color = PTOTeamFlagImage.color;
        }
        PTOLives.text = LivesText;
    }
    
    public void UI_Victory()
    {
        UIPlyToSelf ref_uiplytoself = gameController.local_uiplytoself;

        // Display victory star if in first place
        if (ref_uiplytoself != null && ref_uiplytoself.gamevars_leaderboard_arr != null && ref_uiplytoself.gamevars_leaderboard_arr.Length > 0)
        {
            if (ref_uiplytoself.GetGameRank(owner.playerId, playerAttributes) == 0
                && gameController.option_gamemode != (int)gamemode_name.BossBash
                && gameController.option_gamemode != (int)gamemode_name.Infection
                //&& gameController.round_state != (int)round_state_name.Start
                ) { PTOVictoryStar.SetActive(true); }
            else if (gameController.option_gamemode == (int)gamemode_name.Infection && playerAttributes.ply_team == 1 && playerAttributes.infection_special != 0)
            {
                // Special infected get a victory star
                PTOVictoryStar.SetActive(true);
            }
            //else if (gameController.option_teamplay && ref_uiplytoself.gamevars_leaderboard_arr[0] == playerAttributes.ply_team) { PTOVictoryStar.SetActive(true); }
            //else if (!gameController.option_teamplay && ref_uiplytoself.gamevars_leaderboard_arr[0] == owner.playerId) { PTOVictoryStar.SetActive(true); }
            else { PTOVictoryStar.SetActive(false); }
        }
    }

    public override void PostLateUpdate()
    {
        if (owner == Networking.LocalPlayer || owner == null) { return; }
        float scaleUI = (owner.GetAvatarEyeHeightAsMeters() / 1.6f);
        float posUI = scaleUI;
        if (gameController != null && gameController.local_ppp_options != null)
        {
            PPP_Options ppp_options = gameController.local_ppp_options;
            scaleUI *= ((0.0f + ppp_options.ui_other_scale) / 1.0f);
            posUI *= ((1.5f + ppp_options.ui_other_scale) / 2.5f);
        }
        transform.SetPositionAndRotation(owner.GetPosition() + new Vector3(0.0f, 2.6f * posUI, 0.0f), Networking.LocalPlayer.GetRotation());
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * scaleUI;
    }

}
