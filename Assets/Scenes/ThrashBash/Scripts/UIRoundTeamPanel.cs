
using Newtonsoft.Json.Linq;
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class UIRoundTeamPanel : UdonSharpBehaviour
{

    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject UIScrollPanel;
    [SerializeField] public GridLayoutGroup UIScrollPanelGridLayoutGroup;
    [SerializeField] public GameObject template_UIAssignTeamPanel;
    [SerializeField] public GameObject UITeamCountPanel;
    [SerializeField] public GridLayoutGroup UITeamCountPanelLayoutGroup;
    [SerializeField] public GameObject template_TeamCountDisplay;

    [NonSerialized] public GameObject[] player_obj_list;
    [NonSerialized] public GameObject[] team_obj_list;
    [NonSerialized] public int[] team_count_arr;

    public int stored_team_count = 0;

    private void Start()
    {
        //var template_panel = (RectTransform)template_UIAssignTeamPanel.transform;
        //template_panel.parent = null;
        player_obj_list = new GameObject[0];
        team_obj_list = new GameObject[0];
        team_count_arr = new int[0];
        template_UIAssignTeamPanel.transform.SetParent(null, false);
        template_UIAssignTeamPanel.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        template_UIAssignTeamPanel.SetActive(false);
        template_TeamCountDisplay.transform.SetParent(null, false);
        template_TeamCountDisplay.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        template_TeamCountDisplay.SetActive(false);

        Debug.Log("UIScrollPanel is " + UIScrollPanel);
        if (UIScrollPanel == null)
        {
            UIScrollPanel = GameObject.Find("UIScrollPanel");
            if (UIScrollPanel != null) { UIScrollPanelGridLayoutGroup = UIScrollPanel.GetComponent<GridLayoutGroup>(); }
        }
        else { UIScrollPanelGridLayoutGroup = UIScrollPanel.GetComponent<GridLayoutGroup>(); }

        if (UITeamCountPanel == null)
        {
            UITeamCountPanel = GameObject.Find("UITeamCountPanel");
            if (UITeamCountPanel != null) { UITeamCountPanelLayoutGroup = UITeamCountPanel.GetComponent<GridLayoutGroup>(); }
        }
        else { UITeamCountPanelLayoutGroup = UITeamCountPanel.GetComponent<GridLayoutGroup>(); }
    }

    private void Update()
    {
        if (UIScrollPanel == null)
        {
            UIScrollPanel = GameObject.Find("UIScrollPanel");
            if (UIScrollPanel != null) { UIScrollPanelGridLayoutGroup = UIScrollPanel.GetComponent<GridLayoutGroup>(); }
        }
        else { UIScrollPanelGridLayoutGroup = UIScrollPanel.GetComponent<GridLayoutGroup>(); }

        if (UITeamCountPanel == null)
        {
            UITeamCountPanel = GameObject.Find("UITeamCountPanel");
            if (UITeamCountPanel != null) { UITeamCountPanelLayoutGroup = UITeamCountPanel.GetComponent<GridLayoutGroup>(); }
        }
        else { UITeamCountPanelLayoutGroup = UITeamCountPanel.GetComponent<GridLayoutGroup>(); }

        if (stored_team_count != gameController.team_count)
        {
            CreateTeamCounters();
            stored_team_count = gameController.team_count;
            //if (Networking.LocalPlayer.isMaster) { gameController.RoundOptionAdjust(); }
        }

    }

    
    public void RedrawPlayerUIGroup()
    {
        // Change localScale of panels, gridlayout cell size, and gridlayout column count based on player count (6 [1], 24 [2], 54 [3])
        if (UIScrollPanelGridLayoutGroup != null)
        {
            byte active_obj_count = 0;
            for (int i = 0; i < player_obj_list.Length; i++)
            {
                if (player_obj_list[i].activeInHierarchy) { active_obj_count++; }
            }
            int column_count = 1; float calc_height = 100.0f;
            while (((calc_height * active_obj_count) / column_count) > 600.0f)
            {
                calc_height = 100.0f / column_count;
                column_count++;
            }
            calc_height = 100.0f / column_count;
            UIScrollPanelGridLayoutGroup.cellSize = new Vector2(650.0f / column_count, calc_height);
            UIScrollPanelGridLayoutGroup.constraintCount = column_count;
            for (int i = 0; i < player_obj_list.Length; i++)
            {
                ((RectTransform)player_obj_list[i].transform.GetChild(0).transform).localScale = new Vector3(1.0f / column_count, 1.0f / column_count, 1.0f / column_count);
            }
        }
    }

    public void RedrawTeamUIGroup()
    {
        // We also need to do the same for team counts
        if (UITeamCountPanelLayoutGroup != null)
        {
            byte active_obj_count = (byte)team_obj_list.Length;
            int row_count = 1; float calc_width = 100.0f;
            while (((calc_width * active_obj_count) / row_count) > 600.0f)
            {
                calc_width = 100.0f / row_count;
                row_count++;
            }
            calc_width = 100.0f / row_count;
            UITeamCountPanelLayoutGroup.cellSize = new Vector2(calc_width, 100.0f / row_count);
            UITeamCountPanelLayoutGroup.constraintCount = row_count;
            for (int i = 0; i < team_obj_list.Length; i++)
            {
                ((RectTransform)team_obj_list[i].transform).localScale = new Vector3(1.0f / row_count, 1.0f / row_count, 1.0f / row_count);
            }
        }
    }

    public void CreateTeamCounters()
    {
        for (int i = 0; i < team_obj_list.Length; i++)
        {
            Destroy(team_obj_list[i]);
        }

        team_obj_list = new GameObject[gameController.team_count];
        var new_team_count = new int[gameController.team_count];
        for (int k = 0; k < new_team_count.Length; k++)
        {
            if (k < team_count_arr.Length)
            {
                new_team_count[k] = team_count_arr[k];
            }
        }
        team_count_arr = new_team_count;

        /*for (int l = 0; l < player_team_list_arr.Length; l++)
        {
            if (player_team_list_arr[l] >= gameController.team_count) { player_team_list_arr[l] = gameController.team_count - 1; }
        }
        player_team_list_str = gameController.ConvertIntArrayToString(player_team_list_arr);*/

        for (int j = 0; j < gameController.team_count; j++)
        {
            team_obj_list[j] = Instantiate(template_TeamCountDisplay);
            if (UITeamCountPanel != null) { team_obj_list[j].transform.SetParent(UITeamCountPanel.transform, false); }
            team_obj_list[j].SetActive(true);
            SetTeamCounter(j);
        }

        /*if (gameController.team_count >= 7) { UITeamCountPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = true; }
        else { UITeamCountPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = false; }
        if (player_team_list_arr.Length >= 6) { UIScrollPanel.GetComponent<VerticalLayoutGroup>().childControlHeight = true; }
        else { UIScrollPanel.GetComponent<VerticalLayoutGroup>().childControlHeight = false; }*/
        RedrawTeamUIGroup();
    }

    public void SetTeamCounter(int team_index)
    {
        var counterobj = team_obj_list[team_index];
        var countertxt = counterobj.GetComponentInChildren<TextMeshProUGUI>();
        //var counterimg = counterobj.GetComponentInChildren<TextMeshProUGUI>();

        var players_in_team = 0;
        if (gameController.ply_tracking_dict_values_arr != null)
        {
            for (int j = 0; j < gameController.ply_tracking_dict_values_arr.Length; j++)
            {
                if (team_index == gameController.ply_tracking_dict_values_arr[j]) { players_in_team++; }
            }
        }

        foreach (Transform t in counterobj.transform)
        {
            if (t.GetComponent<TextMeshProUGUI>() != null)
            {
                countertxt = t.GetComponent<TextMeshProUGUI>();
                break;
            }
        }

        team_count_arr[team_index] = players_in_team;

        countertxt.text = players_in_team.ToString();
        counterobj.GetComponent<UnityEngine.UI.Image>().color = gameController.team_colors[team_index];
        countertxt.color = new Color32(
            (byte)Mathf.Min(255, (80 + gameController.team_colors[team_index].r)),
            (byte)Mathf.Min(255, (80 + gameController.team_colors[team_index].g)),
            (byte)Mathf.Min(255, (80 + gameController.team_colors[team_index].b)),
            (byte)gameController.team_colors[team_index].a);

    }

    public void SetAllTeamCounters()
    {
        for (int j = 0; j < team_obj_list.Length; j++)
        {
            SetTeamCounter(j);
        }

        RedrawTeamUIGroup();
    }

    public void CreateNewPanel(int index)
    {
        var uiobj = Instantiate(template_UIAssignTeamPanel);
        //Debug.Log("Creating instance " + uiobj.GetInstanceID());

        uiobj.SetActive(true);
        var uiarrow = uiobj.GetComponentInChildren<UIArrowTeamPanel>();
        foreach (Transform t in uiobj.transform)
        {
            if (t.GetComponent<UIArrowTeamPanel>() != null)
            {
                uiarrow = t.GetComponent<UIArrowTeamPanel>();
                break;
            }
        }
        if (UIScrollPanel != null) { uiobj.transform.SetParent(UIScrollPanel.transform, false); }
        uiobj.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        //uiarrow.parent_teampanel = this; For some reason, this breaks it
        uiarrow.min_value = 0;
        uiarrow.max_value = gameController.team_count - 1;
        uiarrow.wrap_value = true;
        uiarrow.increment_size = 1;
        uiarrow.is_template = false;
        if (gameController.ply_tracking_dict_keys_arr == null || gameController.ply_tracking_dict_values_arr == null)
        {
            UnityEngine.Debug.LogError("Attempted to assign team panel with player at index " + index + " when dictionary does not exist!");
            Destroy(uiobj);
            return;
        }
        else if (index >= gameController.ply_tracking_dict_keys_arr.Length)
        {
            UnityEngine.Debug.LogError("Attempted to assign team panel with player at index " + index + " when local player array only has " + gameController.ply_tracking_dict_keys_arr.Length + "entries!");
            Destroy(uiobj);
            return;
        }

        uiarrow.player = VRCPlayerApi.GetPlayerById(gameController.ply_tracking_dict_keys_arr[index]);
        uiarrow.current_value = gameController.ply_tracking_dict_values_arr[index];
        UnityEngine.Debug.Log("Creating panel for player " + uiarrow.player.playerId + " at index " + index);
  
        uiarrow.array_id = player_obj_list.Length;
        player_obj_list = gameController.AddToGameObjectArray(uiobj, player_obj_list);

        uiarrow.Refresh();

        return;
    }

    public void CreateNewPanelList()
    {
        for (int i = 0; i < player_obj_list.Length; i++) { RemovePanel(i); }
        for (int j = 0; j < gameController.ply_tracking_dict_keys_arr.Length; j++)
        {
            CreateNewPanel(j);
        }
    }

    public void RemovePanel(int index)
    {
        Destroy(player_obj_list[index]);
        player_obj_list = gameController.RemoveIndexFromGameObjectArray(index, player_obj_list);
    }

    
    public void RefreshAllPanels()
    {
        for (int i = 0; i < player_obj_list.Length; i++)
        {
            if (player_obj_list[i] == null) { return; }
            var plyarrow = player_obj_list[i].GetComponentInChildren<UIArrowTeamPanel>();
            foreach (Transform t in player_obj_list[i].transform)
            {
                if (t.GetComponent<UIArrowTeamPanel>() != null)
                {
                    plyarrow = t.GetComponent<UIArrowTeamPanel>();
                    break;
                }
            }
            if (plyarrow == null) { return; }
            if (i < gameController.ply_tracking_dict_keys_arr.Length && gameController.ply_tracking_dict_values_arr != null) 
            { 
                plyarrow.player = VRCPlayerApi.GetPlayerById(gameController.ply_tracking_dict_keys_arr[i]);
                plyarrow.current_value = gameController.ply_tracking_dict_values_arr[i];
                UnityEngine.Debug.Log("Refresh() - Setting panel at index " + i + " for player " + plyarrow.player.playerId + " to value " + plyarrow.current_value);
            }
            plyarrow.Refresh();
        }
        RedrawPlayerUIGroup();
    }
    
    public void PanelListCleanup()
    { 
        if (player_obj_list.Length != VRCPlayerApi.GetPlayerCount()) { CreateNewPanelList(); }
    }

}
