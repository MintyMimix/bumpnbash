
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class UIArrow : GlobalTickReceiver
{
    [Header("References")]
    [SerializeField] public UnityEngine.UI.Image image_front;
    [SerializeField] public UnityEngine.UI.Image image_back;
    [SerializeField] public UnityEngine.UI.Image image_cb;
    [SerializeField] public UnityEngine.UI.Button button_increment, button_decrement;
    [SerializeField] public TMP_Text caption;
    [SerializeField] public RectTransform caption_transform;

    [Header("Configurables")]
    [Tooltip("Should the value reset to its min if going past its max, and vice versa?")]
    [SerializeField] public bool wrap_value;
    [SerializeField] public int min_value, max_value, current_value, increment_size;
    public override void Start()
    {
        base.Start();
    }

    public void ConfigurablesSanityCheck()
    {
        if (min_value > max_value || max_value < min_value) 
        {
            UnityEngine.Debug.LogWarning(gameObject.name + "(" + gameObject.GetInstanceID() + "): Min " + min_value + " and max " + max_value + " are an invalid range! Reversing accordingly.");
            var old_min = min_value;
            min_value = max_value;
            max_value = old_min;
        }
        else if (((min_value + increment_size) > max_value || (max_value - increment_size) < min_value) && min_value != max_value)
        {
            UnityEngine.Debug.LogWarning(gameObject.name + "(" + gameObject.GetInstanceID() + "): Increment size of " + increment_size + " was larger than configured min and max! (" + min_value + "," + max_value + ")");
            increment_size = 1;
        }
    }

    public int IncrementValue()
    {
        ConfigurablesSanityCheck();
        int old_value = current_value;
        if ((current_value + increment_size) > max_value && wrap_value) { current_value = min_value; }
        else if ((current_value + increment_size) <= max_value) { current_value += increment_size; }
        OnValueChanged(old_value, current_value);
        return current_value;
    }

    public int DecrementValue()
    {
        ConfigurablesSanityCheck();
        int old_value = current_value;
        if ((current_value - increment_size) < min_value && wrap_value) { current_value = max_value; }
        else if ((current_value - increment_size) >= min_value) { current_value -= increment_size; }
        OnValueChanged(old_value, current_value);
        return current_value;
    }

    public virtual void OnValueChanged(int old_value, int new_value)
    {
        // Do things in the parent object when the value changes
    }

    /*public void SetDefaultCaptionREPLACETHISEVENT()
    {
        if (caption == null) { return; }
        else { caption.text = current_value.ToString(); }
    }*/
}
