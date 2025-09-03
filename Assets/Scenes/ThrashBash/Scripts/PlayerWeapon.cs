
using System;
using UdonSharp;
using UnityEngine;
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


    void Start()
    {
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
            weapon_mdl[weapon_type].SetActive(true);
        }
        return;
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

        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) && !gameObject.GetComponent<VRCPickup>().IsHeld && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            var localPlayerAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
            if (localPlayerAttr.ply_state == (int)player_state_name.Alive || localPlayerAttr.ply_state == (int)player_state_name.Respawning) {
                /*transform.SetPositionAndRotation(
                    Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward)
                    , Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation
                );*/
                if (Networking.LocalPlayer.IsUserInVR())
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * 0.5f);
                }
                else
                {
                    transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
                }
                //transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + Vector3.forward * 1.0f;
            }
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
                    keep_parent = false;
                    break;
                default:
                    break;
            }

            TrackingDataType handdt;
            if (transform.GetComponent<VRCPickup>().currentHand == VRC_Pickup.PickupHand.Right) { handdt = TrackingDataType.RightHand;  }
            else if (transform.GetComponent<VRCPickup>().currentHand == VRC_Pickup.PickupHand.Left) { handdt = TrackingDataType.LeftHand; }
            else { handdt = TrackingDataType.Head; }

            var distance = 0.0f;
            if (keep_parent) { distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance];  }
            else
            {
                distance = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Distance]
                        + (gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Duration]
                            * transform.InverseTransformDirection(Networking.LocalPlayer.GetVelocity()).x);
            }

            //NetworkCreateProjectile(int weapon_type, Vector3 fire_start_pos, Quaternion fire_angle, float distance, double fire_start_ms, int player_id)
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateProjectile"
                , weapon_type
                , transform.position 
                , transform.rotation //, Networking.LocalPlayer.GetTrackingData(handdt).rotation
                , distance // scale distance with velocity
                , Networking.GetServerTimeInSeconds()
                , keep_parent
                , Networking.LocalPlayer.playerId);

            
        }
        // To-do: play a sound when other people fire their weapon, with distance dropoff
    }

    

}
