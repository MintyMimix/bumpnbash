
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class UIPlyToOthers : UdonSharpBehaviour
{
    public GameController gameController;
    public TMP_Text PTODebugInfo;
    public VRCPlayerApi owner;
    void Start()
    {
        
    }

    private void Update()
    {
        var debugText = "";
        owner = Networking.GetOwner(gameObject);
        var ownerAttr = gameController.FindPlayerAttributes(owner);
        debugText += owner.displayName + " {" + owner.playerId + "}";
        debugText += "\n" + ownerAttr.ply_dp + " % [" + ownerAttr.ply_lives + "]";
        debugText += "\n" + "PlayerAttributes: " + Networking.GetOwner(gameController.FindOwnedObject(owner, "PlayerAttributes")).playerId.ToString();
        debugText += "\n" + "PlayerWeapon: " + Networking.GetOwner(gameController.FindOwnedObject(owner, "PlayerWeapon")).playerId.ToString();
        //debugText += "\n" + "PlayerHitbox: " + .playerId.ToString();
        PTODebugInfo.text = debugText;

        transform.SetPositionAndRotation(owner.GetPosition() + new Vector3 (0.0f, 1.5f, 0.0f), owner.GetRotation());
    }
}
