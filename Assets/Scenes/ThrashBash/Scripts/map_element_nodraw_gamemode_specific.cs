
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_nodraw_gamemode_specific : GlobalTickReceiver
{
    [SerializeField] public GameController gameController;
    [SerializeField] public int[] gamemodes_to_nodraw;
    [NonSerialized] public int local_stored_gamemode;

    public override void Start()
    {
        base.Start();
    }

    public override void OnSlowTick(float tickDeltaTime)
    {
        if (gameController == null) { return; }
        if (gameController.option_gamemode != local_stored_gamemode)
        {
            bool should_render = true;
            for (int i = 0; i < gamemodes_to_nodraw.Length; i++)
            {
                if (gamemodes_to_nodraw[i] == gameController.option_gamemode) { should_render = false; break; }
            }
            transform.GetComponent<Renderer>().enabled = should_render;
            local_stored_gamemode = gameController.option_gamemode;
        }
    }
}
