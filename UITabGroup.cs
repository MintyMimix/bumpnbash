
using System;
using System.Diagnostics.PerformanceData;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class UITabGroup : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [NonSerialized] public int tab_selected = 0;
    [NonSerialized] public UITabChild[] tab_list;
    [SerializeField] public GameObject[] ToggleObjects;
    [SerializeField] public Color[] ToggleObjectColors;
    [SerializeField] public GameObject background;

    void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        SetupTabs();
    }

    public void SetupTabs()
    {
        ushort tab_count = 0;
        foreach (Transform t in transform)
        {
            if (t.GetComponent<UITabChild>() != null)
            {
                tab_count++;
            }
        }
        ushort tab_iter = 0;
        tab_list = new UITabChild[tab_count];
        foreach (Transform t in transform)
        {
            if (t.GetComponent<UITabChild>() != null)
            {
                tab_list[tab_iter] = t.GetComponent<UITabChild>();
                tab_iter++;
            }
        }
        if (tab_count > 0) { TabToggle(tab_list[0]); }

    }

    public void TabToggle(UITabChild tab)
    {
        for (int i = 0; i < tab_list.Length; i++)
        {
            UITabChild tabChild = tab_list[i];
            if (tabChild != null && tabChild != tab)
            {
                tabChild.isOn = false;
                if (ToggleObjectColors != null && i < ToggleObjectColors.Length && ToggleObjectColors[i] != null) 
                {
                    tabChild.background.color = new Color(
                        Mathf.Clamp(ToggleObjectColors[i].r - 0.1f, 0.0f, 1.0f)
                        , Mathf.Clamp(ToggleObjectColors[i].g - 0.1f, 0.0f, 1.0f)
                        , Mathf.Clamp(ToggleObjectColors[i].b - 0.1f, 0.0f, 1.0f)
                        , Mathf.Clamp(ToggleObjectColors[i].a - 0.1f, 0.0f, 1.0f)
                        );
                }
                else { tabChild.background.color = Color.gray; }
            }
            else if (tabChild != null && tabChild == tab)
            {
                tab_selected = i;
                if (ToggleObjectColors != null && i < ToggleObjectColors.Length && ToggleObjectColors[i] != null)
                {
                    tabChild.background.color = ToggleObjectColors[i];
                    background.GetComponent<Image>().color = tabChild.background.color;
                }
                else { tabChild.background.color = Color.white; }
            }
        }
        OnTabSelected(tab);
    }

    public virtual void OnTabSelected(UITabChild tab)
    {
        // Method can be overriden by parent scripts
        for (int i = 0; i < ToggleObjects.Length; i++)
        {
            {
                GameObject g = ToggleObjects[i];
                if (i == tab_selected) { g.SetActive(true); }
                else { g.SetActive(false); }
            }
        }
    }

}
