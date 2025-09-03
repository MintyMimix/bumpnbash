
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Scoreboard : UdonSharpBehaviour
{
    public GameController gameController;
    public TMP_Text DebugText;
    public float refresh_timer = 0.0f;
    public float refresh_impulse = 0.5f;

    void Start()
    {
        DebugText.text = "This is placeholder debug text!";
    }

    private void Update()
    {
        if (refresh_timer < refresh_impulse)
        {
            refresh_timer += Time.deltaTime;
        }
        else
        {
            RefreshScores();
            refresh_timer = 0.0f;
        }
    }

    private void RefreshScores()
    {
        if (gameController == null || gameController.ply_tracking_dict_keys_arr == null || gameController.ply_tracking_dict_values_arr == null) { return; }

        var debugtxtout = "-- PLAYERS --";
        for (int i = 0; i < gameController.ply_tracking_dict_keys_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(gameController.ply_tracking_dict_keys_arr[i]);
            if (player == null || gameController.ply_tracking_dict_values_arr[i] < 0) { continue; }
            debugtxtout += "\n[" + gameController.ply_tracking_dict_values_arr[i] + "] " + player.displayName;
            var plyAttr = gameController.FindPlayerAttributes(player);
            if (plyAttr == null) { continue; }
            debugtxtout += ": " + plyAttr.ply_points + " K / " + plyAttr.ply_deaths + " D";
            if (!gameController.option_goal_points_a || (!gameController.option_goal_points_b && plyAttr.ply_team == 1)) { debugtxtout += " (" + plyAttr.ply_lives + " Lives)"; }
        }
        DebugText.text = debugtxtout;
    }
}
