
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class PPP_Options : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public UnityEngine.UI.Toggle ui_colorblindtoggle;
    [SerializeField] public UnityEngine.UI.Slider ui_uiscaleslider;
    [SerializeField] public TMP_Text ui_uiscaletext;
    [NonSerialized] public bool colorblind = false;
    [NonSerialized] public float ui_scale = 1.0f;
    [NonSerialized] public bool waiting_on_playerdata = true;

    public override void OnPlayerRestored(VRCPlayerApi player)  
    {
        if (!player.isLocal) { return; }
        gameController.local_ppp_options = this;
        RefreshComponents();
        UpdateUIScale();
        UpdateColorblind();
        waiting_on_playerdata = false;
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal) {
            var should_refresh = false;
            for (int i = 0; i < infos.Length; i++)
            {
                // To-do: if we add more keys, make exceptions for those keys
                if (infos[i].State == PlayerData.State.Changed) 
                { 
                    should_refresh = true; 
                    break; 
                }
            }
            if (should_refresh) { RefreshComponents(); }
        }
    }

    public void RefreshComponents()
    {
        ui_uiscaleslider.value = PlayerData.GetFloat(Networking.LocalPlayer, "LocalOptions_UIScale");
        ui_colorblindtoggle.isOn = PlayerData.GetBool(Networking.LocalPlayer, "LocalOptions_Colorblind");
    }

    public void UpdateUIScale()
    {
        ui_scale = ui_uiscaleslider.value / 10.0f;
        var ui_plyself_obj = gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "UIPlyToSelf");
        if (ui_plyself_obj != null && ui_plyself_obj.GetComponent<UIPlyToSelf>() != null)
        {
            ui_plyself_obj.GetComponent<UIPlyToSelf>().ui_demo_timer = 0.0f;
            ui_plyself_obj.GetComponent<UIPlyToSelf>().ui_demo_enabled = true;
        }
        ui_uiscaletext.text = "UI Scale: " + (ui_scale * 100.0f) + "%";

        if (waiting_on_playerdata) { return; }
        PlayerData.SetFloat("LocalOptions_UIScale", ui_uiscaleslider.value);
    }

    public void UpdateColorblind()
    {
        colorblind = ui_colorblindtoggle.isOn;
        gameController.RoundRefreshUI();

        if (waiting_on_playerdata) { return; }
        PlayerData.SetBool("LocalOptions_Colorblind", ui_colorblindtoggle.isOn);
        
    }

}
