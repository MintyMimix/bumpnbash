
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

public enum player_state_name
{
    Inactive, Joined, Alive, Respawning, Dead, Spectator, ENUM_LENGTH
}

public class PlayerAttributes : UdonSharpBehaviour
{

    [NonSerialized] [UdonSynced] public byte ply_state;
    [NonSerialized] [UdonSynced] public float ply_dp;
    [NonSerialized] [UdonSynced] public float ply_dp_default;
    [NonSerialized] [UdonSynced] public ushort ply_lives;
    [NonSerialized] [UdonSynced] public ushort ply_points = 0;
    [NonSerialized] [UdonSynced] public ushort ply_deaths = 0;
    [NonSerialized] [UdonSynced] public int ply_team = (int)player_tracking_name.Unassigned;
    [NonSerialized] public ushort ply_lives_local; // We create local versions of these variables for other clients to compare to. If there is a mismatch, we can have events fire off OnDeserialization().
    [NonSerialized] public ushort ply_points_local = 0;
    [NonSerialized] public ushort ply_deaths_local = 0;
    [NonSerialized] public int ply_team_local = 0;

    // While we aren't syncing the stats below right now, we may want to in the future for UI purposes
    [NonSerialized] [UdonSynced] public float ply_scale = 1.0f; // This is the one stat that needs to be synced most because it affects visuals
    [NonSerialized] public float ply_speed = 1.0f;
    [NonSerialized] [UdonSynced] public float ply_atk = 1.0f;
    [NonSerialized] [UdonSynced] public float ply_def = 1.0f;
    [NonSerialized] public float ply_grav = 1.0f;
    [NonSerialized] [UdonSynced] public int ply_jumps_add = 0;
    [NonSerialized] public int ply_jumps_tracking = 0;
    [NonSerialized] public bool ply_jump_pressed = false;
    [NonSerialized] public float ply_firerate = 1.0f;

    [NonSerialized] public float ply_respawn_duration;
    [NonSerialized] public VRCPlayerApi last_hit_by_ply;
    [NonSerialized] public float last_hit_by_duration = 20.0f;
    [NonSerialized] public float last_hit_by_timer = 0.0f;
    [NonSerialized] public int last_kill_ply = -1;
    [NonSerialized] public float last_kill_duration = 10.0f;
    [NonSerialized] public float last_kill_timer = 0.0f;

    [SerializeField] public GameController gameController;
    [NonSerialized] public float ply_respawn_timer = 0.0f;

    [NonSerialized] public int combo_receive, combo_send = 0;
    [NonSerialized] public float combo_send_duration = 2.0f;
    [NonSerialized] public float combo_send_timer = 0.0f;

    [NonSerialized] public float hazard_cooldown = 0.5f;
    [NonSerialized] public float hazard_timer = 0.0f;

    [NonSerialized] public GameObject[] powerups_active;

    [NonSerialized] [UdonSynced] public float plyEyeHeight_default, plyEyeHeight_desired;
    [Tooltip("How long a size-changing animation should play on a player")]
    [SerializeField] public double plyEyeHeight_lerp_duration = 2.5f;
    [NonSerialized] public double plyEyeHeight_lerp_start_ms = 0.0f;
    [NonSerialized] public bool plyEyeHeight_change = false;


    // To-Do: Have all projectile damage scale to a configurable factor, which is then auto-scaled to the # of players

    void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        powerups_active = new GameObject[0];

