
using UdonSharp;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;
using VRC.Udon;

public class UIArrowSpectator : UIArrow
{
    public GameController gameController;
    public Camera camera_main;
    public Transform[] camera_points;
    private int[][] players_to_spectate;
    public float refresh_impulse = 0.4f;
    private float refresh_timer;

    private void Start()
    {
        wrap_value = true;
        current_value = 0;
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        players_to_spectate = new int[2][];
    }

    private void Update()
    {
        CameraAdjust();

        if (refresh_timer < refresh_impulse)
        {
            refresh_timer += Time.deltaTime;
        }
        else
        {
            ProcessValue();
            refresh_timer = 0.0f;
        }
    }

    private Transform[] RefreshCameraPointsFromController()
    {
        Transform[] out_points;
        if (gameController.mapscript_list == null || gameController.map_selected < 0 || gameController.map_selected > gameController.mapscript_list.Length) {
            out_points = new Transform[1];
            out_points[0] = gameController.room_ready_spawn.transform;
            return out_points;
        }

        out_points = gameController.mapscript_list[gameController.map_selected].map_campoints;

        if (out_points == null || out_points.Length == 0)
        {
            out_points = new Transform[1];
            out_points[0] = gameController.room_ready_spawn.transform;
            return out_points;
        }

        return out_points;
    }


    private Transform[] ValidateCameraPoints(Transform[] in_points)
    {
        Transform[] out_points;
        if (in_points == null || in_points.Length == 0)
        {
            out_points = new Transform[1];
            out_points[0] = gameController.room_ready_spawn.transform;
            return out_points;
        }

        ushort out_count = 0; float max_bound = 10000.0f;
        Transform[] temp_points = new Transform[in_points.Length];
        for (int i = 0; i < in_points.Length; i++)
        {
            if (in_points[i] == null || (Mathf.Abs(in_points[i].position.x) > max_bound || Mathf.Abs(in_points[i].position.y) > max_bound || Mathf.Abs(in_points[i].position.z) > max_bound)) { continue; }
            temp_points[out_count] = in_points[i];
            out_count++;
        }
        out_points = new Transform[out_count];
        for (int j = 0; j < out_count; j++)
        {
            out_points[j] = temp_points[j];
        }

        if (out_points == null || out_points.Length == 0)
        {
            out_points = new Transform[1];
            out_points[0] = gameController.room_ready_spawn.transform;
            return out_points;
        }

        return out_points;
    }

    private int[][] GetPlayers()
    {
        if (gameController == null) { return null; }

        int[][] players_out = gameController.GetPlayersInGame();
        if (camera_points != null && current_value >= camera_points.Length && (players_out == null || players_out.Length == 0 || players_out[0].Length == 0)) { current_value = 0; }
        return players_out;
    }

    public override void OnValueChanged(int old_value, int new_value)
    {
        ProcessValue();
    }

    public void ProcessValue()
    {
        camera_points = ValidateCameraPoints(RefreshCameraPointsFromController());
        if (camera_points == null) { return; }
        players_to_spectate = GetPlayers();
        if (players_to_spectate != null) { max_value = camera_points.Length + players_to_spectate[0].Length - 1; }
        else { max_value = camera_points.Length - 1; }

        if (current_value > max_value) { current_value = min_value; }
        if (current_value >= camera_points.Length && (players_to_spectate == null || players_to_spectate.Length < 1 && players_to_spectate[0].Length < 1)) { current_value = min_value; }

        CameraAdjust();
    }

    public void CameraAdjust()
    {
        if (current_value >= camera_points.Length && (current_value - camera_points.Length) >= 0 && camera_points.Length > 0)
        {
            // Camera is on a player
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(players_to_spectate[0][current_value - camera_points.Length]);
            if (player != null)
            {
                caption.text = player.displayName;
                if (players_to_spectate[1][current_value - camera_points.Length] >= 0) { caption.color = gameController.team_colors_bright[players_to_spectate[1][current_value - camera_points.Length]]; }
                else { caption.color = Color.white; }

                camera_main.transform.SetPositionAndRotation(player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (player.GetRotation() * -Vector3.forward * 3.0f * (player.GetAvatarEyeHeightAsMeters() / 1.6f)) + (player.GetRotation() * Vector3.up * 0.5f * (player.GetAvatarEyeHeightAsMeters() / 1.6f))
                    , player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation);
            }
            else
            {
                IncrementValue();
            }
        }
        else if (camera_points.Length > 0)
        {
            // Camera is on one of the map cameras
            caption.text = "Map Camera " + (current_value + 1).ToString();
            caption.color = Color.white;
            camera_main.transform.SetPositionAndRotation(camera_points[current_value].position, camera_points[current_value].rotation);
        }
    }

}
