
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Room_Ready : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;

    // Why we use BOTH plyAttr.ply_team and GetGlobalTeam(): one is local, the other is networked. If the network is busy, we do not want to flood them with requests!

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            var plyAttr = gameController.local_plyAttr;
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Inactive) { 
                plyAttr.ply_state = (int)player_state_name.Joined; 

                if (gameController.GetGlobalTeam(player.playerId) < 0 && plyAttr.ply_team < 0)
                {
                    if (gameController.round_state != (int)round_state_name.Start 
                        && gameController.GetGlobalTeam(player.playerId) != (int)player_tracking_name.WaitingForLobby
                        && plyAttr.ply_team != (int)player_tracking_name.WaitingForLobby) 
                    {
                        plyAttr.ply_team = (int)player_tracking_name.WaitingForLobby;
                        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, (int)player_tracking_name.WaitingForLobby, true);
                    }
                    else if (gameController.round_state == (int)round_state_name.Start)
                    {
                        plyAttr.ply_team = 0;
                        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, 0, true); 
                    }
                }
            }
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {

        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            var plyAttr = gameController.local_plyAttr;
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Joined)
            {
                plyAttr.ply_state = (int)player_state_name.Inactive;

                if (gameController.round_state == (int)round_state_name.Start 
                    && gameController.GetGlobalTeam(player.playerId) != (int)player_tracking_name.Spectator 
                    && gameController.GetGlobalTeam(player.playerId) != (int)player_tracking_name.Unassigned
                    && plyAttr.ply_team != (int)player_tracking_name.Unassigned
                    && plyAttr.ply_team != (int)player_tracking_name.Unassigned
                    )
                {
                    //if (gameController.round_state == (int)round_state_name.Start) { 
                    plyAttr.ply_team = (int)player_tracking_name.Unassigned;
                    gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, (int)player_tracking_name.Unassigned, false); 
                }
            }
        }
    }

}
