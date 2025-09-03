
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_spawn : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public int min_players = 0;
    [SerializeField] public int team_id = -1;
    [NonSerialized] public int spawnzone_global_index = -1;

    void Start()
    {
       transform.GetComponent<Renderer>().enabled = false;
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        ToggleSpawn(false);
    }

    void ReportEligibleSpawns(int team_id)
    {

    }

    void ReportSpawnsFarthestFromPlayers(int team_id)
    {

    }

    void ToggleSpawn(bool toggleBool)
    {

    }
}
