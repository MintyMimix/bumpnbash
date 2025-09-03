
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ClockDisplay : UdonSharpBehaviour
{
    private TMP_Text textComponent;
    // To-do: Optimization by having this on a cycle rather than per-frame
    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
    }
    void OnEnable()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (textComponent == null) { return; }
        System.DateTime dt = System.DateTime.Now;
        if (dt == null) { return; }
        int hours = dt.TimeOfDay.Hours;
        if (dt.TimeOfDay.Hours > 12) { hours -= 12; }
        else if (dt.TimeOfDay.Hours == 0) { hours = 12; }
        textComponent.text = hours.ToString().PadLeft(2, '0') + ":" + dt.TimeOfDay.Minutes.ToString().PadLeft(2, '0');
    }
}
