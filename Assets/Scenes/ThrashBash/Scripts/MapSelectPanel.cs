
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class MapSelectPanel : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    public GameObject template_map_panel;
    public GameObject[] map_obj_list;
    public GridLayoutGroup map_grid;

    void Start()
    {
        map_obj_list = new GameObject[0];
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
            else { return; }
        }
    }

    public void BuildMapList()
    {
        // Delete old objects before creating a new one
        for (int i = 0; i < map_obj_list.Length; i++)
        {
            MapSelectTemplate panel_attr = map_obj_list[i].GetComponent<MapSelectTemplate>();
            Destroy(map_obj_list[i]);
            //panel_attr.Refresh();
        }

        // Then create a new one based on the length of the mapscript list
        ushort map_count = (ushort)gameController.mapscript_list.Length;
        map_obj_list = new GameObject[map_count];

        for (int i = 0; i < map_count; i++)
        {
            map_obj_list[i] = Instantiate(template_map_panel);
            map_obj_list[i].transform.SetParent(map_grid.transform, false);
            map_obj_list[i].SetActive(true);
            MapSelectTemplate panel_attr = map_obj_list[i].GetComponent<MapSelectTemplate>();
            panel_attr.is_template = false;
            panel_attr.array_id = i;
            panel_attr.Refresh();

            if (Networking.IsMaster) 
            {
                if (i > 0) { gameController.maps_active_str += ","; }
                gameController.maps_active_str += "0";
            }
        }
        ArrangeMaps();
        if (Networking.IsMaster)
        {
            gameController.RequestSerialization();
        }
    }

    public void RefreshMapList()
    {
        if (gameController == null || gameController.maps_active_str == null) { return; }
        if (map_obj_list == null) { BuildMapList(); }

        int[] maps_active_arr = gameController.ConvertStrToIntArray(gameController.maps_active_str);
        for (int i = 0; i < map_obj_list.Length; i++)
        {
            MapSelectTemplate panel_attr = map_obj_list[i].GetComponent<MapSelectTemplate>();
            panel_attr.Refresh();
            if (!Networking.IsMaster) { map_obj_list[i].GetComponent<Toggle>().isOn = gameController.IntToBool(maps_active_arr[i]); }
        }
    }

    public int[] GetActiveMaps()
    {
        ushort maps_active = 0;
        int[] active_maps_from_full = new int[map_obj_list.Length];
        for (int i = 0; i < map_obj_list.Length; i++)
        {
            Toggle panel_toggle = map_obj_list[i].GetComponent<Toggle>();
            // Since we put on a "unselected" overlay when the checkbox is on, we want to add to the list when the checkbox is off
            if (!panel_toggle.isOn)
            {
                active_maps_from_full[maps_active] = i;
                maps_active++;
            }
        }

        int[] active_maps_condensed = new int[maps_active];
        for (int j = 0; j < maps_active; j++)
        {
            active_maps_condensed[j] = active_maps_from_full[j];
        }
        UnityEngine.Debug.Log("Active maps: " + gameController.ConvertIntArrayToString(active_maps_condensed));
        return active_maps_condensed;
    }

    public int SelectRandomActiveMap()
    {
        int[] maps_to_select_from = GetActiveMaps();
        int RandRoll = UnityEngine.Random.Range(0, maps_to_select_from.Length);
        return maps_to_select_from[RandRoll];
    }

    public void ArrangeMaps()
    {
        if (map_grid != null)
        {
            // Rescale the panels based on how many there are
            var active_panels = map_obj_list.Length;
            var base_columns = 1;
            Vector2 base_dims = ((RectTransform)template_map_panel.transform).sizeDelta;
            Vector3 base_scale = ((RectTransform)template_map_panel.transform).localScale;
            Vector2 grid_dims = ((RectTransform)map_grid.transform).sizeDelta;
            float[] grid_result = gameController.CalcGridDistr(active_panels, base_columns, base_dims, base_scale, grid_dims, true);
            map_grid.constraintCount = (int)grid_result[0];
            //map_grid.cellSize = base_dims * grid_result[1];
            map_grid.spacing = new Vector2(-grid_result[4], -grid_result[5] );
            for (int i = 0; i < map_obj_list.Length; i++)
            {
                ((RectTransform)map_obj_list[i].transform).localScale = new Vector3(grid_result[1], grid_result[2], grid_result[3]);
            }
        }
    }
}
