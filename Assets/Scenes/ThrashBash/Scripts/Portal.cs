
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Portal : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public Renderer display;
    [SerializeField] public Sprite sprite;
    [SerializeField] public Transform teleport_to_point;

    void Start()
    {
        display.material.SetTexture("_MainTex", sprite.texture);
    }

    public virtual void Teleport()
    {
        gameController.platformHook.custom_force_unhook = true;
        Networking.LocalPlayer.TeleportTo(teleport_to_point.position, teleport_to_point.rotation);
        gameController.platformHook.custom_force_unhook = false;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            Teleport();
        }
    }
}
