
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Room_Ready : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {

        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        var plyAttr = gameController.FindPlayerAttributes(player);
        if (player == Networking.LocalPlayer)
        {
            if (plyAttr != null) { 
                plyAttr.ply_state = (int)player_state_name.Joined; 

                if (gameController.DictValueFromKey(player.playerId, gameController.ply_tracking_dict_keys_arr, gameController.ply_tracking_dict_values_arr) < 1)
                {
                    if (gameController.round_state != (int)round_state_name.Start) { gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, (int)player_tracking_name.WaitingForLobby); }
                    else { gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, 0, true); }
                }
            }
        }



        // below logic will fail if the master just straight up doesn't see anything. Let's make it a networked event instead.

        // Run this only if we are the master, and that what's being collided with is in fact a player
        /*
        if (!Networking.LocalPlayer.isMaster) { return; }
        // to-do: mimic state shown in exit state
        if (gameController.DictValueFromKey(player.playerId, gameController.ply_tracking_dict_keys_arr, gameController.ply_tracking_dict_values_arr) != (int)player_tracking_name.Spectator)
        {
            if (gameController.round_state != (int)round_state_name.Start
                && ((plyAttr != null && plyAttr.ply_team < 0) || (plyAttr == null))
                ) { gameController.ChangeTeam(player.playerId, (int)player_tracking_name.WaitingForLobby); }
            else { gameController.ChangeTeam(player.playerId, 0); }
        }
        */

    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {

        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        var plyAttr = gameController.FindPlayerAttributes(player);
        if (player == Networking.LocalPlayer)
        {
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Joined)
            {
                plyAttr.ply_state = (int)player_state_name.Inactive;

                if (gameController.DictValueFromKey(player.playerId, gameController.ply_tracking_dict_keys_arr, gameController.ply_tracking_dict_values_arr) != (int)player_tracking_name.Spectator)
                {
                    //if (gameController.round_state == (int)round_state_name.Start) { 
                    gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, (int)player_tracking_name.Unassigned); 
                }
            }
        }
    }

}
