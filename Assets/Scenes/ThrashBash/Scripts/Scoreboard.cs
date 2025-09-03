
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Scoreboard : UdonSharpBehaviour
{
    public GameController gameController;
    public float refresh_timer = 0.0f;
    public float refresh_impulse = 0.5f;
    public GameObject template_scoreboard_panel;
    public GameObject[] scoreboard_obj_list;
    public GridLayoutGroup scoreboard_grid;

    // Scaling: fixed column count, where spacing = dims * (4 - column-count) and scale = (4 - column_count)


    void Start()
    {
        //DebugText.text = "This is placeholder debug text!";
        scoreboard_obj_list = new GameObject[80];

        for (int i = 0; i < 80; i++)
        {
            scoreboard_obj_list[i] = Instantiate(template_scoreboard_panel);
            scoreboard_obj_list[i].GetComponent<UIScoreboardPanelTemplate>().gameController = gameController;
            scoreboard_obj_list[i].transform.SetParent(scoreboard_grid.transform, false);
            scoreboard_obj_list[i].SetActive(false);
        }

    }

    private void Update()
    {
        /*if (refresh_timer < refresh_impulse)
        {
            refresh_timer += Time.deltaTime;
        }
        else
        {
            RefreshScores();
            refresh_timer = 0.0f;
            if (gameController.local_uiplytoself != null) { refresh_impulse = gameController.local_uiplytoself.ui_check_gamevars_impulse; }
        }*/
    }

    public void RefreshScores()
    {
        if (gameController == null || gameController.ply_tracking_dict_keys_arr == null || gameController.ply_tracking_dict_values_arr == null) { return; }

        for (int i = 0; i < scoreboard_obj_list.Length; i++)
        {
            var score_panel = scoreboard_obj_list[i].GetComponent<UIScoreboardPanelTemplate>();
            if (i >= gameController.ply_tracking_dict_values_arr.Length) { score_panel.player = null; score_panel.plyAttr = null; scoreboard_obj_list[i].SetActive(false); continue; }

            var player = VRCPlayerApi.GetPlayerById(gameController.ply_tracking_dict_keys_arr[i]);

            if (player == null || gameController.ply_tracking_dict_values_arr[i] < 0) 
            {
                if (player == null) { score_panel.player = null; score_panel.plyAttr = null; }
                scoreboard_obj_list[i].SetActive(false);
                continue; 
            }
            else if (player != score_panel.player)
            {
                score_panel.player = player;
                score_panel.plyAttr = gameController.FindPlayerAttributes(player);
            }
            // We don't make this an else if because the plyAttr could have been set in the prior condition
            if (score_panel.plyAttr == null) { continue; }

            score_panel.RefreshAttributes();
            scoreboard_obj_list[i].SetActive(true);
        }

        // Sort the panels
        var active_panels = GetComponentsInChildren<UIScoreboardPanelTemplate>().GetLength(0);
        var base_columns = 4;
        Vector2 base_dims = ((RectTransform)template_scoreboard_panel.transform).sizeDelta;
        Vector3 base_scale = ((RectTransform)template_scoreboard_panel.transform).localScale;
        Vector2 grid_dims = ((RectTransform)scoreboard_grid.transform).sizeDelta;
        float[] grid_result = gameController.CalcGridDistr(active_panels, base_columns, base_dims, base_scale, grid_dims);
        scoreboard_grid.constraintCount = (int)grid_result[0];
        scoreboard_grid.spacing = new Vector2(grid_result[4], grid_result[5]);
        for (int i = 0; i < scoreboard_obj_list.Length; i++)
        {
            ((RectTransform)scoreboard_obj_list[i].transform).localScale = new Vector3(grid_result[1], grid_result[2], grid_result[3]);
        }
    }

    public void RearrangeScoreboard(int[] leaderboard_arr)
    {
        if (leaderboard_arr == null || leaderboard_arr.Length == 0) { return; }
        //UnityEngine.Debug.Log("Scoreboard rearranged based on: " + gameController.ConvertIntArrayToString(leaderboard_arr));
        // First, unparent all of the filled templates
        for (int j = 0; j < scoreboard_obj_list.Length; j++)
        {
            if (scoreboard_obj_list[j] == null) { continue; }
            scoreboard_obj_list[j].transform.SetParent(null, false);
        }
        // Then, reparent in order of the leaderboard
        for (int i = 0; i < leaderboard_arr.Length; i++)
        {
            for (int j = 0; j < scoreboard_obj_list.Length; j++)
            {
                if (scoreboard_obj_list[j] == null) { continue; }
                UIScoreboardPanelTemplate score_panel = scoreboard_obj_list[j].GetComponent<UIScoreboardPanelTemplate>();
                if (score_panel == null || score_panel.plyAttr == null) { scoreboard_obj_list[j].transform.SetParent(scoreboard_grid.transform, false); continue; }

                if ((gameController.option_teamplay && score_panel.plyAttr.ply_team == leaderboard_arr[i])
                    || (!gameController.option_teamplay && score_panel.player.playerId == leaderboard_arr[i])) 
                { scoreboard_obj_list[j].transform.SetParent(scoreboard_grid.transform, false); }
            }
        }
    }
}
