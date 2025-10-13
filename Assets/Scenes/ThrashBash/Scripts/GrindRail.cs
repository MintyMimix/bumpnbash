
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class GrindRail : UdonSharpBehaviour
{
    [SerializeField] GameController gameController;
    [SerializeField] public double rail_speed = 0.4f; // How long, in seconds, should the rail grind take?
    [NonSerialized] public double rail_ride_duration = 0.0f; // How long between the individual points on the curve it should take
    [NonSerialized] public double rail_ride_timer = 0.0f;
    [SerializeField] public float rail_cooldown_duration = 20.0f;
    [NonSerialized] public float rail_cooldown_timer = 0.0f;
    [NonSerialized] public bool rail_ready = true;
    [SerializeField] public Renderer obj_renderer;
    [SerializeField] public Collider entrance_collider; 
    [SerializeField] public Collider exit_collider;
    [SerializeField] public Transform[] curve_points;
    [NonSerialized] public byte on_curve = 0; // 0 = none, 1 = from entrance, 2 = from exit
    [NonSerialized] public int curve_iter = 0;
    [NonSerialized] public float cached_gravity = 0.0f;
    [NonSerialized] public VRCPlayerApi player;
    [NonSerialized] public bool initializing = true;

    private void Start()
    {
        initializing = true;
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        player = Networking.LocalPlayer;

        // First, try to find some curve points
        if (curve_points == null || curve_points.Length < 2)
        {
            var item_index = 0;
            var item_size = 0;
            foreach (Transform child in transform)
            {
                if (child.name.ToUpper().Contains("RAILPOINT") || child.name.ToUpper().Contains("CURVEPOINT")) { item_size++; }
            }
            curve_points = new Transform[item_size];
            foreach (Transform child in transform)
            {
                if (child.name.ToUpper().Contains("RAILPOINT") || child.name.ToUpper().Contains("CURVEPOINT")) 
                {
                    curve_points[item_index] = child;
                    item_index++; 
                }
            }
        }
        // If we can't find any, end operation
        if (curve_points == null || curve_points.Length < 2) { Destroy(gameObject); }
        rail_ride_duration = rail_speed / curve_points.Length;
        initializing = false;
    }

    public void Update()
    {
        if (initializing) { return; }
        if (!rail_ready && on_curve == 0 && rail_cooldown_timer < rail_cooldown_duration)
        {
            rail_cooldown_timer += Time.deltaTime;
        }
        else if (!rail_ready && on_curve == 0 && rail_cooldown_timer >= rail_cooldown_duration)
        {
            ToggleReady(true);
        }
        else if (on_curve > 0)
        {
            if (rail_ride_timer < rail_ride_duration)
            {
                rail_ride_timer += Time.deltaTime;
            }
            else
            {
                IterCurve();
                rail_ride_timer = 0.0f;
            }
        }
    }

    void ToggleReady(bool toggle)
    {
        if (toggle) 
        {
            rail_cooldown_timer = rail_cooldown_duration;
            ToggleRenderer(true);
        }
        else 
        {
            rail_cooldown_timer = 0.0f; 
        }
        rail_ready = toggle;
        entrance_collider.enabled = toggle;
        exit_collider.enabled = toggle;
    }

    void ToggleRenderer(bool toggle)
    {
        if (obj_renderer == null) { return; }
        obj_renderer.enabled = toggle;
    }

    void StartCurve(bool from_entrance = true)
    {
        if (player == null) { player = Networking.LocalPlayer; }

        

        rail_ride_timer = 0.0f;
        if (from_entrance) 
        { 
            on_curve = 1;
            curve_iter = 0;
        }

        else 
        { 
            on_curve = 2;
            curve_iter = curve_points.Length - 1;
        }

        ToggleReady(false);
        player.SetVelocity(Vector3.zero);
        if (gameController.local_plyAttr != null) 
        {
            cached_gravity = gameController.local_plyAttr.ply_grav > 0 ? gameController.local_plyAttr.ply_grav : 1.0f ;
            gameController.local_plyAttr.ply_grav = 0.0f; 
        }
        
        gameController.platformHook.custom_force_unhook = true;
        Quaternion rotateTo = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        if (from_entrance)
        {
            Networking.LocalPlayer.TeleportTo(curve_points[0].position, Networking.LocalPlayer.GetRotation());
        }
        else
        {
            Networking.LocalPlayer.TeleportTo(curve_points[curve_points.Length - 1].position, Networking.LocalPlayer.GetRotation());
        }
        UnityEngine.Debug.Log("[GRIND_TEST]: Initiating grind on " + gameObject.name);
    }

    void IterCurve()
    {
        if (player == null) { player = Networking.LocalPlayer; }

        Vector3 vel = player.GetVelocity();
        if (on_curve == 1)
        {
            if (curve_iter + 1 >= curve_points.Length)
            {
                Networking.LocalPlayer.TeleportTo(curve_points[curve_iter].position, Networking.LocalPlayer.GetRotation());
                vel = (curve_points[curve_iter - 1].position - curve_points[curve_iter].position);
                vel *= (float)(1.0 / rail_ride_duration);
                //vel *= 0.125f;
                player.SetVelocity(vel);
                on_curve = 0;
                ToggleRenderer(false);
                rail_ride_timer = 0.0f;
                UnityEngine.Debug.Log("[GRIND_TEST]: Ending grind on " + gameObject.name);
                gameController.platformHook.custom_force_unhook = false;
                if (gameController.local_plyAttr != null)
                {
                    gameController.local_plyAttr.ply_grav = cached_gravity;
                    player.SetGravityStrength(cached_gravity);
                }
            }
            else
            {
                player.SetVelocity(vel);
                vel = (curve_points[curve_iter + 1].position - curve_points[curve_iter].position);
                vel *= (float)(1.0 / rail_ride_duration);
                //vel *= 0.5f;
                player.SetVelocity(vel);
                curve_iter++;
            }
        }
        else if (on_curve == 2)
        {

            if (curve_iter - 1 < 0)
            {
                Networking.LocalPlayer.TeleportTo(curve_points[curve_iter].position, Networking.LocalPlayer.GetRotation());
                vel = (curve_points[0].position - curve_points[1].position);
                vel *= (float)(1.0 / rail_ride_duration);
                //vel *= 0.125f;
                player.SetVelocity(vel);
                on_curve = 0;
                ToggleRenderer(false);
                rail_ride_timer = 0.0f;
                UnityEngine.Debug.Log("[GRIND_TEST]: Ending grind on " + gameObject.name);
                gameController.platformHook.custom_force_unhook = false;
                if (gameController.local_plyAttr != null)
                {
                    gameController.local_plyAttr.ply_grav = cached_gravity;
                    player.SetGravityStrength(cached_gravity);
                }
            }
            else
            {
                player.SetVelocity(vel);
                vel = (curve_points[curve_iter - 1].position - curve_points[curve_iter].position);
                vel *= (float)(1.0 / rail_ride_duration);
                //vel *= 0.5f;
                curve_iter--;
            }
        }
    }

    public bool CheckPlayerPoint(VRCPlayerApi player)
    {
        if (player == null) { return false; }
        Vector3 plyPos = player.GetPosition();
        float entranceDistance = Mathf.Abs(Vector3.Distance(plyPos, entrance_collider.ClosestPoint(plyPos)));
        float exitDistance = Mathf.Abs(Vector3.Distance(plyPos, exit_collider.ClosestPoint(plyPos)));
        return exitDistance > entranceDistance;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!rail_ready || player == null || player != Networking.LocalPlayer) { return; }

        StartCurve(CheckPlayerPoint(player));

    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
    {
        if (!rail_ready || player == null || player != Networking.LocalPlayer) { return; }

        StartCurve(CheckPlayerPoint(player));
    }

}
