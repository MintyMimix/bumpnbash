
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Android;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;

public enum weapon_type_name // NOTE: NEEDS TO ALSO BE CHANGED IN GAMECONTROLLER IF ANY ARE ADDED/REMOVED FOR KeyToWeaponType()
{
    PunchingGlove, Bomb, Rocket, BossGlove, HyperGlove, MegaGlove, SuperLaser, ENUM_LENGTH
}

public class PlayerWeapon : UdonSharpBehaviour
{
    [NonSerialized][UdonSynced] public int weapon_type;
    [NonSerialized][UdonSynced] public int weapon_type_default;
    [NonSerialized] public float use_cooldown;
    [NonSerialized] public float use_timer;
    [NonSerialized] public bool use_ready;
    [NonSerialized] public int weapon_temp_ammo = -1;
    [NonSerialized] public float weapon_temp_duration = -1.0f;
    [NonSerialized] public float weapon_temp_timer = 0.0f;
    [NonSerialized][UdonSynced] public bool weapon_is_charging = false;
    [NonSerialized][UdonSynced] public double weapon_charge_start_ms = 0.0f;
    [NonSerialized][UdonSynced] public double weapon_charge_duration = 0.0f;
    [NonSerialized][UdonSynced] public float animate_pct;
    [NonSerialized][UdonSynced] public float animate_stored_pct = 1.0f;
    [NonSerialized][UdonSynced] public bool animate_handled_by_hurtbox = false;
    [NonSerialized][UdonSynced] public float animate_stored_use_timer = 0.0f;
    [NonSerialized][UdonSynced] public Vector3 velocity_stored = Vector3.zero;
    [SerializeField] public GameObject[] weapon_mdl;
    [SerializeField] public GameController gameController;
    [SerializeField] public AudioSource snd_source_weaponfire;
    [SerializeField] public AudioSource snd_source_weaponcharge;
    [SerializeField] public AudioSource snd_source_weaponcontact;
    //[SerializeField] public bool is_secondary = false;
    [NonSerialized] public float scale_inital;

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

        if (weapon_type >= 0 && weapon_type < weapon_mdl.Length && weapon_mdl[weapon_type] != null )
        {
            for (int i = 0; i < weapon_mdl.Length; i++)
            {
                if (i == weapon_type) { weapon_mdl[i].SetActive(true); }
                else if (weapon_mdl[i] != null) { weapon_mdl[i].SetActive(false); }
            }
        }

