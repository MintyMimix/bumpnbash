
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DeathZone : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;

    private void Start()
    {
        transform.GetComponent<Renderer>().enabled = false;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) { return; }
        player.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        PlayerAttributes playerAttributes = gameController.FindPlayerAttributes(player);
        if (playerAttributes != null) { playerAttributes.HandleLocalPlayerDeath(); }
    }
}
