
using System;
using System.Net.Sockets;
using UdonSharp;
using UnityEngine;
using UnityEngine.Android;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using static UnityEngine.ParticleSystem;
using static VRC.SDKBase.VRCPlayerApi;

public enum weapon_type_name // NOTE: NEEDS TO ALSO BE CHANGED IN GAMECONTROLLER IF ANY ARE ADDED/REMOVED FOR KeyToWeaponType()
{
    PunchingGlove, Bomb, Rocket, BossGlove, HyperGlove, MegaGlove, SuperLaser, ENUM_LENGTH
}

public class PlayerWeapon : UdonSharpBehaviour
{
    [NonSerialized] [UdonSynced] public int weapon_type;
    [NonSerialized] [UdonSynced] public int weapon_type_default;
    [NonSerialized] public float use_cooldown;
    [NonSerialized] public float use_timer;
    [NonSerialized] public bool use_ready;
    [NonSerialized] public int weapon_temp_ammo = -1;
    [NonSerialized] public float weapon_temp_duration = -1.0f;
    [NonSerialized] public float weapon_temp_timer = 0.0f;
    [NonSerialized] [UdonSynced] public bool weapon_is_charging = false;
    [NonSerialized] [UdonSynced] public double weapon_charge_start_ms = 0.0f;
    [NonSerialized] [UdonSynced] public double weapon_charge_duration = 0.0f;
    [NonSerialized] public double weapon_charge_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float anim_state;
    [NonSerialized] [UdonSynced] public float animate_pct;
    [NonSerialized] [UdonSynced] public float animate_stored_pct = 1.0f;
    [NonSerialized] [UdonSynced] public bool animate_handled_by_hurtbox = false;
    [NonSerialized] [UdonSynced] public float animate_stored_use_timer = 0.0f;
    [NonSerialized] [UdonSynced] public Vector3 velocity_stored = Vector3.zero;
    [SerializeField] public GameObject[] weapon_mdl;
    [SerializeField] public GameController gameController;
    [SerializeField] public VRCPickup pickup_component;
    [SerializeField] public AudioSource snd_source_weaponfire;
    [SerializeField] public AudioSource snd_source_weaponcharge;
    [SerializeField] public AudioSource snd_source_weaponcontact;
    //[SerializeField] public bool is_secondary = false;
    [NonSerialized] public float scale_inital;

    [SerializeField] public AudioClip snd_game_sfx_clip_weaponexpire;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponfire; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcharge; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcontact; // NOTE: Corresponds to weapon_type

    [NonSerialized] public PlayerAttributes owner_attributes;

    [SerializeField] public float[] haptic_cooldowns; // NEEDS TO MATCH LENGTH OF game_sfx_name
    [NonSerialized] public float haptic_countdown = 0.0f; // We call this countdown rather than timer since it decrements rather than increments
    [NonSerialized] public int haptic_cooldown_type = -1;

    [NonSerialized] public bool waiting_for_toss = false;
    [NonSerialized] public bool[] local_tutorial_message_bool;
    [NonSerialized] public string[] local_tutorial_message_str_desktop;
    [NonSerialized] public string[] local_tutorial_message_str_vr;

    void Start()
    {
        scale_inital = transform.localScale.x;
        SetupTutorialMessages();
        /*if (is_secondary) 
        { 
            gameObject.SetActive(false);
        }
        else { weapon_type = (int)weapon_type_name.PunchingGlove; }*/
        weapon_type_default = (int)weapon_type_name.PunchingGlove;
        ResetWeaponToDefault();

        if (pickup_component == null) { pickup_component = gameObject.GetComponent<VRCPickup>(); }

    }

