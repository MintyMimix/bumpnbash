
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UIArrowTeamPanel : UIArrow
{
    [SerializeField] public UIRoundTeamPanel parent_teampanel;
    [NonSerialized] public VRCPlayerApi player;
    [NonSerialized] public bool is_template = true;
    [NonSerialized] public int array_id = -1;

    public void Start()
    {
        wrap_value = true;
        UpdateOwnership();
    }

    public void Refresh()
    {
        // Sanitize input
        transform.parent.gameObject.gameObject.SetActive(true);
        max_value = parent_teampanel.gameController.team_count - 1;
        min_value = 0;
        increment_size = 1;
        if (current_value > max_value)
        {
            current_value = max_value;
        }
        else if (current_value < min_value)
        {
            transform.parent.gameObject.SetActive(false);
            return;
        }

        image_front.color = parent_teampanel.gameController.team_colors[current_value];
        caption.color = new Color32(
            (byte)Mathf.Min(255,(80 + parent_teampanel.gameController.team_colors[current_value].r)), 
            (byte)Mathf.Min(255,(80 + parent_teampanel.gameController.team_colors[current_value].g)), 
            (byte)Mathf.Min(255,(80 + parent_teampanel.gameController.team_colors[current_value].b)), 
            (byte)parent_teampanel.gameController.team_colors[current_value].a);
        if (player != null) 
        { 
            caption.text = player.displayName + " (" + array_id + ") ";
            image_cb.sprite = parent_teampanel.gameController.team_sprites[current_value];
            image_cb.color = image_front.color;
            if (parent_teampanel.gameController.local_ppp_options != null && parent_teampanel.gameController.local_ppp_options.colorblind) { image_cb.enabled = true; }
            else { image_cb.enabled = false; }
            image_front.enabled = !image_cb.enabled;
            image_back.enabled = !image_cb.enabled;
        }
        else if (!is_template) { Destroy(gameObject); }
        
        UpdateOwnership();
    }

    public void UpdateOwnership()
    {
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
    }

    public void SignalToUpdateFromPanel()
    {
        parent_teampanel.gameController.ChangeTeam(player.playerId, current_value, false);
    }

}
