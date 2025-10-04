
using System;
using TMPro;
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
    [SerializeField] public string name_default;
    [SerializeField] public string name_localizer_variable;
    [SerializeField] public TMP_Text label;
    [NonSerialized] private int cached_language_type = -1;

    void Start()
    {
        display.material.SetTexture("_MainTex", sprite.texture);
    }

    public void Update()
    {
        if (cached_language_type != gameController.localizer.language_type)
        {
            label.text = gameController.localizer.FetchText(name_localizer_variable, name_default);
            cached_language_type = gameController.localizer.language_type;
        }
    }

    public virtual void Teleport()
    {
        gameController.platformHook.custom_force_unhook = true;
        UnityEngine.Debug.Log("[PORTAL_TEST]: Teleporting using portal " + gameObject.name);
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
