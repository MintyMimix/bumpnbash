
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_gamemode_specific : UdonSharpBehaviour
{
    public bool survival_enabled = true;
    public bool clash_enabled = true;
    public bool bossbash_enabled = true;
    public bool infection_enabled = true;
    public bool fittingin_enabled = true;
    public bool koth_enabled = true;

    [NonSerialized] public bool[] gamemode_enabled;

    private void Start()
    {
        if (gamemode_enabled == null || gamemode_enabled.Length < (int)gamemode_name.ENUM_LENGTH)
        {
            gamemode_enabled = new bool[(int)gamemode_name.ENUM_LENGTH];
            gamemode_enabled[(int)gamemode_name.Survival] = survival_enabled;
            gamemode_enabled[(int)gamemode_name.Clash] = clash_enabled;
            gamemode_enabled[(int)gamemode_name.BossBash] = bossbash_enabled;
            gamemode_enabled[(int)gamemode_name.Infection] = infection_enabled;
            gamemode_enabled[(int)gamemode_name.FittingIn] = fittingin_enabled;
            gamemode_enabled[(int)gamemode_name.KingOfTheHill] = koth_enabled;

        }
    }

}
