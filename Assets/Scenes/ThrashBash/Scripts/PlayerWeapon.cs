
using System;
using System.Net.Sockets;
using UdonSharp;
using UnityEngine;
using UnityEngine.Android;
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
    [NonSerialized] [UdonSynced] public bool network_active = false;

    [NonSerialized] [UdonSynced] public int weapon_type;
    [NonSerialized] [UdonSynced] public int weapon_type_default;
    [NonSerialized] public int local_weapon_type;
    [NonSerialized] [UdonSynced] public byte weapon_extra_data = 0;
    [NonSerialized] public byte local_extra_data = 0;
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
    [NonSerialized] [UdonSynced] public int animate_state = 0;
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
    [NonSerialized] public float scale_inital = -1;

    [SerializeField] public AudioClip snd_game_sfx_clip_weaponexpire;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponfire; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcharge; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcontact; // NOTE: Corresponds to weapon_type

    [NonSerialized] public PlayerAttributes owner_attributes;

    [SerializeField] public float[] haptic_cooldowns; // NEEDS TO MATCH LENGTH OF game_sfx_name
    [NonSerialized] public float haptic_countdown = 0.0f; // We call this countdown rather than timer since it decrements rather than increments
    [NonSerialized] public int haptic_cooldown_type = -1;

    [NonSerialized] public bool waiting_for_toss = false;


    void Start()
    {
        scale_inital = transform.localScale.x;
        /*if (is_secondary) 
        { 
            gameObject.SetActive(false);
        }
        else { weapon_type = (int)weapon_type_name.PunchingGlove; }*/
        weapon_type_default = (int)weapon_type_name.PunchingGlove;
        ResetWeaponToDefault();

        if (pickup_component == null) { pickup_component = gameObject.GetComponent<VRCPickup>(); }

    }

    private void OnEnable()
    {
        network_active = true;
    }

    private void OnDisable()
    {
        network_active = false;
    }

    /*public override void OnDeserialization()
    {
        UpdateStatsFromWeaponType();
    }*/

    [NetworkCallable]
    public void UpdateStatsFromWeaponType()
    {
        if (gameController == null) { return; }

        UnityEngine.Debug.Log("[" + transform.name + "]: Updating weapon stats based on type " + weapon_type);
        
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

        // Recolor child objects
        Renderer m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        Material weapon_mat = null; 
        if (m_Renderer != null)
        {
            weapon_mat = m_Renderer.material;
            if ((weapon_type == (int)weapon_type_name.Rocket || weapon_type == (int)weapon_type_name.Bomb) && m_Renderer.materials.Length > 1) { weapon_mat = m_Renderer.materials[1]; }
        }

        Transform particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
        if (particle != null && particle.GetComponent<ParticleSystem>() != null)
        {
            var particle_main = particle.GetComponent<ParticleSystem>().main;
            if (weapon_mat != null) {
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

        // Render the item sprite, if this is a throwable item
        Transform item_shell = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "ItemShell");
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
                        sprite_to_render = gameController.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemPowerup").GetComponent<ItemPowerup>().powerup_sprites[item_index];
                    }
                    else
                    {
                        item_index = weapon_extra_data - (int)powerup_type_name.ENUM_LENGTH;
                        sprite_to_render = gameController.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemWeapon").GetComponent<ItemWeapon>().iweapon_sprites[item_index];

                    }
                    if (sprite_to_render != null) { sprite_Renderer.material.SetTexture("_MainTex", sprite_to_render.texture); }
                }
            }

        }

        if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem) {
            if (Networking.GetOwner(gameObject).IsUserInVR()) 
            {
                waiting_for_toss = true;
                use_timer = use_cooldown;
                use_ready = true;
                ToggleParticle(true);
            }
            else { waiting_for_toss = false; }
        }
        GetComponent<Rigidbody>().useGravity = false;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        local_weapon_type = weapon_type;
        local_extra_data = weapon_extra_data;
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
        if ((gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && !gameObject.activeInHierarchy && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            gameObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        if (local_weapon_type != weapon_type || local_extra_data != weapon_extra_data) { UpdateStatsFromWeaponType(); }
    }

    private void FixedUpdate()
    {
        if (pickup_component == null) { pickup_component = gameObject.GetComponent<VRCPickup>(); }

        if (scale_inital <= 0.0f) { scale_inital = transform.localScale.x; }

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

        if (weapon_charge_duration > 0.0f && weapon_is_charging) { weapon_charge_timer = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), weapon_charge_start_ms); }

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
        else if ((gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && !use_ready)
        {
            use_ready = true;
            ToggleParticle(true);
            if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.SuperLaser) { animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false; }

            // For bombs, we use the charge sound as the "ready to be thrown again" SFX, but only if we are the owner
            if (Networking.GetOwner(gameObject).playerId == Networking.LocalPlayer.playerId && (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem)) { gameController.PlaySFXFromArray(snd_source_weaponcharge, snd_game_sfx_clips_weaponcharge, weapon_type, 1.0f); }
        }
        else if (!(gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && use_ready)
        {
            use_ready = false;
            ToggleParticle(false);
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

        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing || owner_attributes.ply_training) && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {

            // Reposition the object if we're not holding it
            if (!pickup_component.IsHeld && owner_attributes != null && (owner_attributes.ply_state == (int)player_state_name.Alive || owner_attributes.ply_state == (int)player_state_name.Respawning || owner_attributes.ply_state == (int)player_state_name.Dead || owner_attributes.ply_training))
            {
                float ply_scale_offset = 1.0f;
                if (gameController != null && gameController.local_plyAttr != null && gameController.local_plyAttr.ply_scale != 1.0f) { ply_scale_offset = gameController.local_plyAttr.ply_scale; }
                if (Networking.LocalPlayer.IsUserInVR())
                {
                     transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * 0.5f * ply_scale_offset);
                }
                else if (!Networking.LocalPlayer.IsUserInVR())
                {
                    //transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
                    transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + new Vector3(1.0f * ply_scale_offset, 0.0f, 0.0f);

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
        weapon_mdl[weapon_type].GetComponent<Animator>().SetInteger("AnimationState", animate_state);
        Transform laser_sprite = null;
        if (weapon_type == (int)weapon_type_name.SuperLaser)
        {
            laser_sprite = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "LaserSprite");
            Transform laser_particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "LaserParticle");
            if (animate_state == 0)
            {
                if (laser_sprite != null) { laser_sprite.localScale = Vector3.zero; }
                if (laser_particle != null && laser_particle.gameObject.activeInHierarchy) { laser_particle.gameObject.SetActive(false); }
            }
            else if (animate_state == 1) 
            { 
                animate_pct = Mathf.Lerp(0.0f, 0.999f, System.Convert.ToSingle(weapon_charge_timer / weapon_charge_duration));
                if (laser_sprite != null) { laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.35f * animate_pct; }
                if (laser_particle != null && laser_particle.gameObject.activeInHierarchy) { laser_particle.gameObject.SetActive(true); }
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
                if (laser_sprite != null) { laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.35f; }
                }
                else
                {
                    use_pct = (float)((use_timer - timer_at_pct) / (use_cooldown - timer_at_pct));
                    animate_pct = Mathf.Lerp(0.5f, 0.999f, use_pct);
                    if (laser_sprite != null) { laser_sprite.localScale = new Vector3(1.0f, 1.0f, 1.0f) * 0.3f * Mathf.Lerp(1.0f, 0.0f, use_pct); }
                }
                if (use_ready) { animate_state = 0; }
                if (laser_particle != null && laser_particle.gameObject.activeInHierarchy) { laser_particle.gameObject.SetActive(false); }
            }
        }

        // Do not else if this; it needs to be callable even after assignment
        if (owner_attributes != null)
        {

            Renderer m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (m_Renderer != null && owner_attributes != null && gameController.team_colors != null)
            {
                Material weapon_mat = m_Renderer.material; byte emissionOffset = 0;
                int team = Mathf.Max(0, owner_attributes.ply_team);
                if ((weapon_type == (int)weapon_type_name.Rocket || weapon_type == (int)weapon_type_name.Bomb) && m_Renderer.materials.Length > 1) { weapon_mat = m_Renderer.materials[1]; emissionOffset = 80; } // 127
                else if (weapon_type == (int)weapon_type_name.SuperLaser) { emissionOffset = 0; } //67
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
                if (laser_sprite != null) { laser_sprite.GetComponent<Renderer>().material.SetColor("_Color", weapon_mat.GetColor("_EmissionColor")); }
            }
        }

        // Handle weapon charging timer
        if (weapon_is_charging)
        {
            //UnityEngine.Debug.Log("[WEAPON_TEST]: Charging: " + weapon_charge_timer + " / " + weapon_charge_duration + " (start: " + weapon_charge_start_ms + ")");
            if (weapon_charge_timer > weapon_charge_duration && weapon_charge_duration > 0.0f)
            {
                FireWeapon();
                weapon_is_charging = false;
                weapon_charge_timer = 0.0f;
                if (weapon_type == (int)weapon_type_name.SuperLaser) { animate_state = 2; }
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

        // Handle boss glove-specific behavior
        if (weapon_type == (int)weapon_type_name.BossGlove)
        {
            if (gameController.ply_in_game_auto_dict != null && gameController.ply_in_game_auto_dict.Length > 0 && gameController.ply_in_game_auto_dict[0] != null)
            {
                // Fire rate increases with number of players
                use_cooldown = Mathf.Lerp(2.0f, 0.25f, Mathf.Min(1.0f, gameController.ply_in_game_auto_dict[0].Length / 24.0f)) 
                    * gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Cooldown];
            }
        }

    }

    public void FireWeapon()
    {
        use_ready = false;
        use_timer = 0;
        if (weapon_type != (int)weapon_type_name.Bomb) { animate_pct = 0.0f; }
        ToggleParticle(false);

        //if (transform.GetComponent<VRCPickup>().currentPlayer == Networking.LocalPlayer)
        if (Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            bool keep_parent = false;
            Vector3 pos_start = transform.position;
            // Play the firing sound, if applicable
            float pitch_low = 0.5f; float pitch_high = 1.5f;
            if (weapon_type == (int)weapon_type_name.PunchingGlove) { pitch_low = 0.5f; pitch_high = 1.5f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.BossGlove) { pitch_low = 0.75f; pitch_high = 1.25f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.HyperGlove) { pitch_low = 1.5f; pitch_high = 2.0f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.MegaGlove) { pitch_low = 0.5f; pitch_high = 0.75f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.Rocket) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; }
            else if (weapon_type == (int)weapon_type_name.SuperLaser) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; }
            else if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem) 
            { 
                pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; 
                if (gameController.local_plyAttr != null) { pos_start += Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * gameController.local_plyAttr.ply_scale * 1.0f; }
            }

            gameController.PlaySFXFromArray(
                    snd_source_weaponfire
                    , snd_game_sfx_clips_weaponfire
                    , weapon_type
                    , UnityEngine.Random.Range(pitch_low, pitch_high)
                    );

            if (weapon_temp_ammo != -1) { weapon_temp_ammo--; }

            float distance = 0.0f;
            if (keep_parent) { distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance]; }
            else
            {
                distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance];
            }
            
            PlayerAttributes plyAttr = gameController.local_plyAttr;
            if (plyAttr == null) { return; }
            distance *= plyAttr.ply_scale; // scale distance with player size

            if (weapon_type != (int)weapon_type_name.ThrowableItem) { weapon_extra_data = 0; }

            PlayHapticEvent((int)game_sfx_name.ENUM_LENGTH);  // ENUM_LENGTH is used for weapon fire

            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateProjectile"
                , weapon_type
                , pos_start
                , transform.rotation //, Networking.LocalPlayer.GetTrackingData(handdt).rotation
                , new Vector3(distance, plyAttr.ply_scale, weapon_extra_data)
                , velocity_stored
                , keep_parent
                , Networking.LocalPlayer.playerId);

            // After throwing the weapon, reroll the item on it, if it has more than 1 ammo
            if (weapon_type == (int)weapon_type_name.ThrowableItem) 
            { 
                weapon_extra_data = RollForPowerupBombExtraData();
            }
        }
    }

    public override void OnPickupUseDown()
    {
        if (gameController == null) { return; }
        if (!use_ready) { return; }
        if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem) 
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
                /*else
                {
                    Transform particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
                    if (particle != null && particle.GetComponent<ParticleSystem>() != null)
                    {
                        if (gameController != null && gameController.local_ppp_options != null)
                        {
                            var particle_emission = particle.GetComponent<ParticleSystem>().emission;
                            particle_emission.enabled = gameController.local_ppp_options.particles_on;
                        }
                        particle.gameObject.SetActive(true);
                        particle.GetComponent<ParticleSystem>().Play();
                    }
                }*/
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

 
    public override void OnPickupUseUp()
    {
        if (weapon_is_charging) 
        { 
            weapon_is_charging = false;
            weapon_charge_timer = 0.0f;
            if (use_ready && snd_source_weaponcharge != null) { snd_source_weaponcharge.Stop(); }
            if (weapon_type == (int)weapon_type_name.SuperLaser && animate_state != 2) { animate_state = 0; }
        }
    }

    public override void OnDrop()
    {
        if (waiting_for_toss && use_ready)
        {
            TossWeapon();
        }
    }

    public override void OnPickup()
    {
        waiting_for_toss = (Networking.GetOwner(gameObject).IsUserInVR() && (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.ThrowableItem));
    }

    private void TossWeapon()
    {
        // Only manually apply velocity in non-VR
        // If it's a throwable and has just been thrown, however, we want to create a flying projectile in its direction
        if (!Networking.GetOwner(gameObject).IsUserInVR())
        {
            Vector3 throwDir = Networking.GetOwner(gameObject).GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
            float throwForce = 11.0f;
            velocity_stored = GetComponent<Rigidbody>().velocity + (throwDir * throwForce);
        }
        else { velocity_stored = GetComponent<Rigidbody>().velocity * 2.5f; }
        //UnityEngine.Debug.Log("[WEAPON_TEST] THROW WEAPON AT VELOCITY " + velocity_stored.ToString());

        FireWeapon();
        waiting_for_toss = false;
    }

    public void PlayHapticEvent(int event_type)
    {
        if (!Networking.LocalPlayer.IsUserInVR()) { return; }
        if (gameController != null && gameController.local_ppp_options != null && !gameController.local_ppp_options.haptics_on) { return; }

        // Haptic events will go in the following priority order: Death > Kill > Hit Receive > Hit Send > Fire Weapon
        // Only play hit send haptic if we aren't playing anything else (lowest priority)
        float duration = 0.0f; float amplitude = 0.0f; float frequency = 0.1f;
        if (event_type == (int)game_sfx_name.ENUM_LENGTH && haptic_cooldown_type == -1 && haptic_cooldown_type != (int)game_sfx_name.ENUM_LENGTH) { duration = 0.05f; amplitude = 0.05f; } // Fire weapon
        else if (event_type == (int)game_sfx_name.HitSend && !(haptic_cooldown_type == (int)game_sfx_name.HitSend || haptic_cooldown_type == (int)game_sfx_name.HitReceive || haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 0.1f; amplitude = 0.1f; }
        else if (event_type == (int)game_sfx_name.HitReceive && !(haptic_cooldown_type == (int)game_sfx_name.HitReceive || haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 0.2f; amplitude = 0.2f; }
        else if (event_type == (int)game_sfx_name.Kill && !(haptic_cooldown_type == (int)game_sfx_name.Kill || haptic_cooldown_type == (int)game_sfx_name.Death)) { duration = 0.15f; amplitude = 0.15f; }
        else if (event_type == (int)game_sfx_name.Death && haptic_cooldown_type != (int)game_sfx_name.Death) { duration = 0.3f; amplitude = 0.3f; }

        if (GetComponent<VRCPickup>().currentHand != VRCPickup.PickupHand.None &&
            (event_type == (int)game_sfx_name.ENUM_LENGTH || event_type == (int)game_sfx_name.HitSend || event_type == (int)game_sfx_name.Kill))
        { 
            Networking.LocalPlayer.PlayHapticEventInHand(GetComponent<VRCPickup>().currentHand, duration, amplitude, frequency); 
        }
        else
        {
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, duration, amplitude, frequency);
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, duration, amplitude, frequency);
        }
                    
        
        // Set the haptic on cooldown after playing it
        //UnityEngine.Debug.Log("[HAPTIC_TEST] PLAY HAPTIC EVENT OF TYPE " + event_type + " WHERE CURRENT COOLDOWN TYPE IS " + haptic_cooldown_type + " WITH CURRENT COOLDOWN " + haptic_countdown + " THAT WILL BECOME " + haptic_cooldowns[Mathf.Max(0,event_type)]);
        haptic_cooldown_type = event_type;
        if (haptic_cooldowns.Length > event_type) { haptic_countdown = haptic_cooldowns[event_type]; }
        else { haptic_countdown = 1.0f; }

    }


    [NetworkCallable]
    public void ToggleActive(bool toggle)
    {
        gameObject.SetActive(toggle);
    }

    public byte RollForPowerupBombExtraData()
    {
        byte extra_data = (byte)UnityEngine.Random.Range(0, (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH);
        // If the random powerup rolls the boss glove, set it to be another throwable item for maximum chaos
        if (extra_data == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.PunchingGlove
            || extra_data == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.BossGlove
            ) { extra_data = (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem; }
        return extra_data;
    }

    public void ToggleParticle(bool toggle)
    {
        Transform particle = gameController.GetChildTransformByName(weapon_mdl[weapon_type].transform, "Particle");
        if (particle != null && particle.GetComponent<ParticleSystem>() != null)
        {
            if (gameController != null && gameController.local_ppp_options != null)
            {
                var particle_emission = particle.GetComponent<ParticleSystem>().emission;
                particle_emission.enabled = toggle && gameController.local_ppp_options.particles_on;
            }
            particle.gameObject.SetActive(toggle);
            if (toggle) { particle.GetComponent<ParticleSystem>().Play(); }
            else {  particle.GetComponent<ParticleSystem>().Stop(); }
        }
    }

}