        return;
    }

    public void ResetWeaponToDefault()
    {
        weapon_type = weapon_type_default;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
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
        // Scale the object with the owner's size
        if (owner_attributes != null && owner_attributes.plyEyeHeight_default > 0.0f && scale_inital > 0.0f)
        {
            float playerVisualScale = owner_attributes.plyEyeHeight_desired / owner_attributes.plyEyeHeight_default;
            //Debug.Log(Networking.GetOwner(gameObject).displayName + "'s playerVisualScale calc: " + playerVisualScale);
            transform.localScale = new Vector3(scale_inital * playerVisualScale, scale_inital * playerVisualScale, scale_inital * playerVisualScale);
        }

        if (gameObject.GetComponent<VRCPickup>().IsHeld)
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }

    }

    private void Update()
    {
        if (gameController == null) { return; }

        // Make sure you can only fire when off cooldown
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
            if (weapon_type != (int)weapon_type_name.Bomb && weapon_type != (int)weapon_type_name.Rocket)
            {
                if (!animate_handled_by_hurtbox) { animate_pct = Mathf.Lerp(animate_stored_pct, 0.0f, (float)((use_timer - animate_stored_use_timer) / (use_cooldown - animate_stored_use_timer))); }
            }
            else if (weapon_type == (int)weapon_type_name.Rocket)
            {
                float rocket_fire_pct = 0.4f;
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
            if (weapon_type != (int)weapon_type_name.Bomb) { animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false; }
        }
        else if (gameController.round_state != (int)round_state_name.Ongoing && use_ready)
        {
            use_ready = false;
            if (weapon_type != (int)weapon_type_name.Bomb) { animate_pct = 0.0f; animate_stored_pct = 1.0f; animate_stored_use_timer = 0.0f; animate_handled_by_hurtbox = false; }
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
            if (!gameObject.GetComponent<VRCPickup>().IsHeld && owner_attributes != null && (owner_attributes.ply_state == (int)player_state_name.Alive || owner_attributes.ply_state == (int)player_state_name.Respawning || owner_attributes.ply_state == (int)player_state_name.Dead))
            {
                // If it's a throwable and has just been thrown, however, we want to create a flying projectile in its direction
                if (waiting_for_toss && weapon_type == (int)weapon_type_name.Bomb)
                {
                    FireWeapon();
                    waiting_for_toss = false;
                }
                else if (Networking.LocalPlayer.IsUserInVR())
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * 0.5f);
                }
                else if (!Networking.LocalPlayer.IsUserInVR())
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
                }
            }
        }

        if (!transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            transform.GetComponent<VRCPickup>().pickupable = true;
        }
        else if (transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) != Networking.LocalPlayer)
        {
            transform.GetComponent<VRCPickup>().pickupable = false;
        }

        if (weapon_type != (int)weapon_type_name.Bomb)
        {
            weapon_mdl[weapon_type].GetComponent<Animator>().SetFloat("AnimationTimer", animate_pct);
        }


        // Do not else if this; it needs to be callable even after assignment
        if (owner_attributes != null)
        {

            var m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (m_Renderer != null && owner_attributes != null && gameController.team_colors != null && owner_attributes.ply_team >= 0)
            {
                if (gameController.option_teamplay)
                {
                    m_Renderer.material.SetColor("_Color",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_attributes.ply_team].r)),
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_attributes.ply_team].g)),
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_attributes.ply_team].b)),
                        (byte)gameController.team_colors[owner_attributes.ply_team].a));
                    m_Renderer.material.EnableKeyword("_EMISSION");
                    m_Renderer.material.SetColor("_EmissionColor",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_attributes.ply_team].r)),
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_attributes.ply_team].g)),
                        (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_attributes.ply_team].b)),
                        (byte)gameController.team_colors[owner_attributes.ply_team].a));
                }
                else
                {
                    m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 255));
                    m_Renderer.material.EnableKeyword("_EMISSION");
                    m_Renderer.material.SetColor("_EmissionColor", new Color32(255, 255, 255, 255));
                }
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
                ResetWeaponToDefault();
            }
            if (weapon_temp_ammo != -1 && weapon_temp_ammo <= 0)
            {
                ResetWeaponToDefault();
            }
        }

        // Handle weapon charging timer
        if (weapon_is_charging) {
            double weapon_network_timer = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), weapon_charge_start_ms);
            if (weapon_network_timer > weapon_charge_duration && weapon_charge_duration > 0.0f)
            {
                FireWeapon();
                weapon_is_charging = false;
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

            // Play the firing sound, if applicable
            float pitch_low = 0.5f; float pitch_high = 1.5f;
            if (weapon_type == (int)weapon_type_name.PunchingGlove) { pitch_low = 0.5f; pitch_high = 1.5f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.BossGlove) { pitch_low = 0.75f; pitch_high = 1.25f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.HyperGlove) { pitch_low = 1.5f; pitch_high = 2.0f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.MegaGlove) { pitch_low = 0.5f; pitch_high = 0.75f; keep_parent = true; }
            else if (weapon_type == (int)weapon_type_name.Rocket) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; }
            else if (weapon_type == (int)weapon_type_name.Bomb) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; velocity_stored = gameObject.GetComponent<Rigidbody>().velocity; }
            else if (weapon_type == (int)weapon_type_name.SuperLaser) { pitch_low = 1.0f; pitch_high = 1.0f; keep_parent = false; }

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
                , transform.position
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
            }
            return; 
        }
        if (weapon_type == (int)weapon_type_name.SuperLaser) 
        {
            if (!weapon_is_charging)
            {
                gameController.PlaySFXFromArray(snd_source_weaponcharge, snd_game_sfx_clips_weaponcharge, weapon_type, 1.0f);
                weapon_charge_start_ms = Networking.GetServerTimeInSeconds();
                weapon_is_charging = true;
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
            if (snd_source_weaponcharge != null) { snd_source_weaponcharge.Stop(); }
        }
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
        Networking.LocalPlayer.PlayHapticEventInHand(gameObject.GetComponent<VRCPickup>().currentHand, duration, amplitude, frequency);
        // Set the haptic on cooldown after playing it
        //UnityEngine.Debug.Log("PLAY HAPTIC EVENT OF TYPE " + event_type + " WHERE CURRENT COOLDOWN TYPE IS " + haptic_cooldown_type + " WITH COOLDOWN " + haptic_countdown " AND WILL BECOME " );

        haptic_cooldown_type = event_type;
        if (haptic_cooldowns.Length > event_type) { haptic_countdown = haptic_cooldowns[event_type]; }
        else { haptic_countdown = 1.0f; }

    }


}
