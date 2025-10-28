
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MapSelectTemplate : GlobalTickReceiver
{
    [SerializeField] public MapSelectPanel parent_mapselectpanel;
    [SerializeField] public TMP_Text map_name;
    [SerializeField] public TMP_Text map_description;
    [SerializeField] public UnityEngine.UI.Image map_image;
    [NonSerialized] public bool is_template = true;
    [NonSerialized] public int array_id = -1;

    public override void Start()
    {
        base.Start();
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        UnityEngine.UI.Toggle getToggle = GetComponent<UnityEngine.UI.Toggle>();
        if (getToggle != null)
        {
            bool toggle_should_be_on = Networking.IsOwner(parent_mapselectpanel.gameController.gameObject);
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
        if (parent_mapselectpanel == null || !Networking.IsOwner(parent_mapselectpanel.gameController.gameObject)) { return; }
        GameController gc = parent_mapselectpanel.gameController;
        if (gc == null || array_id < 0 || array_id >= gc.mapscript_list.Length) { return; }
        int[] maps_active_arr = GlobalHelperFunctions.ConvertStrToIntArray(gc.maps_active_str);
        maps_active_arr[array_id] = GlobalHelperFunctions.BoolToInt(GetComponent<UnityEngine.UI.Toggle>().isOn);
        gc.maps_active_str = GlobalHelperFunctions.ConvertIntArrayToString(maps_active_arr);
        gc.RequestSerialization();
        gc.RefreshSetupUI();
    }
}