
using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class PPP_Pickup : UdonSharpBehaviour
{
    [SerializeField] public PPP_Options ppp_options;

    void Start()
    {
        //if (!Networking.LocalPlayer.IsUserInVR()) { gameObject.SetActive(false); }
    }

    public void Update()
    {
        PostLateUpdate();
    }

    public override void PostLateUpdate()
    {
        if (!Networking.IsOwner(gameObject)) { return; }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
        {
            OnPickup();
        }

        float heightUI = 0.5f * Mathf.Max(0.25f, (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f));
        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * -1.2f * heightUI; //too short: -1.0f; too long: -1.333f;
        Vector3 posFinal = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + plyForward;
        transform.SetPositionAndRotation(
            posFinal
            , Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation
            );
    }

    public override void OnPickup()
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        if (!ppp_options.gameController.room_ready_script.warning_acknowledged) { GetComponent<VRC_Pickup>().Drop(); return; }

        ppp_options = ppp_options.gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "PPPCanvas").GetComponent<PPP_Options>();

        if (ppp_options != null)
        {
            ppp_options.PushPPPCanvas();
            GetComponent<VRC_Pickup>().Drop();
        }
    }

}
