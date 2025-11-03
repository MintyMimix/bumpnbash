
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.Core;
using VRC.SDK3.Persistence;
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
    [NonSerialized] [UdonSynced] public float ply_damage_dealt = 0.0f; // Total damage dealt tracker. Used only for Boss Bash.
    [NonSerialized] [UdonSynced] public int ply_team = (int)player_tracking_name.Unassigned;
    [NonSerialized] [UdonSynced] public bool ply_training = false;
    [NonSerialized] [UdonSynced] public bool in_spectator_area = false;
    [NonSerialized] [UdonSynced] public bool in_ready_room = true;

    [NonSerialized] public ushort ply_lives_local; // We create local versions of these variables for other clients to compare to. If there is a mismatch, we can have events fire off OnDeserialization().
    [NonSerialized] public ushort ply_points_local = 0;
    [NonSerialized] public ushort ply_deaths_local = 0;
    [NonSerialized] public int ply_team_local = 0;
    [NonSerialized] public bool ply_dual_wield_local = false;

    // While we aren't syncing the stats below right now, we may want to in the future for UI purposes
    [NonSerialized] [UdonSynced] public float ply_scale = 1.0f; // This is the one stat that needs to be synced most because it affects visuals
    [NonSerialized] public float ply_speed = 1.0f;
    [NonSerialized] [UdonSynced] public float ply_atk = 1.0f;
    [NonSerialized] [UdonSynced] public float ply_def = 1.0f;
    [NonSerialized] public float ply_grav = 1.0f;
    [NonSerialized] public bool in_grav_well = false;
    [NonSerialized] public int ply_jumps_add = 0; //[UdonSynced] 
    [NonSerialized] public int ply_jumps_tracking = 0;
    [NonSerialized] public bool ply_jump_pressed = false;
    [NonSerialized] public float ply_firerate = 1.0f;
    [NonSerialized] [UdonSynced] public bool ply_dual_wield = false;
    [NonSerialized] public bool ply_desktop_applied_dual_compensation_buff = false;

    [NonSerialized] [UdonSynced] public float ply_respawn_duration;
    [NonSerialized] public VRCPlayerApi last_hit_by_ply;
    [NonSerialized] public float last_hit_by_duration = 20.0f;
    [NonSerialized] public float last_hit_by_timer = 0.0f;
    [NonSerialized] public int last_kill_ply = -1;
    [NonSerialized] public float last_kill_duration = 10.0f;
    [NonSerialized] public float last_kill_timer = 0.0f;

    [SerializeField] public GameController gameController;
    [NonSerialized] [UdonSynced] public float ply_respawn_timer = 0.0f;

    [NonSerialized] public int combo_receive, combo_send = 0;
    [NonSerialized] public float combo_send_duration = 2.0f;
    [NonSerialized] public float combo_send_timer = 0.0f;

    [NonSerialized] public int killstreak, killbo = 0;
    [NonSerialized] public float killbo_duration = 6.0f;
    [NonSerialized] public float killbo_timer = 0.0f;

    [NonSerialized] public float hazard_cooldown = 0.5f;
    [NonSerialized] public float hazard_timer = 0.0f;

    [NonSerialized] public float air_thrust_cooldown = 1.1f;
    [NonSerialized] public float air_thrust_timer = 0.0f;
    [NonSerialized] public bool air_thrust_ready = false;
    [NonSerialized] public bool air_thrust_enabled = false;

    [NonSerialized] public bool powerups_are_resetting = false;

    [NonSerialized] public GameObject[] powerups_active;
    [NonSerialized] public PlayerWeapon owner_plyweapon;
    [NonSerialized] public PlayerWeapon owner_secondweapon;
    [NonSerialized] public PlayerHitbox owner_plyhitbox;

    [NonSerialized] public float plyEyeHeight_default = 0.0f; //[UdonSynced]
    [NonSerialized] public float plyEyeHeight_desired = 0.0f; //[UdonSynced]
    [Tooltip("How long a size-changing animation should play on a player")]
    [SerializeField] public double plyEyeHeight_lerp_duration = 2.5f;
    [NonSerialized] public double plyEyeHeight_lerp_start_ms = 0.0f;
    [NonSerialized] public bool plyEyeHeight_change = false;

    [NonSerialized] public bool[] local_tutorial_message_bool;
    [NonSerialized] public string[] local_tutorial_message_str_desktop;
    [NonSerialized] public string[] local_tutorial_message_str_vr;

    [NonSerialized] [UdonSynced] public byte infection_special = 0;
    [NonSerialized] public map_element_kothtainer cached_kothtainer;
    [NonSerialized] public float local_tick_timer = 0.0f;

    [NonSerialized] public bool tutorial_messages_ready = false;

    [SerializeField] public byte KILLSTREAK_THRESHOLD_0 = 3;
    [SerializeField] public byte KILLSTREAK_THRESHOLD_1 = 5;
    [SerializeField] public byte KILLSTREAK_THRESHOLD_2 = 7;
    [SerializeField] public byte KILLSTREAK_THRESHOLD_3 = 10;
    [SerializeField] public byte KILLSTREAK_THRESHOLD_4 = 15;

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
        if (gameController != null && gameController.round_state != (int)round_state_name.Start)
        {
            if (ply_lives_local != ply_lives || ply_points_local != ply_points || ply_deaths_local != ply_deaths || ply_team_local != ply_team || ply_dual_wield_local != ply_dual_wield)
            {
                ply_lives_local = ply_lives;
                ply_points_local = ply_points;
                ply_deaths_local = ply_deaths;
                ply_team_local = ply_team;
                if (ply_dual_wield_local != ply_dual_wield) { gameController.GetSecondaryWeaponFromID(Networking.GetOwner(gameObject).playerId).gameObject.SetActive(ply_dual_wield); }
                ply_dual_wield_local = ply_dual_wield;
                gameController.RefreshGameUI();
                if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.gamevars_force_refresh_on_next_tick = true; }
                if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); } // Because we are already confirmed to be the game master, we can send this locally instead of as a networked event
            }

        }
    }

    private void Update()
    {

        
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
            else { return; }
        }
        else
        {
            if (owner_plyweapon == null) { owner_plyweapon = gameController.GetPlayerWeaponFromID(Networking.GetOwner(gameObject).playerId); }
            if (owner_secondweapon == null) { owner_secondweapon = gameController.GetSecondaryWeaponFromID(Networking.GetOwner(gameObject).playerId); }
            if (owner_plyhitbox == null) { owner_plyhitbox = gameController.GetPlayerHitboxFromID(Networking.GetOwner(gameObject).playerId); }

            if (owner_plyweapon != null && owner_plyhitbox != null
                && (in_ready_room || in_spectator_area || gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Queued || gameController.round_state == (int)round_state_name.Loading || gameController.round_state == (int)round_state_name.Over)
                && !(ply_training || (ply_team >= 0 && (ply_state == (int)player_state_name.Alive || ply_state == (int)player_state_name.Respawning)))
                )
            {
                owner_plyweapon.ToggleActive(false);
                owner_plyhitbox.ToggleHitbox(false);
            }
            else if (owner_plyweapon != null && owner_plyhitbox != null) 
            { 
                owner_plyweapon.ToggleActive(true);
                owner_plyhitbox.ToggleHitbox(true);
            }
        }

        // -- Only the owner should run the following --
        if (!Networking.IsOwner(gameObject)) { return; }

        // Handle player state
        if (ply_respawn_timer < ply_respawn_duration)
        {
            ply_respawn_timer += Time.deltaTime;
            // Update the UI accordingly
            if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.UI_Damage(); }
            // Update KOTH timers, if applicable
            if (((gameController.option_gamemode == (int)gamemode_name.Infection && infection_special == 2) || gameController.option_gamemode == (int)gamemode_name.KingOfTheHill) && cached_kothtainer != null) { cached_kothtainer.RefreshTimers(ply_respawn_duration - ply_respawn_timer + 1); }
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            ply_state = (int)player_state_name.Alive;
            // Update the UI accordingly
            if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.UI_Damage(); }
            if ((gameController.option_gamemode == (int)gamemode_name.Infection && infection_special == 2) || gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                if (cached_kothtainer != null) { cached_kothtainer.gameObject.SetActive(false); cached_kothtainer = null; }
                gameController.TeleportLocalPlayerToGameSpawnZone();
            }
        }
        else if (ply_state == (int)player_state_name.Dead && gameController.round_state == (int)round_state_name.Ongoing)
        {
            //gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForRoundGoal");
            ply_respawn_timer = 0.0f;
        }

        // Handle air thrust
        if (air_thrust_enabled && air_thrust_timer < air_thrust_cooldown && !air_thrust_ready)
        {
            air_thrust_timer += Time.deltaTime;
        }
        else if (air_thrust_enabled && !air_thrust_ready)
        {
            air_thrust_ready = true;
            air_thrust_timer = 0.0f;
        }
        /*else if (air_thrust_enabled && air_thrust_ready)
        {
            if (!Networking.LocalPlayer.IsUserInVR() && Input.GetKeyDown(KeyCode.Q))
            {
                AirThrust(); 
            }
        }*/

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

        // Handle kill combos
        if (killbo_timer < killbo_duration && killbo > 0)
        {
            killbo_timer += Time.deltaTime;
        }
        else if (killbo_timer >= killbo_duration && killbo > 0)
        {
            killbo = 0;
        }

        // Handle receive combos
        if (Networking.LocalPlayer.IsPlayerGrounded()) { combo_receive = 0; }

        // Handle hazard
        if (hazard_timer < hazard_cooldown) { hazard_timer += Time.deltaTime; }

        // Handle player stats
        // To-do: this more efficiently
        if (!in_spectator_area && (gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing || ply_training)) {
            float koth_mod = 1.0f;
            //if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill && ply_state == (int)player_state_name.Respawning && !ply_training) { koth_mod = 0.01f; }
            Networking.LocalPlayer.SetWalkSpeed(2.0f * ply_speed * koth_mod);
            Networking.LocalPlayer.SetRunSpeed(4.0f * ply_speed * koth_mod);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f * ply_speed * koth_mod);
            if (in_grav_well) { Networking.LocalPlayer.SetGravityStrength(0.0f); }
            else { Networking.LocalPlayer.SetGravityStrength(Mathf.Max(0.125f, 1.0f * ply_grav * (1.0f / koth_mod))); }
            float jump_height = 4.0f + (1.0f - ply_grav);
            jump_height = ((2.0f * jump_height) + (jump_height * Mathf.Max(1.0f, ply_scale))) / 3.0f; // Jump height scales with player at half rate (i.e. 2x = 1.5x jump height, 3x = 2x jump height, etc.)
            Networking.LocalPlayer.SetJumpImpulse(jump_height); // Default is 3.0f, but we want some verticality to our maps, so we'll make it 4.0
        }
        else
        {
            float spec_mod = 1.0f;
            if (in_spectator_area) { spec_mod = Mathf.Max(ply_speed, 4.5f); }
            Networking.LocalPlayer.SetWalkSpeed(2.0f * spec_mod);
            Networking.LocalPlayer.SetRunSpeed(4.0f * spec_mod);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f * spec_mod);
            Networking.LocalPlayer.SetGravityStrength(1.0f);
            Networking.LocalPlayer.SetJumpImpulse(4.0f);
        }
        Networking.GetOwner(gameObject).SetManualAvatarScalingAllowed(true);

        // Hnadle multi-jump
        if (Networking.LocalPlayer.IsPlayerGrounded()) { ply_jumps_tracking = 0; }

        local_tick_timer += Time.deltaTime;
        if (local_tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            LocalPerTickUpdate();
            local_tick_timer = 0.0f;
        }


    }

    private void LocalPerTickUpdate()
    {
        // Update size
        if (Networking.IsOwner(gameObject) && plyEyeHeight_change && gameController.room_ready_script.warning_acknowledged)
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
        else if (Networking.IsOwner(gameObject) && (in_ready_room || !gameController.room_ready_script.warning_acknowledged) && !ply_training && plyEyeHeight_desired != plyEyeHeight_default && !plyEyeHeight_change) 
        { 
            LocalResetScale(); 
        }

    }

    public void LocalPerSecondUpdate()
    {

        // If the ZomBig has been alive for too long, start taking damage
        if (gameController.infection_zombig_spawn_time != 0.0d && infection_special == 2) //  && ply_dp != 0 We also do ply_dp != 0 to account for ping delay for if a 2nd zombig is queued to spawn immediately after death 
        {
            double time_elapsed = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), gameController.infection_zombig_spawn_time);
            UnityEngine.Debug.Log("[ASSERT_ZOMBIG_STATUS]: time_elapsed: " + time_elapsed + "combo_send: " + combo_send + "combo_send_timer: " + combo_send_timer + "combo_send_duration: " + combo_send_duration + "killbo: " + killbo + "killbo_timer: " + killbo_timer + "killbo_duration: " + killbo_duration);
            if (time_elapsed > 25 && combo_send <= 0 && combo_send_timer >= combo_send_duration && killbo <= 0)
            {
                if (ply_def >= 0.25f) { ply_def *= 0.9f; }
                ReceiveDamage(3, Networking.LocalPlayer.GetRotation() * Vector3.forward, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, -1, (int)damage_type_name.ZombigIdle, false, 0);
            }
        }
    }

    public override void OnAvatarChanged(VRCPlayerApi player) {
        if (player != Networking.LocalPlayer || !Networking.IsOwner(gameObject)) { return; }
        SetDefaultEyeHeight();
    }

    public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevHeight)
    {
        if (player != Networking.LocalPlayer || !Networking.IsOwner(gameObject)) { return; }
        if (prevHeight == 0) { SetDefaultEyeHeight(); }
        // We should also allow users to change their default height in the ready roomw
        else if (gameController != null && 
            !(ply_state == (int)player_state_name.Alive || ply_state == (int)player_state_name.Respawning)
            && !ply_training
            && !plyEyeHeight_change // This is the key; if this is true, then we know it's the game saying to change the eye height, not the user
            )
        { SetDefaultEyeHeight(); }
        //else if (ply_scale == 1.0f && player.GetAvatarEyeHeightAsMeters() != prevHeight && plyEyeHeight_default != player.GetAvatarEyeHeightAsMeters()) { ResetDefaultEyeHeight(); }
    }

    public void TryHapticEvent(int haptic_event_type)
    {
        //GameObject weapon_obj = gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon");
        //PlayerWeapon weapon_script = null; 
        if (gameController.local_plyweapon != null) { gameController.local_plyweapon.PlayHapticEvent(haptic_event_type); } //Debug.Log("TRY HAPTIC: " + haptic_event_type); }
        if (gameController.local_secondaryweapon != null) { gameController.local_secondaryweapon.PlayHapticEvent(haptic_event_type); } //Debug.Log("TRY HAPTIC: " + haptic_event_type); }

    }

    [NetworkCallable]
    public void SetDefaultEyeHeight()
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        ResetPowerups();
        if (gameController.option_gamemode != (int)gamemode_name.Infection && gameController.option_gamemode != (int)gamemode_name.FittingIn) { ResetToDefaultStats(); }

        float default_height = Mathf.Clamp(Networking.LocalPlayer.GetAvatarEyeHeightAsMeters(), Networking.LocalPlayer.GetAvatarEyeHeightMinimumAsMeters(), Networking.LocalPlayer.GetAvatarEyeHeightMaximumAsMeters());
        plyEyeHeight_default = default_height;
        plyEyeHeight_desired = plyEyeHeight_default;
        
    }
    public void LocalResetScale()
    {
        ply_scale = 1.0f;
        plyEyeHeight_desired = plyEyeHeight_default;
        plyEyeHeight_change = true;
    }

    [NetworkCallable]
    public void HitOtherPlayer(int attacker_id, int defender_id, float damage, int damage_type, Vector3 hitSpot)
    {
        // Only the attacker should have a registered hit
        if (attacker_id != Networking.LocalPlayer.playerId) { return; }
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitSend], gameController.snd_game_sfx_clips[(int)game_sfx_name.HitSend], damage_type, 1 + 0.1f * (combo_send));
        combo_send++;
        combo_send_timer = 0.0f;
        if (!ply_training) { ply_damage_dealt += damage; }

        TryHapticEvent((int)game_sfx_name.HitSend);
        // Damage indicator
        if (gameController.local_uiplytoself != null)
        {
            gameController.local_uiplytoself.ShowHarmNumber(defender_id, damage, hitSpot);
        }
    }

    [NetworkCallable]
    public void ReceiveDamage(float damage, Vector3 forceDirection, Vector3 hitSpot, int attacker_id, int damage_type, bool hit_self, byte extra_data)
    {
        //if (attacker_id == Networking.LocalPlayer.playerId) { return; }
        if (ply_state != (int)player_state_name.Alive) { return; }
        // We want to ensure hazards aren't processed
        if (damage_type == (int)damage_type_name.HazardBurn && (hazard_timer < hazard_cooldown)) { return; } 
        else { hazard_timer = 0.0f; }

        float calcDmg = damage; 
        Vector3 modForceDirection = forceDirection; // To-do: make this into a slider, game setting, or serialized field

        // Input damage should already have the attacker's attack & scale added onto it; we only handle defense from here
        if (ply_def == 0) { ply_def = 0.01f; }
        calcDmg *= (1.0f / ply_def) * (1.0f / (ply_scale * gameController.scale_damage_factor));

        float baseLift = 0.5f; // 0.66f
        if (hit_self) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }
        //modforcedirection not considering getvelocity().

        /*if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, baseLift / 2.0f, 0.0f); }
        if (hit_self) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }*/
        if (!Networking.LocalPlayer.IsPlayerGrounded()) { baseLift *= 0.5f; }
        modForceDirection = new Vector3(modForceDirection.x, Mathf.Max(Mathf.Abs(modForceDirection.y), baseLift), modForceDirection.z);
        UnityEngine.Debug.Log("Resulting force direction: " + modForceDirection + " (input: " + forceDirection + ")");
        if (modForceDirection.magnitude < baseLift) { modForceDirection *= baseLift/modForceDirection.magnitude; }
        UnityEngine.Debug.Log("Resulting force direction after magnitude modification: " + modForceDirection);
        //modForceDirection = new Vector3(modForceDirection.x * Networking.LocalPlayer.GetVelocity().normalized.x, modForceDirection.y * Networking.LocalPlayer.GetVelocity().normalized.y, modForceDirection.z * Networking.LocalPlayer.GetVelocity().normalized.z);
        //UnityEngine.Debug.Log("Resulting force direction after factoring velocity: " + modForceDirection);

        // Old formula: float xDmg = calcDmg + ply_dp; float calcMagnitude = 0.004f * Mathf.Pow(xDmg, 1.85f) + 8.0f;
        float xDmg = calcDmg + ply_dp;
        float calcMagnitude = 0.006f * Mathf.Pow(xDmg, 1.85f) + 11.0f; // originally 0.004f * and +8.0f
        UnityEngine.Debug.Log("Resulting force magnitude: " + calcMagnitude);
        calcMagnitude = Mathf.Max(calcMagnitude, Vector3.Dot(modForceDirection, Networking.LocalPlayer.GetVelocity()), Vector3.Dot(modForceDirection, -Networking.LocalPlayer.GetVelocity()));
        UnityEngine.Debug.Log("Resulting force magnitude after factoring velocity: " + calcMagnitude);

        Vector3 calcForce = modForceDirection;
        calcForce *= calcMagnitude;
        if (gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1 && !ply_training) { calcForce *= 0.66f; }
        //calcForce += Networking.LocalPlayer.GetVelocity().normalized;
        //Mathf.Pow((calcDmg + ply_dp) / 2.2f, 1.08f);

        // Don't apply additional force if this is a hazard or a throwable item
        if (damage_type != (int)damage_type_name.HazardBurn && damage_type != (int)damage_type_name.ItemHit)
        {
            //Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);
            Vector3 velAdd = new Vector3(Networking.LocalPlayer.GetVelocity().x, Mathf.Max(Networking.LocalPlayer.GetVelocity().y, 0.0f), Networking.LocalPlayer.GetVelocity().z);
            Networking.LocalPlayer.SetVelocity(velAdd + (calcForce * 0.5f));
        }

        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        if (!hit_self) { 
            ply_dp += Mathf.Abs(calcDmg); 
            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitReceive], gameController.snd_game_sfx_clips[(int)game_sfx_name.HitReceive], damage_type, 1 + 0.1f * (combo_receive));
            combo_receive++;
            if (attacker_id >= 0) {
                last_hit_by_ply = VRCPlayerApi.GetPlayerById(attacker_id);
                last_hit_by_timer = 0.0f;
                var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
                plyAttr.SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attacker_id, Networking.LocalPlayer.playerId, calcDmg, damage_type, hitSpot); //Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position
            }
        }
        TryHapticEvent((int)game_sfx_name.HitReceive);

        // Damage indicator
        if (gameController.local_uiplytoself != null)
        {
            gameController.local_uiplytoself.ShowPainIndicator(calcDmg, hitSpot);
        }

        // If this is a throwable item, apply a powerup to self
        if (damage_type == (int)damage_type_name.ItemHit)
        {
            //gameController.template_ItemSpawner.SetActive(true);
            ItemSpawner itemSpawnerTemplate = gameController.template_ItemSpawner.GetComponent<ItemSpawner>();
            itemSpawnerTemplate.item_spawn_index = extra_data;
            itemSpawnerTemplate.SpawnItem(extra_data, true);
            //if (extra_data < (int)powerup_type_name.ENUM_LENGTH) { itemSpawnerTemplate.child_powerup.OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>()); }
            //else { itemSpawnerTemplate.child_weapon.OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>()); }
            //itemSpawnerTemplate.DespawnItem((int)item_sfx_index.ItemExpire, -1, false);
            //gameController.template_ItemSpawner.SetActive(false);
        }
        
        // Update the UI accordingly
        if (gameController.local_uiplytoself != null) 
        { 
            gameController.local_uiplytoself.UI_Damage();
            if (!hit_self) { gameController.local_uiplytoself.FlashRecentDamage(calcDmg); }
        }
    }

    [NetworkCallable]
    public void KillOtherPlayer(int attackerPlyId, int defenderPlyId, bool defenderIsTraining)
    {
        //Debug.Log("Is owner? " + Networking.IsOwner(gameObject));
        //Debug.Log("LocalPlayer.playerId: " + Networking.LocalPlayer.playerId);
        //Debug.Log("Attacker ID: " + attackerPlyId);
        if (attackerPlyId != Networking.LocalPlayer.playerId) { return; }
        Debug.Log("We killed Defender ID: " + defenderPlyId);
        // Add points if we aren't on KOTH (which is capture time)
        if (gameController.option_gamemode != (int)gamemode_name.KingOfTheHill && !defenderIsTraining && !ply_training) 
        { 
            ply_points++; 
            if (gameController.option_gamemode == (int)gamemode_name.Clash)
            {
                if ((!gameController.option_teamplay || (gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1)) && ply_points == gameController.option_gm_goal - 1) 
                { 
                    gameController.SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkAddToTextQueue", "NOTIFICATION_POINTSMODE_NEARWIN_FFA", Networking.LocalPlayer.displayName, Color.red, 7.5f);
                }
                else if (gameController.option_teamplay && ply_team >= 0 && ply_team < gameController.team_names.Length)
                {
                    int team_points = 0;
                    gameController.CheckSingleTeamPoints(ply_team, gameController.cached_ply_in_game_dict, ref team_points);
                    if (team_points == gameController.option_gm_goal - 1)
                    {
                        gameController.SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkAddToTextQueue", "NOTIFICATION_POINTSMODE_NEARWIN_TEAM", "TEAM_NAME_" + ply_team.ToString(), (Color)gameController.team_colors_bright[ply_team], 7.5f);
                    }
                }
            }
        } 
        last_kill_timer = 0.0f;
        last_kill_ply = defenderPlyId;

        // Add to the kill combo & killstreak, playing voicelines as appropriate
        string KOtext = ""; Color KOColor = Color.white;
        if (VRCPlayerApi.GetPlayerById(defenderPlyId) != null) { KOtext = gameController.localizer.FetchText("NOTIFICATION_KILL", "You knocked out $ARG0!", VRCPlayerApi.GetPlayerById(defenderPlyId).displayName); }
        killbo_timer = 0.0f; 
        killbo++;
        killstreak++;
        if (killbo == 1) {
            if (killstreak == KILLSTREAK_THRESHOLD_4) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Streak4); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_STREAK_TIER4", "Bash Master! (Streak: $ARG0)", killstreak.ToString()); KOColor = Color.magenta; KOColor.r += -0.2f; }
            else if (killstreak == KILLSTREAK_THRESHOLD_3) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Streak3); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_STREAK_TIER3", "Perfect Pummler! (Streak: $ARG0)", killstreak.ToString()); KOColor = Color.magenta; KOColor.g += 0.1f; }
            else if (killstreak == KILLSTREAK_THRESHOLD_2) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Streak2); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_STREAK_TIER2", "Mega Menace! (Streak: $ARG0)", killstreak.ToString()); KOColor = Color.magenta; KOColor.g += 0.2f; }
            else if (killstreak == KILLSTREAK_THRESHOLD_1) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Streak1); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_STREAK_TIER1", "Bone Breaker! (Streak: $ARG0)", killstreak.ToString()); KOColor = Color.magenta; KOColor.g += 0.3f; }
            else if (killstreak == KILLSTREAK_THRESHOLD_0) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Streak0); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_STREAK_TIER0", "Knockout Spree! (Streak: $ARG0)", killstreak.ToString()); KOColor = Color.magenta; KOColor.g += 0.4f; }
        }
        else if (killbo == 2) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Time0); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_COMBO_TIER0", "Double KO!!"); KOColor = Color.magenta; KOColor.g += 0.4f; }
        else if (killbo == 3) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Time1); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_COMBO_TIER1", "Triple KO!!!"); KOColor = Color.magenta; KOColor.g += 0.3f; }
        else if (killbo == 4) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Time2); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_COMBO_TIER2", "Quadtactular!!!!"); KOColor = Color.magenta; KOColor.g += 0.2f; }
        else if (killbo == 5) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Time3); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_COMBO_TIER3", "Pentulimate!!!!!"); KOColor = Color.magenta; KOColor.g += 0.1f; }
        else if (killbo >= 6) { gameController.vopack_selected.PlayVoiceover((int)voiceover_event_name.KO, (int)voiceover_ko_sfx_name.Time4); KOtext += "\n" + gameController.localizer.FetchText("NOTIFICATION_KOSTREAK_COMBO_TIER4", "Outscension! (KO Combo: $ARG0)", killbo.ToString()); KOColor = Color.magenta; KOColor.r += -0.2f; }

        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Kill], gameController.snd_game_sfx_clips[(int)game_sfx_name.Kill]);
        gameController.AddToLocalTextQueue(KOtext, KOColor);
        TryHapticEvent((int)game_sfx_name.Kill);

        // If this is the first KO of the match, snap a highlight photo
        if (gameController.highlight_cameras_snapped != null && gameController.highlight_cameras_snapped.Length > 1
            && gameController.highlight_cameras_waiting_on_sync != null && gameController.highlight_cameras_waiting_on_sync.Length > 1
            && gameController.highlight_cameras_snapped[1] == false && gameController.highlight_cameras_waiting_on_sync[1] == false)
        {
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 1, Vector3.zero, Quaternion.identity, Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation() * Vector3.forward, true, ply_scale);
            gameController.highlight_cameras_waiting_on_sync[1] = true;
        }

        // If this is infection and we are a survivor heal a little bit of damage
        if (gameController.option_gamemode == (int)gamemode_name.Infection && ply_team == 0 && !ply_training) 
        { 
            if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.FlashRecentDamage(ply_dp - (int)GLOBAL_CONST.INFECTION_HEAL_AMOUNT >= 0 ? -(int)GLOBAL_CONST.INFECTION_HEAL_AMOUNT : -ply_dp); }
            ply_dp = Mathf.Max(0.0f, ply_dp - (int)GLOBAL_CONST.INFECTION_HEAL_AMOUNT);
            gameController.local_uiplytoself.UI_Damage();
        }

        // If we are the game master, we don't get an OnDeserialization event for ourselves, so check the round goal whenever we die or get a KO
        if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); }
    }

    public void HandleLocalPlayerDeath()
    {

        bool ply_was_invul = false;
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Death], gameController.snd_game_sfx_clips[(int)game_sfx_name.Death]);
        TryHapticEvent((int)game_sfx_name.Death);
        ply_respawn_timer = 0;
        combo_receive = 0; combo_send = 0; killbo = 0; killstreak = 0; 
        if (ply_training)
        {
            // If we're in training mode, do nothing
        }
        else if (ply_state == (int)player_state_name.Alive && gameController.round_state == (int)round_state_name.Ongoing)
        {
            ply_state = (int)player_state_name.Respawning;

            if (gameController.option_gamemode != (int)gamemode_name.FittingIn
                || (gameController.option_gamemode == (int)gamemode_name.FittingIn && last_hit_by_ply != null)) { ply_deaths++; }
            // Check if we are in a gamemode that tracks lives, and if so, if we are on the team that tracks lives
            if (gameController.option_gamemode == (int)gamemode_name.Survival || (ply_team == 1 && gameController.option_gamemode == (int)gamemode_name.BossBash))
            { 
                ply_lives--;
            }
            //else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill && !ply_training)
            //{
            //    gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_KOTH_RESPAWN_FREEZE", "Frozen during respawn invulnerability! ($ARG0 seconds)", Mathf.RoundToInt(ply_respawn_duration).ToString()), Color.cyan, ply_respawn_duration);
            //}
        }
        else if (ply_state == (int)player_state_name.Dead || gameController.round_state == (int)round_state_name.Ready)
        {
            ply_state = (int)player_state_name.Respawning;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            ply_was_invul = true;
            //if (gameController.option_gamemode == (int)gamemode_name.FittingIn && local_respawn_count >= gameController.option_gm_goal) { ply_lives = 0; }
        }
        else
        {
            //UnityEngine.Debug.Log("Whoa, you died in an unusual way! Contact a developer!");
        }

        if (gameController.local_plyweapon != null) { gameController.local_plyweapon.ResetWeaponToDefault(); gameController.local_plyweapon.CacheWeaponPos(true); }
        if (gameController.local_secondaryweapon != null && gameController.local_secondaryweapon.gameObject.activeInHierarchy) { gameController.local_secondaryweapon.ResetWeaponToDefault(); gameController.local_secondaryweapon.CacheWeaponPos(true); }
        ResetPowerups();
        if (gameController.option_gamemode != (int)gamemode_name.Infection && gameController.option_gamemode != (int)gamemode_name.FittingIn) { ResetToDefaultStats(); }

        //ply_desktop_applied_dual_compensation_buff = false;
        bool is_boss_any = gameController.local_plyweapon.weapon_type_default == (int)weapon_type_name.BossGlove || gameController.local_secondaryweapon.weapon_type_default == (int)weapon_type_name.BossGlove || (gameController.option_gamemode == (int)gamemode_name.BossBash && gameController.local_plyAttr.ply_team == 1);
        bool is_boss_vr = is_boss_any && Networking.LocalPlayer.IsUserInVR();
        if (is_boss_vr) { ply_dual_wield = true; }
        else { ply_dual_wield = false; }

        // Manage behavior based on gamemode
        if (!ply_training && gameController.option_gamemode == (int)gamemode_name.Infection && ply_team != 1)
        {
            UnityEngine.Debug.Log("Requesting game master to change team to Infected...");
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, 1, false);
            ply_team = 1;
            //ply_points = 0;
            InfectionStatReset(ply_was_invul);
            gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_INFECTION_DEATH", "-- You are now Infected! --"), Color.red);
        }
        else if (!ply_training && gameController.option_gamemode == (int)gamemode_name.Infection && ply_team == 1)
        {
            InfectionStatReset(ply_was_invul);
        }
        else if (!ply_training && gameController.option_gamemode == (int)gamemode_name.FittingIn && last_hit_by_ply != null)
        {
            ply_scale += (float)((float)gameController.option_gm_config_a / 100.0f);
            plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
            plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
            plyEyeHeight_change = true;
            if (ply_scale > (float)((float)gameController.option_gm_goal / 100.0f))
            {
                ply_lives = 0;
                gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_FITTINGIN_DEFEAT", "You've grown too big! ($ARG0% vs max of $ARG1%)", (ply_scale * 100.0f).ToString(), gameController.option_gm_goal.ToString()));
            }
        }

        if (!ply_training && ply_lives > 0)
        {
            // Reset the player's damage value to default, unless this is Boss Bash or Fitting In and they fell off without being struck (anti-exploit measure)
            if (!(last_hit_by_ply == null && (gameController.option_gamemode == (int)gamemode_name.BossBash || gameController.option_gamemode == (int)gamemode_name.FittingIn)))
            { ply_dp = ply_dp_default; }
            // Edge case: if this is on infection, it's possible for this event to off after RoundEnd() [usually for the master].
            // So, check if our local team is 1, but the networked team is 0, and if the lives calculated from the networked team is 1 (the lives count for Infection).
            var total_lives = 0; var members_alive = 0;
            gameController.CheckSingleTeamLives(0, gameController.cached_ply_in_game_dict, ref members_alive, ref total_lives);
            if (gameController.option_gamemode == (int)gamemode_name.Infection && ply_team == 1 && gameController.GetGlobalTeam(Networking.LocalPlayer.playerId) == 0
                && total_lives <= 1) { gameController.TeleportLocalPlayerToReadyRoom(); ply_state = (int)player_state_name.Dead; }
            else 
            { 
                if ((gameController.option_gamemode == (int)gamemode_name.Infection && infection_special == 2) || gameController.option_gamemode == (int)gamemode_name.KingOfTheHill) { gameController.TeleportLocalPlayerToKOTHtainer(); }
                else { gameController.TeleportLocalPlayerToGameSpawnZone(); }
            }
        }
        else if (!ply_training)
        {
            gameController.TeleportLocalPlayerToReadyRoom();
            ply_state = (int)player_state_name.Dead;
            gameController.room_training_portal.SetActive(true);
            int team_color_id = 0;
            team_color_id = Mathf.Clamp(ply_team, 0, gameController.team_colors.Length);
            if (gameController.option_teamplay) 
            { 
                gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkAddToTextQueue", "NOTIFICATION_ELIMINATION", Networking.LocalPlayer.displayName, (Color)gameController.team_colors_bright[team_color_id], 5.0f); 
            }
            else
            {
                gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkAddToTextQueue", "NOTIFICATION_ELIMINATION", Networking.LocalPlayer.displayName, Color.red, 5.0f);
            }
        }
        else
        {
            ply_dp = ply_dp_default;
            gameController.TeleportLocalPlayerToTrainingHall();
        }

        if (last_hit_by_ply != null)
        {
            gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_DEATH_OTHER", "Knocked out by $ARG0!", last_hit_by_ply.displayName));
            var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
            if (plyAttr != null) { plyAttr.SendCustomNetworkEvent(NetworkEventTarget.Owner, "KillOtherPlayer", last_hit_by_ply.playerId, Networking.LocalPlayer.playerId, ply_training); }
            last_hit_by_ply = null;
        }
        else
        {
            gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_DEATH_SELF", "Knocked out!"));
        }

        if (gameController != null && gameController.local_plyweapon != null) { gameController.local_plyweapon.ResetWeaponToDefault(); }
        if (gameController != null && gameController.local_secondaryweapon != null) { gameController.local_secondaryweapon.ResetWeaponToDefault(); }

        // If we are the game master, we don't get an OnDeserialization event for ourselves, so check the round goal whenever we die or get a KO
        if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); }
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
                        Vector2 scale_cap = new Vector2(0.15f, 1000.0f);
                        if (ply_training) { scale_cap.y = 5.0f; }
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) 
                        {
                            powerup.powerup_stat_value[i] = Mathf.Clamp(powerup.powerup_stat_value[i], scale_cap.x, scale_cap.y);
                            ply_scale = powerup.powerup_stat_value[i]; 
                        }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) 
                        {
                            if (ply_scale + powerup.powerup_stat_value[i] > scale_cap.y) { powerup.powerup_stat_value[i] = (scale_cap.y - ply_scale); }
                            else if (ply_scale + powerup.powerup_stat_value[i] < scale_cap.x && powerup.powerup_stat_value[i] < 0) { powerup.powerup_stat_value[i] = (scale_cap.x - ply_scale); }
                            else if (ply_scale + powerup.powerup_stat_value[i] < scale_cap.x && powerup.powerup_stat_value[i] > 0) { powerup.powerup_stat_value[i] = (ply_scale - scale_cap.x); }
                            ply_scale += powerup.powerup_stat_value[i]; 
                        }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) 
                        {
                            if (powerup.powerup_stat_value[i] < 0) { powerup.powerup_stat_value[i] = 0; }
                            else if(ply_scale * powerup.powerup_stat_value[i] > scale_cap.y) { powerup.powerup_stat_value[i] = (scale_cap.y / ply_scale); }
                            else if (ply_scale * powerup.powerup_stat_value[i] < scale_cap.x) { powerup.powerup_stat_value[i] = (scale_cap.x / ply_scale); }
                            ply_scale *= powerup.powerup_stat_value[i]; 
                        }
                        // If after managing caps the result is a powerup that does nothing, just set its duration to be minimal
                        //if (powerup.powerup_stat_value[i] == 0) { powerup.powerup_duration = 0.001f; powerup.power }
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
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) 
                        {
                            if (gameController != null && gameController.local_uiplytoself != null) { gameController.local_uiplytoself.FlashRecentDamage(-Mathf.Max(0.0f, Mathf.Abs(ply_dp - powerup.powerup_stat_value[i]))); }
                            ply_dp = powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); 
                        }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) 
                        {
                            if (gameController != null && gameController.local_uiplytoself != null) { gameController.local_uiplytoself.FlashRecentDamage((ply_dp + powerup.powerup_stat_value[i]) >= 0 ? powerup.powerup_stat_value[i] : -ply_dp); }
                            ply_dp += powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); 
                        }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) 
                        {
                            if (gameController != null && gameController.local_uiplytoself != null) { gameController.local_uiplytoself.FlashRecentDamage(-Mathf.Max(0.0f, Mathf.Abs(ply_dp * powerup.powerup_stat_value[i]))); }
                            ply_dp *= powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); 
                        }
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
            //Debug.Log(gameObject.name + ": Attempting to play sound " + powerup.powerup_snd_clips[powerup.powerup_type].name + " for type " + powerup.powerup_type);
            powerups_active = GlobalHelperFunctions.AddToGameObjectArray(powerup_template, powerups_active);
		}

        else
        {
            ItemPowerup powerup = powerup_template.GetComponent<ItemPowerup>();
			for (int i = 0; i < powerup.powerup_stat_behavior.Length; i++)
            {
                //Debug.Log("EXPIRING POWERUP WITH STAT BEHAVIORS " + powerup.powerup_stat_behavior[i].ToString() + " AND STAT VALUES " + powerup.powerup_stat_value[i].ToString());
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
            powerups_active = GlobalHelperFunctions.RemoveEntryFromGameObjectArray(powerup.gameObject, powerups_active);
        }

        if (gameController != null && gameController.local_uiplytoself != null) { gameController.local_uiplytoself.UI_Damage(); }
    }

    public void ResetPowerups()
    {
        if (powerups_are_resetting) { return; }

        if (gameController != null && gameController.local_plyweapon != null)
        {
            bool is_boss_any = gameController.local_plyweapon.weapon_type_default == (int)weapon_type_name.BossGlove || gameController.local_secondaryweapon.weapon_type_default == (int)weapon_type_name.BossGlove || (gameController.option_gamemode == (int)gamemode_name.BossBash && gameController.local_plyAttr.ply_team == 1);
            if (!ply_desktop_applied_dual_compensation_buff && is_boss_any && !Networking.LocalPlayer.IsUserInVR())
            {
                ply_atk *= 2.0f;
                ply_desktop_applied_dual_compensation_buff = true;
                UnityEngine.Debug.Log("[DESKTOP_DUAL_TEST]: Applying compensation from ResetPowerups()");
            }
            else if (ply_desktop_applied_dual_compensation_buff && (!is_boss_any || Networking.LocalPlayer.IsUserInVR()))
            {
                ply_atk /= 2.0f;
                ply_desktop_applied_dual_compensation_buff = false;
                UnityEngine.Debug.Log("[DESKTOP_DUAL_TEST]: Removing compensation from ResetPowerups()");
            }
        }

        var index_iter = 0;
        var powerup_count = powerups_active.Length;
        powerups_are_resetting = true;
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
                powerups_active = GlobalHelperFunctions.RemoveIndexFromGameObjectArray(0, powerups_active);
            }
            index_iter++;
        }
        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
        plyEyeHeight_change = true;

        powerups_are_resetting = false;
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
                float jump_height = 4.0f + (1.0f - ply_grav);
                jump_height = ((2.0f * jump_height) + (jump_height * Mathf.Max(1.0f, ply_scale))) / 3.0f;
                Networking.LocalPlayer.SetVelocity(new Vector3(plyVel.x, jump_height, plyVel.z));
                ply_jumps_tracking++;
            }
            else if (!Networking.LocalPlayer.IsPlayerGrounded() && ply_jumps_tracking >= ply_jumps_add && air_thrust_enabled && air_thrust_ready)
            {
                AirThrust();
            }
            ply_jump_pressed = true;
        }
        else if (!value && ply_jump_pressed) { ply_jump_pressed = false; }
    }

    /*public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        base.InputMoveVertical(value, args);

        if (!Networking.LocalPlayer.IsUserInVR()) { return; }
        if (air_thrust_enabled && air_thrust_ready && Mathf.Abs(value) > 0.75f)
        {
            AirThrust();
        }
    }*/

    public void AirThrust()
    {
        if (Networking.IsOwner(gameObject) && air_thrust_enabled)
        {
            Networking.LocalPlayer.SetVelocity(
                Networking.LocalPlayer.GetVelocity() 
                + Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation 
                * Vector3.forward
                * 4.0f * ((1.0f + ply_speed) / 2.0f)
                * 2.0f
                //* Networking.LocalPlayer.GetRunSpeed() * 2.0f
                );
            air_thrust_ready = false;
            air_thrust_timer = 0.0f;
        }
    }

    public void SetupTutorialMessages()
    {
        if (gameController == null || gameController.local_plyweapon == null) { return; }

        local_tutorial_message_bool = new bool[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];
        local_tutorial_message_str_desktop = new string[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];
        local_tutorial_message_str_vr = new string[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];

        // Tutorial messages for all      
        local_tutorial_message_str_desktop[(int)powerup_type_name.SizeUp] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_SIZEUP", "Increases size, range, and attack/defense! Be a massive threat!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.SizeDown] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_SIZEDOWN", "Decreases size, range, and attack/defense, but increases movement speed! Be a hard to hit menace!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.SpeedUp] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_SPEEDUP", "Dramatically increases movement speed! Nyoom!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.AtkUp] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_ATKUP", "Multiplies damage dealt by a large factor!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.AtkDown] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_DEFUP", "Divides damage dealt by a large factor!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.DefUp] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_ATKDOWN", "Divides damage received by a large factor!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.DefDown] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_DEFDOWN", "Multiplies damage received by a large factor!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.LowGrav] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_LOWGRAV", "Increases time spent in mid-air!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.PartialHeal] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_PARTIALHEAL", "Removes 50% of damage dealt to the user!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.FullHeal] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_FULLHEAL", "Removes 100 % of damage dealt to the user!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.Multijump] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_MULTIJUMP", "Grants an additional jump while in mid-air!");
        local_tutorial_message_str_desktop[(int)powerup_type_name.HighGrav] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_POWERUP_HIGHGRAV", "Decreases time spent in mid-air!");

        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.PunchingGlove] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_PUNCHINGGLOVE", "The default weapon. Push your fire key to knock opponents out of the arena! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.PunchingGlove)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.Bomb] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_BOMB_DESKTOP", "Push your fire key to toss it forward! It will detonate after $ARG0 seconds! (Power: $ARG1)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Projectile_Duration].ToString(), gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.Rocket] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_ROCKET", "Fire off projectiles that will explode in a radius! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Rocket)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.BossGlove] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_BOSSGLOVE", "Used by the Big Boss during the Boss Bash gamemode. Has a much bigger hitbox. Attack rate scales with # of players in-game! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.BossGlove)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.HyperGlove] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_HYPERGLOVE", "Hyper-fast attacks, but less damage! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.HyperGlove)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.MegaGlove] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_MEGAGLOVE", "Mega damage, but slow to fire! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.MegaGlove)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.SuperLaser] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_SUPERLASER_DESKTOP", "Hold down your fire key to charge it up and fire a huge beam! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.SuperLaser)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_THROWABLEITEM_DESKTOP", "Push your fire key to toss it forward! (Contains: $ARG0)", "$NAME");

        // VR-specific messages
        local_tutorial_message_str_vr[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.Bomb] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_BOMB_VR", "Toss it by releasing your Grip! It will detonate after $ARG0 seconds!", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Projectile_Duration].ToString());
        local_tutorial_message_str_vr[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.SuperLaser] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_SUPERLASER_VR", "Hold down your Trigger to charge it up and fire a huge beam! (Power: $ARG0)", gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.SuperLaser)[(int)weapon_stats_name.Hurtbox_Damage].ToString());
        local_tutorial_message_str_vr[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem] = gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_THROWABLEITEM_VR", "Toss it by releasing your Grip! (Contains: $ARG0)", "$NAME");

        for (int i = 0; i < (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH; i++)
        {
            local_tutorial_message_bool[i] = false;
            if (local_tutorial_message_str_desktop[i] != "" && i < (int)powerup_type_name.ENUM_LENGTH)
            {
                local_tutorial_message_str_desktop[i] = gameController.PowerupTypeToStr(i).ToUpper() + ": " + local_tutorial_message_str_desktop[i];
            }
            else if (local_tutorial_message_str_desktop[i] != "" && i >= (int)powerup_type_name.ENUM_LENGTH) 
            { 
                local_tutorial_message_str_desktop[i] = gameController.WeaponTypeToStr(i - (int)powerup_type_name.ENUM_LENGTH).ToUpper() + ": " + local_tutorial_message_str_desktop[i]; 
            }

            if (local_tutorial_message_str_vr[i] != "")
            {
                local_tutorial_message_str_vr[i] = local_tutorial_message_str_desktop[i].Replace(gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_GENERIC_DESKTOP", "fire key"), gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_GENERIC_VR", "Trigger"));
            }
        }

        tutorial_messages_ready = true;
    }

    public void ResetTutorialMessage(int item_type = -1)
    {
        if (local_tutorial_message_bool == null) { return; }
        if (item_type < 0)
        {
            for (int i = 0; i < (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH; i++)
            {
                local_tutorial_message_bool[i] = false;
            }
        }
        else
        {
            if (item_type >= local_tutorial_message_bool.Length) { return; }
            local_tutorial_message_bool[item_type] = false;
        }
    }

    public void SendTutorialMessage(int item_type)
    {
        if (!tutorial_messages_ready && Networking.IsOwner(gameObject) && gameController != null) { SetupTutorialMessages(); }

        // Send a tutorial message
        if (tutorial_messages_ready && Networking.IsOwner(gameObject) && gameController != null && local_tutorial_message_bool != null && !local_tutorial_message_bool[item_type])
        {
            string display_str = local_tutorial_message_str_desktop[item_type];
            if (Networking.LocalPlayer.IsUserInVR()) { display_str = local_tutorial_message_str_vr[item_type]; }
            if (item_type == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem)
            {
                string item_name = "";
                if (gameController.local_plyweapon.weapon_extra_data < (int)powerup_type_name.ENUM_LENGTH) { item_name = gameController.PowerupTypeToStr(gameController.local_plyweapon.weapon_extra_data); }
                else { item_name = gameController.WeaponTypeToStr(gameController.local_plyweapon.weapon_extra_data - (int)powerup_type_name.ENUM_LENGTH); }
                display_str = display_str.Replace("$NAME", item_name);
            }

            Color display_color = Color.cyan;
            if (item_type >= (int)powerup_type_name.ENUM_LENGTH) { display_color = new Color(1.0f, 0.5f, 0.0f, 1.0f); }
            else if (item_type == (int)powerup_type_name.AtkDown || item_type == (int)powerup_type_name.DefDown || item_type == (int)powerup_type_name.HighGrav) { display_color = Color.red; }

            if (display_str != "")
            {
                gameController.AddToLocalTextQueue(display_str, display_color, 8.0f);
            }
            local_tutorial_message_bool[item_type] = true;
        }
    }

    public string GetTutorialMessage(int item_type)
    {
        // Send a tutorial message
        string display_str = "";
        if (Networking.IsOwner(gameObject) && gameController != null && local_tutorial_message_str_desktop != null && local_tutorial_message_str_desktop.Length > item_type)
        {
            display_str = local_tutorial_message_str_desktop[item_type];
            if (Networking.LocalPlayer.IsUserInVR()) { display_str = local_tutorial_message_str_vr[item_type]; }
            if (display_str != "")
            {
                string[] split_str = display_str.Split(" (Power: ");
                if (split_str != null && split_str.Length >= 2)
                {
                    display_str = split_str[0];
                }
                if (item_type == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem)
                {
                    display_str = display_str.Replace("(Contains: $NAME)", gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_THROWABLEITEM_GENERIC", "Contains a random powerup or weapon!"));
                    display_str = display_str.Replace("（内容物：$NAME）", gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_THROWABLEITEM_GENERIC", "Contains a random powerup or weapon!"));
                    display_str = display_str.Replace("(Trae: NAME)", gameController.localizer.FetchText("NOTIFICATION_TUTORIAL_WEAPON_THROWABLEITEM_GENERIC", "Contains a random powerup or weapon!"));
                }
            }

            Color display_color = Color.cyan;
            if (item_type >= (int)powerup_type_name.ENUM_LENGTH) { display_color = new Color(1.0f, 0.5f, 0.0f, 1.0f); }

        }
        return display_str;
    }

    [NetworkCallable]
    public void BecomeZombig(bool isFirst)
    {
        ResetToDefaultStats();
        if (isFirst) 
        {
            TryHapticEvent((int)game_sfx_name.Death);
            ply_respawn_timer = 0;
            ply_respawn_duration = gameController.plysettings_respawn_duration * 1.25f;
            ply_state = (int)player_state_name.Respawning;
            gameController.TeleportLocalPlayerToKOTHtainer();
        }
        else { gameController.TeleportLocalPlayerToGameSpawnZone(); }
        infection_special = 2;
        ply_dp = ply_dp_default;
        ply_scale *= 2.0f;
        ply_speed *= 1.33f;
        combo_send_duration = killbo_duration;

        gameController.local_plyweapon.weapon_temp_ammo = -1;
        gameController.local_plyweapon.weapon_temp_duration = -1;
        gameController.local_plyweapon.weapon_temp_timer = 0.0f;
        gameController.local_plyweapon.weapon_type = (int)weapon_type_name.MegaGlove;
        gameController.local_plyweapon.weapon_extra_data = 0;

        if (gameController.local_uiplytoself != null && gameController.template_ItemSpawner != null) 
        {
            ItemWeapon iweapon = gameController.template_ItemSpawner.GetComponent<ItemSpawner>().child_weapon;
            gameController.local_uiplytoself.PTSWeaponSprite.sprite = iweapon.iweapon_sprites[(int)weapon_type_name.MegaGlove];
            gameController.PlaySFXFromArray(gameController.local_plyweapon.snd_source_weaponcharge, iweapon.iweapon_snd_clips, iweapon.iweapon_type);
        }
        gameController.local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
        plyEyeHeight_change = true;
        if (isFirst) { gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_INFECTION_ZOMBIG_LOCAL", "You are the ZomBig! Crush the Survivors into dust!"), gameController.team_colors_bright[1]); }
    }

    [NetworkCallable]
    public void InfectionStatReset(bool ply_was_invul)
    {
        combo_send_duration = 2.0f;
        if (infection_special == 2)
        {
            // ZomBig
            if (ply_was_invul)
            {
                BecomeZombig(false);
            }
            else
            {
                ResetToDefaultStats();
                ply_respawn_duration = gameController.plysettings_respawn_duration;
                infection_special = 1; // We set this to 1 because GameController will automatically resolve it down to 0 if the player count condition is met
                gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForZombigs", Networking.LocalPlayer.playerId);
                gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkAddToTextQueue", "NOTIFICATION_INFECTION_ZOMBIG_DEATH", "", Color.red, 5.0f);
            }
        }
        else if (infection_special == 1)
        {
            // Patient Zero
            ply_speed = gameController.plysettings_speed * 1.33f;
            ply_def = gameController.plysettings_def * 0.6f;
            ply_atk = gameController.plysettings_atk * 1.5f;
        }
        else if (infection_special == 0)
        { 
            // Normal Zombie
            //ply_speed = gameController.plysettings_speed * 0.9f;
            ply_def = gameController.plysettings_def * 0.4f;
            ply_atk = gameController.plysettings_atk * 0.5f;
        }
    }

    [NetworkCallable]
    public void ResetToDefaultStats()
    {
        combo_send_duration = 2.0f;
        ply_dp = ply_dp_default;
        //ply_damage_dealt = 0;
        //ply_lives = gameController.plysettings_lives;
        //ply_points = gameController.plysettings_points;
        //ply_respawn_duration = gameController.plysettings_respawn_duration;
        ply_scale = gameController.plysettings_scale;
        ply_speed = gameController.plysettings_speed;
        ply_atk = gameController.plysettings_atk;
        ply_def = gameController.plysettings_def;
        ply_grav = gameController.plysettings_grav;
        if (gameController.mapscript_list != null && gameController.map_selected >= 0 && gameController.map_selected < gameController.mapscript_list.Length && gameController.mapscript_list[gameController.map_selected] != null)
        {
            ply_grav *= gameController.mapscript_list[gameController.map_selected].map_gravity_scale;
        }

        if (gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1)
        {
            //playerData.ply_lives = option_goal_value_b;
            ply_scale = gameController.plysettings_scale * gameController.plysettings_boss_scale_mod;
            ply_atk = gameController.plysettings_atk + gameController.plysettings_boss_atk_mod; // (ply_parent_arr[0].Length / 4.0f);
            ply_def = gameController.plysettings_def + gameController.plysettings_boss_def_mod; //+ Mathf.Max(0.0f, -0.2f + (ply_parent_arr[0].Length * 0.2f));
            ply_speed = gameController.plysettings_speed + gameController.plysettings_boss_speed_mod;
        }

        ply_desktop_applied_dual_compensation_buff = false;
        if (gameController.local_plyweapon != null && gameController.local_secondaryweapon != null)
        {
            bool is_boss_desktop = !Networking.LocalPlayer.IsUserInVR() && gameController.local_plyweapon.weapon_type_default == (int)weapon_type_name.BossGlove || gameController.local_secondaryweapon.weapon_type_default == (int)weapon_type_name.BossGlove || (gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1);
            if (is_boss_desktop && !ply_desktop_applied_dual_compensation_buff)
            {
                ply_atk *= 2.0f;
                ply_desktop_applied_dual_compensation_buff = true;
                UnityEngine.Debug.Log("[DESKTOP_DUAL_TEST]: Applying compensation from ResetToDefaultStats()");
            }
            /*else if (!is_boss_desktop && ply_desktop_applied_dual_compensation_buff)
            {
                ply_atk -= 1.0f;
                ply_desktop_applied_dual_compensation_buff = false;
                UnityEngine.Debug.Log("[DESKTOP_DUAL_TEST]: Removing compensation from ResetToDefaultStats()");
            }*/
        }

        if (gameController.local_plyweapon != null)
        {
            if (!(gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1)) { gameController.local_plyweapon.weapon_type_default = gameController.plysettings_weapon; }
            gameController.local_plyweapon.weapon_type = gameController.local_plyweapon.weapon_type_default;
            if (gameController.local_plyweapon.weapon_type_default != (int)weapon_type_name.PunchingGlove)
            {
                gameController.local_plyweapon.weapon_temp_ammo = -1;
                gameController.local_plyweapon.weapon_temp_duration = -1;
            }
            gameController.local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");

        }

        if (gameController.local_secondaryweapon != null && gameController.local_secondaryweapon.gameObject.activeInHierarchy)
        {
            if (!(gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1)) { gameController.local_secondaryweapon.weapon_type_default = gameController.plysettings_weapon; }
            gameController.local_secondaryweapon.weapon_type = gameController.local_secondaryweapon.weapon_type_default;
            if (gameController.local_secondaryweapon.weapon_type_default != (int)weapon_type_name.PunchingGlove)
            {
                gameController.local_secondaryweapon.weapon_temp_ammo = -1;
                gameController.local_secondaryweapon.weapon_temp_duration = -1;
            }
            gameController.local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        }

        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
        plyEyeHeight_change = true;
    }


}