        SetDefaultEyeHeight();
    }

    public override void OnDeserialization(DeserializationResult deserializationResult)
    {
        if (gameController != null && gameController.round_state == (int)round_state_name.Ongoing)
        {
            if (ply_lives_local != ply_lives || ply_points_local != ply_points || ply_deaths_local != ply_deaths || ply_team_local != ply_team)
            {
                ply_lives_local = ply_lives;
                ply_points_local = ply_points;
                ply_deaths_local = ply_deaths;
                ply_team_local = ply_team;
                gameController.RefreshGameUI();
                if (Networking.IsMaster) { gameController.CheckForRoundGoal(); } // Because we are already confirmed to be the game master, we can send this locally instead of as a networked event
            }
        }
    }

   

    private void Update()
    {

        // Only the owner should run the following:
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
            else { return; }
        }
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
            //gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForRoundGoal");
            ply_respawn_timer = 0.0f;
        }

        // Handle last hit by
        if (last_hit_by_timer < last_hit_by_duration && last_hit_by_ply != null)
        {
            last_hit_by_timer += Time.deltaTime;
        }
        else if (last_hit_by_timer >= last_hit_by_duration && last_hit_by_ply != null)
        {
            last_hit_by_ply = null;
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

        // Handle hazard
        if (hazard_timer < hazard_cooldown) { hazard_timer += Time.deltaTime; }

        // Handle player stats
        // To-do: this more efficiently
        if (gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) { 
            Networking.LocalPlayer.SetWalkSpeed(2.0f * ply_speed);
            Networking.LocalPlayer.SetRunSpeed(4.0f * ply_speed);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f * ply_speed);
            Networking.LocalPlayer.SetGravityStrength(1.0f * ply_grav);
            Networking.LocalPlayer.SetJumpImpulse(4.0f + (1.0f - ply_grav)); // Default is 3.0f, but we want some verticality to our maps, so we'll make it 4.0
        }
        else
        {
            Networking.LocalPlayer.SetWalkSpeed(2.0f);
            Networking.LocalPlayer.SetRunSpeed(4.0f);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f);
            Networking.LocalPlayer.SetGravityStrength(1.0f);
            Networking.LocalPlayer.SetJumpImpulse(4.0f);
        }
        Networking.GetOwner(gameObject).SetManualAvatarScalingAllowed(true);

        if (Networking.LocalPlayer.IsPlayerGrounded()) { ply_jumps_tracking = 0; }
    }

    private void FixedUpdate()
    {
        // Update size
        if (plyEyeHeight_change && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
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
                if (ply_scale > gameController.largest_ply_scale) { gameController.SendCustomNetworkEvent(NetworkEventTarget.Owner, "UpdateLargestPlayer", ply_scale); }
                plyEyeHeight_change = false;
            }
            else if (plyCurrentEyeHeight == plyEyeHeight_desired) 
            {
                if (ply_scale > gameController.largest_ply_scale) { gameController.SendCustomNetworkEvent(NetworkEventTarget.Owner, "UpdateLargestPlayer", ply_scale); }
                plyEyeHeight_change = false; 
            }
        }
    }

    public override void OnAvatarChanged(VRCPlayerApi player) {
        if (player != Networking.LocalPlayer || Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }
        SetDefaultEyeHeight();
    }

    public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevHeight)
    {
        if (player != Networking.LocalPlayer || Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }
        if (prevHeight == 0) { SetDefaultEyeHeight(); }
        //else if (ply_scale == 1.0f && player.GetAvatarEyeHeightAsMeters() != prevHeight && plyEyeHeight_default != player.GetAvatarEyeHeightAsMeters()) { ResetDefaultEyeHeight(); }
    }

    public void TryHapticEvent(int haptic_event_type)
    {
        GameObject weapon_obj = gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon");
        PlayerWeapon weapon_script = null;
        if (weapon_obj != null) { weapon_script = weapon_obj.GetComponent<PlayerWeapon>(); }
        if (weapon_script != null) { weapon_script.PlayHapticEvent(haptic_event_type); } //Debug.Log("TRY HAPTIC: " + haptic_event_type); }
    }

    [NetworkCallable]
    public void SetDefaultEyeHeight()
    {
        if (Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }
        ResetPowerups();
        plyEyeHeight_default = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        plyEyeHeight_desired = plyEyeHeight_default;
        
    }

    [NetworkCallable]
    public void HitOtherPlayer(int attacker_id, int defender_id, float damage, int damage_type, Vector3 hitSpot)
    {
        // Only the attacker should have a registered hit
        if (attacker_id != Networking.LocalPlayer.playerId) { return; }
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitSend], gameController.snd_game_sfx_clips[(int)game_sfx_name.HitSend], damage_type, 1 + 0.1f * (combo_send));
        combo_send++;

        TryHapticEvent((int)game_sfx_name.HitSend);
        // Damage indicator
        if (gameController.local_uiplytoself != null)
        {
            gameController.local_uiplytoself.ShowHarmNumber(defender_id, damage, hitSpot);
        }
    }

    [NetworkCallable]
    public void ReceiveDamage(float damage, Vector3 forceDirection, Vector3 hitSpot, int attacker_id, int damage_type, bool hit_self)
    {
        //if (attacker_id == Networking.LocalPlayer.playerId) { return; }
        if (ply_state != (int)player_state_name.Alive) { return; }
        // We want to ensure hazards aren't processed
        if (damage_type == (int)damage_type_name.HazardBurn && (hazard_timer < hazard_cooldown)) { return; } 
        else { hazard_timer = 0.0f; }

        float calcDmg = damage; 
        Vector3 modForceDirection = forceDirection; // To-do: make this into a slider, game setting, or serialized field

        // Input damage should already have the attacker's attack & scale added onto it; we only handle defense from here
        calcDmg *= (1.0f / ply_def) * (1.0f / (ply_scale * gameController.scale_damage_factor));

        float baseLift = 0.66f; // 0.33f

        /*if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, baseLift / 2.0f, 0.0f); }
        if (hit_self) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }*/
        if (!Networking.LocalPlayer.IsPlayerGrounded()) { baseLift *= 0.5f; }
        modForceDirection = new Vector3(modForceDirection.x, Mathf.Max(Mathf.Abs(modForceDirection.y), baseLift), modForceDirection.z);
        UnityEngine.Debug.Log("Resulting force direction: " + modForceDirection + " (input: " + forceDirection + ")");
        if (modForceDirection.magnitude < baseLift) { modForceDirection *= baseLift/modForceDirection.magnitude; }
        UnityEngine.Debug.Log("Resulting force direction after magnitude modification: " + modForceDirection);
        float xDmg = calcDmg + (ply_dp * (1.0f / ply_def));
        float calcMagnitude = 0.004f * Mathf.Pow(xDmg, 1.85f) + 8.0f;
        // (100.0f + (xDmg / 3.0f))
        // / (1 + Mathf.Exp(-0.02f * (xDmg - 100.0f)));
        UnityEngine.Debug.Log("Resulting force magnitude: " + calcMagnitude);
        calcMagnitude = Mathf.Max(calcMagnitude, Vector3.Dot(modForceDirection, Networking.LocalPlayer.GetVelocity()), Vector3.Dot(modForceDirection, -Networking.LocalPlayer.GetVelocity()));
        UnityEngine.Debug.Log("Resulting force magnitude after factoring velocity: " + calcMagnitude);

        Vector3 calcForce = modForceDirection;
        calcForce *= calcMagnitude;
        
        //Mathf.Pow((calcDmg + ply_dp) / 2.2f, 1.08f);

        // Don't apply additional force if this is a hazard
        if (damage_type != (int)damage_type_name.HazardBurn)
        {
            //Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);

            Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);
        }

        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        if (!hit_self) { 
            ply_dp += calcDmg; 
            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitReceive], gameController.snd_game_sfx_clips[(int)game_sfx_name.HitReceive], damage_type, 1 + 0.1f * (combo_receive));
            combo_receive++;
            if (attacker_id >= 0) {
                last_hit_by_ply = VRCPlayerApi.GetPlayerById(attacker_id);
                last_hit_by_timer = 0.0f;
                var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
                plyAttr.SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attacker_id, Networking.LocalPlayer.playerId, calcDmg, damage_type, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
            }

            TryHapticEvent((int)game_sfx_name.HitReceive);
        }

        // Damage indicator
        if (gameController.local_uiplytoself != null)
        {
            gameController.local_uiplytoself.ShowPainIndicator(calcDmg, hitSpot);
        }
    }

    // This is failing BECAUSE IT'S BEING RAN ON THE OTHER PLAYER'S ATTRIBUTE
    [NetworkCallable]
    public void KillOtherPlayer(int attackerPlyId, int defenderPlyId)
    {
        //Debug.Log("Is owner? " + Networking.IsOwner(gameObject));
        //Debug.Log("LocalPlayer.playerId: " + Networking.LocalPlayer.playerId);
        //Debug.Log("Attacker ID: " + attackerPlyId);
        if (attackerPlyId != Networking.LocalPlayer.playerId) { return; }
        Debug.Log("We killed Defender ID: " + defenderPlyId);
        if (gameController.option_gamemode != (int)gamemode_name.KingOfTheHill) { ply_points++; } // Add points if we aren't on KOTH (which is capture time)
        last_kill_timer = 0.0f;
        last_kill_ply = defenderPlyId;
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Kill], gameController.snd_game_sfx_clips[(int)game_sfx_name.Kill]);
        gameController.AddToLocalTextQueue("You knocked out " + VRCPlayerApi.GetPlayerById(defenderPlyId).displayName + "!");
        TryHapticEvent((int)game_sfx_name.Kill);
        // If we are the game master, we don't get an OnDeserialization event for ourselves, so check the round goal whenever we die or get a KO
        if (Networking.IsMaster) { gameController.CheckForRoundGoal(); }
    }

    public void HandleLocalPlayerDeath()
    {
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Death], gameController.snd_game_sfx_clips[(int)game_sfx_name.Death]);
        TryHapticEvent((int)game_sfx_name.Death);
        ply_respawn_timer = 0;
        if (ply_state == (int)player_state_name.Alive && gameController.round_state == (int)round_state_name.Ongoing)
        {
            ply_state = (int)player_state_name.Respawning;

            if (gameController.option_gamemode != (int)gamemode_name.FittingIn
                || (gameController.option_gamemode == (int)gamemode_name.FittingIn && last_hit_by_ply != null)) { ply_deaths++; }
            // Check if we are in a gamemode that tracks lives, and if so, if we are on the team that tracks lives
            if (gameController.option_gamemode == (int)gamemode_name.Survival || (ply_team == 1 && gameController.option_gamemode == (int)gamemode_name.BossBash))
            { 
                ply_lives--;
            }
        }
        else if (ply_state == (int)player_state_name.Dead || gameController.round_state == (int)round_state_name.Ready)
        {
            ply_state = (int)player_state_name.Respawning;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            //if (gameController.option_gamemode == (int)gamemode_name.FittingIn && local_respawn_count >= gameController.option_gm_goal) { ply_lives = 0; }
        }
        else
        {
            //UnityEngine.Debug.Log("Whoa, you died in an unusual way! Contact a developer!");
        }

        ResetPowerups();
        if (gameController.local_plyweapon != null) { gameController.local_plyweapon.ResetWeaponToDefault(); }

        // Manage behavior based on gamemode
        if (gameController.option_gamemode == (int)gamemode_name.Infection && ply_team != 1)
        {
            UnityEngine.Debug.Log("Requesting game master to change team to Infected...");
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, 1, false);
            ply_team = 1;
            ply_points = 0;
        }
        else if (gameController.option_gamemode == (int)gamemode_name.FittingIn && last_hit_by_ply != null)
        {
            ply_scale += (float)((float)gameController.option_gm_config_a / 100.0f);
            plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
            plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
            plyEyeHeight_change = true;
            if (ply_scale > (float)((float)gameController.option_gm_goal / 100.0f)) 
            { 
                ply_lives = 0;
                gameController.AddToLocalTextQueue("You've grown too big! (" + ply_scale * 100.0f + "% vs max of " + gameController.option_gm_goal + "%)");
            }
        }

        if (ply_lives > 0)
        {
            // Reset the player's damage value to default, unless this is Boss Bash or Fitting In and they fell off without being struck (anti-exploit measure)
            if (!(last_hit_by_ply == null && (gameController.option_gamemode == (int)gamemode_name.BossBash || gameController.option_gamemode == (int)gamemode_name.FittingIn)))
            { ply_dp = ply_dp_default; }
            // Edge case: if this is on infection, it's possible for this event to off after RoundEnd() [usually for the master].
            // So, check if our local team is 1, but the networked team is 0, and if the lives calculated from the networked team is 1 (the lives count for Infection).
            var total_lives = 0; var members_alive = 0;
            gameController.CheckSingleTeamLives(0, ref members_alive, ref total_lives);
            if (gameController.option_gamemode == (int)gamemode_name.Infection && ply_team == 1 && gameController.GetGlobalTeam(Networking.LocalPlayer.playerId) == 0
                && total_lives <= 1) { gameController.TeleportLocalPlayerToReadyRoom(); ply_state = (int)player_state_name.Dead; }
            else { gameController.TeleportLocalPlayerToGameSpawnZone(); }
        }
        else
        {
            gameController.TeleportLocalPlayerToReadyRoom();
            ply_state = (int)player_state_name.Dead;

        }

        if (last_hit_by_ply != null) {
            gameController.AddToLocalTextQueue("Knocked out by " + last_hit_by_ply.displayName + "!");
            var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
            if (plyAttr != null) { plyAttr.SendCustomNetworkEvent(NetworkEventTarget.Owner, "KillOtherPlayer", last_hit_by_ply.playerId, Networking.LocalPlayer.playerId); }
            last_hit_by_ply = null;
        }
        else
        {
            gameController.AddToLocalTextQueue("Knocked out!");
        }

        if (gameController != null && gameController.local_plyweapon != null) { gameController.local_plyweapon.ResetWeaponToDefault(); }

        // If we are the game master, we don't get an OnDeserialization event for ourselves, so check the round goal whenever we die or get a KO
        if (Networking.IsMaster) { gameController.CheckForRoundGoal(); }
        else { gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForRoundGoal"); }

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
                        ply_scale = Mathf.Max(0.05f, ply_scale);
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
                    case (int)powerup_stat_name.Damage:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_dp = powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_dp += powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_dp *= powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); }
                        break;
                    case (int)powerup_stat_name.Jumps:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_jumps_add = Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_jumps_add += Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_jumps_add *= Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
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
                    case (int)powerup_stat_name.Jumps:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_jumps_add -= Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_jumps_add /= Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        break;
                    // We do not have a case for damage, because those are intended to be permanent
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

    [NetworkCallable]
    public void SetPoints(ushort amount)
    {
        ply_points = amount;
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        base.InputJump(value, args);
        if (value && !ply_jump_pressed) 
        {
            if (!Networking.LocalPlayer.IsPlayerGrounded() && ply_jumps_tracking < ply_jumps_add)
            {
                Vector3 plyVel = Networking.LocalPlayer.GetVelocity();
                Networking.LocalPlayer.SetVelocity(new Vector3(plyVel.x, 4.0f + (1.0f - ply_grav), plyVel.z));
                ply_jumps_tracking++;
            }
            ply_jump_pressed = true;
        }
        else if (!value && ply_jump_pressed) { ply_jump_pressed = false; }
    }

}
