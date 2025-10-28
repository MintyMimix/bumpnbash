
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;
using VRC.Udon;

public class HarmTesterUI : GlobalTickReceiver
{
    [SerializeField] public GameController gameController;
    [SerializeField] public UnityEngine.UI.Image FlagImage;
    [SerializeField] public UnityEngine.UI.Image PoleImage;
    [SerializeField] public UnityEngine.UI.Image CBSpriteImage;
    public float timer = 0.0f;
    public float duration = 0.0f;
    public Transform child_canvas = null;
    public override void Start()
    {
        base.Start();
    }

    public override void OnFastTick(float tickDeltaTime)
    {

        float fade_at_time = 0.4f * 2.0f; float fadeProgress = 1.0f;
        if (timer < fade_at_time) { fadeProgress = 1.0f; }
        else { fadeProgress = 1.0f - ((timer - fade_at_time) / (duration - fade_at_time)); }

        Transform[] AllChildren = child_canvas.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            TMP_Text component = t.GetComponent<TMP_Text>();
            if (component != null)
            {
                Color newColor = component.color;
                newColor.a = fadeProgress;
                component.color = newColor;
            }
            UnityEngine.UI.Image componentb = t.GetComponent<UnityEngine.UI.Image>();
            if (componentb != null)
            {
                Color newColor = componentb.color;
                newColor.a = fadeProgress;
                componentb.color = newColor;
            }
        }

        if (timer >= duration) { timer = 0.0f; gameObject.SetActive(false); }
        else { timer += tickDeltaTime; }

        if (gameController != null && gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { CBSpriteImage.enabled = true; }
        else { CBSpriteImage.enabled = false; }
        FlagImage.enabled = !CBSpriteImage.enabled;
        PoleImage.enabled = FlagImage.enabled;

    }
}
