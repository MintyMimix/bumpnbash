
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_kothtainer : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public int team_id = 0;
    [SerializeField] public Collider start_zone; 
    [SerializeField] public TMP_Text[] RespawnTexts;

    public void RefreshTimers(float timer)
    {
        foreach (TMP_Text respawnText in RespawnTexts)
        {
            respawnText.text = Mathf.RoundToInt(timer).ToString();
        }
    }
}
