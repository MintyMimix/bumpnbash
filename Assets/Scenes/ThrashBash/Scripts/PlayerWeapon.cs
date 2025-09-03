
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
    PunchingGlove, Bomb, Rocket, BossGlove, ENUM_LENGTH
}

public class PlayerWeapon : UdonSharpBehaviour
{
    [NonSerialized] public int weapon_type;
    [NonSerialized] public float use_cooldown;
    [NonSerialized] public float use_timer;
    [NonSerialized] public bool use_ready;
    [NonSerialized] public int weapon_temp_ammo = -1;
    [NonSerialized] public float weapon_temp_duration = 30.0f;
    [NonSerialized] public float weapon_temp_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float animate_pct;
    [SerializeField] public GameObject[] weapon_mdl;
    [SerializeField] public GameController gameController;
    [SerializeField] public AudioSource snd_source_weaponfire;
    [SerializeField] public AudioSource snd_source_weaponcontact;
    [SerializeField] public bool is_boss_secondary = false;
    [NonSerialized] public float scale_inital;

    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponfire; // NOTE: Corresponds to weapon_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponcontact; // NOTE: Corresponds to weapon_type

    [NonSerialized] public PlayerAttributes owner_attributes;

    void Start()
    {
        scale_inital = transform.localScale.x;
        if (is_boss_secondary) 
        { 
            weapon_type = (int)weapon_type_name.BossGlove;
            gameObject.SetActive(false);
        }
        else { weapon_type = (int)weapon_type_name.PunchingGlove; }
        UpdateStatsFromWeaponType();
    }

    public void UpdateStatsFromWeaponType()
    {
        if (gameController == null) { return; }

        use_cooldown = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Cooldown];

        if (weapon_mdl[weapon_type] != null )
        {
            for (int i = 0; i < weapon_mdl.Length; i++)
            {
                if (i == weapon_type) { weapon_mdl[i].SetActive(true); }
                else if (weapon_mdl[i] != null) { weapon_mdl[i].SetActive(false); }
            }
        }

        return;
    }

    public override void OnDeserialization()
    {
        UpdateStatsFromWeaponType();
    }

    public void DoubleCheckLocalPlyAttr()
    {
        if (gameController == null) { return; }
        if (owner_attributes == null)
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
    }

    [NetworkCallable]
    public void ProcessOwnerChanged()
    {
        DoubleCheckLocalPlyAttr();
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
    }

    private void Update()
    {
        if (gameController == null) { return; }

        // Make sure you can only fire when off cooldown
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
            if (weapon_type == (int)weapon_type_name.PunchingGlove || weapon_type == (int)weapon_type_name.BossGlove) { animate_pct = (use_timer / use_cooldown); }
        }
        else if (gameController.round_state == (int)round_state_name.Ongoing && !use_ready)
        {
            use_ready = true;
            if (weapon_type == (int)weapon_type_name.PunchingGlove || weapon_type == (int)weapon_type_name.BossGlove) { animate_pct = 0.0f; }
        }
        else if (gameController.round_state != (int)round_state_name.Ongoing && use_ready)
        {
            use_ready = false;
            if (weapon_type == (int)weapon_type_name.PunchingGlove || weapon_type == (int)weapon_type_name.BossGlove) { animate_pct = 0.0f; }
        }

        DoubleCheckLocalPlyAttr();
        //ForceActive();

        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
           
            // Reposition the object if we're not holding it
            if (!gameObject.GetComponent<VRCPickup>().IsHeld && owner_attributes != null && (owner_attributes.ply_state == (int)player_state_name.Alive || owner_attributes.ply_state == (int)player_state_name.Respawning || owner_attributes.ply_state == (int)player_state_name.Dead))
            {
                if (!is_boss_secondary)
                {
                    if (Networking.LocalPlayer.IsUserInVR())
                    {
                        transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * 0.5f);
                    }
                    else
                    {
                        transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
                    }
                }
                else if (Networking.LocalPlayer.IsUserInVR() && owner_attributes.ply_team == 1 && gameController.option_gamemode == (int)round_mode_name.BossBash)
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + new Vector3(0.0f, -0.05f, 0.0f) + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * 0.5f);
                }
            }
        }

        // Boss secondary should only be enabled if the game is ongoing and the owner is the boss and is in VR
        if (is_boss_secondary) {
            if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing)
            && owner_attributes.ply_team == 1 && gameController.option_gamemode == (int)round_mode_name.BossBash && Networking.GetOwner(gameObject).IsUserInVR())
            {
                if (!gameObject.activeInHierarchy) { gameObject.SetActive(true); }
            }
            else
            {
                gameObject.SetActive(false);
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

        if (weapon_type == (int)weapon_type_name.PunchingGlove || weapon_type == (int)weapon_type_name.BossGlove)
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

    }
    public override void OnPickupUseUp()
    {
        if (gameController == null) { return; }
        if (!use_ready) { return; }
        use_ready = false;
        use_timer = 0;
        if (weapon_type == (int)weapon_type_name.PunchingGlove || weapon_type == (int)weapon_type_name.BossGlove) { animate_pct = 0.0f; }

        if (transform.GetComponent<VRCPickup>().currentPlayer == Networking.LocalPlayer)
        {
            var keep_parent = false;
            // Play the firing sound, if applicable
            switch (weapon_type)
            {
                case (int)weapon_type_name.PunchingGlove:
                    gameController.PlaySFXFromArray(
                    snd_source_weaponfire
                    , snd_game_sfx_clips_weaponfire
                    , weapon_type
                    , UnityEngine.Random.Range(0.5f, 1.5f)
                    );
                    keep_parent = true;
                    break;
                case (int)weapon_type_name.BossGlove:
                    gameController.PlaySFXFromArray(
                    snd_source_weaponfire
                    , snd_game_sfx_clips_weaponfire
                    , weapon_type
                    , UnityEngine.Random.Range(0.75f, 1.25f)
                    );
                    keep_parent = true;
                    break;
                default:
                    break;
            }

            /*TrackingDataType handdt;
            if (transform.GetComponent<VRCPickup>().currentHand == VRC_Pickup.PickupHand.Right) { handdt = TrackingDataType.RightHand;  }
            else if (transform.GetComponent<VRCPickup>().currentHand == VRC_Pickup.PickupHand.Left) { handdt = TrackingDataType.LeftHand; }
            else { handdt = TrackingDataType.Head; }*/

            var distance = 0.0f;
            if (keep_parent) { distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance];  }
            else
            {
                distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance]
                        + (gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Duration]
                            * transform.InverseTransformDirection(Networking.LocalPlayer.GetVelocity()).x);
            }

            var plyAttr = gameController.local_plyAttr;
            if (plyAttr == null) { return; }
            //NetworkCreateProjectile(int weapon_type, Vector3 fire_start_pos, Quaternion fire_angle, float distance, double fire_start_ms, int player_id)
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

    

}
