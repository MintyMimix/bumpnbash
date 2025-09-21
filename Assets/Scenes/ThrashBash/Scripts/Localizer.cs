
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public enum language_type_name
{
    English, French, Japanese, SpanishLatin, SpanishEurope
}

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Localizer : UdonSharpBehaviour
{
    [Header("Language Objects")]
    [SerializeField] private TMP_Text lang_en_json;
    [SerializeField] private TMP_Text lang_fr_json;
    [SerializeField] private TMP_Text lang_jp_json;
    [SerializeField] private TMP_Text lang_es_s_json;
    [SerializeField] private TMP_Text lang_es_l_json;

    [NonSerialized] public int language_type = (int)language_type_name.English;
    [NonSerialized] public DataDictionary lang_dict;

    [Header("Dynamic Object References")]
    [SerializeField] public GameController gameController;

    [Header("Static Object References")]
    [SerializeField] private TMP_Text RoundHeaderText;
    [SerializeField] private TMP_Text RoundOptionsDefaultButton;
    [SerializeField] private TMP_Text RoundToggleMasterLock;
    [SerializeField] private TMP_Text UITabChildMode;
    [SerializeField] private TMP_Text UITabChildPlayer;
    [SerializeField] private TMP_Text UITabChildAdv;
    [SerializeField] private TMP_Text RoundTimerText;
    [SerializeField] private TMP_Text RoundDamageInput;
    [SerializeField] private TMP_Text RoundScaleInput;
    [SerializeField] private TMP_Text RoundAttackInput;
    [SerializeField] private TMP_Text RoundDefenseInput;
    [SerializeField] private TMP_Text RoundSpeedInput;
    [SerializeField] private TMP_Text RoundGravInput;
    [SerializeField] private TMP_Text RoundWeaponToggle;
    [SerializeField] private TMP_Text RoundPowerupToggle;
    [SerializeField] private TMP_Text RoundAdvRespawnDuration;
    [SerializeField] private TMP_Text RoundPowerupBombDebuffToggle;
    [SerializeField] private TMP_Text RoundAdvItemDuration;
    [SerializeField] private TMP_Text RoundAdvItemFrequency;
    [SerializeField] private TMP_Text SpecialLabel;
    [SerializeField] private TMP_Text RoundSpecialScaleInput;
    [SerializeField] private TMP_Text RoundSpecialAttackInput;
    [SerializeField] private TMP_Text RoundSpecialDefenseInput;
    [SerializeField] private TMP_Text RoundSpecialSpeedInput;
    [SerializeField] private TMP_Text RoundButtonTeamAutoAssign;
    [SerializeField] private TMP_Text RoundToggleTeamplay;
    [SerializeField] private TMP_Text RoundTogglePersonalTeam;
    [SerializeField] private TMP_Text HighlightHeader;
    [SerializeField] private TMP_Text HighlightTutorialText;
    [SerializeField] private TMP_Text HighlightText_1;
    [SerializeField] private TMP_Text HighlightText_2;
    [SerializeField] private TMP_Text HighlightText_3;
    [SerializeField] private TMP_Text HighlightText_4;
    [SerializeField] private TMP_Text HighlightText_5;
    [SerializeField] private TMP_Text HighlightText_6;
    [SerializeField] private TMP_Text MegaphoneResetButton;
    [SerializeField] private TMP_Text TutorialHeader;
    [SerializeField] private TMP_Text TutorialBasicsHeader;
    [SerializeField] private TMP_Text TutorialBasicsText;
    [SerializeField] private TMP_Text TutorialUIHeader;
    [SerializeField] private TMP_Text TutorialGamemodesHeader;
    [SerializeField] private TMP_Text TutorialBasicsImageTxt;
    [SerializeField] private TMP_Text TutorialBasicsImageTxt_1_0;
    [SerializeField] private TMP_Text TutorialBasicsImageTxt_1_1;
    [SerializeField] private TMP_Text TutorialUIGridTxt_0;
    [SerializeField] private TMP_Text TutorialUIGridTxt_1;
    [SerializeField] private TMP_Text TutorialUIGridTxt_2;
    [SerializeField] private TMP_Text TutorialUIGridTxt_3;
    [SerializeField] private TMP_Text TutorialUIGridTxt_4;
    [SerializeField] private TMP_Text TutorialUIGridTxt_5;
    [SerializeField] private TMP_Text TutorialUIGridTxt_6;
    [SerializeField] private TMP_Text WarningText;
    [SerializeField] private TMP_Text WarningText_1;
    [SerializeField] private TMP_Text WarningText_2;
    [SerializeField] private TMP_Text AcknowledgeButton;
    [SerializeField] private TMP_Text BuildHeader;
    [SerializeField] private TMP_Text TrelloHeader;
    [SerializeField] private TMP_Text CreditsHeader;
    [SerializeField] private TMP_Text CreditsMusicHeader;
    [SerializeField] private TMP_Text CreditsContributorsHeader;
    [SerializeField] private TMP_Text CreditsTestersHeader;
    [SerializeField] private TMP_Text CreditsMusicText;
    [SerializeField] private TMP_Text CreditsMintyMimixText;
    [SerializeField] private TMP_Text CreditsMappersText;
    [SerializeField] private TMP_Text CreditsMappersText_1;
    [SerializeField] private TMP_Text CreditsVAText;
    [SerializeField] private TMP_Text WeaponDemoHeader;
    [SerializeField] private TMP_Text PowerupDemoHeader;
    [SerializeField] private TMP_Text WarningText_3;
    [SerializeField] private TMP_Text WarningText_4;

    void Start()
    {
        SetLangDict();
    }

    public void SetLangDict()
    {
        lang_dict = CreateLangDict(language_type);
        SetStaticText();
    }

    private DataDictionary CreateLangDict(int language_type)
    {
        string json = "";
        if (language_type == (int)language_type_name.French && lang_fr_json != null) { json = lang_fr_json.text; }
        else if (language_type == (int)language_type_name.Japanese && lang_jp_json != null) { json = lang_jp_json.text; }
        else if (language_type == (int)language_type_name.SpanishLatin && lang_es_l_json != null) { json = lang_es_l_json.text; }
        else if (language_type == (int)language_type_name.SpanishEurope && lang_es_s_json != null) { json = lang_es_s_json.text; }
        else if (lang_en_json != null) { json = lang_en_json.text; }

        if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
        {
            // Deserialization succeeded! Let's figure out what we've got.
            if (result.TokenType == TokenType.DataDictionary)
            {
                Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                return result.DataDictionary;
            }
            else if (result.TokenType == TokenType.DataList)
            {
                Debug.Log($"Successfully deserialized as a list with {result.DataList.Count} items.");
            }
            else
            {
                // This should not be possible. If TryDeserializeFromJson returns true, this it *must* be either a dictionary or a list.
            }
        }
        else
        {
            // Deserialization failed. Let's see what the error was.
            Debug.Log($"Failed to Deserialize json {json} - {result.ToString()}");
        }

        return null;
    }

    private string TryGetText(string key, string fallback = "[?]")
    {
        if (lang_dict == null) { return fallback; }
        // This approach has a type check built in! It's functionally the same, but streamlined.
        DataToken value;
        if (lang_dict.TryGetValue(key.ToUpper(), TokenType.String, out value))
        {
            return value.String;
        }
        else
        {
            return fallback;
        }
    }

    public string FetchText(string key, string fallback="[?]", string argument_0 = "", string argument_1="", string argument_2="", string argument_3="", string argument_4="")
    {
        return TryGetText(key, fallback).Replace("$ARG0", argument_0).Replace("$ARG1", argument_1).Replace("$ARG2", argument_2).Replace("$ARG3", argument_3).Replace("$ARG4", argument_4);
    }

    public void RefreshGamemodeText()
    {
        gameController.round_option_names[(int)gamemode_name.Survival] = FetchText("GAMEMODE_NAME_SURVIVAL", "Survival");
        gameController.round_option_names[(int)gamemode_name.Clash] = FetchText("GAMEMODE_NAME_CLASH", "Clash");
        gameController.round_option_names[(int)gamemode_name.BossBash] = FetchText("GAMEMODE_NAME_BOSSBASH", "Boss Bash");
        gameController.round_option_names[(int)gamemode_name.Infection] = FetchText("GAMEMODE_NAME_INFECTION", "Infection");
        gameController.round_option_names[(int)gamemode_name.FittingIn] = FetchText("GAMEMODE_NAME_FITTINGIN", "Fitting In");
        gameController.round_option_names[(int)gamemode_name.KingOfTheHill] = FetchText("GAMEMODE_NAME_KOTH", "King of the Hill");
        gameController.round_option_descriptions[(int)gamemode_name.Survival] = FetchText("GAMEMODE_DESCRIPTION_SURVIVAL", "You have $LIVES lives! Last one standing wins!");
        gameController.round_option_descriptions[(int)gamemode_name.Clash] = FetchText("GAMEMODE_DESCRIPTION_CLASH", "First to $POINTS_A KOS wins! ");
        gameController.round_option_descriptions[(int)gamemode_name.BossBash] = FetchText("GAMEMODE_DESCRIPTION_BOSSBASH", "KO The Big Boss $LIVES times before they earn $POINTS_A KOS!");
        gameController.round_option_descriptions[(int)gamemode_name.Infection] = FetchText("GAMEMODE_DESCRIPTION_INFECTION", "Survivors must stay alive for $TIMER seconds! If a survivor is KO'd, they become Infected!");
        gameController.round_option_descriptions[(int)gamemode_name.FittingIn] = FetchText("GAMEMODE_DESCRIPTION_FITTINGIN", "Every time you are KO'd, you grow! Become too large, and you lose!");
        gameController.round_option_descriptions[(int)gamemode_name.KingOfTheHill] = FetchText("GAMEMODE_DESCRIPTION_KOTH", "Claim and hold the capture zones for $POINTS_A seconds!");
        gameController.local_gamemode_count = 0; // Reset the gamemode dropdown
        gameController.local_weapon_count = 0;  // Reset the weapon dropdown
        if (gameController.local_plyAttr != null) { gameController.local_plyAttr.SetupTutorialMessages(); }
        if (gameController.local_ppp_options != null) { gameController.local_ppp_options.ResetMusicNames(); }

        for (int i = 0; i < gameController.team_names.Length; i++)
        {
            gameController.team_names[i] = FetchText("TEAM_NAME_" + i.ToString(), gameController.team_names[i]);
        }

    }

    public void RefreshMapscriptText()
    {
        if (gameController.mapscript_list == null) { return; }
        for (int i = 0; i < gameController.mapscript_list.Length; i++)
        {
            gameController.mapscript_list[i].map_name = FetchText(gameController.mapscript_list[i].map_localization_name_key, gameController.mapscript_list[i].map_name);
            gameController.mapscript_list[i].map_description = FetchText(gameController.mapscript_list[i].map_localization_description_key, gameController.mapscript_list[i].map_description);
        }
    }


    public void SetStaticText()
    {
        // The big one. This is what will change the text on EVERYTHING.
        RefreshGamemodeText();
        RefreshMapscriptText();
        gameController.RefreshSetupUI();
        gameController.megaphone.GetComponent<VRC_Pickup>().InteractionText = FetchText("PICKUP_MEGAPHONE", gameController.megaphone.GetComponent<VRC_Pickup>().InteractionText);
        if (gameController.local_plyweapon != null) { gameController.local_plyweapon.pickup_component.InteractionText = FetchText("PICKUP_WEAPON", gameController.local_plyweapon.pickup_component.InteractionText); }
        if (gameController.local_secondaryweapon != null) { gameController.local_secondaryweapon.pickup_component.InteractionText = FetchText("PICKUP_WEAPON", gameController.local_secondaryweapon.pickup_component.InteractionText); }
        if (gameController.local_ppp_options != null) { gameController.local_ppp_options.RefreshAllOptions(false); }

        RoundHeaderText.text = FetchText("GAMESETTINGS_HEADER_MAIN", RoundHeaderText.text);
        RoundOptionsDefaultButton.text = FetchText("GAMESETTINGS_RESET", RoundOptionsDefaultButton.text);
        RoundToggleMasterLock.text = FetchText("GAMESETTINGS_MASTER_TOGGLE", RoundToggleMasterLock.text);
        UITabChildMode.text = FetchText("GAMESETTINGS_TAB_GAMEMODE", UITabChildMode.text);
        UITabChildPlayer.text = FetchText("GAMESETTINGS_TAB_STARTING", UITabChildPlayer.text);
        UITabChildAdv.text = FetchText("GAMESETTINGS_TAB_ADVANCED", UITabChildAdv.text);
        RoundTimerText.text = FetchText("GAMESETTINGS_GAMEMODE_TIMELIMIT", RoundTimerText.text);
        RoundDamageInput.text = FetchText("GAMESETTINGS_STARTING_DAMAGE", RoundDamageInput.text);
        RoundScaleInput.text = FetchText("GAMESETTINGS_STARTING_SIZE", RoundScaleInput.text);
        RoundAttackInput.text = FetchText("GAMESETTINGS_STARTING_ATTACK", RoundAttackInput.text);
        RoundDefenseInput.text = FetchText("GAMESETTINGS_STARTING_DEFENSE", RoundDefenseInput.text);
        RoundSpeedInput.text = FetchText("GAMESETTINGS_STARTING_SPEED", RoundSpeedInput.text);
        RoundGravInput.text = FetchText("GAMESETTINGS_STARTING_GRAVITY", RoundGravInput.text);
        RoundWeaponToggle.text = FetchText("GAMESETTINGS_STARTING_WEAPON_TOGGLE", RoundWeaponToggle.text);
        RoundPowerupToggle.text = FetchText("GAMESETTINGS_STARTING_POWERUP_TOGGLE", RoundPowerupToggle.text);
        RoundAdvRespawnDuration.text = FetchText("GAMESETTINGS_ADVANCED_RESPAWN", RoundAdvRespawnDuration.text);
        RoundPowerupBombDebuffToggle.text = FetchText("GAMESETTINGS_ADVANCED_THROWABLEBEHAVIOR", RoundPowerupBombDebuffToggle.text);
        RoundAdvItemDuration.text = FetchText("GAMESETTINGS_ADVANCED_PICKUP_DURATION", RoundAdvItemDuration.text);
        RoundAdvItemFrequency.text = FetchText("GAMESETTINGS_ADVANCED_PICKUP_FREQUENCY", RoundAdvItemFrequency.text);
        SpecialLabel.text = FetchText("GAMESETTINGS_ADVANCED_HEADER_BOSS", SpecialLabel.text);
        RoundSpecialScaleInput.text = FetchText("GAMESETTINGS_ADVANCED_BOSS_SCALE", RoundSpecialScaleInput.text);
        RoundSpecialAttackInput.text = FetchText("GAMESETTINGS_ADVANCED_BOSS_ATTACK", RoundSpecialAttackInput.text);
        RoundSpecialDefenseInput.text = FetchText("GAMESETTINGS_ADVANCED_BOSS_DEFENSE", RoundSpecialDefenseInput.text);
        RoundSpecialSpeedInput.text = FetchText("GAMESETTINGS_ADVANCED_BOSS_SPEED", RoundSpecialSpeedInput.text);
        RoundButtonTeamAutoAssign.text = FetchText("GAMESETTINGS_TEAM_AUTOASSIGN", RoundButtonTeamAutoAssign.text);
        RoundToggleTeamplay.text = FetchText("GAMESETTINGS_TEAM_TEAMPLAY_TOGGLE", RoundToggleTeamplay.text);
        RoundTogglePersonalTeam.text = FetchText("GAMESETTINGS_TEAM_SELFSELECT_TOGGLE", RoundTogglePersonalTeam.text);
        HighlightHeader.text = FetchText("HIGHLIGHTS_HEADER", HighlightHeader.text);
        HighlightTutorialText.text = FetchText("HIGHLIGHTS_TUTORIAL", HighlightTutorialText.text);
        HighlightText_1.text = FetchText("HIGHLIGHTS_LABEL_0", HighlightText_1.text);
        HighlightText_2.text = FetchText("HIGHLIGHTS_LABEL_1", HighlightText_2.text);
        HighlightText_3.text = FetchText("HIGHLIGHTS_LABEL_2", HighlightText_3.text);
        HighlightText_4.text = FetchText("HIGHLIGHTS_LABEL_3", HighlightText_4.text);
        HighlightText_5.text = FetchText("HIGHLIGHTS_LABEL_4", HighlightText_5.text);
        HighlightText_6.text = FetchText("HIGHLIGHTS_LABEL_5", HighlightText_6.text);
        MegaphoneResetButton.text = FetchText("MEGAPHONE_RESET", MegaphoneResetButton.text);
        TutorialHeader.text = FetchText("TUTORIAL_HEADER_MAIN", TutorialHeader.text);
        TutorialBasicsHeader.text = FetchText("TUTORIAL_HEADER_BASICS", TutorialBasicsHeader.text);
        TutorialBasicsText.text = FetchText("TUTORIAL_LABEL_TRAININGDIRECT", TutorialBasicsText.text);
        TutorialUIHeader.text = FetchText("TUTORIAL_HEADER_UI", TutorialUIHeader.text);
        TutorialGamemodesHeader.text = FetchText("TUTORIAL_HEADER_GAMEMODE", TutorialGamemodesHeader.text);
        TutorialBasicsImageTxt.text = FetchText("TUTORIAL_BASICS_IMAGE_KO", TutorialBasicsImageTxt.text);
        TutorialBasicsImageTxt_1_0.text = FetchText("TUTORIAL_BASICS_IMAGE_DAMAGE", TutorialBasicsImageTxt_1_0.text);
        TutorialBasicsImageTxt_1_1.text = FetchText("TUTORIAL_BASICS_IMAGE_FORCE", TutorialBasicsImageTxt_1_1.text);
        TutorialUIGridTxt_0.text = FetchText("TUTORIAL_UI_IMAGE_RANKING", TutorialUIGridTxt_0.text);
        TutorialUIGridTxt_1.text = FetchText("TUTORIAL_UI_IMAGE_DAMAGE", TutorialUIGridTxt_1.text);
        TutorialUIGridTxt_2.text = FetchText("TUTORIAL_UI_IMAGE_TIME", TutorialUIGridTxt_2.text);
        TutorialUIGridTxt_3.text = FetchText("TUTORIAL_UI_IMAGE_SCORE", TutorialUIGridTxt_3.text);
        TutorialUIGridTxt_4.text = FetchText("TUTORIAL_UI_IMAGE_BUFFS", TutorialUIGridTxt_4.text);
        TutorialUIGridTxt_5.text = FetchText("TUTORIAL_UI_IMAGE_WEAPON", TutorialUIGridTxt_5.text);
        TutorialUIGridTxt_6.text = FetchText("TUTORIAL_UI_IMAGE_POWERUP", TutorialUIGridTxt_6.text);
        WarningText.text = FetchText("JOINWARNING_DEVELOPMENT_HEADER", WarningText.text);
        WarningText_1.text = FetchText("JOINWARNING_DEVELOPMENT_DESCRIPTION", WarningText_1.text);
        WarningText_2.text = FetchText("JOINWARNING_DEVELOPMENT_FINALWARNING", WarningText_2.text);
        AcknowledgeButton.text = FetchText("JOINWARNING_ACKNOWLEDGE", AcknowledgeButton.text);
        BuildHeader.text = FetchText("AD_BUILD", BuildHeader.text, GlobalHelperFunctions.BUILD_VERSION);
        TrelloHeader.text = FetchText("AD_TRELLO", TrelloHeader.text);
        CreditsHeader.text = FetchText("CREDITS_HEADER_MAIN", CreditsHeader.text);
        CreditsMusicHeader.text = FetchText("CREDITS_HEADER_MUSIC", CreditsMusicHeader.text);
        CreditsContributorsHeader.text = FetchText("CREDITS_HEADER_CONTRIBUTORS", CreditsContributorsHeader.text);
        CreditsTestersHeader.text = FetchText("CREDITS_HEADER_TESTERS", CreditsTestersHeader.text);
        CreditsMusicText.text = FetchText("CREDITS_DESCRIPTION_MUSIC", CreditsMusicText.text);
        CreditsMintyMimixText.text = FetchText("CREDITS_DESCRIPTION_CONTRIBUTORS_0", CreditsMintyMimixText.text);
        CreditsMappersText.text = FetchText("CREDITS_DESCRIPTION_CONTRIBUTORS_1", CreditsMappersText.text);
        CreditsMappersText_1.text = FetchText("CREDITS_DESCRIPTION_CONTRIBUTORS_2", CreditsMappersText_1.text);
        CreditsVAText.text = FetchText("CREDITS_DESCRIPTION_CONTRIBUTORS_3", CreditsVAText.text);
        WeaponDemoHeader.text = FetchText("TRAINING_HEADER_WEAPON", WeaponDemoHeader.text);
        PowerupDemoHeader.text = FetchText("TRAINING_HEADER_POWERUP", PowerupDemoHeader.text);
        WarningText_3.text = FetchText("LOCALOPTIONS_LANGUAGE_HEADER", WarningText_3.text);
        WarningText_4.text = FetchText("LOCALOPTIONS_LANGUAGE_WARNING", WarningText_4.text);

        if (gameController != null && gameController.local_ppp_options != null)
        {
            PPP_LocalizerContainer container = gameController.local_ppp_options.txt_container;
            container.PPPTitle.text = FetchText("LOCALOPTIONS_HEADER_MAIN", container.PPPTitle.text);
            container.PPPPanel_UITabChild_0.text = FetchText("LOCALOPTIONS_TAB_UI", container.PPPPanel_UITabChild_0.text);
            container.PPPPanel_UITabChild_1.text = FetchText("LOCALOPTIONS_TAB_SOUND", container.PPPPanel_UITabChild_1.text);
            container.PPPPanel_UITabChild_2.text = FetchText("LOCALOPTIONS_TAB_GAME", container.PPPPanel_UITabChild_2.text);
            container.PPPPanel_UITabChild_3.text = FetchText("LOCALOPTIONS_TAB_LANGUAGE", container.PPPPanel_UITabChild_3.text);
            container.PPPWristNoneToggle.text = FetchText("LOCALOPTIONS_UI_FRONT", container.PPPWristNoneToggle.text);
            container.PPPWristLToggle.text = FetchText("LOCALOPTIONS_UI_LEFTWRIST", container.PPPWristLToggle.text);
            container.PPPWristRToggle.text = FetchText("LOCALOPTIONS_UI_RIGHTWIRST", container.PPPWristRToggle.text);
            container.PPPUIInvertedToggle.text = FetchText("LOCALOPTIONS_UI_INVERTED", container.PPPUIInvertedToggle.text);
            container.PPPUIResetButton.text = FetchText("LOCALOPTIONS_UI_RESET", container.PPPUIResetButton.text);
            container.PPPSoundGlobalHeader.text = FetchText("LOCALOPTIONS_SOUND_VOLUME_HEADER", container.PPPSoundGlobalHeader.text);
            container.PPPMusicOptionsCaption.text = FetchText("LOCALOPTIONS_SOUND_MUSICOVERRIDE_HEADER", container.PPPMusicOptionsCaption.text);
            container.PPPMusicOptionsWarning.text = FetchText("LOCALOPTIONS_SOUND_MUSICOVERRIDE_WARNING", container.PPPMusicOptionsWarning.text);
            container.PPPVOHeader.text = FetchText("LOCALOPTIONS_SOUND_VO_HEADER", container.PPPVOHeader.text);
            container.PPPVOEventAToggle.text = FetchText("LOCALOPTIONS_SOUND_VO_ROUND_TOGGLE", container.PPPVOEventAToggle.text);
            container.PPPVOEventBToggle.text = FetchText("LOCALOPTIONS_SOUND_VO_TUTORIAL_TOGGLE", container.PPPVOEventBToggle.text);
            container.PPPVOEventCToggle.text = FetchText("LOCALOPTIONS_SOUND_VO_KOSTREAK_TOGGLE", container.PPPVOEventCToggle.text);
            container.PPPTutorialCanvas.text = FetchText("LOCALOPTIONS_TUTORIAL_DESCRIPTION", container.PPPTutorialCanvas.text);
            container.PPPTutorialResetButtonTxt.text = FetchText("LOCALOPTIONS_TUTORIAL_RESET_BUTTON", container.PPPTutorialResetButtonTxt.text);
            container.PPPSpectatorToggle.text = FetchText("LOCALOPTIONS_GAME_SPECTATE_TOGGLE", container.PPPSpectatorToggle.text);
            container.PPPHitboxToggle.text = FetchText("LOCALOPTIONS_GAME_PLAYERHITBOX_TOGGLE", container.PPPHitboxToggle.text);
            container.PPPHurtboxToggle.text = FetchText("LOCALOPTIONS_GAME_WEAPONHURTBOX_TOGGLE", container.PPPHurtboxToggle.text);
            container.PPPParticleToggle.text = FetchText("LOCALOPTIONS_GAME_PARTICLE_TOGGLE", container.PPPParticleToggle.text);
            container.PPPHapticsToggle.text = FetchText("LOCALOPTIONS_GAME_HAPTICS_TOGGLE", container.PPPHapticsToggle.text);
            container.PPPMotionSicknessToggleFloor.text = FetchText("LOCALOPTIONS_GAME_MOTIONSICKNESS_FLOOR_TOGGLE", container.PPPMotionSicknessToggleFloor.text);
            container.PPPMotionSicknessToggleCage.text = FetchText("LOCALOPTIONS_GAME_MOTIONSICKNESS_CAGE_TOGGLE", container.PPPMotionSicknessToggleCage.text);
            container.PPPColorblindHeader.text = FetchText("LOCALOPTIONS_GAME_COLORBLIND_HEADER", container.PPPColorblindHeader.text);
            container.PPPColorblindToggle.text = FetchText("LOCALOPTIONS_GAME_COLORBLIND_SYMBOL_TOGGLE", container.PPPColorblindToggle.text);
        }
    }
}
