
using System;
using System.Net.Sockets;
using UdonSharp;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum weapon_type_name // NOTE: NEEDS TO ALSO BE CHANGED IN GAMECONTROLLER IF ANY ARE ADDED/REMOVED FOR KeyToWeaponType()
{
    PunchingGlove, Bomb, Rocket, BossGlove, HyperGlove, MegaGlove, SuperLaser, ThrowableItem, ENUM_LENGTH
}
public class PlayerWeapon : UdonSharpBehaviour
{
    // References
    [SerializeField] public GameObject[] weapon_mdl;
    [SerializeField] public GameController gameController;
    [SerializeField] public VRCPickup pickup_component;
    [SerializeField] public Rigidbody pickup_rb;
    [SerializeField] public WeaponHurtbox hurtbox_melee;
    [SerializeField] public WeaponHurtbox[] hurtboxes_pool;
    [SerializeField] public WeaponProjectile[] projectiles_pool;
    [SerializeField] public AudioSource snd_source_weaponfire;
    [SerializeField] public AudioSource snd_source_weaponcharge;
    [SerializeField] public AudioSource snd_source_weaponcontact;
    [SerializeField] public AudioClip snd_game_sfx_clip_weaponexpire;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponfire; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcharge; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcontact; // NOTE: Corresponds to weapon_type
    // Weapon type
    [NonSerialized][UdonSynced] public int weapon_type;
    [NonSerialized] public int local_weapon_type = -1;
    [NonSerialized][UdonSynced] public int weapon_type_default;
    [NonSerialized][UdonSynced] public byte weapon_extra_data = 0;
    [NonSerialized] public byte local_extra_data = 0;
    [SerializeField][UdonSynced] public bool is_secondary = false;
    // Cooldown variables
    [NonSerialized] public float use_cooldown;
    [NonSerialized] public float use_timer;
    [NonSerialized] public bool use_ready;
    [NonSerialized] public bool waiting_for_toss = false;
    // Temporary ammo (from pickups)
    [NonSerialized] public int weapon_temp_ammo = -1;
    [NonSerialized] public float weapon_temp_duration = -1.0f;
    [NonSerialized] public float weapon_temp_timer = 0.0f;
    // Charging variables (for laser)
    [NonSerialized][UdonSynced] public bool weapon_is_charging = false;
    [NonSerialized][UdonSynced] public double weapon_charge_start_ms = 0.0f;
    [NonSerialized][UdonSynced] public double weapon_charge_duration = 0.0f;
    [NonSerialized] public double weapon_charge_timer = 0.0f;
    // Animation variables
    [NonSerialized][UdonSynced] public bool animate_active = false;
    [NonSerialized][UdonSynced] public int animate_state = 0;
    [NonSerialized][UdonSynced] public float animate_pct;
    [NonSerialized][UdonSynced] public float animate_stored_pct = 1.0f;
    [NonSerialized][UdonSynced] public bool animate_handled_by_hurtbox = false;
    [NonSerialized][UdonSynced] public float animate_stored_use_timer = 0.0f;
    // Haptics variables
    [SerializeField] public float[] haptic_cooldowns; // NEEDS TO MATCH LENGTH OF game_sfx_name
    [NonSerialized] public float haptic_countdown = 0.0f; // We call this countdown rather than timer since it decrements rather than increments
    [NonSerialized] public int haptic_cooldown_type = -1;
    // Cached variables
    [NonSerialized] public PlayerAttributes owner_attributes;
    [NonSerialized] public Renderer weapon_renderer;
    [NonSerialized] public Animator weapon_animator;
    [NonSerialized] public Transform cached_laser_particle;
    [NonSerialized] public Transform cached_laser_sprite;
    [NonSerialized] public float scale_inital = -1;
    // Misc Networked variables
    [NonSerialized][UdonSynced] public Vector3 velocity_stored = Vector3.zero;
    [NonSerialized][UdonSynced] public bool network_active = false;
    // Misc Local variables
    [NonSerialized] private float tick_timer = 0.0f;
    [NonSerialized] public float[][] all_weapon_stats;
    [NonSerialized] private float cached_scale = -1.0f;
    [NonSerialized] private int cached_team = -1;

    private void Start()
    {
        scale_inital = transform.localScale.x;
        all_weapon_stats = SetupAllWeaponStats();

        weapon_type_default = (int)weapon_type_name.PunchingGlove;
        ResetWeaponToDefault();

        if (pickup_component == null) { pickup_component = gameObject.GetComponent<VRCPickup>(); }
        if (pickup_rb == null) { pickup_rb = gameObject.GetComponent<Rigidbody>(); }

        if (hurtboxes_pool == null || hurtboxes_pool.Length == 0)
        {
            int hurtboxes_cnt = 0;
            foreach (Transform child in transform)
            {
                if (child.name.Contains("WeaponHurtboxRanged"))
                {
                    hurtboxes_cnt++;
                }
            }
            hurtboxes_pool = new WeaponHurtbox[hurtboxes_cnt];
            foreach (Transform child in transform)
            {
                if (child.name.Contains("WeaponHurtboxRanged"))
                {
                    hurtboxes_pool[hurtboxes_cnt] = child.GetComponent<WeaponHurtbox>();
                }
            }
        }


        if (projectiles_pool == null || projectiles_pool.Length == 0)
        {
            int projectiles_cnt = 0;
            foreach (Transform child in transform)
            {
                if (child.name.Contains("WeaponProjectile"))
                {
                    projectiles_cnt++;
                }
            }
            projectiles_pool = new WeaponProjectile[projectiles_cnt];
            foreach (Transform child in transform)
            {
                if (child.name.Contains("WeaponProjectile"))
                {
                    projectiles_pool[projectiles_cnt] = child.GetComponent<WeaponProjectile>();
                }
            }
        }

    }

