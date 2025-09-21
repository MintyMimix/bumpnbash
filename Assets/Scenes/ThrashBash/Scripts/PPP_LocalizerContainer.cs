
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class PPP_LocalizerContainer : UdonSharpBehaviour
{
    // The purpose of this object is solely to have local references available for the Localizer object to access. No actual behavior is performed.
    [SerializeField] public TMP_Text PPPTitle;
    [SerializeField] public TMP_Text PPPPanel_UITabChild_0;
    [SerializeField] public TMP_Text PPPPanel_UITabChild_1;
    [SerializeField] public TMP_Text PPPPanel_UITabChild_2;
    [SerializeField] public TMP_Text PPPPanel_UITabChild_3;
    [SerializeField] public TMP_Text PPPWristNoneToggle;
    [SerializeField] public TMP_Text PPPWristLToggle;
    [SerializeField] public TMP_Text PPPWristRToggle;
    [SerializeField] public TMP_Text PPPUIInvertedToggle;
    [SerializeField] public TMP_Text PPPUIResetButton;
    [SerializeField] public TMP_Text PPPSoundGlobalHeader;
    [SerializeField] public TMP_Text PPPMusicOptionsCaption;
    [SerializeField] public TMP_Text PPPMusicOptionsWarning;
    [SerializeField] public TMP_Text PPPVOHeader;
    [SerializeField] public TMP_Text PPPVOEventAToggle;
    [SerializeField] public TMP_Text PPPVOEventBToggle;
    [SerializeField] public TMP_Text PPPVOEventCToggle;
    [SerializeField] public TMP_Text PPPTutorialCanvas;
    [SerializeField] public TMP_Text PPPTutorialResetButtonTxt;
    [SerializeField] public TMP_Text PPPSpectatorToggle;
    [SerializeField] public TMP_Text PPPHitboxToggle;
    [SerializeField] public TMP_Text PPPHurtboxToggle;
    [SerializeField] public TMP_Text PPPParticleToggle;
    [SerializeField] public TMP_Text PPPHapticsToggle;
    [SerializeField] public TMP_Text PPPMotionSicknessToggleFloor;
    [SerializeField] public TMP_Text PPPMotionSicknessToggleCage;
    [SerializeField] public TMP_Text PPPColorblindHeader;
    [SerializeField] public TMP_Text PPPColorblindToggle;

}
