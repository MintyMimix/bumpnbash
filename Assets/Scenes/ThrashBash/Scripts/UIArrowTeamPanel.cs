
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UIArrowTeamPanel : UIArrow
{
    [SerializeField] public UIRoundTeamPanel parent_teampanel;
    [NonSerialized] public VRCPlayerApi player;
    [NonSerialized] public int global_index = -1;

    public void Start()
    {
        UpdateOwnership();
    }

    public void Refresh()
    {
        // Sanitize input
        max_value = parent_teampanel.gameController.team_count - 1;
        min_value = 0;
        increment_size = 1;
        if (current_value > max_value)
        {
            current_value = max_value;
        }
        else if (current_value < min_value) 
        {
            current_value = min_value; 
        }
        image_front.color = parent_teampanel.gameController.team_colors[current_value];
        if (player != null) { caption.text = player.displayName; }

        if (parent_teampanel.gameController.team_count <= 1 || !Networking.LocalPlayer.isMaster) 
        {
            button_increment.gameObject.SetActive(false);
            button_decrement.gameObject.SetActive(false);
        }
        else if (parent_teampanel.gameController.team_count > 1 && Networking.LocalPlayer.isMaster)
        {
            button_increment.gameObject.SetActive(true);
            button_decrement.gameObject.SetActive(true);
        }

        UpdateOwnership();
    }

    public void UpdateOwnership()
    {
        if (Networking.LocalPlayer.isMaster)
        {
            button_decrement.enabled = true;
            button_increment.enabled = true;
        }
        else
        {
            button_decrement.enabled = false;
            button_increment.enabled = false;
        }
    }

    public void SignalToUpdateFromPanel()
    {
        parent_teampanel.UpdateValueFromPanel(gameObject);
    }

}
