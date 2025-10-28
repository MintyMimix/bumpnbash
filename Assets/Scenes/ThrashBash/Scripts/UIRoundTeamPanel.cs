
using Newtonsoft.Json.Linq;
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class UIRoundTeamPanel : GlobalTickReceiver
{

    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject UIScrollPanel;
    [SerializeField] public GridLayoutGroup UIScrollPanelGridLayoutGroup;
    [SerializeField] public GameObject template_UIAssignTeamPanel;
    [SerializeField] public GameObject UITeamCountPanel;
    [SerializeField] public GameObject UIHostChangePanel;
    [SerializeField] public GridLayoutGroup UITeamCountPanelLayoutGroup;
    [SerializeField] public GameObject template_TeamCountDisplay;

    [NonSerialized] public GameObject[] player_obj_list;
    [NonSerialized] public GameObject[] team_obj_list;
    [NonSerialized] public int[] team_count_arr;

    public int stored_team_count = 0;
    public int new_host_proposed_id = 0;

    public override void Start()
    {
        base.Start();
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
        UIHostChangePanel.SetActive(false);

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

        GeneratePanels();
    }

    public override void OnSlowTick(float tickDeltaTime)
    {
        if (UIScrollPanel == null)
        {
            UIScrollPanel = GameObject.Find("UIScrollPanel");
            if (UIScrollPanel != null) { UIScrollPanelGridLayoutGroup = UIScrollPanel.GetComponent<GridLayoutGroup>(); }
        }
        else if (UIScrollPanelGridLayoutGroup == null) { UIScrollPanelGridLayoutGroup = UIScrollPanel.GetComponent<GridLayoutGroup>(); }

        if (UITeamCountPanel == null)
        {
            UITeamCountPanel = GameObject.Find("UITeamCountPanel");
            if (UITeamCountPanel != null) { UITeamCountPanelLayoutGroup = UITeamCountPanel.GetComponent<GridLayoutGroup>(); }
        }
        else if (UITeamCountPanelLayoutGroup == null) { UITeamCountPanelLayoutGroup = UITeamCountPanel.GetComponent<GridLayoutGroup>(); }

        if (stored_team_count != gameController.team_count)
        {
            CreateTeamCounters();
            stored_team_count = gameController.team_count;
        }
    }


    public void RedrawPlayerUIGroup()
    {
        if (UIScrollPanelGridLayoutGroup != null)
        {
            // Rescale the panels based on how many there are
            var active_panels = GetComponentsInChildren<UIArrowTeamPanel>().GetLength(0);
            var base_columns = 1;
            Vector2 base_dims = ((RectTransform)template_UIAssignTeamPanel.transform).sizeDelta;
            Vector3 base_scale = ((RectTransform)template_UIAssignTeamPanel.transform).localScale;
            Vector2 grid_dims = ((RectTransform)UIScrollPanelGridLayoutGroup.transform).sizeDelta;
            float[] grid_result = GlobalHelperFunctions.CalcGridDistr(active_panels, base_columns, base_dims, base_scale, grid_dims);
            UIScrollPanelGridLayoutGroup.constraintCount = (int)grid_result[0];
            UIScrollPanelGridLayoutGroup.cellSize = base_dims * grid_result[1];
            //UIScrollPanelGridLayoutGroup.spacing = new Vector2(grid_result[4], grid_result[5]);
            for (int i = 0; i < player_obj_list.Length; i++)
            {
                ((RectTransform)player_obj_list[i].transform.GetChild(0).transform).localScale = new Vector3(grid_result[1], grid_result[2], grid_result[3]);
            }
        }
    }

    public void RedrawTeamUIGroup()
    {
        // We also need to do the same for team counts
        if (UITeamCountPanelLayoutGroup != null)
        {
            // Rescale the panels based on how many there are
            var active_panels = (byte)team_obj_list.Length;
            var base_rows = 1;
            Vector2 base_dims = ((RectTransform)template_TeamCountDisplay.transform).sizeDelta;
            Vector3 base_scale = ((RectTransform)template_TeamCountDisplay.transform).localScale;
            Vector2 grid_dims = ((RectTransform)UITeamCountPanelLayoutGroup.transform).sizeDelta;
            float[] grid_result = GlobalHelperFunctions.CalcGridDistr(active_panels, base_rows, base_dims, base_scale, grid_dims, true);
            UITeamCountPanelLayoutGroup.constraintCount = (int)grid_result[0];
            UITeamCountPanelLayoutGroup.cellSize = base_dims * grid_result[1];
            //UIScrollPanelGridLayoutGroup.spacing = new Vector2(grid_result[4], grid_result[5]);
            for (int i = 0; i < team_obj_list.Length; i++)
            {
                //((RectTransform)team_obj_list[i].transform).localScale = new Vector3(grid_result[1], grid_result[2], grid_result[3]);
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
        if (team_index >= team_obj_list.Length) { return; }
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
        if (gameController.option_teamplay)
        {
            counterobj.GetComponent<UnityEngine.UI.Image>().color = gameController.team_colors[team_index];
            countertxt.color = new Color32(
                (byte)Mathf.Min(255, (80 + gameController.team_colors[team_index].r)),
                (byte)Mathf.Min(255, (80 + gameController.team_colors[team_index].g)),
                (byte)Mathf.Min(255, (80 + gameController.team_colors[team_index].b)),
                (byte)gameController.team_colors[team_index].a);
        }
        else
        {
            counterobj.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            countertxt.color = Color.white;
        }

    }

    public void SetAllTeamCounters()
    {
        for (int j = 0; j < team_obj_list.Length; j++)
        {
            SetTeamCounter(j);
        }

        RedrawTeamUIGroup();
    }


    /*public void PanelListCleanup()
    { 
        if (player_obj_list.Length != VRCPlayerApi.GetPlayerCount()) { CreateNewPanelList(); }
    }
    */

    public void GeneratePanels()
    {
        // 80 is the absolute hardcap of players in VRChat
        for (int i = 0; i < (int)GLOBAL_CONST.UDON_MAX_PLAYERS; i++)
        {
            var uiobj = Instantiate(template_UIAssignTeamPanel);
            //Debug.Log("Creating instance " + uiobj.GetInstanceID());

            uiobj.SetActive(true);
            UIArrowTeamPanel uiarrow = GetUIArrow(uiobj);
            if (UIScrollPanel != null) { uiobj.transform.SetParent(UIScrollPanel.transform, false); }
            uiobj.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            uiarrow.min_value = 0;
            uiarrow.max_value = gameController.team_count - 1;
            uiarrow.wrap_value = true;
            uiarrow.increment_size = 1;
            uiarrow.is_template = false;
            uiarrow.player = null;
            uiarrow.current_value = -3;
            uiarrow.array_id = i;
            player_obj_list = GlobalHelperFunctions.AddToGameObjectArray(uiobj, player_obj_list);

            uiarrow.Refresh();
        }
    }

    public UIArrowTeamPanel GetUIArrow(GameObject uiobj)
    {
        var uiarrow = uiobj.GetComponentInChildren<UIArrowTeamPanel>();
        if (uiarrow == null)
        {
            foreach (Transform t in uiobj.transform)
            {
                if (t.GetComponent<UIArrowTeamPanel>() != null)
                {
                    uiarrow = t.GetComponent<UIArrowTeamPanel>();
                    break;
                }
            }
        }
        return uiarrow;
    }

    public void RefreshAllPanels()
    {
        if (gameController == null || gameController.ply_tracking_dict_keys_arr == null || gameController.ply_tracking_dict_values_arr == null) { return; }

        for (int i = 0; i < player_obj_list.Length; i++)
        {

            UIArrowTeamPanel uiarrow = GetUIArrow(player_obj_list[i]);

            if (i >= gameController.ply_tracking_dict_values_arr.Length) { uiarrow.player = null; player_obj_list[i].SetActive(false); continue; }

            var player = VRCPlayerApi.GetPlayerById(gameController.ply_tracking_dict_keys_arr[i]);

            if (player == null || gameController.ply_tracking_dict_values_arr[i] < 0)
            {
                if (player == null) { uiarrow.player = null; }
                player_obj_list[i].SetActive(false);
                continue;
            }
            else if (player != uiarrow.player)
            {
                uiarrow.player = player;
            }

            uiarrow.Refresh();
            player_obj_list[i].SetActive(true);
        }

        RedrawPlayerUIGroup();
    }

    public void HostChangeRequest(int host_requested)
    {
        // Pop up a window to confirm if they want to change hosts to a chosen host
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(host_requested);
        if (player == null) { return; }

        new_host_proposed_id = host_requested;
        string display_text = gameController.localizer.FetchText("GAMESETTINGS_HOST_CHANGE_CONFIRM", "The Game Master will be changed to:\n$NAME\n\nAre you sure?");
        display_text = display_text.Replace("$NAME", player.displayName);
        GlobalHelperFunctions.GetChildTransformByName(UIHostChangePanel.transform, "UIHostText").GetComponent<TMP_Text>().text = display_text;
        UIHostChangePanel.SetActive(true);
    }

    public void HostChangeAccept()
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(new_host_proposed_id);
        if (player == null) { HostChangeCancel(); }

        gameController.ChangeHost(new_host_proposed_id);
        UIHostChangePanel.SetActive(false);
    }

    public void HostChangeCancel()
    {
        UIHostChangePanel.SetActive(false);
    }
}