    private void SetupTutorialMessages()
    {
        local_tutorial_message_bool = new bool[(int)weapon_type_name.ENUM_LENGTH];
        local_tutorial_message_str_desktop = new string[(int)weapon_type_name.ENUM_LENGTH];
        local_tutorial_message_str_vr = new string[(int)weapon_type_name.ENUM_LENGTH];

        // Tutorial messages for all
        local_tutorial_message_str_desktop[(int)weapon_type_name.PunchingGlove] = "";
        local_tutorial_message_str_desktop[(int)weapon_type_name.Bomb] = "BOMB: Push your fire key to toss it forward! It will detonate after " + gameController.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Projectile_Duration] + " seconds!";
        local_tutorial_message_str_desktop[(int)weapon_type_name.Rocket] = "ROCKET LAUNCHER: Fire off projectiles that will explode!";
        local_tutorial_message_str_desktop[(int)weapon_type_name.BossGlove] = "";
        local_tutorial_message_str_desktop[(int)weapon_type_name.HyperGlove] = "HYPER GLOVE: Hyper-fast attacks, but less damage!";
        local_tutorial_message_str_desktop[(int)weapon_type_name.MegaGlove] = "MEGA GLOVE: Mega damage, but slow to fire!";
        local_tutorial_message_str_desktop[(int)weapon_type_name.SuperLaser] = "SUPERLASER: Hold down your fire key to charge it up and fire a huge beam!";

        for (int i = 0; i < (int)weapon_type_name.ENUM_LENGTH; i++)
        {
            local_tutorial_message_bool[i] = false;
            local_tutorial_message_str_vr[i] = local_tutorial_message_str_desktop[i];
        }

