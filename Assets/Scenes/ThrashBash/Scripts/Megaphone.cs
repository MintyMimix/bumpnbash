
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class Megaphone : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject resetCanvas;
    [SerializeField] private Renderer m_Renderer;
    [NonSerialized] public Vector3 start_pos;

    private void Start()
    {
        start_pos = transform.position;
        resetCanvas.SetActive(false);
        SetPickupable();
    }

    public void ResetPosition()
    {
        if (start_pos != null) { transform.position = start_pos; }
        resetCanvas.SetActive(false);
    }

    public override void OnPickup()
    {
        SetVisible();
        if (!Networking.IsOwner(gameObject)) { return; }
        resetCanvas.SetActive(true);
    }

    public override void OnDrop()
    {
        SetVisible();
        if (!Networking.IsOwner(gameObject)) { return; }
        resetCanvas.SetActive(true);
    }

    public override void OnPickupUseDown()
    {
        // Press use
        if (!Networking.IsOwner(gameObject)) { return; }
        gameController.megaphone_active = true;
        gameController.RequestSerialization();
        gameController.AdjustVoiceRange();
    }

    public override void OnPickupUseUp()
    {
        // Release use
        if (!Networking.IsOwner(gameObject)) { return; }
        gameController.megaphone_active = false;
        gameController.RequestSerialization();
        gameController.AdjustVoiceRange();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        if (!Networking.IsOwner(gameObject) && resetCanvas.activeInHierarchy) { resetCanvas.SetActive(false); }
        if (GetComponent<VRCPickup>().IsHeld && GetComponent<VRCPickup>().currentPlayer != newOwner) { GetComponent<VRCPickup>().Drop(); }
        SetPickupable();
    }

    public void SetPickupable()
    {
        if (Networking.IsOwner(gameObject)) { GetComponent<VRCPickup>().pickupable = true; }
        else { GetComponent<VRCPickup>().pickupable = false; }
        SetVisible();
    }

    public void SetVisible()
    {
        if (Networking.IsOwner(gameObject) || GetComponent<VRCPickup>().IsHeld) { m_Renderer.enabled = true; }
        else { m_Renderer.enabled = false; }
    }
}
