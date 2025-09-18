
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Room_Ready : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public Collider trigger_player;
    [SerializeField] public GameObject WarningCanvas;
    [SerializeField] public GameObject WarningPanel;
    [SerializeField] public GameObject monitor_obj;
    [NonSerialized] public bool warning_acknowledged = false;
    // Why we use BOTH plyAttr.ply_team and GetGlobalTeam(): one is local, the other is networked. If the network is busy, we do not want to flood them with requests!

    /*private void Update()
    {
        if (WarningCanvas != null && WarningCanvas.activeInHierarchy)
        {
            Vector3 pos = WarningPanel.transform.position;
            pos.x = Networking.LocalPlayer.GetPosition().x;
            WarningPanel.transform.position = pos;
            pos = WarningPanel.transform.localPosition;
            float widthAdjust = ((RectTransform)WarningCanvas.transform).rect.width / 2.0f;
            widthAdjust -= ((RectTransform)WarningPanel.transform).rect.width / 2.0f;
            pos.x = Mathf.Clamp(pos.x, -widthAdjust, widthAdjust);
            WarningPanel.transform.localPosition = pos;
        }
    }*/

    public void AcknowledgeWarning()
    {
        warning_acknowledged = true;
        GetComponent<Collider>().isTrigger = true;
        WarningCanvas.SetActive(false);
        if (gameController.round_state == (int)round_state_name.Start 
            || gameController.round_state == (int)round_state_name.Over 
            || gameController.round_state == (int)round_state_name.Queued
            || gameController.round_state == (int)round_state_name.Loading) 
        { gameController.PlaySFXFromArray(gameController.snd_game_music_source, gameController.snd_ready_music_clips, -1, 1, true); }
        gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_START_0", "Step in the square to join the game!"), Color.cyan);
        gameController.AddToLocalTextQueue(gameController.localizer.FetchText("NOTIFICATION_START_1", "Alternatively, you can spectate by using the 'Game' Tab in the Local Options menu!"), Color.cyan);
        gameController.room_training_portal.SetActive(true);
        if (gameController.mapscript_list != null && gameController.mapscript_list.Length > 0)
        {
            foreach (ItemSpawner itemSpawner in gameController.mapscript_list[0].GetItemSpawnerFromParent(gameController.room_training.transform))
            {
                if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                itemSpawner.item_spawn_powerups_enabled = true;
                itemSpawner.item_spawn_weapons_enabled = true;
                itemSpawner.item_spawn_frequency_mul = 1.0f;
                itemSpawner.item_spawn_duration_mul = 1.0f;
                itemSpawner.SetSpawnChances();
                if (itemSpawner.item_spawn_state == (int)item_spawn_state_name.Disabled) { itemSpawner.item_spawn_state = (int)item_spawn_state_name.Spawnable; }
                else { itemSpawner.SyncSpawns(); }
            }
        }
        //gameController.room_training.SetActive(true); // Just a temporary measure so the item spawners can be setup
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            if (!warning_acknowledged) { return; }
            PlayerAttributes plyAttr = gameController.local_plyAttr;
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Inactive && !plyAttr.ply_training) { 
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

            if (!monitor_obj.activeInHierarchy) { monitor_obj.SetActive(true); }
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!warning_acknowledged) { return; }
        // Have the local player sync their own attribute stating readiness. PlayerAttributes syncs continously.
        if (player == Networking.LocalPlayer)
        {
            PlayerAttributes plyAttr = gameController.local_plyAttr;
            if (plyAttr != null && plyAttr.ply_state == (int)player_state_name.Joined && !plyAttr.ply_training)
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

        if (monitor_obj.activeInHierarchy) { monitor_obj.SetActive(false); }
    }

}
