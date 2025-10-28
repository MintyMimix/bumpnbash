
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum voiceover_event_name
{
    Round, Tutorial, KO, ENUM_LENGTH
}
public enum voiceover_round_sfx_name
{
    Start, End, Victory, Defeat, Stalemate, Infection_End_Victory, Infection_End_Defeat
        , Mode_Survival, Mode_Clash, Mode_BossBash, Mode_Infection, Mode_FittingIn, Mode_KingOfTheHill
        , KOTH_CaptureSelf, KOTH_CaptureOther, KOTH_Overtime
        , MapAnnounce
        , ENUM_LENGTH
}
public enum voiceover_tutorial_sfx_name
{
    Mode_Survival, Mode_Clash, Mode_BossBash_A, Mode_BossBash_B, Mode_Infection_A, Mode_Infection_B, Mode_FittingIn, Mode_KingOfTheHill, ENUM_LENGTH
}
public enum voiceover_ko_sfx_name
{
    Time0, Time1, Time2, Time3, Time4, Streak0, Streak1, Streak2, Streak3, Streak4, ENUM_LENGTH
}

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class VoiceoverPack : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public string vo_name;
    [NonSerialized] public AudioClip[][] all_clips; 
    [SerializeField] public AudioClip[] clips_voiceover_round; // Corresponds to voiceover_round_sfx_name
    [SerializeField] public AudioClip[] clips_voiceover_tutorial; // Corresponds to voiceover_tutorial_sfx_name
    [SerializeField] public AudioClip[] clips_voiceover_ko; // Corresponds to voice_ko_sfx_name
    [SerializeField] public AudioClip[] clips_voiceover_map; // Corresponds to GameController.mapscript_list

    public void Start()
    {
        all_clips = new AudioClip[(int)voiceover_event_name.ENUM_LENGTH][];
        all_clips[0] = clips_voiceover_round;
        all_clips[1] = clips_voiceover_tutorial;
        all_clips[2] = clips_voiceover_ko;
    }

    public bool DetermineInterruptable(AudioClip[] clips, int index = -1)
    {
        if (clips == clips_voiceover_round)
        {
            if (index == (int)voiceover_round_sfx_name.Start) { return true; }
            else if (index == (int)voiceover_round_sfx_name.End) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Victory) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Defeat) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Stalemate) { return false; } // Stalemate always comes before victory/defeat, so it needs to be non-interruptable
            else if (index == (int)voiceover_round_sfx_name.Infection_End_Victory) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Infection_End_Defeat) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Mode_Survival) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Mode_Clash) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Mode_BossBash) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Mode_Infection) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Mode_FittingIn) { return true; }
            else if (index == (int)voiceover_round_sfx_name.Mode_KingOfTheHill) { return true; }
            else if (index == (int)voiceover_round_sfx_name.KOTH_CaptureSelf) { return true; }
            else if (index == (int)voiceover_round_sfx_name.KOTH_CaptureOther) { return true; }
            else if (index == (int)voiceover_round_sfx_name.KOTH_Overtime) { return false; } // Overtime should always override captureself or captureother, as it's more important
            else if (index == (int)voiceover_round_sfx_name.MapAnnounce) { return true; }
        }
        else if (clips == clips_voiceover_tutorial)
        {
            // None of the gamemode tutorials should be interruptable
            if (index == (int)voiceover_tutorial_sfx_name.Mode_Survival) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_Clash) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_BossBash_A) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_BossBash_B) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_Infection_A) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_Infection_B) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_FittingIn) { return false; }
            else if (index == (int)voiceover_tutorial_sfx_name.Mode_KingOfTheHill) { return false; }
            // Weapon tutorials, however, can be
            // To-do: add weapon tutorials
        }
        else if (clips == clips_voiceover_ko)
        {
            // All combos and killstreaks can interrupt each other; the highest one will always take priority
            return true;
        }

        return true;
    }
    public bool AllowPlay(int type_index)
    {
        if (gameController == null || gameController.local_ppp_options == null) { return false; }
        else if (type_index == 0 && !gameController.local_ppp_options.vo_pref_a) { return false; }
        else if (type_index == 1 && !gameController.local_ppp_options.vo_pref_b) { return false; }
        else if (type_index == 2 && !gameController.local_ppp_options.vo_pref_c) { return false; }
        return true;
    }

    public void PlayVoiceover(int type_index, int clip_index = -1)
    {
        //UnityEngine.Debug.Log("[VO_TEST] Attempting to play voiceover of type " + type_index + " and clip " + clip_index);
        if (all_clips == null || type_index < 0 || type_index > all_clips.Length || gameController == null || !gameController.voiceover_interruptable || !AllowPlay(type_index)) { return; }
        AudioSource source = gameController.snd_voiceover_sfx_source;
        AudioClip[] clips = all_clips[type_index];
        AudioClip clip_to_play = null;
        if (clip_index == (int)voiceover_round_sfx_name.MapAnnounce) { clips = clips_voiceover_map; clip_index = gameController.map_selected; }

        float volume_scale = 1.0f;
        if (gameController.local_ppp_options != null) { volume_scale = gameController.local_ppp_options.sound_volume; }
        if (clips == null || clips.Length <= 0) { return; }
        if (clip_index < 0)
        {
            int randClip = UnityEngine.Random.Range(0, clips.Length);
            clip_to_play = clips[randClip];
        }
        else if (clip_index >= 0 && clip_index < clips.Length && clips[clip_index] != null)
        {
            clip_to_play = clips[clip_index];
        }
        if (clip_to_play == null) { return; }
        source.Stop();
        source.clip = clip_to_play;
        source.volume = gameController.voiceover_volume_default * volume_scale;
        gameController.voiceover_countdown = clip_to_play.length;
        gameController.voiceover_interruptable = DetermineInterruptable(clips, clip_index);
        source.Play();
        //UnityEngine.Debug.Log("[VO_TEST] Now playing: " + clip_to_play.name + " by " + gameObject.name + " for " + clip_to_play.length + " seconds");

    }
}
