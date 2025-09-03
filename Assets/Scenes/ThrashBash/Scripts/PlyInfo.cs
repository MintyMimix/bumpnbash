
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;

public class PlyInfo : UdonSharpBehaviour
{
    public GameHandler gameHandler;
    private PlayerHandler localPlayerHandler;
    public TMP_Text secondaryInfo;

    void Start()
    {
        localPlayerHandler = gameHandler.FindPlayerHandler(Networking.LocalPlayer);
    }

    private void Update()
    {

        // Update UI
        transform.parent.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward);
        transform.parent.rotation = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation;
        //transform.GetComponent<TextMeshProUGUI>().SetText("Damage: " + localPlayerHandler.plyDP + "%");
        var showTextPrimary = "Damage: " + localPlayerHandler.plyDP + "%\nLives: " + localPlayerHandler.plyLives;
        var showTextSecondary = "";
        switch (localPlayerHandler.localPlayerState) 
        {
            case 0:
                //showTextPrimary = "Ready to Start";
                break;
            case 1:
                break;
            case 2:
                if (localPlayerHandler.lastHitByPly == null) { showTextSecondary = "You fell off the map!"; }
                else { showTextSecondary = "You were knocked out by " + localPlayerHandler.lastHitByPly.displayName + "!" ; }
                break;
            case 3:
                showTextPrimary = "You were defeated!";
                if (localPlayerHandler.lastHitByPly != null) { showTextSecondary = "Your last KO was from " + localPlayerHandler.lastHitByPly.displayName + "!"; }
                break;
            default:
                break;
        }
        switch (gameHandler.roundState) 
        {
            case 1:
                showTextPrimary = Mathf.Floor(5.0f - gameHandler.roundTimer + 1.0f).ToString();
                break;
            case 2:
                showTextPrimary = Mathf.Floor(gameHandler.roundLength - gameHandler.roundTimer).ToString() + "\n" + showTextPrimary;
                break;
            case 3:
                showTextPrimary = "Game Over!" + "\n" + showTextPrimary;
                break;
            default:
                break;
        }
        transform.GetComponent<TMP_Text>().text = showTextPrimary;
        secondaryInfo.text = showTextSecondary;
    }
}
