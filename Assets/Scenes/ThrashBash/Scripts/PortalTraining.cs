
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PortalTraining : Portal
{
    [SerializeField] public int portal_type; // 0: Ready Room -> Hall; 1: Hall -> Ready Room; 2: Hall -> Arena

    public override void Teleport()
    {
        if (portal_type == 0)
        {
            gameController.TeleportLocalPlayerToTrainingHall();
        }
        else if (portal_type == 1)
        {
            gameController.TeleportLocalPlayerToReadyRoom();
        }
        else if (portal_type == 2)
        {
            gameController.TeleportLocalPlayerToTrainingArena();
        }
    }
}
