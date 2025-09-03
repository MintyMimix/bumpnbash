
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PortalSpectator : Portal
{
    public override void Teleport()
    {
        gameController.TeleportLocalPlayerToSpectatorArea();
    }
}
