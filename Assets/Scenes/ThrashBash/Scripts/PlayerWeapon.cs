
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
    PunchingGlove, Bomb, Rocket, ENUM_LENGTH
}

public class PlayerWeapon : UdonSharpBehaviour
{
    [NonSerialized] public int weapon_type;
    [NonSerialized] public float use_cooldown;
    [NonSerialized] public float use_timer;
    [NonSerialized] public bool use_ready;
    [NonSerialized] [UdonSynced] public float animate_pct;
    [SerializeField] public GameObject[] weapon_mdl;
    [SerializeField] public GameController gameController;
    [NonSerialized] public float scale_inital;

    void Start()
    {
        scale_inital = transform.localScale.x; 
        weapon_type = (int)weapon_type_name.PunchingGlove;
        UpdateStatsFromWeaponType();
    }

    public void UpdateStatsFromWeaponType()
    {
        switch (weapon_type)
        {
            case (int)weapon_type_name.PunchingGlove:
                use_cooldown = 1.2f;
                break;
            default:
                use_cooldown = 1.0f;
                break;
        }
        if (weapon_mdl[weapon_type] != null )
        {
            for (int i = 0; i < weapon_mdl.Length; i++)
            {
                if (i == weapon_type) { weapon_mdl[i].SetActive(true); }
                else { weapon_mdl[i].SetActive(false); }
            }
        }
        return;
    }

    public override void OnDeserialization()
    {
        UpdateStatsFromWeaponType();
    }

    private void Update()
    {
        // Make sure you can only fire when off cooldown
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
            if (weapon_type == (int)weapon_type_name.PunchingGlove) { animate_pct = (use_timer / use_cooldown); }
        }
        else if (gameController.round_state == (int)round_state_name.Ongoing && !use_ready)
        {
            use_ready = true;
            if (weapon_type == (int)weapon_type_name.PunchingGlove) { animate_pct = 0.0f; }
        }
        else if (gameController.round_state != (int)round_state_name.Ongoing && use_ready)
        {
            use_ready = false;
            if (weapon_type == (int)weapon_type_name.PunchingGlove) { animate_pct = 0.0f; }
        }

        
        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            var localPlayerAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
            
            // Reposition the object if we're not holding it
            if (!gameObject.GetComponent<VRCPickup>().IsHeld && (localPlayerAttr.ply_state == (int)player_state_name.Alive || localPlayerAttr.ply_state == (int)player_state_name.Respawning)) {
                
                if (Networking.LocalPlayer.IsUserInVR())
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * 0.5f);
                }
                else
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
                }
            }
            // Regardless of if we're holding it or not, scale the object with ourselves
            float playerVisualScale = localPlayerAttr.plyEyeHeight_desired / localPlayerAttr.plyEyeHeight_default;
            transform.localScale = new Vector3(scale_inital * playerVisualScale, scale_inital * playerVisualScale, scale_inital * playerVisualScale);
        }

        if (!transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) == Networking.LocalPlayer) {
            transform.GetComponent<VRCPickup>().pickupable = true;
        }
        else if (transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) != Networking.LocalPlayer)
        {
            transform.GetComponent<VRCPickup>().pickupable = false;
        }

        if (weapon_type == (int)weapon_type_name.PunchingGlove)
        {
            weapon_mdl[weapon_type].GetComponent<Animator>().SetFloat("AnimationTimer", animate_pct);
        }

        var playerAttributes = gameController.FindPlayerAttributes(Networking.LocalPlayer);
        var m_Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (m_Renderer != null && playerAttributes.gameController.team_colors != null && playerAttributes.ply_team >= 0)
        {
            if (playerAttributes.gameController.option_teamplay)
            {
                m_Renderer.material.SetColor("_Color",
                    new Color32(
                    (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].r),
                    (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].g),
                    (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].b),
                    (byte)playerAttributes.gameController.team_colors[playerAttributes.ply_team].a));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor",
                    new Color32(
                    (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].r),
                    (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].g),
                    (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].b),
                    (byte)playerAttributes.gameController.team_colors[playerAttributes.ply_team].a));
            }
            else
            {
                m_Renderer.material.SetColor("_Color",new Color32(255,255,255,255));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor", new Color32(255,255,255,255));
            }
        }

    }
    public override void OnPickupUseUp()
    {
        if (!use_ready) { return; }
        use_ready = false;
        use_timer = 0;
        if (weapon_type == (int)weapon_type_name.PunchingGlove) { animate_pct = 0.0f; }

        if (transform.GetComponent<VRCPickup>().currentPlayer == Networking.LocalPlayer)
        {
            var keep_parent = false;
            // Play the firing sound, if applicable
            switch (weapon_type)
            {
                case (int)weapon_type_name.PunchingGlove:
                    gameController.PlaySFXFromArray(
                    gameController.snd_game_sfx_sources[(int)game_sfx_index.WeaponFire]
                    , gameController.snd_game_sfx_clips[(int)game_sfx_index.WeaponFire]
                    , weapon_type
                    , UnityEngine.Random.Range(0.5f, 1.5f)
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

            var plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
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
        // To-do: play a sound when other people fire their weapon, with distance dropoff
    }

    

}
