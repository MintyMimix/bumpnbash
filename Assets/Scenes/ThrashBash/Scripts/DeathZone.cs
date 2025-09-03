
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DeathZone : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) { return; }
        player.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        var playerAttributes = gameController.FindPlayerAttributes(player);
        playerAttributes.HandleLocalPlayerDeath();
    }
}
