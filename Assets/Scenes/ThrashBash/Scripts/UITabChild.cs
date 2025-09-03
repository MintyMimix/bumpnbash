
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VRC.SDKBase;
using VRC.Udon;

public class UITabChild : UdonSharpBehaviour
{
    public UITabGroup parent_tabgroup;
    public Image background;
    public bool isOn = false;

    void Start()
    {
        if (parent_tabgroup == null)
        {
            foreach (Transform t in transform)
            {
                if (t.GetComponentInParent<UITabGroup>() != null)
                {
                    parent_tabgroup = t.GetComponentInParent<UITabGroup>();
                    break;
                }
            }
        }
        if (background == null) { background = GetComponent<Image>(); }

        // If we can't find these two elements even after searching, destroy this object(?)
        if (parent_tabgroup == null || background == null)
        {
            UnityEngine.Debug.LogError("[" + transform.name + "]: Could not find parent tab group or background!");
        }
    }

    public void SendTabToggle()
    {
        if (parent_tabgroup != null) { parent_tabgroup.TabToggle(this); }
    }

}
