
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_gravitywell : GlobalTickReceiver
{
    [SerializeField] public GameController gameController;
    [SerializeField] public ParticleSystem particle_system;
    [SerializeField] public Renderer obj_renderer;
    [SerializeField] public float cooldown_duration = 1.2f;
    [NonSerialized] public float cooldown_timer = 0.0f;
    [NonSerialized] public PlayerAttributes localAttr;
    [NonSerialized] public bool particle_active = false;
    [NonSerialized] private bool initializing = false;

    public override void Start()
    {
        base.Start();
        initializing = true;
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        initializing = false;
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        if (initializing) { return; }
        if (localAttr == null) { localAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer); }

        // Not sure if this is necessary to have every frame, but we'll keep it for now unless there's a lot of lag reported from it
        if (gameController != null && gameController.local_ppp_options != null)
        {
            var particle_emission = particle_system.emission;
            particle_emission.enabled = gameController.local_ppp_options.particles_on;
            obj_renderer.enabled = !particle_emission.enabled;
        }

        if (localAttr == null) { localAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer); }
        if (localAttr == null) { return; }
        if (!localAttr.in_grav_well && cooldown_timer < cooldown_duration)
        {
            cooldown_timer += Time.deltaTime;
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        if (localAttr == null) { localAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer); }
        if (localAttr == null) { return; }
        if (localAttr.in_grav_well) { return; }
        if (cooldown_timer < cooldown_duration) { return; }

        UnityEngine.Debug.Log("[GRAVWELL_TEST]: Entered " + gameObject.name);
        Vector3 vel = Networking.LocalPlayer.GetVelocity();
        vel.y = 0.0f;
        Networking.LocalPlayer.SetVelocity(vel);
        cooldown_timer = 0.0f;
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        if (localAttr == null) { localAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer); }
        if (localAttr == null) { return; }
        if (cooldown_timer < cooldown_duration) { return; }

        localAttr.in_grav_well = true;
        cooldown_timer = 0.0f;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        if (localAttr == null) { localAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer); }
        if (localAttr == null) { return; }

        UnityEngine.Debug.Log("[GRAVWELL_TEST]: Exited " + gameObject.name);
        localAttr.in_grav_well = false;
    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        OnPlayerTriggerEnter(player);
    }

    public override void OnPlayerCollisionStay(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        OnPlayerTriggerStay(player);
    }

    public override void OnPlayerCollisionExit(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        OnPlayerTriggerExit(player);
    }
}
