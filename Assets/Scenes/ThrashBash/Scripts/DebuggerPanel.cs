
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DebuggerPanel : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_uiplytoself;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_uiplytoothers;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_playerweapon;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_playerhitbox;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_scoreboard;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_gamecontroller;
    [SerializeField] public UnityEngine.UI.Toggle ui_toggle_dualwield;
    [SerializeField] public TMP_InputField ui_input_gamevarsimpulse;

    public void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        
        if ((Networking.LocalPlayer.displayName.ToLower() != "mintymimix" && Networking.LocalPlayer.displayName.ToLower() != "spectremint" && Networking.LocalPlayer.displayName.ToLower() != "themitzez")
            || !Networking.GetOwner(gameController.gameObject).isLocal) { gameObject.SetActive(false); }
    }

    public void ToggleUIPlyToSelf()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugToggleObject", ui_toggle_uiplytoself.isOn, "UIPlyToSelf");
    }

    public void ToggleUIPlyToOthers()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugToggleObject", ui_toggle_uiplytoothers.isOn, "UIPlyToOthers");
    }

    public void TogglePlayerWeapon()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugToggleObject", ui_toggle_playerweapon.isOn, "PlayerWeapon");
    }
    public void TogglePlayerHitbox()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugToggleObject", ui_toggle_playerhitbox.isOn, "PlayerHitbox");
    }

    public void ToggleScoreboard()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugToggleObject", ui_toggle_scoreboard.isOn, "Scoreboard");
    }

    public void ToggleGameController()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugToggleObject", ui_toggle_gamecontroller.isOn, "GameController");
    }

    public void ParamDualWield()
    {
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugModifyVariable", GlobalHelperFunctions.BoolToInt(ui_toggle_dualwield.isOn), "DualWield");
    }
    public void ParamGamevarsImpulse()
    {
        int try_goal_parse = 1;
        Int32.TryParse(ui_input_gamevarsimpulse.text, out try_goal_parse);
        try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DebugModifyVariable", try_goal_parse, "GamevarsImpulse");
    }
}
