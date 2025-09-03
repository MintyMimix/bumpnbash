
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MapSelectTemplate : UdonSharpBehaviour
{
    [SerializeField] public MapSelectPanel parent_mapselectpanel;
    [SerializeField] public TMP_Text map_name;
    [SerializeField] public TMP_Text map_description;
    [SerializeField] public UnityEngine.UI.Image map_image;
    [NonSerialized] public bool is_template = true;
    [NonSerialized] public int array_id = -1;

    private void Update()
    {
        UnityEngine.UI.Toggle getToggle = GetComponent<UnityEngine.UI.Toggle>();
        if (getToggle != null)
        {
            bool toggle_should_be_on = Networking.GetOwner(parent_mapselectpanel.gameController.gameObject) == Networking.LocalPlayer;
            if (parent_mapselectpanel != null && parent_mapselectpanel.gameController.round_state != (int)round_state_name.Start) { toggle_should_be_on = false; }

            if (toggle_should_be_on && getToggle.interactable == false) { getToggle.interactable = true; }
            else if (!toggle_should_be_on && getToggle.interactable == true) { getToggle.interactable = false; }
        }
    }

    public void Refresh()
    {
        if (parent_mapselectpanel == null) { return; }
        GameController gc = parent_mapselectpanel.gameController;
        if (gc == null || array_id < 0 || array_id >= gc.mapscript_list.Length) { return; }

        Mapscript mapscript = gc.mapscript_list[array_id];
        map_name.text = mapscript.map_name;
        map_description.text = mapscript.map_description;
        map_image.sprite = mapscript.map_image;

    }

    public void Press()
    {
        if (parent_mapselectpanel == null || Networking.GetOwner(parent_mapselectpanel.gameController.gameObject) != Networking.LocalPlayer) { return; }
        GameController gc = parent_mapselectpanel.gameController;
        if (gc == null || array_id < 0 || array_id >= gc.mapscript_list.Length) { return; }
        int[] maps_active_arr = gc.ConvertStrToIntArray(gc.maps_active_str);
        maps_active_arr[array_id] = gc.BoolToInt(GetComponent<UnityEngine.UI.Toggle>().isOn);
        gc.maps_active_str = gc.ConvertIntArrayToString(maps_active_arr);
        gc.RequestSerialization();
        gc.RefreshSetupUI();
    }
}