        // VR-specific messages
        local_tutorial_message_str_vr[(int)weapon_type_name.Bomb] = "BOMB: Push Trigger to activate, then toss it by releasing your Grip! It will detonate after " + gameController.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Projectile_Duration] + " seconds!";
        local_tutorial_message_str_vr[(int)weapon_type_name.SuperLaser] = "SUPERLASER: Hold down your Trigger to charge it up and fire a huge beam!";

    }

    public override void OnDeserialization()
    {
        UpdateStatsFromWeaponType();
    }

    [NetworkCallable]
    public void UpdateStatsFromWeaponType()
    {
        if (gameController == null) { return; }

        use_cooldown = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Cooldown];
        weapon_charge_duration = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.ChargeTime];

        if (weapon_type >= 0 && weapon_type < weapon_mdl.Length && weapon_mdl[weapon_type] != null )
        {
            for (int i = 0; i < weapon_mdl.Length; i++)
            {
                if (i == weapon_type) { weapon_mdl[i].SetActive(true); }
                else if (weapon_mdl[i] != null) { weapon_mdl[i].SetActive(false); }
            }
        }

        // Send a tutorial message
        if (Networking.GetOwner(gameObject) == Networking.LocalPlayer && gameController != null && local_tutorial_message_bool != null && !local_tutorial_message_bool[weapon_type])
        {
            if (Networking.LocalPlayer.IsUserInVR() && local_tutorial_message_str_vr[weapon_type] != "") { gameController.AddToLocalTextQueue(local_tutorial_message_str_vr[weapon_type], Color.cyan); }
            else if (!Networking.LocalPlayer.IsUserInVR() && local_tutorial_message_str_desktop[weapon_type] != "") { gameController.AddToLocalTextQueue(local_tutorial_message_str_desktop[weapon_type], Color.cyan); }
            local_tutorial_message_bool[weapon_type] = true;
        }

        Transform particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
        if (particle != null && particle.GetComponent<ParticleSystem>() != null)
        {
            var particle_main = particle.GetComponent<ParticleSystem>().main;
            Renderer m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (m_Renderer != null) { particle_main.startColor = m_Renderer.material.GetColor("_EmissionColor"); }
            particle.GetComponent<ParticleSystem>().Stop();
            particle.gameObject.SetActive(false);
        }

        waiting_for_toss = false;
        GetComponent<Rigidbody>().useGravity = false;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        return;
    }

    public void ResetWeaponToDefault(bool play_sfx = false)
    {
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
    }

    public void DoubleCheckLocalPlyAttr(bool forceUpdate = false)
    {
        if (gameController == null) { return; }
        if (owner_attributes == null || forceUpdate || Networking.GetOwner(owner_attributes.gameObject) != Networking.GetOwner(gameObject))
        {
            owner_attributes = gameController.FindPlayerAttributes(Networking.GetOwner(gameObject));
        }
    }

    [NetworkCallable]
    public void ChangeOwner(int new_owner_id)
    {
        if (Networking.LocalPlayer != Networking.GetOwner(gameObject)) { return; }
        Networking.SetOwner(VRCPlayerApi.GetPlayerById(new_owner_id), gameObject);
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ProcessOwnerChanged");
        //ForceActive();
    }

    [NetworkCallable]
    public void ProcessOwnerChanged()
    {
        DoubleCheckLocalPlyAttr(true);
        UpdateStatsFromWeaponType();
    }

    public void ForceActive()
    {
        if (gameController == null || owner_attributes == null) { return; }
        if (gameController.round_state == (int)round_state_name.Ongoing && owner_attributes.ply_team >= 0 && !gameObject.activeInHierarchy && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            gameObject.SetActive(true);
        }
    }

    private void FixedUpdate()
    {
        if (pickup_component == null) { pickup_component = gameObject.GetComponent<VRCPickup>(); }

        // Scale the object with the owner's size
        if (owner_attributes != null && owner_attributes.plyEyeHeight_default > 0.0f && scale_inital > 0.0f)
        {
            float playerVisualScale = owner_attributes.plyEyeHeight_desired / owner_attributes.plyEyeHeight_default;
            //Debug.Log(Networking.GetOwner(gameObject).displayName + "'s playerVisualScale calc: " + playerVisualScale);
            transform.localScale = new Vector3(scale_inital * playerVisualScale, scale_inital * playerVisualScale, scale_inital * playerVisualScale);
        }

        if (pickup_component.IsHeld)
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }

    }

    private void Update()
    {
        if (gameController == null) { return; }

        if (pickup_component == null) { pickup_component = gameObject.GetComponent<VRCPickup>(); }

        if (weapon_charge_duration > 0.0f && weapon_charge_start_ms > 0.0f) { weapon_charge_timer = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), weapon_charge_start_ms); }

        // Make sure you can only fire when off cooldown
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
 
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
                else {
                    use_pct = (float)((use_timer - timer_at_pct) / (use_cooldown - timer_at_pct));
                    animate_pct = Mathf.Lerp(0.999f, 0.0f, use_pct); 
                }
            }
        }
        else if (gameController.round_state == (int)round_state_name.Ongoing && !use_ready)
        {
            use_ready = true;
            if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.SuperLaser) { animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false; }
        }
        else if (gameController.round_state != (int)round_state_name.Ongoing && use_ready)
        {
            use_ready = false;
            if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.SuperLaser) { animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false; }
        }

        DoubleCheckLocalPlyAttr();
        //ForceActive();

        // Handle haptic countdown decrementing timer
        if (haptic_cooldown_type >= 0 && haptic_countdown > 0.0f)
        {
            haptic_countdown--;
        }
        else if (haptic_cooldown_type >= 0 && haptic_countdown <= 0.0f)
        {
            haptic_countdown = 0.0f;
            haptic_cooldown_type = -1;
        }

        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {

            // Reposition the object if we're not holding it
            if (!pickup_component.IsHeld && owner_attributes != null && (owner_attributes.ply_state == (int)player_state_name.Alive || owner_attributes.ply_state == (int)player_state_name.Respawning || owner_attributes.ply_state == (int)player_state_name.Dead))
            {
                if (Networking.LocalPlayer.IsUserInVR())
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * 0.5f);
                }
                else if (!Networking.LocalPlayer.IsUserInVR())
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
                }
            }
        }

        if (!pickup_component.pickupable && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            pickup_component.pickupable = true;
        }
        else if (pickup_component.pickupable && Networking.GetOwner(gameObject) != Networking.LocalPlayer)
        {
            pickup_component.pickupable = false;
        }

        // Handle animations
        weapon_mdl[weapon_type].GetComponent<Animator>().SetFloat("AnimationTimer", animate_pct);
        Transform laser_sprite = null;
        anim_state = 0;
        if (weapon_type == (int)weapon_type_name.SuperLaser)
        {
            anim_state = weapon_mdl[weapon_type].GetComponent<Animator>().GetInteger("AnimState");
            laser_sprite = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "LaserSprite");
            Transform laser_particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "LaserParticle");
            if (anim_state == 0)
            {
                if (laser_sprite != null) { laser_sprite.localScale = Vector3.zero; }
                if (laser_particle != null && laser_particle.gameObject.activeInHierarchy) { laser_particle.gameObject.SetActive(false); }
            }
            else if (anim_state == 1) 
            { 
                animate_pct = Mathf.Lerp(0.0f, 0.999f, System.Convert.ToSingle(weapon_charge_timer / weapon_charge_duration));
                if (laser_sprite != null) { laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.35f * animate_pct; }
                if (laser_particle != null && laser_particle.gameObject.activeInHierarchy) { laser_particle.gameObject.SetActive(true); }
            }
            else if (anim_state == 2)
            {
                float laser_fire_pct = 0.4f;
                float timer_at_pct = laser_fire_pct * use_cooldown;
                float use_pct = (float)(use_timer / use_cooldown);
                if (use_pct < laser_fire_pct)
                {
                    use_pct = (float)use_pct / laser_fire_pct;
                    animate_pct = Mathf.Lerp(0.0f, 0.5f, use_pct);
                if (laser_sprite != null) { laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.35f; }
                }
                else
                {
                    use_pct = (float)((use_timer - timer_at_pct) / (use_cooldown - timer_at_pct));
                    animate_pct = Mathf.Lerp(0.5f, 0.999f, use_pct);
                    if (laser_sprite != null) { laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.3f * Mathf.Lerp(1.0f, 0.0f, use_pct); }
                }
                if (use_ready) { weapon_mdl[weapon_type].GetComponent<Animator>().SetInteger("AnimState", 0); }
                if (laser_particle != null && laser_particle.gameObject.activeInHierarchy) { laser_particle.gameObject.SetActive(false); }
            }
        }

        // Do not else if this; it needs to be callable even after assignment
        if (owner_attributes != null)
        {

            Renderer m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (m_Renderer != null && owner_attributes != null && gameController.team_colors != null && owner_attributes.ply_team >= 0)
            {
                Material weapon_mat = m_Renderer.material; byte emissionOffset = 0;
                if ((weapon_type == (int)weapon_type_name.Rocket || weapon_type == (int)weapon_type_name.Bomb) && m_Renderer.materials.Length > 1) { weapon_mat = m_Renderer.materials[1]; emissionOffset = 0; } // 127
                else if (weapon_type == (int)weapon_type_name.SuperLaser) { emissionOffset = 0; } //67
                if (gameController.option_teamplay)
                {

                    weapon_mat.SetColor("_Color",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min((byte)(255 - emissionOffset), 80 + gameController.team_colors[owner_attributes.ply_team].r)),
                        (byte)Mathf.Max(0, Mathf.Min((byte)(255 - emissionOffset), 80 + gameController.team_colors[owner_attributes.ply_team].g)),
                        (byte)Mathf.Max(0, Mathf.Min((byte)(255 - emissionOffset), 80 + gameController.team_colors[owner_attributes.ply_team].b)),
                        (byte)gameController.team_colors[owner_attributes.ply_team].a));
                    weapon_mat.EnableKeyword("_EMISSION");
                    weapon_mat.SetColor("_EmissionColor",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min((byte)(255 - emissionOffset), 80 + gameController.team_colors[owner_attributes.ply_team].r)),
                        (byte)Mathf.Max(0, Mathf.Min((byte)(255 - emissionOffset), 80 + gameController.team_colors[owner_attributes.ply_team].g)),
                        (byte)Mathf.Max(0, Mathf.Min((byte)(255 - emissionOffset), 80 + gameController.team_colors[owner_attributes.ply_team].b)),
                        (byte)gameController.team_colors[owner_attributes.ply_team].a));
                }
                else
                {
                    weapon_mat.SetColor("_Color", new Color32(255, 255, 255, 255));
                    weapon_mat.EnableKeyword("_EMISSION");
                    weapon_mat.SetColor("_EmissionColor", new Color32((byte)(255 - emissionOffset), (byte)(255 - emissionOffset), (byte)(255 - emissionOffset), 255));
                }
                if (laser_sprite != null) { laser_sprite.GetComponent<Renderer>().material.SetColor("_Color", weapon_mat.GetColor("_EmissionColor")); }
            }
        }

        // Handle weapon charging timer
        if (weapon_is_charging)
        {
            if (weapon_charge_timer > weapon_charge_duration && weapon_charge_duration > 0.0f)
            {
                FireWeapon();
                weapon_is_charging = false;
                weapon_charge_timer = 0.0f;
                if (weapon_type == (int)weapon_type_name.SuperLaser) { weapon_mdl[weapon_type].GetComponent<Animator>().SetInteger("AnimState", 2); }
            }
        }

        // Handle weapon powerup timer
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


    }

    public void FireWeapon()
    {
        use_ready = false;
        use_timer = 0;
        if (weapon_type != (int)weapon_type_name.Bomb) { animate_pct = 0.0f; }

        //if (transform.GetComponent<VRCPickup>().currentPlayer == Networking.LocalPlayer)
        if (Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            var keep_parent = false;
            Vector3 pos_start = transform.position;
            // Play the firing sound, if applicable
            float pitch_low = 0.5f; float pitch_high = 1.5f;
            if (weapon_type == (int)weapon_type_name.PunchingGlove) { pitch_low = 0.5f; pitch_high = 1.5f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.BossGlove) { pitch_low = 0.75f; pitch_high = 1.25f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.HyperGlove) { pitch_low = 1.5f; pitch_high = 2.0f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.MegaGlove) { pitch_low = 0.5f; pitch_high = 0.75f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.Rocket) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; }
            else if (weapon_type == (int)weapon_type_name.SuperLaser) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; }
            else if (weapon_type == (int)weapon_type_name.Bomb) 
            { 
                pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; 
                if (gameController.local_plyAttr != null) { pos_start += Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * gameController.local_plyAttr.ply_scale * 1.0f; }
            }

            gameController.PlaySFXFromArray(
                    snd_source_weaponfire
                    , snd_game_sfx_clips_weaponfire
                    , weapon_type
                    , UnityEngine.Random.Range(pitch_low, pitch_high)
                    );

            if (weapon_temp_ammo != -1) { weapon_temp_ammo--; }

            var distance = 0.0f;
            if (keep_parent) { distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance]; }
            else
            {
                distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance];
            }

            var plyAttr = gameController.local_plyAttr;
            if (plyAttr == null) { return; }
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateProjectile"
                , weapon_type
                , pos_start
                , transform.rotation //, Networking.LocalPlayer.GetTrackingData(handdt).rotation
                , distance * plyAttr.ply_scale // scale distance with player size
                , Networking.GetServerTimeInSeconds()
                , keep_parent
                , plyAttr.ply_scale
                , Networking.LocalPlayer.playerId);
        }
    }

    public override void OnPickupUseDown()
    {
        if (gameController == null) { return; }
        if (!use_ready) { return; }
        if (weapon_type == (int)weapon_type_name.Bomb) 
        {
            if (!waiting_for_toss)
            {
                gameController.PlaySFXFromArray(snd_source_weaponcharge, snd_game_sfx_clips_weaponcharge, weapon_type, 1.0f);
                waiting_for_toss = true;
                GetComponent<Rigidbody>().useGravity = true;
                if (!Networking.GetOwner(gameObject).IsUserInVR())
                {
                    // If we are a desktop user, just have it left click toss after the cooldown
                    TossWeapon();
                }
                else
                {
                    Transform particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
                    if (particle != null && particle.GetComponent<ParticleSystem>() != null)
                    {
                        particle.gameObject.SetActive(true);
                        particle.GetComponent<ParticleSystem>().Play();
                    }
                }
            }
            return; 
        }
        if (weapon_type == (int)weapon_type_name.SuperLaser) 
        {
            if (!weapon_is_charging)
            {
                gameController.PlaySFXFromArray(snd_source_weaponcharge, snd_game_sfx_clips_weaponcharge, weapon_type, 1.0f);
                weapon_charge_start_ms = Networking.GetServerTimeInSeconds();
                weapon_charge_timer = 0.0f;
                weapon_is_charging = true;
                weapon_mdl[weapon_type].GetComponent<Animator>().SetInteger("AnimState", 1);
            }
            return;
        }

        FireWeapon();
    }

 
    public override void OnPickupUseUp()
    {
        if (weapon_is_charging) 
        { 
            weapon_is_charging = false;
            weapon_charge_timer = 0.0f;
            if (snd_source_weaponcharge != null) { snd_source_weaponcharge.Stop(); }
            if (weapon_type == (int)weapon_type_name.SuperLaser && weapon_mdl[weapon_type].GetComponent<Animator>().GetInteger("AnimState") != 2) { weapon_mdl[weapon_type].GetComponent<Animator>().SetInteger("AnimState", 0); }
        }
    }

    public override void OnDrop()
    {
        if (waiting_for_toss)
        {
            TossWeapon();
        }
    }

    private void TossWeapon()
    {
        // Only manually apply velocity in non-VR
        // If it's a throwable and has just been thrown, however, we want to create a flying projectile in its direction
        if (!Networking.GetOwner(gameObject).IsUserInVR())
        {
            Vector3 throwDir = Networking.GetOwner(gameObject).GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward;
            float throwForce = 13.0f;
            velocity_stored = GetComponent<Rigidbody>().velocity + (throwDir * throwForce);
        }
        else { velocity_stored = GetComponent<Rigidbody>().velocity; }
        UnityEngine.Debug.Log("[WEAPON_TEST] THROW A BOMB AT VELOCITY " + velocity_stored.ToString());

        FireWeapon();
        waiting_for_toss = false;
    }

    public void PlayHapticEvent(int event_type)
    {
        // Haptic events will go in the following priority order: Death > Kill > Hit Receive > Hit Send
        // Only play hit send haptic if we aren't playing anything else (lowest priority)
        float duration = 0.0f; float amplitude = 0.0f; float frequency = 0.0f;
        if (event_type == (int)game_sfx_name.HitSend && haptic_cooldown_type == -1) { duration = 2.0f; amplitude = 0.1f; frequency = 0.1f; }
        else if (event_type == (int)game_sfx_name.HitReceive && (haptic_cooldown_type == -1 || haptic_cooldown_type == (int)game_sfx_name.HitSend)) { duration = 2.0f; amplitude = 0.3f; frequency = 0.3f; }
        else if(event_type == (int)game_sfx_name.Kill && !(haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 2.0f; amplitude = 0.6f; frequency = 0.6f; }
        else if(event_type == (int)game_sfx_name.Death && haptic_cooldown_type != (int)game_sfx_name.Death) { duration = 2.0f; amplitude = 2.0f; frequency = 1.0f; }
        //Networking.LocalPlayer.PlayHapticEventInHand(gameObject.GetComponent<VRCPickup>().currentHand, duration, amplitude, frequency);
        // Set the haptic on cooldown after playing it
        //UnityEngine.Debug.Log("PLAY HAPTIC EVENT OF TYPE " + event_type + " WHERE CURRENT COOLDOWN TYPE IS " + haptic_cooldown_type + " WITH COOLDOWN " + haptic_countdown " AND WILL BECOME " );

        haptic_cooldown_type = event_type;
        if (haptic_cooldowns.Length > event_type) { haptic_countdown = haptic_cooldowns[event_type]; }
        else { haptic_countdown = 1.0f; }

    }


}
