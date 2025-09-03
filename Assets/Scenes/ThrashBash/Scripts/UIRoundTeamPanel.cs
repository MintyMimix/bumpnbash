
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
    [SerializeField] public GameObject template_UIAssignTeamPanel;
    [SerializeField] public GameObject template_TeamCountDisplay;
    [SerializeField] public GameObject UITeamCountPanel;

    [NonSerialized] public GameObject[] player_obj_list;
    [NonSerialized] public int[] player_team_list_arr;
    [NonSerialized] [UdonSynced] public string player_team_list_str;
    [NonSerialized] public GameObject[] team_obj_list;
    [NonSerialized] public int[] team_count_arr; 

    private int stored_team_count = 0;

    private void Start()
    {
        //var template_panel = (RectTransform)template_UIAssignTeamPanel.transform;
        //template_panel.parent = null;
        player_obj_list = new GameObject[0];
        player_team_list_arr = new int[0];
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
        }
    }

    private void Update()
    {
        if (stored_team_count != gameController.team_count) {
            CreateTeamCounters();
            stored_team_count = gameController.team_count;
            if (Networking.LocalPlayer.isMaster) { gameController.RoundOptionAdjust(); RequestSerialization(); }
        }
    }

    public override void OnDeserialization()
    {
        // We do not need to care if the actual player names match, because the master handles all of those calls.
        // All we care about is our list of names (which gameController handles) and list of flags (which is synced by player_team_list_str).

        // First, check that the team array sizes match. If so, we just update the values. If not, we need to make a new list.
        int[] new_team_arr = gameController.ConvertStrToIntArray(player_team_list_str);
        if (new_team_arr.Length == player_team_list_arr.Length) {
            player_team_list_arr = new_team_arr;
            UpdateFlags();
        }
        else 
        { 
            GenerateList(); 
        }

     }

    public void UpdateFlags()
    {
        for (int i = 0; i < player_obj_list.Length; i++)
        {
            UIArrowTeamPanel uiarrow = player_obj_list[i].GetComponentInChildren<UIArrowTeamPanel>();
            foreach (Transform t in player_obj_list[i].transform)
            {
                if (t.GetComponent<UIArrowTeamPanel>() != null)
                {
                    uiarrow = t.GetComponent<UIArrowTeamPanel>();
                    break;
                }
            }
            ResetGlobalIndex();
            uiarrow.current_value = player_team_list_arr[i];
            uiarrow.Refresh();
        }
        SetAllTeamCounters();
        Canvas.ForceUpdateCanvases();
    }


    private void DestroyList()
    {
        for (int i = 0; i < player_obj_list.Length; i++)
        {
            Destroy(player_obj_list[i]);
        }
        player_obj_list = new GameObject[0];
        player_team_list_arr = new int[0];
    }

    public void GenerateList()
    {
        //if (template_UIAssignTeamPanel.GetComponentInChildren<UIArrowTeamPanel>() == null) { UnityEngine.Debug.LogError(gameObject.name + "(" + gameObject.GetInstanceID() + "): Could not find team plane or arrow template for creating a list of players!"); return; }
        UIArrowTeamPanel uiarrow = template_UIAssignTeamPanel.GetComponentInChildren<UIArrowTeamPanel>();
        foreach (Transform t in template_UIAssignTeamPanel.transform)
        {
            if (t.GetComponent<UIArrowTeamPanel>() != null)
            {
                uiarrow = t.GetComponent<UIArrowTeamPanel>();
                break;
            }
        }

        // If we already have an object list, prune the old one
        var player_count = (byte)gameController.ply_ready_arr.Length;
        if (player_count <= 0) { return; }

        if (player_obj_list != null)
        {
            DestroyList();
        }

        player_obj_list = new GameObject[gameController.ply_ready_arr.Length];
        player_team_list_arr = gameController.ConvertStrToIntArray(player_team_list_str);
        for (int i = 0; i < player_count; i++)
        {
            player_obj_list[i] = CreateNewPanel(i);
        }

        /*if (player_team_list_arr.Length >= 6) { UITeamCountPanel.GetComponent<VerticalLayoutGroup>().childControlHeight = true; }
        else { UITeamCountPanel.GetComponent<VerticalLayoutGroup>().childControlHeight = false; }*/
    }

    private GameObject CreateNewPanel(int index)
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
        //uiobj.transform.SetParent(ScrollViewContent.transform, false);
        uiobj.transform.SetParent(UIScrollPanel.transform, false);
        uiobj.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f); 
        //uiarrow.parent_teampanel = this; For some reason, this breaks it
        uiarrow.global_index = index;
        uiarrow.min_value = 0;
        uiarrow.max_value = gameController.team_count - 1;
        uiarrow.increment_size = 1;
        if (index >= player_team_list_arr.Length || index == 0) 
        { 
            uiarrow.current_value = 0; 
            player_team_list_arr = gameController.AddToIntArray(0, player_team_list_arr);
        }
        else { uiarrow.current_value = player_team_list_arr[index]; }
        uiarrow.player = VRCPlayerApi.GetPlayerById(gameController.ply_ready_arr[index]);
        
        uiarrow.Refresh();

        return uiobj;
    }

    [NetworkCallable]
    public void ManipulateTeamArray(int player_id, bool op_add)
    {
        if (op_add) {
            player_obj_list = gameController.AddToGameObjectArray(CreateNewPanel(gameController.ply_ready_arr.Length - 1), player_obj_list);
            ResetGlobalIndex();
        }
        else
        {
            if (player_team_list_arr.Length > 0)
            {
                var remove_index = 0;
                for (var i = 0; i < player_team_list_arr.Length; i++)
                {
                    if (player_id == player_team_list_arr[i]) { remove_index = i; break; }
                }
                Destroy(player_obj_list[remove_index]);
                player_obj_list = gameController.RemoveIndexFromGameObjectArray(remove_index, player_obj_list);
                ResetGlobalIndex();
                player_team_list_arr = gameController.RemoveIndexFromIntArray(remove_index, player_team_list_arr);
            }
        }

        if (Networking.LocalPlayer.isMaster)
        {
            player_team_list_str = gameController.ConvertIntArrayToString(player_team_list_arr);
            UpdateFlags();
            gameController.RoundOptionAdjust();
            RequestSerialization();
        }

    }

    public void ResetGlobalIndex()
    {
        for (int i = 0; i < player_obj_list.Length; i++)
        {
            if (player_obj_list[i] == null) { continue; }
            UIArrowTeamPanel uiarrow = player_obj_list[i].GetComponentInChildren<UIArrowTeamPanel>();
            uiarrow.global_index = i;
        }
    }


    public void UpdateValueFromPanel(GameObject uiobj) { 
        if (!Networking.LocalPlayer.isMaster) { return; }

        var uiarrow = uiobj.GetComponentInChildren<UIArrowTeamPanel>();
        var panel_index = uiarrow.global_index;

        if (panel_index < 0) {
            UnityEngine.Debug.LogError("Attempted to update an unregistered panel!"); 
            GenerateList(); 
            return; 
        }

        //global_index
        //var uiarrow = uiobj.GetComponentInChildren<UIArrowTeamPanel>();
        foreach (Transform t in uiobj.transform)
        {
            if (t.GetComponent<UIArrowTeamPanel>() != null)
            {
                uiarrow = t.GetComponent<UIArrowTeamPanel>();
                break;
            }
        }
        player_team_list_arr[panel_index] = uiarrow.GetComponent<UIArrowTeamPanel>().current_value;
        player_team_list_str = gameController.ConvertIntArrayToString(player_team_list_arr);
        UpdateFlags();
        gameController.RoundOptionAdjust();
        RequestSerialization();

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

        for (int l = 0; l < player_team_list_arr.Length; l++)
        {
            if (player_team_list_arr[l] >= gameController.team_count) { player_team_list_arr[l] = gameController.team_count - 1; }
        }
        player_team_list_str = gameController.ConvertIntArrayToString(player_team_list_arr);

        for (int j = 0; j < gameController.team_count; j++)
        {
            team_obj_list[j] = Instantiate(template_TeamCountDisplay);
            team_obj_list[j].transform.SetParent(UITeamCountPanel.transform, false);
            team_obj_list[j].SetActive(true);
            SetTeamCounter(j);
        }

        /*if (gameController.team_count >= 7) { UITeamCountPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = true; }
        else { UITeamCountPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = false; }
        if (player_team_list_arr.Length >= 6) { UIScrollPanel.GetComponent<VerticalLayoutGroup>().childControlHeight = true; }
        else { UIScrollPanel.GetComponent<VerticalLayoutGroup>().childControlHeight = false; }*/

    }

    public void SetTeamCounter(int team_index)
    {
        var counterobj = team_obj_list[team_index];
        var countertxt = counterobj.GetComponentInChildren<TextMeshProUGUI>();
        //var counterimg = counterobj.GetComponentInChildren<TextMeshProUGUI>();

        var players_in_team = 0;
        for (int j = 0; j < player_team_list_arr.Length; j++)
        {
            if (team_index == player_team_list_arr[j]) { players_in_team++; }
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


    }

    public void SetAllTeamCounters() {
        for (int j = 0; j < team_obj_list.Length; j++)
        {
            SetTeamCounter(j);
        }
    }


}
