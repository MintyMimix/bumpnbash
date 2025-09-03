
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class UIArrowTemplate : UdonSharpBehaviour
{
    [Header("References")]
    [SerializeField] public UnityEngine.UI.Image image_front;
    [SerializeField] public UnityEngine.UI.Image image_back;
    [SerializeField] public UnityEngine.UI.Button button_increment, button_decrement;
    [SerializeField] public TMP_Text caption;

    [Header("Configurables")]
    [Tooltip("Should the value reset to its min if going past its max, and vice versa?")]
    [SerializeField] public bool wrap_value;
    [SerializeField] public int min_value, max_value, current_value, increment_size;

    private void Start()
    {
        ConfigurablesSanityCheck();
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
        if ((current_value + increment_size) > max_value && wrap_value) { current_value = min_value; }
        else if ((current_value + increment_size) <= max_value) { current_value += increment_size; }
        return current_value;
    }

    public int DecrementValue()
    {
        ConfigurablesSanityCheck();
        if ((current_value - increment_size) < min_value && wrap_value) { current_value = min_value; }
        else if ((current_value - increment_size) >= min_value) { current_value -= increment_size; }
        return current_value;
    }
}
