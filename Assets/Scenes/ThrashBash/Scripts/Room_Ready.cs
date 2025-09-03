
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Room_Ready : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            var plyAttr = gameController.FindPlayerAttributes(player);
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Inactive) { 
                plyAttr.ply_state = (int)player_state_name.Joined; 
            }
        }
        // Run this only if we are the master, and that what's being collided with is in fact a player
        if (!Networking.LocalPlayer.isMaster) { return; }
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ManipulatePlyTrackingArray", player.playerId, ply_tracking_arr_name.Ready, true);
    }
    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            var plyAttr = gameController.FindPlayerAttributes(player);
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Joined)
            {
                plyAttr.ply_state = (int)player_state_name.Inactive;
            }
        }

        // Run this only if we are the master, and that what's being collided with is in fact a player
        if (!Networking.LocalPlayer.isMaster) { return; }
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ManipulatePlyTrackingArray", player.playerId, ply_tracking_arr_name.Ready, false);
    }

}
