
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDK3.Components;

public class PlayerWeapon : UdonSharpBehaviour
{

    public float use_cooldown;
    private float use_timer;
    private bool use_ready;
    [SerializeField] public GameController gameController;

    void Start()
    {
        
    }

    private void Update()
    {
        // Make sure you can only fire when off cooldown
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
        }
        else if (gameController.round_state == (int)round_state_name.Ongoing)
        {
            use_ready = true;
        }
        else if (gameController.round_state != (int)round_state_name.Ongoing)
        {
            use_ready = false;
        }

        if ((gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing) && !gameObject.GetComponent<VRCPickup>().IsHeld && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            var localPlayerAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
            if (localPlayerAttr.ply_state == (int)player_state_name.Alive || localPlayerAttr.ply_state == (int)player_state_name.Respawning) {
                /*transform.SetPositionAndRotation(
                    Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward)
                    , Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation
                );*/
                transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + Vector3.forward * 1.0f;
            }
        }



        if (!transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) == Networking.LocalPlayer) {
            transform.GetComponent<VRCPickup>().pickupable = true;
        }
        else if (transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) != Networking.LocalPlayer)
        {
            transform.GetComponent<VRCPickup>().pickupable = false;
        }
    }

    public override void OnPickupUseUp()
    {
        if (!use_ready) { return; }
        use_ready = false;
        use_timer = 0;
        if (transform.GetComponent<VRCPickup>().currentPlayer == Networking.LocalPlayer)
        {
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkCreateProjectile", transform.position, Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation, 0.05f, 0.3f, Networking.LocalPlayer.playerId);
        }
    }

    

}
