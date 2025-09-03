
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum player_state_name
{
    Inactive, Joined, Alive, Respawning, Dead, Spectator, ENUM_LENGTH
}

public class PlayerAttributes : UdonSharpBehaviour
{

    [NonSerialized][UdonSynced] public byte ply_state;
    [NonSerialized][UdonSynced] public float ply_dp;
    [NonSerialized][UdonSynced] public float ply_dp_default;
    [NonSerialized][UdonSynced] public ushort ply_lives;
    [NonSerialized][UdonSynced] public ushort ply_points = 0;
    // While we aren't syncing the stats below right now, we may want to in the future for UI purposes
    [NonSerialized][UdonSynced] public float ply_scale = 1.0f; // This is the one stat that needs to be synced because it affects visuals
    [NonSerialized] public float ply_speed = 1.0f;
    [NonSerialized] public float ply_atk = 1.0f;
    [NonSerialized] public float ply_def = 1.0f;
    [NonSerialized] public float ply_grav = 1.0f;
    [NonSerialized] public float ply_respawn_duration;
    [NonSerialized] public int ply_team = -1; // -1: Spectator, all others are teams
    [NonSerialized] public VRCPlayerApi last_hit_by_ply;
    [NonSerialized] public int last_kill_ply = -1;
    [NonSerialized] public float last_kill_duration = 4.0f;
    [NonSerialized] public float last_kill_timer = 0.0f;

    [SerializeField] public GameController gameController;
    [NonSerialized] public float ply_respawn_timer = 0.0f;

    [NonSerialized] public int combo_receive, combo_send = 0;
    [NonSerialized] public float combo_send_duration = 2.0f;
    [NonSerialized] public float combo_send_timer = 0.0f;

    [NonSerialized] public GameObject[] powerups_active;

    [NonSerialized] public float plyEyeHeight_default, plyEyeHeight_desired;
    [Tooltip("How long a size-changing animation should play on a player")]
    [SerializeField] public double plyEyeHeight_lerp_duration = 2.5f;
    [NonSerialized] public double plyEyeHeight_lerp_start_ms = 0.0f;
    [NonSerialized] public bool plyEyeHeight_change = false;

    // To-Do: Have all projectile damage scale to a configurable factor, which is then auto-scaled to the # of players

    void Start()
    {
        powerups_active = new GameObject[0];
        ResetDefaultEyeHeight();
    }

