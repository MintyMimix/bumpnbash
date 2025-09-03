
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Room_Ready : UdonSharpBehaviour
{
    public GameController gameController;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        // Run this only if we are the master, and that what's being collided with is in fact a player
        if (!Networking.LocalPlayer.isMaster || gameController.round_state != (int)round_state_name.Start) { return; }
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "AddPlayerToActive", player.playerId);
    }
    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        // Run this only if we are the master, and that what's being collided with is in fact a player
        if (!Networking.LocalPlayer.isMaster || gameController.round_state != (int)round_state_name.Start) { return; }
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "RemovePlayerFromActive", player.playerId);
    }

}