    private void OnEnable()
    {
        UpdateStatsFromWeaponType();
        if (!Networking.IsOwner(gameObject)) { return; }
        network_active = true;
    }

    private void OnDisable()
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        network_active = false;
    }

    public override void OnDeserialization()
    {
        if (local_weapon_type != weapon_type || local_extra_data != weapon_extra_data) { UpdateStatsFromWeaponType(); }
        if (owner_attributes != null && cached_team != owner_attributes.ply_team) { SetTeamColor();  }
        gameObject.SetActive(network_active);
    }

    private void Update()
    {
        // Tick Timer
        tick_timer += Time.deltaTime;

        if (tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            UpdatePerActiveTick();
            tick_timer = 0.0f;
        }

        if (owner_attributes == null) { return; }

        // Weapon use timer
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
        }
        else if ((gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && !use_ready)
        {
            animate_active = false;
            use_ready = true;
            ToggleParticle(true);
            // For bombs, we use the charge sound as the "ready to be thrown again" SFX, but only if we are the owner
            if (Networking.GetOwner(gameObject) == Networking.LocalPlayer && (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem))
            { gameController.PlaySFXFromArray(snd_source_weaponcharge, snd_game_sfx_clips_weaponcharge, weapon_type, 1.0f); }
        }
        else if (!(gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && use_ready)
        {
            animate_active = false;
            use_ready = false;
            ToggleParticle(false);
        }

        // Animation variables
        if (animate_active && weapon_animator != null)
        {
            weapon_animator.SetFloat("AnimationTimer", animate_pct);
            weapon_animator.SetInteger("AnimationState", animate_state);
        }

        // -- Owner Only Events --
        if (!Networking.IsOwner(gameObject)) { return; }

        // Haptic countdown decrementing timer
        if (haptic_cooldown_type >= 0 && haptic_countdown > 0.0f)
        {
            haptic_countdown -= Time.deltaTime;
        }
        else if (haptic_cooldown_type >= 0 && haptic_countdown <= 0.0f)
        {
            haptic_countdown = 0.0f;
            haptic_cooldown_type = -1;
        }

        // Weapon powerup timer
        if (weapon_type != weapon_type_default)
        {
            if (weapon_temp_duration > 0.0f && weapon_temp_timer < weapon_temp_duration)
            {
                weapon_temp_timer += Time.deltaTime;
            }
            else if (weapon_temp_duration > 0.0f && weapon_temp_timer >= weapon_temp_duration)
            {
                weapon_temp_duration = -1.0f;
                ResetWeaponToDefault(true);
            }
            if (weapon_temp_ammo != -1 && weapon_temp_ammo <= 0)
            {
                ResetWeaponToDefault(true);
            }
        }

        // Handle weapon charging timer
        if (weapon_is_charging && weapon_charge_duration > 0.0f)
        {
            weapon_charge_timer = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), weapon_charge_start_ms);
            if (weapon_charge_timer > weapon_charge_duration)
            {
                FireWeapon();
                weapon_is_charging = false;
                weapon_charge_timer = 0.0f;
                if (weapon_type == (int)weapon_type_name.SuperLaser) { animate_state = 2; }
            }
        }
    }

    private void UpdatePerActiveTick()
    {

        if (!pickup_component.IsHeld)
        {
            TeleportWeaponToOwner();
        }
        else if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.ThrowableItem)
        {
            pickup_rb.velocity = Vector3.zero;
            pickup_rb.angularVelocity = Vector3.zero;
        }
        
        if (owner_attributes != null && cached_scale != owner_attributes.ply_scale)
        {
            ScaleWeapon();
            cached_scale = owner_attributes.ply_scale;
        }

        ProcessAnimations();
    }

    private void ProcessAnimations()
    {
        if (use_timer < use_cooldown)
        {
            if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.Rocket && weapon_type != (int)weapon_type_name.SuperLaser)
            {
                if (!animate_handled_by_hurtbox) { animate_pct = Mathf.Lerp(animate_stored_pct, 0.0f, (float)((use_timer - animate_stored_use_timer) / (use_cooldown - animate_stored_use_timer))); }
            }
            else if (weapon_type == (int)weapon_type_name.Rocket)
            {
                float rocket_fire_pct = 0.25f;
                float timer_at_pct = rocket_fire_pct * use_cooldown;
                float use_pct = (float)(use_timer / use_cooldown);
                if (use_pct < rocket_fire_pct)
                {
                    use_pct = (float)use_pct / rocket_fire_pct;
                    animate_pct = Mathf.Lerp(0.0f, 0.999f, use_pct);
                }
                else
                {
                    use_pct = (float)((use_timer - timer_at_pct) / (use_cooldown - timer_at_pct));
                    animate_pct = Mathf.Lerp(0.999f, 0.0f, use_pct);
                }
            }
        }
        else if ((gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && !use_ready && weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.SuperLaser)
        {
            animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false;    
        }
        else if (!(gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && use_ready && weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.SuperLaser)
        {
            animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false;
        }

        if (weapon_type == (int)weapon_type_name.SuperLaser)
        {
            if (animate_state == 0)
            {
                if (cached_laser_sprite != null) { cached_laser_sprite.localScale = Vector3.zero; }
                if (cached_laser_particle != null && cached_laser_particle.gameObject.activeInHierarchy) { cached_laser_particle.gameObject.SetActive(false); }
            }
            else if (animate_state == 1)
            {
                animate_pct = Mathf.Lerp(0.0f, 0.999f, System.Convert.ToSingle(weapon_charge_timer / weapon_charge_duration));
                if (cached_laser_sprite != null) { cached_laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.35f * animate_pct; }
                if (cached_laser_particle != null && cached_laser_particle.gameObject.activeInHierarchy) { cached_laser_particle.gameObject.SetActive(true); }
            }
            else if (animate_state == 2)
            {
                float laser_fire_pct = 0.4f;
                float timer_at_pct = laser_fire_pct * use_cooldown;
                float use_pct = (float)(use_timer / use_cooldown);
                if (use_pct < laser_fire_pct)
                {
                    use_pct = (float)use_pct / laser_fire_pct;
                    animate_pct = Mathf.Lerp(0.0f, 0.5f, use_pct);
                    if (cached_laser_sprite != null) { cached_laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.35f; }
                }
                else
                {
                    use_pct = (float)((use_timer - timer_at_pct) / (use_cooldown - timer_at_pct));
                    animate_pct = Mathf.Lerp(0.5f, 0.999f, use_pct);
                    if (cached_laser_sprite != null) { cached_laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.3f * Mathf.Lerp(1.0f, 0.0f, use_pct); }
                }
                if (use_ready) { animate_state = 0; }
                if (cached_laser_particle != null && cached_laser_particle.gameObject.activeInHierarchy) { cached_laser_particle.gameObject.SetActive(false); }
            }
        }
    }

    private void TeleportWeaponToOwner()
    {
        // -- Owner Only Event --
        if (!Networking.IsOwner(gameObject)) { return; }
        // Reposition the object if we're not holding it
        float ply_scale_offset = 1.0f;
        if (gameController != null && gameController.local_plyAttr != null && gameController.local_plyAttr.ply_scale != 1.0f) { ply_scale_offset = gameController.local_plyAttr.ply_scale; }
        if (Networking.LocalPlayer.IsUserInVR())
        {
            if (gameController.local_secondaryweapon != null && gameController.local_secondaryweapon.gameObject.activeInHierarchy)
            {
                transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * 0.5f * ply_scale_offset);
                transform.position = transform.position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.right * 0.2f * ply_scale_offset);
                if (is_secondary) { transform.position = transform.position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.right * (-0.2f * 2.0f) * ply_scale_offset); }
            }
            else
            {
                transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * 0.5f * ply_scale_offset);
            }
        }
        else if (!Networking.LocalPlayer.IsUserInVR())
        {
            transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + new Vector3(1.0f * ply_scale_offset, 0.0f, 0.0f);
            if (is_secondary) { transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + new Vector3(1.0f * ply_scale_offset, 0.0f, 0.5f * ply_scale_offset); }
        }
        pickup_rb.velocity = Vector3.zero;
        pickup_rb.angularVelocity = Vector3.zero;
    }

    private WeaponProjectile GetInactiveProjectile()
    {
        for (int i = 0; i < projectiles_pool.Length; i++)
        {
            if (projectiles_pool[i] == null) { continue; }
            if (!projectiles_pool[i].gameObject.activeInHierarchy) { return projectiles_pool[i]; }
        }
        return null;
    }

    private WeaponHurtbox GetInactiveRangedHurtbox()
    {
        for (int i = 0; i < hurtboxes_pool.Length; i++)
        {
            if (hurtboxes_pool[i] == null) { continue; }
            if (!hurtboxes_pool[i].gameObject.activeInHierarchy) { return hurtboxes_pool[i]; }
        }
        return null;
    }

    [NetworkCallable]
    public void ToggleActive(bool toggle)
    {
        if (pickup_component != null && pickup_component.IsHeld) { pickup_component.Drop(); }
        network_active = toggle;
        gameObject.SetActive(toggle);
    }

    public override void OnPickup()
    {
        waiting_for_toss = (Networking.GetOwner(gameObject).IsUserInVR() && (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem));
        if (pickup_component != null && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            pickup_component.pickupable = false;
            if (gameController != null && gameController.local_ppp_options != null && gameController.local_ppp_options.ppp_pickup != null) { gameController.local_ppp_options.ppp_pickup.GetComponent<VRC_Pickup>().pickupable = false; }
        }
        pickup_rb.velocity = Vector3.zero;
        pickup_rb.angularVelocity = Vector3.zero;
    }

    public override void OnDrop()
    {
        if (waiting_for_toss && use_ready && (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem))
        {
            TossWeapon();
        }
        if (pickup_component != null && Networking.IsOwner(gameObject))
        {
            pickup_component.pickupable = true;
            if (gameController != null && gameController.local_ppp_options != null && gameController.local_ppp_options.ppp_pickup != null)
            { gameController.local_ppp_options.ppp_pickup.GetComponent<VRC_Pickup>().pickupable = true; }
        }
    }

    public override void OnPickupUseUp()
    {
        // Release use
        if (weapon_is_charging)
        {
            weapon_is_charging = false;
            weapon_charge_timer = 0.0f;
            if (use_ready && snd_source_weaponcharge != null) { snd_source_weaponcharge.Stop(); }
            if (weapon_type == (int)weapon_type_name.SuperLaser && animate_state != 2) { animate_state = 0; }
            animate_active = false;
        }
    }

    public override void OnPickupUseDown()
    {
        // Press use
        if (gameController == null) { return; }
        if (!use_ready) { return; }
        animate_active = true;
        if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem)
        {
            if (!waiting_for_toss)
            {
                waiting_for_toss = true;
                if (pickup_rb != null) { pickup_rb.useGravity = true; }
                if (!Networking.GetOwner(gameObject).IsUserInVR())
                {
                    // If we are a desktop user, just have it left click toss after the cooldown
                    TossWeapon();
                }
            }
            return;
        }
        else if (weapon_type == (int)weapon_type_name.SuperLaser)
        {
            if (!weapon_is_charging)
            {
                gameController.PlaySFXFromArray(snd_source_weaponcharge, snd_game_sfx_clips_weaponcharge, weapon_type, 1.0f);
                weapon_charge_start_ms = Networking.GetServerTimeInSeconds();
                weapon_charge_timer = 0.0f;
                weapon_is_charging = true;
                animate_state = 1;
            }
            return;
        }

        FireWeapon();
    }


    private void TossWeapon()
    {
        // Only manually apply velocity in non-VR
        // If it's a throwable and has just been thrown, however, we want to create a flying projectile in its direction
        //if (pickup_rb != null) { pickup_rb.isKinematic = false; }
        if (!Networking.GetOwner(gameObject).IsUserInVR())
        {
            Vector3 throwDir = Networking.GetOwner(gameObject).GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
            float throwForce = 11.0f;
            if (pickup_rb != null) { velocity_stored = pickup_rb.velocity + (throwDir * throwForce); }
        }
        else if (pickup_rb != null) { velocity_stored = pickup_rb.velocity * 2.5f; }

        FireWeapon();
        waiting_for_toss = false;
    }

    private void FireWeapon()
    {
        if (!Networking.IsOwner(gameObject)) { return; }

        use_ready = false;
        use_timer = 0;
        if (weapon_type != (int)weapon_type_name.Bomb) { animate_pct = 0.0f; }
        ToggleParticle(false);
        if (weapon_temp_ammo != -1) { weapon_temp_ammo--; }
        animate_active = true;

        bool use_melee = false; Vector3 pos_start = transform.position;
        // Play the firing sound as applicable
        float pitch_low = 0.5f; float pitch_high = 1.5f;
        if (weapon_type == (int)weapon_type_name.PunchingGlove) { pitch_low = 0.5f; pitch_high = 1.5f; use_melee = true; }
        else if (weapon_type == (int)weapon_type_name.BossGlove) { pitch_low = 0.75f; pitch_high = 1.25f; use_melee = true; }
        else if (weapon_type == (int)weapon_type_name.HyperGlove) { pitch_low = 1.5f; pitch_high = 2.0f; use_melee = true; }
        else if (weapon_type == (int)weapon_type_name.MegaGlove) { pitch_low = 0.5f; pitch_high = 0.75f; use_melee = true; }
        else if (weapon_type == (int)weapon_type_name.Rocket) { pitch_low = 1.0f; pitch_high = 1.0f; use_melee = false; }
        else if (weapon_type == (int)weapon_type_name.SuperLaser) { pitch_low = 1.0f; pitch_high = 1.0f; use_melee = false; }
        else if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem)
        {
            pitch_low = 1.0f; pitch_high = 1.0f; use_melee = false;
            if (gameController.local_plyAttr != null) { pos_start += Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * gameController.local_plyAttr.ply_scale * 1.0f; }
        }
        gameController.PlaySFXFromArray(
            snd_source_weaponfire
            , snd_game_sfx_clips_weaponfire
            , weapon_type
            , UnityEngine.Random.Range(pitch_low, pitch_high)
        );

        float distance = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance];
        if (owner_attributes == null) { return; }
        distance *= owner_attributes.ply_scale; // scale distance with player size

        PlayHapticEvent((int)game_sfx_name.ENUM_LENGTH);  // ENUM_LENGTH is used for weapon fire

        if (use_melee)
        {
            float damage = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Damage];
            damage *= owner_attributes.ply_atk * (owner_attributes.ply_scale * gameController.scale_damage_factor);
            SendCustomNetworkEvent(
                VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateHurtBox"
                , transform.position
                , transform.rotation
                , new Vector4(damage, owner_attributes.ply_scale, 1.0f, weapon_extra_data)
                , use_melee
                , Networking.LocalPlayer.playerId
                , weapon_type
                , is_secondary
                );
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All
            , "NetworkCreateProjectile"
            , weapon_type
            , pos_start
            , transform.rotation
            , new Vector3(distance, owner_attributes.ply_scale, weapon_extra_data)
            , velocity_stored
            , use_melee
            , Networking.LocalPlayer.playerId
            );
        }

        // After throwing an item, reroll the item on it, if it has more than 1 ammo
        if (weapon_type == (int)weapon_type_name.ThrowableItem)
        {
            weapon_extra_data = RollForPowerupBombExtraData();
            OnDeserialization();
        }
    }

    [NetworkCallable]
    public void NetworkCreateProjectile(int weapon_type, Vector3 fire_start_pos, Quaternion fire_angle, Vector3 float_in, Vector3 fire_velocity, bool keep_parent, int player_id)
    {
        float[] stats_from_weapon = GetStatsFromWeaponType(weapon_type);

        float distance = float_in.x;
        float player_scale = float_in.y;
        byte extra_data = (byte)float_in.z;

        WeaponProjectile projectile = GetInactiveProjectile();
        if (projectile == null) { return; }
        GameObject newProjectileObj = projectile.gameObject;
        if (newProjectileObj == null) { return; }
        projectile.ResetProjectile();

        newProjectileObj.transform.parent = null;
        newProjectileObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * stats_from_weapon[(int)weapon_stats_name.Projectile_Size] * player_scale;
        newProjectileObj.transform.SetPositionAndRotation(fire_start_pos, fire_angle);
        if (projectile.rb != null)
        {
            projectile.rb.position = fire_start_pos;
            projectile.rb.rotation = fire_angle;
            projectile.previousPosition = projectile.rb.position;
            projectile.rb.velocity = Vector3.zero;
            projectile.rb.angularVelocity = Vector3.zero;
        }
        else { projectile.previousPosition = newProjectileObj.transform.position; }
        projectile.weapon_type = weapon_type;

        projectile.projectile_type = (int)stats_from_weapon[(int)weapon_stats_name.Projectile_Type];
        projectile.projectile_duration = stats_from_weapon[(int)weapon_stats_name.Projectile_Duration];
        projectile.projectile_start_ms = Networking.GetServerTimeInSeconds();
        projectile.pos_start = fire_start_pos;
        projectile.projectile_distance = distance;
        projectile.owner_scale = player_scale;
        projectile.extra_data = extra_data;
        PlayerWeapon source_weaponScript = gameController.GetPlayerWeaponFromID(player_id);
        projectile.weapon_script = source_weaponScript;

        if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.ThrowableItem) { projectile.rb.velocity = Vector3.zero; }

        if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem)
        {
            projectile.rb.velocity = fire_velocity;
            projectile.has_physics = true;
            projectile.rb.useGravity = true;
            projectile.GetComponent<Collider>().isTrigger = false;

            if (source_weaponScript != null)
            {
                source_weaponScript.pickup_rb.useGravity = false;
                source_weaponScript.pickup_rb.velocity = Vector3.zero;
            }
        }

        projectile.UpdateProjectileModel();
        newProjectileObj.SetActive(true);
    }

    [NetworkCallable]
    public void NetworkCreateHurtBox(Vector3 position, Quaternion rotation, Vector4 float_in, bool is_melee, int player_id, int weapon_type, bool from_secondary)
    {
        float damage = float_in.x;
        float player_scale = float_in.y;
        float dist_scale = float_in.z;
        byte extra_data = (byte)float_in.w;

        WeaponHurtbox hurtbox = null;
        if (is_melee) 
        { 
            hurtbox = hurtbox_melee; 
            hurtbox.weapon_rb = pickup_rb; 
        }
        else 
        { 
            hurtbox = GetInactiveRangedHurtbox();
            hurtbox.transform.parent = null;
        }
        if (hurtbox == null) { return; }

        GameObject newHurtboxObj = hurtbox.gameObject;
        hurtbox.OnReset(false);

        float scaleBias = 1.0f;
        // Explosive type weapons should not be resized as harshly as other hurtbox types
        if (weapon_type == (int)weapon_type_name.Rocket) { scaleBias = 0.5f; }
        else if (weapon_type == (int)weapon_type_name.Bomb) { scaleBias = 0.75f; }
        // And melee weapons should not have any additional scaling whatsoever, since that's handled by the parent
        else if (is_melee) { scaleBias = 0.0f; }
        newHurtboxObj.transform.localScale = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Size] * Mathf.Lerp(1.0f, player_scale, scaleBias) * new Vector3(Mathf.Max(1.0f, dist_scale), 1.0f, 1.0f);
        if (is_melee)
        {
            newHurtboxObj.transform.position = transform.position;
            newHurtboxObj.transform.rotation = transform.rotation;
            newHurtboxObj.transform.localPosition = Vector3.zero;
            newHurtboxObj.transform.localRotation = Quaternion.identity;
            //hurtbox.hurtbox_rb.MovePosition(transform.position);
            //hurtbox.hurtbox_rb.MoveRotation(transform.rotation);
        }
        else
        {
            newHurtboxObj.transform.position = position;
            newHurtboxObj.transform.rotation = rotation;
            //hurtbox.hurtbox_rb.MovePosition(position);
            //hurtbox.hurtbox_rb.MoveRotation(rotation);
        }

        bool is_boss = weapon_type == (int)weapon_type_name.BossGlove || (gameController.option_gamemode == (int)gamemode_name.BossBash && owner_attributes.ply_team == 1); 

        hurtbox.start_scale = newHurtboxObj.transform.lossyScale;
        hurtbox.hurtbox_damage = damage;
        if (is_boss && !Networking.LocalPlayer.IsUserInVR()) { hurtbox.hurtbox_damage *= 2.0f; }
        hurtbox.hurtbox_duration = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Duration];
        hurtbox.hurtbox_start_ms = Networking.GetServerTimeInSeconds();
        hurtbox.hurtbox_timer_network = 0.0f;
        hurtbox.damage_type = (int)GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Damage_Type];
        hurtbox.source_weapon_type = weapon_type;
        hurtbox.hurtbox_extra_data = extra_data;
        hurtbox.is_melee = is_melee;
        PlayerWeapon source_weaponScript = null;
        if (from_secondary) { source_weaponScript = gameController.GetSecondaryWeaponFromID(player_id); }
        else { source_weaponScript = gameController.GetPlayerWeaponFromID(player_id); }
        hurtbox.weapon_script = source_weaponScript;
        //hurtbox.can_collide = !is_melee;
        hurtbox.SetMesh();
        newHurtboxObj.SetActive(true);
        
        UnityEngine.Debug.Log("[HURTBOX_TEST] " + newHurtboxObj.name + " created. position = " + newHurtboxObj.transform.position + "; duration = " + hurtbox.hurtbox_duration + "; start_ms = " + hurtbox.hurtbox_start_ms);
    }

    public byte RollForPowerupBombExtraData()
    {
        byte extra_data = 0;
        if (gameController != null && gameController.plysettings_item_debuff)
        {
            byte[] powerup_debuffs = { (int)powerup_type_name.AtkDown, (int)powerup_type_name.DefDown, (int)powerup_type_name.HighGrav, (int)powerup_type_name.SizeDown };
            extra_data = (byte)UnityEngine.Random.Range(0, powerup_debuffs.Length);
            extra_data = powerup_debuffs[extra_data];
        }
        else
        {
            extra_data = (byte)UnityEngine.Random.Range(0, (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH);
        }
        // If the random powerup rolls the boss glove, set it to be another throwable item for maximum chaos
        if (extra_data == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.PunchingGlove
            || extra_data == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.BossGlove
            ) { extra_data = (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem; }
        return extra_data;
    }

    private void ToggleParticle(bool toggle)
    {
        Transform particle = GlobalHelperFunctions.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
        if (particle != null && particle.GetComponent<ParticleSystem>() != null)
        {
            if (gameController != null && gameController.local_ppp_options != null)
            {
                var particle_emission = particle.GetComponent<ParticleSystem>().emission;
                particle_emission.enabled = toggle && gameController.local_ppp_options.particles_on;
            }
            particle.gameObject.SetActive(toggle);
            if (toggle) { particle.GetComponent<ParticleSystem>().Play(); }
            else { particle.GetComponent<ParticleSystem>().Stop(); }
        }
    }

    private void ScaleWeapon()
    {
        // Scale the object with the owner's size
        if (owner_attributes != null && owner_attributes.plyEyeHeight_default > 0.0f && scale_inital > 0.0f)
        {
            //float playerVisualScale = owner_attributes.plyEyeHeight_desired / owner_attributes.plyEyeHeight_default;
            //transform.localScale = new Vector3(scale_inital * playerVisualScale, scale_inital * playerVisualScale, scale_inital * playerVisualScale);
            transform.localScale = new Vector3(scale_inital * owner_attributes.ply_scale, scale_inital * owner_attributes.ply_scale, scale_inital * owner_attributes.ply_scale);
        }
    }

    public void PlayHapticEvent(int event_type)
    {
        if (!Networking.LocalPlayer.IsUserInVR() || pickup_component == null) { return; }
        if (gameController != null && gameController.local_ppp_options != null && !gameController.local_ppp_options.haptics_on) { return; }

        // Haptic events will go in the following priority order: Death > Kill > Hit Receive > Hit Send > Fire Weapon
        // Only play hit send haptic if we aren't playing anything else (lowest priority)
        float duration = 0.0f; float amplitude = 0.0f; float frequency = 0.1f;
        if (event_type == (int)game_sfx_name.ENUM_LENGTH && haptic_cooldown_type == -1 && haptic_cooldown_type != (int)game_sfx_name.ENUM_LENGTH) { duration = 0.05f; amplitude = 0.05f; } // Fire weapon
        else if (event_type == (int)game_sfx_name.HitSend && !(haptic_cooldown_type == (int)game_sfx_name.HitSend || haptic_cooldown_type == (int)game_sfx_name.HitReceive || haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 0.1f; amplitude = 0.1f; }
        else if (event_type == (int)game_sfx_name.HitReceive && !(haptic_cooldown_type == (int)game_sfx_name.HitReceive || haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 0.2f; amplitude = 0.2f; }
        else if (event_type == (int)game_sfx_name.Kill && !(haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 0.15f; amplitude = 0.15f; }
        else if (event_type == (int)game_sfx_name.Death && haptic_cooldown_type != (int)game_sfx_name.Death) { duration = 0.3f; amplitude = 0.3f; }

        if (pickup_component.currentHand != VRCPickup.PickupHand.None &&
            (event_type == (int)game_sfx_name.ENUM_LENGTH || event_type == (int)game_sfx_name.HitSend || event_type == (int)game_sfx_name.Kill))
        {
            Networking.LocalPlayer.PlayHapticEventInHand(pickup_component.currentHand, duration, amplitude, frequency);
        }
        else
        {
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, duration, amplitude, frequency);
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, duration, amplitude, frequency);
        }

        // Set the haptic on cooldown after playing it
        haptic_cooldown_type = event_type;
        if (haptic_cooldowns.Length > event_type) { haptic_countdown = haptic_cooldowns[event_type]; }
        else { haptic_countdown = 1.0f; }

    }

    public float[] GetStatsFromWeaponType(int weapon_type)
    {
        if (all_weapon_stats == null || weapon_type < 0 || weapon_type > all_weapon_stats.Length) { return SetupWeaponStat(weapon_type); }
        return all_weapon_stats[weapon_type];
    }

    private float[][] SetupAllWeaponStats()
    {
        float[][] all_weapon_stats = new float[(int)weapon_stats_name.ENUM_LENGTH][];
        for (int i = 0; i < (int)weapon_stats_name.ENUM_LENGTH; i++)
        {
            all_weapon_stats[i] = SetupWeaponStat(i);
        }
        return all_weapon_stats;
    }

    public void ResetWeaponToDefault(bool play_sfx = false)
    {
        if (!Networking.IsOwner(gameObject)) { return; }

        weapon_type = weapon_type_default;
        weapon_temp_ammo = -1;
        weapon_temp_duration = -1;
        weapon_temp_timer = 0.0f;
        weapon_charge_timer = 0.0f;
        weapon_charge_duration = 0.0f;

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        if (play_sfx)
        {
            AudioClip[] temp_arr = new AudioClip[1];
            temp_arr[0] = snd_game_sfx_clip_weaponexpire;
            gameController.PlaySFXFromArray(snd_source_weaponcontact, temp_arr, 0);
        }


        if (is_secondary && gameObject.activeInHierarchy && gameController != null && owner_attributes != null)
        {
            bool is_boss = weapon_type == (int)weapon_type_name.BossGlove || (gameController.option_gamemode == (int)gamemode_name.BossBash && owner_attributes.ply_team == 1);
            is_boss = is_boss && Networking.LocalPlayer.IsUserInVR();
            if (!is_boss) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", false); }
        }
    }

    [NetworkCallable]
    public void UpdateStatsFromWeaponType()
    {
        use_timer = 0.0f;
        use_cooldown = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Cooldown];
        weapon_charge_duration = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.ChargeTime];
        weapon_is_charging = false;
        weapon_charge_start_ms = 0.0f;
        weapon_charge_timer = 0.0f;
        animate_active = true;

        if (pickup_rb != null)
        {
            pickup_rb.useGravity = false;
            pickup_rb.velocity = Vector3.zero;
            pickup_rb.angularVelocity = Vector3.zero;
        }

        if (local_weapon_type != weapon_type || local_extra_data != weapon_extra_data)
        {
            UpdateWeaponModel();
        }

        if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem)
        {
            if (Networking.GetOwner(gameObject).IsUserInVR())
            {
                waiting_for_toss = true;
                use_timer = use_cooldown;
                use_ready = true;
                ToggleParticle(true);
            }
            else
            {
                waiting_for_toss = false;
                ToggleParticle(false);
            }
        }
        else if (weapon_type == (int)weapon_type_name.BossGlove)
        {
            if (gameController.ply_in_game_auto_dict != null && gameController.ply_in_game_auto_dict.Length > 0 && gameController.ply_in_game_auto_dict[0] != null)
            {
                // Fire rate increases with number of players
                use_cooldown = Mathf.Lerp(2.0f, 0.5f, Mathf.Min(1.0f, gameController.ply_in_game_auto_dict[0].Length / 18.0f))
                    * GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Cooldown];
            }
        }

        ScaleWeapon();

        // Must be the last lines
        local_weapon_type = weapon_type;
        local_extra_data = weapon_extra_data;
    }

    public void UpdateWeaponModel()
    {
        if (weapon_type >= 0 && weapon_type < weapon_mdl.Length && weapon_mdl[weapon_type] != null)
        {
            for (int i = 0; i < weapon_mdl.Length; i++)
            {
                if (i == weapon_type) { weapon_mdl[i].SetActive(true); }
                else if (weapon_mdl[i] != null) { weapon_mdl[i].SetActive(false); }
            }
        }
        // Cache and recolor child objects
        if (weapon_type == (int)weapon_type_name.SuperLaser)
        {
            cached_laser_sprite = GlobalHelperFunctions.GetChildTransformByName(weapon_mdl[weapon_type].transform, "LaserSprite");
            cached_laser_particle = GlobalHelperFunctions.GetChildTransformByName(weapon_mdl[weapon_type].transform, "LaserParticle");
        }

        SetTeamColor();
    }

    public void SetTeamColor()
    {
        
        weapon_renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        Material weapon_mat = null;
        if (weapon_renderer != null)
        {
            weapon_mat = weapon_renderer.material;
            
            byte emissionOffset = 0;
            if ((weapon_type == (int)weapon_type_name.Rocket || weapon_type == (int)weapon_type_name.Bomb) && weapon_renderer.materials.Length > 1) { weapon_mat = weapon_renderer.materials[1]; emissionOffset = 80; } // 127
            else if (weapon_type == (int)weapon_type_name.SuperLaser) { emissionOffset = 0; } //67

            if (owner_attributes != null && gameController.team_colors != null)
            {
                int team = Mathf.Max(0, owner_attributes.ply_team);
                if (gameController.option_teamplay)
                {
                    weapon_mat.SetColor("_Color",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[team].r)),
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[team].g)),
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[team].b)),
                        (byte)gameController.team_colors[team].a));
                    weapon_mat.EnableKeyword("_EMISSION");
                    weapon_mat.SetColor("_EmissionColor",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min(255, -emissionOffset + gameController.team_colors[team].r)),
                        (byte)Mathf.Max(0, Mathf.Min(255, -emissionOffset + gameController.team_colors[team].g)),
                        (byte)Mathf.Max(0, Mathf.Min(255, -emissionOffset + gameController.team_colors[team].b)),
                        (byte)gameController.team_colors[team].a));
                }
                else
                {
                    weapon_mat.SetColor("_Color", new Color32(255, 255, 255, 255));
                    weapon_mat.EnableKeyword("_EMISSION");
                    weapon_mat.SetColor("_EmissionColor", new Color32((byte)(255 - emissionOffset), (byte)(255 - emissionOffset), (byte)(255 - emissionOffset), 255));
                }
                if (cached_laser_sprite != null) { cached_laser_sprite.GetComponent<Renderer>().material.SetColor("_Color", weapon_mat.GetColor("_EmissionColor")); }
            }
        }

        Transform particle = GlobalHelperFunctions.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
        if (particle != null && particle.GetComponent<ParticleSystem>() != null)
        {
            var particle_main = particle.GetComponent<ParticleSystem>().main;
            if (weapon_mat != null)
            {
                particle_main.startColor = new Color(weapon_mat.GetColor("_Color").r, weapon_mat.GetColor("_Color").g, weapon_mat.GetColor("_Color").b, 1.0f);
            }
            if (gameController != null && gameController.local_ppp_options != null)
            {
                var particle_emission = particle.GetComponent<ParticleSystem>().emission;
                particle_emission.enabled = gameController.local_ppp_options.particles_on;
            }
            particle.gameObject.SetActive(true);
            particle.GetComponent<ParticleSystem>().Play();
        }

        weapon_animator = weapon_mdl[weapon_type].GetComponent<Animator>();

        // Render the item sprite, if this is a throwable item
        Transform item_shell = GlobalHelperFunctions.GetChildTransformByName(weapon_mdl[weapon_type].transform, "ItemShell");
        if (item_shell != null)
        {
            Renderer shell_Renderer = item_shell.GetComponent<Renderer>();
            if (shell_Renderer != null)
            {
                if (owner_attributes != null && gameController.option_teamplay && owner_attributes.ply_team >= 0)
                {
                    Color32 TeamColor = gameController.team_colors[owner_attributes.ply_team]; TeamColor.a = 92; TeamColor.r = (byte)Mathf.Clamp(-80 + TeamColor.r, 0, 255); TeamColor.g = (byte)Mathf.Clamp(-80 + TeamColor.g, 0, 255); TeamColor.b = (byte)Mathf.Clamp(-80 + TeamColor.b, 0, 255);
                    Color32 TeamColorB = gameController.team_colors_bright[owner_attributes.ply_team]; TeamColorB.a = 92;
                    shell_Renderer.material.SetColor("_Color", TeamColorB);
                    shell_Renderer.material.EnableKeyword("_EMISSION");
                    shell_Renderer.material.SetColor("_EmissionColor", TeamColor);
                }
                else
                {
                    shell_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 92));
                    shell_Renderer.material.EnableKeyword("_EMISSION");
                    shell_Renderer.material.SetColor("_EmissionColor", new Color32(83, 83, 83, 92));
                }
            }

            foreach (Transform child in item_shell)
            {
                if (child.name.Contains("ItemSprite"))
                {
                    Renderer sprite_Renderer = child.GetComponent<Renderer>();
                    int item_index = 0; Sprite sprite_to_render = null;
                    if (weapon_extra_data < (int)powerup_type_name.ENUM_LENGTH)
                    {
                        item_index = weapon_extra_data;
                        sprite_to_render = GlobalHelperFunctions.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemPowerup").GetComponent<ItemPowerup>().powerup_sprites[item_index];
                    }
                    else
                    {
                        item_index = weapon_extra_data - (int)powerup_type_name.ENUM_LENGTH;
                        sprite_to_render = GlobalHelperFunctions.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemWeapon").GetComponent<ItemWeapon>().iweapon_sprites[item_index];

                    }
                    if (sprite_to_render != null) { sprite_Renderer.material.SetTexture("_MainTex", sprite_to_render.texture); }
                }
            }
        }

        if (owner_attributes != null) { cached_team = owner_attributes.ply_team; }
    }

    private float[] SetupWeaponStat(int weapon_in_type)
    {
        float[] weapon_stats = new float[(int)weapon_stats_name.ENUM_LENGTH];
        if (weapon_in_type == (int)weapon_type_name.PunchingGlove || weapon_in_type == (int)weapon_type_name.BossGlove || weapon_in_type == (int)weapon_type_name.HyperGlove || weapon_in_type == (int)weapon_type_name.MegaGlove)
        {
            weapon_stats[(int)weapon_stats_name.IsMelee] = 1;
            weapon_stats[(int)weapon_stats_name.Cooldown] = 1.1f;
            weapon_stats[(int)weapon_stats_name.ChargeTime] = 0.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 1.8f * 0.8f * 0.9f;
            weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 0.05f;
            weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
            weapon_stats[(int)weapon_stats_name.Projectile_Size] = 0.1f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 10.0f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 0.33f * 0.6f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 0.4f; //0.4f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Strike;
            if (weapon_in_type == (int)weapon_type_name.BossGlove)
            {
                //weapon_stats[(int)weapon_stats_name.Cooldown] *= 0.5f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] *= 2.5f; //2.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] *= 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Kapow;
            }
            else if (weapon_in_type == (int)weapon_type_name.HyperGlove)
            {
                weapon_stats[(int)weapon_stats_name.Cooldown] *= 0.5f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] *= 0.4f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Strike;
            }
            else if (weapon_in_type == (int)weapon_type_name.MegaGlove)
            {
                weapon_stats[(int)weapon_stats_name.Cooldown] *= 1.5f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] *= 2.5f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Kapow;
            }
        }
        else if (weapon_in_type == (int)weapon_type_name.Rocket)
        {
            weapon_stats[(int)weapon_stats_name.IsMelee] = 0;
            weapon_stats[(int)weapon_stats_name.Cooldown] = 1.0f;
            weapon_stats[(int)weapon_stats_name.ChargeTime] = 0.0f;
            // To emulate a "projectile speed", we can determine the distance based on projectile time
            float projectile_speed = 14.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 10.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Distance] = projectile_speed * weapon_stats[(int)weapon_stats_name.Projectile_Duration];
            weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
            weapon_stats[(int)weapon_stats_name.Projectile_Size] = 0.1f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 24.0f; // 30.0
            weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 3.0f; // 2.85
            weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.ForceExplosion;
        }
        else if (weapon_in_type == (int)weapon_type_name.Bomb || weapon_in_type == (int)weapon_type_name.ThrowableItem)
        {
            weapon_stats[(int)weapon_stats_name.IsMelee] = 0;
            weapon_stats[(int)weapon_stats_name.Cooldown] = 1.5f;
            weapon_stats[(int)weapon_stats_name.ChargeTime] = 0.0f;
            // To emulate a "projectile speed", we can determine the distance based on projectile time
            weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 3.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 100.0f;
            if (weapon_in_type == (int)weapon_type_name.Bomb)
            {
                weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bomb;
                weapon_stats[(int)weapon_stats_name.Projectile_Size] = 0.5f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 48.0f; // 50.0f
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 3.6f; // 3.4
                weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.ForceExplosion;
            }
            else if (weapon_in_type == (int)weapon_type_name.ThrowableItem)
            {
                weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.ItemProjectile;
                weapon_stats[(int)weapon_stats_name.Projectile_Size] = 0.5f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 0.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 3.6f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.ItemHit;
            }
        }
        else if (weapon_in_type == (int)weapon_type_name.SuperLaser)
        {
            weapon_stats[(int)weapon_stats_name.IsMelee] = 0;
            weapon_stats[(int)weapon_stats_name.Cooldown] = 1.0f;
            weapon_stats[(int)weapon_stats_name.ChargeTime] = 2.0f;
            // To emulate a "projectile speed", we can determine the distance based on projectile time
            weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 0.05f;
            weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 200.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Laser;
            weapon_stats[(int)weapon_stats_name.Projectile_Size] = 0.05f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 24.0f; // 30.0f
            weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.5f; // 2.0
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Laser;
        }
        else
        {
            weapon_stats[(int)weapon_stats_name.IsMelee] = 0;
            weapon_stats[(int)weapon_stats_name.Cooldown] = 1.0f;
            weapon_stats[(int)weapon_stats_name.ChargeTime] = 0.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Projectile_Size] = 0.1f;
            weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.0f;
            weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Strike;
        }
        return weapon_stats;
    }
}