    private void Update()
    {

        // Only the owner should run the following:
        if (Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }

        // Handle player state
        if (ply_respawn_timer < ply_respawn_duration)
        {
            ply_respawn_timer += Time.deltaTime;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            ply_state = (int)player_state_name.Alive;
        }
        else if (ply_state == (int)player_state_name.Dead && gameController.round_state == (int)round_state_name.Ongoing)
        {
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForRoundGoal");
            ply_respawn_timer = 0.0f;
        }

        // Handle last kill
        if (last_kill_timer < last_kill_duration && last_kill_ply > -1)
        {
            last_kill_timer += Time.deltaTime;
        }
        else if (last_kill_timer >= last_kill_duration && last_kill_ply > -1)
        {
            last_kill_ply = -1;
        }

        // Handle send combos
        if (combo_send_timer < combo_send_duration && combo_send > 0)
        {
            combo_send_timer += Time.deltaTime;
        }
        else if (combo_send_timer >= combo_send_duration && combo_send > 0)
        {
            combo_send = 0;
        }

        // Handle receive combos
        if (Networking.LocalPlayer.IsPlayerGrounded()) { combo_receive = 0; }

        // Handle player stats
        // To-do: this more efficiently
        if (gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) { 
            Networking.LocalPlayer.SetWalkSpeed(2.0f * ply_speed);
            Networking.LocalPlayer.SetRunSpeed(4.0f * ply_speed);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f * ply_speed);
            Networking.LocalPlayer.SetGravityStrength(1.0f * ply_grav);
        }
        else
        {
            Networking.LocalPlayer.SetWalkSpeed(2.0f);
            Networking.LocalPlayer.SetRunSpeed(4.0f);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f);
            Networking.LocalPlayer.SetGravityStrength(1.0f);
        }
    }

    private void FixedUpdate()
    {
        // Update size
        if (plyEyeHeight_change)
        {
            var plyCurrentEyeHeight = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
            var lerp_delta = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), plyEyeHeight_lerp_start_ms);
            if (plyCurrentEyeHeight != plyEyeHeight_desired && lerp_delta < plyEyeHeight_lerp_duration)
            {
                Networking.LocalPlayer.SetAvatarEyeHeightByMeters(Mathf.Lerp(plyCurrentEyeHeight, plyEyeHeight_desired, (float)(lerp_delta / plyEyeHeight_lerp_duration)));
            }
            else if (plyCurrentEyeHeight != plyEyeHeight_desired && lerp_delta >= plyEyeHeight_lerp_duration)
            {
                Networking.LocalPlayer.SetAvatarEyeHeightByMeters(plyEyeHeight_desired);
                plyEyeHeight_change = false;
            }
            else if (plyCurrentEyeHeight == plyEyeHeight_desired) 
            { 
                plyEyeHeight_change = false; 
            }
        }
    }

    public override void OnAvatarChanged(VRCPlayerApi player) {
        if (player != Networking.LocalPlayer || Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }
        ResetDefaultEyeHeight();
    }

    public void ResetDefaultEyeHeight()
    {
        if (Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }
        ResetPowerups();
        plyEyeHeight_default = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        plyEyeHeight_desired = plyEyeHeight_default;
        
    }

    [NetworkCallable]
    public void HitOtherPlayer(int attacker_id, int damage_type)
    {
        // Only the attacker should have a registered hit
        if (attacker_id != Networking.LocalPlayer.playerId) { return; }
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_index.HitSend], gameController.snd_game_sfx_clips[(int)game_sfx_index.HitSend], damage_type, 1 + 0.1f * (combo_send));
        combo_send++;

    }

    [NetworkCallable]
    public void ReceiveDamage(float damage, Vector3 forceDirection, int attacker_id, int damage_type)
    {
        //if (attacker_id == Networking.LocalPlayer.playerId) { return; }
        if (ply_state != (int)player_state_name.Alive) { return; }
        var calcDmg = damage; 
        var modForceDirection = forceDirection; // To-do: make this into a slider, game setting, or serialized field

        // Input damage should already have the attacker's attack & scale added onto it; we only handle defense from here
        calcDmg *= (1.0f / ply_def) * (1.0f / (ply_scale * gameController.scale_damage_factor)); 

        if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, 0.66f, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, 0.33f, 0.0f); }
        var calcForce = (modForceDirection + new Vector3(0.0f, 0.33f, 0.0f));
        calcForce *= Mathf.Pow((calcDmg + ply_dp) / 3.66f, 1.05f);

        Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);
        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        ply_dp += calcDmg;

        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_index.HitReceive], gameController.snd_game_sfx_clips[(int)game_sfx_index.HitReceive], damage_type, 1 + 0.1f * (combo_receive));
        combo_receive++;
        last_hit_by_ply = VRCPlayerApi.GetPlayerById(attacker_id);
        if (attacker_id >= 0) {
            var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
            plyAttr.SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attacker_id, damage_type);
        }
    }

    // This is failing BECAUSE IT'S BEING RAN ON THE OTHER PLAYER'S ATTRIBUTE
    [NetworkCallable]
    public void KillOtherPlayer(int attackerPlyId, int defenderPlyId)
    {
        Debug.Log("Is owner? " + Networking.IsOwner(gameObject));
        Debug.Log("LocalPlayer.playerId: " + Networking.LocalPlayer.playerId);
        Debug.Log("Attacker ID: " + attackerPlyId);
        if (attackerPlyId != Networking.LocalPlayer.playerId) { return; }
        Debug.Log("We killed Defender ID: " + defenderPlyId);
        ply_points++;
        last_kill_timer = 0.0f;
        last_kill_ply = defenderPlyId;
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_index.Kill], gameController.snd_game_sfx_clips[(int)game_sfx_index.Kill]);
    }

    public void HandleLocalPlayerDeath()
    {
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_index.Death], gameController.snd_game_sfx_clips[(int)game_sfx_index.Death]);
        ply_respawn_timer = 0;
        if (ply_state == (int)player_state_name.Alive && gameController.round_state == (int)round_state_name.Ongoing)
        {
            ply_state = (int)player_state_name.Respawning;
            // Check if we are in lives mode, and if so, if we are on the team that tracks lives
            if (!gameController.option_goal_points_a || (!gameController.option_goal_points_b && ply_team == 1) ) { ply_lives--; }
        }
        else if (ply_state == (int)player_state_name.Dead || gameController.round_state == (int)round_state_name.Ready)
        {
            ply_state = (int)player_state_name.Respawning;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            //UnityEngine.Debug.Log("Stop trying to die while respawning!");
        }
        else
        {
            //UnityEngine.Debug.Log("Whoa, you died in an unusual way! Contact a developer!");
        }

        // To-Do: Manage behavior based on gamemode
        if (ply_lives > 0)
        {
            ply_dp = ply_dp_default;
            gameController.TeleportLocalPlayerToGameSpawnZone();
        }
        else
        {
            gameController.TeleportLocalPlayerToReadyRoom();
            ply_state = (int)player_state_name.Dead;

        }

        if (last_hit_by_ply != null) {
            var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
            plyAttr.SendCustomNetworkEvent(NetworkEventTarget.Owner, "KillOtherPlayer", last_hit_by_ply.playerId, Networking.LocalPlayer.playerId); 
        }

        if (gameController.option_gamemode == (int)round_mode_name.Infection && ply_team != 1) { ply_team = 1; }

        ResetPowerups();

    }

    public void ProcessPowerUp(GameObject powerup_template, bool is_add = false)
    {
        //powerups_active
        if (powerup_template == null || powerup_template.GetComponent<ItemPowerup>() == null) { return; }

        if (is_add) {

			// Create a new copy of the powerup with all its inherited properties, then make sure it's set to be a template
			ItemPowerup powerup = powerup_template.GetComponent<ItemPowerup>();
            if (powerup == null) { return; }
			
			for (int i = 0; i < powerup.powerup_stat_behavior.Length; i++)
            {
                //Debug.Log("PROCESSING POWERUP WITH STAT BEHAVIORS " + powerup.powerup_stat_behavior[i].ToString() + " AND STAT VALUES " + powerup.powerup_stat_value[i].ToString());
                switch (i)
                {
                    case (int)powerup_stat_name.Scale:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_scale = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_scale += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_scale *= powerup.powerup_stat_value[i]; }
                        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
                        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
                        plyEyeHeight_change = true;
                        break;
                    case (int)powerup_stat_name.Speed:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_speed = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_speed += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_speed *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Atk:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_atk = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_atk += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_atk *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Def:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_def = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_def += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_def *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Grav:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_grav = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_grav += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_grav *= powerup.powerup_stat_value[i]; }
                        break;
                    default:
                        break;
                }
            }

            gameController.PlaySFXFromArray(powerup.item_snd_source, powerup.powerup_snd_clips, powerup.powerup_type);
            Debug.Log(gameObject.name + ": Attempting to play sound " + powerup.powerup_snd_clips[powerup.powerup_type].name + " for type " + powerup.powerup_type);
            powerups_active = gameController.AddToGameObjectArray(powerup_template, powerups_active);
		}

        else
        {
            ItemPowerup powerup = powerup_template.GetComponent<ItemPowerup>();
			for (int i = 0; i < powerup.powerup_stat_behavior.Length; i++)
            {
                switch (i)
                {
                    case (int)powerup_stat_name.Scale:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_scale -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_scale /= powerup.powerup_stat_value[i]; }
                        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
                        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
                        plyEyeHeight_change = true;
                        break;
                    case (int)powerup_stat_name.Speed:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_speed -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_speed /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Atk:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_atk -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_atk /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Def:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_def -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_def /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Grav:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_grav -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_grav /= powerup.powerup_stat_value[i]; }
                        break;
                    default:
                        break;
                }
            }
            //Debug.Log("Removing effects of active powerup of type " + powerup.powerup_type);
            powerups_active = gameController.RemoveEntryFromGameObjectArray(powerup.gameObject, powerups_active);
        }

    }

    public void ResetPowerups()
    {
        var index_iter = 0;
        var powerup_count = powerups_active.Length;
        // The issue is that this fails because the length is continously shortening. Instead, let's have it on a while loop with the iter++ until we reach the length of the initial
        while (index_iter < powerup_count) {
            if (powerups_active.Length <= 0) { break; }
            if (powerups_active[0] != null && powerups_active[0].GetComponent<ItemPowerup>() != null)
            {
                var powerup = powerups_active[0].GetComponent<ItemPowerup>();
                //Debug.Log("Found powerup at " + i + ": " + powerups_active[i - index_iter].name + "; type: " + powerups_active[i].GetComponent<ItemPowerup>().powerup_type + "; global index: " + powerups_active[i].GetComponent<ItemPowerup>().powerup_stored_global_index);
                //ProcessPowerUp(powerup.gameObject, false); // this may remove entries from the list as it occurs, resulting in errors.
                powerup.FadeOutAndDestroy();
			}
            else
            {
                //Debug.Log("Found powerup at " + i + " index that does not exist; resetting in array");
                powerups_active = gameController.RemoveIndexFromGameObjectArray(0, powerups_active);
            }
            index_iter++;
        }

    }

}
