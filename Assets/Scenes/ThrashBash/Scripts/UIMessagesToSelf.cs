
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UIElements;
using TMPro;

public class UIMessagesToSelf : UdonSharpBehaviour
{
    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] public RectTransform PTMCanvas;
    [SerializeField] public RectTransform[] PTMTextStack;

    void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }
    
    private void Update()
    {
        if (owner == null && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            TransferOwner(Networking.LocalPlayer);
        }

        if (owner != Networking.LocalPlayer) { return; }

        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
            else { return; }
        }

        if (gameController == null || gameController.local_uiplytoself == null) { return; }
        
        string[] splitStr = gameController.local_uiplytoself.text_queue_full_str.Split(gameController.local_uiplytoself.text_queue_separator);
        for (int i = 0; i < gameController.local_uiplytoself.text_queue_limited_lines; i++)
        {
            if (i < gameController.local_uiplytoself.text_queue_full_colors.Length) { PTMTextStack[i].GetComponent<TMP_Text>().color = gameController.local_uiplytoself.text_queue_full_colors[i]; } // Needs to happen first, because alpha is modified after
            if (i < splitStr.Length)
            {
                PTMTextStack[i].GetComponent<TMP_Text>().text = splitStr[i].ToUpper();
                float duration_modified = gameController.local_uiplytoself.text_queue_full_durations[i];
                float fade_time = duration_modified - (gameController.local_uiplytoself.text_queue_limited_fade_time_percent * duration_modified);
                if (gameController.local_uiplytoself.text_queue_limited_timers[i] >= fade_time) { PTMTextStack[i].GetComponent<TMP_Text>().alpha = 1 - ((gameController.local_uiplytoself.text_queue_limited_timers[i] - fade_time) / (duration_modified - fade_time)); }
                else { PTMTextStack[i].GetComponent<TMP_Text>().alpha = 1.0f; }
            }
            else { PTMTextStack[i].GetComponent<TMP_Text>().text = ""; }
        }

    }

    public void TransferOwner(VRCPlayerApi newOwner)
    {
        owner = newOwner;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        TransferOwner(newOwner);
    }
    public override void PostLateUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        SetUIForward();
    }

    public void SetUIForward()
    {
        var heightUI = 0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        var scaleUI = 1.0f * 0.66f;
        if (gameController != null && gameController.local_ppp_options != null && gameController.local_uiplytoself != null)
        {
            PPP_Options ppp_options = gameController.local_ppp_options;
            
            scaleUI *= (ppp_options.ui_scale);
            PTMCanvas.sizeDelta = new Vector2(500, 300);
            PTMCanvas.sizeDelta = new Vector2(500 * ppp_options.ui_stretch, 300 * ppp_options.ui_separation);

            ((RectTransform)PTMTextStack[0].parent).sizeDelta = new Vector2(
                ((RectTransform)PTMTextStack[0].parent).sizeDelta.x
                , gameController.local_uiplytoself.text_queue_limited_lines * (PTMCanvas.sizeDelta.y / 10.0f)
                );

            for (int i = 0; i < gameController.local_uiplytoself.text_queue_limited_lines; i++)
            {
                //PTSTextStack[i].sizeDelta = new Vector2(PTSTextStack[i].sizeDelta.x, PTSCanvas.sizeDelta.y / 10.0f);
                float size_delta = PTMCanvas.sizeDelta.y / 10.0f;
                float half_line = (gameController.local_uiplytoself.text_queue_limited_lines / 2);
                if (i < half_line)
                {
                    PTMTextStack[i].localPosition = new Vector3(
                        PTMTextStack[i].localPosition.x
                        , ((half_line - i) * size_delta) - (size_delta / 2)
                        , PTMTextStack[i].localPosition.z);
                }
                else
                {
                    PTMTextStack[i].localPosition = new Vector3(
                        PTMTextStack[i].localPosition.x
                        , (-(i - half_line) * size_delta) - (size_delta / 2)
                        , PTMTextStack[i].localPosition.z);
                }

            }

        }

        Vector3 plyForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        Vector3 posOut = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (plyForward * heightUI);
        Vector3 posFinal = posOut; //+ velAdd;
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * heightUI * scaleUI;
        transform.SetPositionAndRotation(
            posFinal
            , Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation
            );

       
        return;
    }

}
