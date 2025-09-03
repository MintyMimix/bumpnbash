
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DeathPlane : UdonSharpBehaviour
{
    public GameHandler gameHandler;

    void Start()
    {
        
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) { return; }
        player.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        var playerHandler = gameHandler.FindPlayerHandler(player);
        playerHandler.HandleOwnDeath();
    }

}
