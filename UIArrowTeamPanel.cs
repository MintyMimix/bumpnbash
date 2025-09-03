
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UIArrowTeamPanel : UIArrow
{
    [SerializeField] public UIRoundTeamPanel parent_teampanel;
    [SerializeField] public UnityEngine.UI.Button button_make_host;

    [NonSerialized] public VRCPlayerApi player;
    [NonSerialized] public bool is_template = true;
    [NonSerialized] public int array_id = -1;
    [NonSerialized] public float local_width_init = 380.0f;
    [NonSerialized] public float local_xoffset_init = 120.0f;
    [NonSerialized] private float HOST_MARGIN_SIZE = 15.0f;

    public void Start()
    {
        wrap_value = true;
        //local_width_init = caption_transform.sizeDelta.x;
        //local_xoffset_init = caption_transform.localPosition.x;
        UpdateOwnership();
    }

    private void OnEnable()
    {
        Start();
    }

    private void LateUpdate()
    {
        if (button_increment != null && button_decrement != null)
        {
            bool toggle_should_be_on = Networking.IsOwner(parent_teampanel.gameController.gameObject);
            if (parent_teampanel != null && parent_teampanel.gameController.round_state != (int)round_state_name.Start) { toggle_should_be_on = false; }
            else if (parent_teampanel != null && parent_teampanel.gameController.option_personal_teams && player == Networking.LocalPlayer) { toggle_should_be_on = true; }

            if (toggle_should_be_on && button_increment.interactable == false) { button_increment.interactable = true; }
            else if (!toggle_should_be_on && button_increment.interactable == true) { button_increment.interactable = false; }
            if (toggle_should_be_on && button_decrement.interactable == false) { button_decrement.interactable = true; }
            else if (!toggle_should_be_on && button_decrement.interactable == true) { button_decrement.interactable = false; }
        }
    }

    public void Refresh()
    {
        // Check if we can latch onto a player
        if (player != null)
        {
            caption.text = player.displayName;
            if (array_id >= 0 && parent_teampanel.gameController.ply_tracking_dict_keys_arr != null && array_id < parent_teampanel.gameController.ply_tracking_dict_keys_arr.Length)
            {
                current_value = parent_teampanel.gameController.ply_tracking_dict_values_arr[array_id];
                //caption.text += " [" + current_value + "]";
            }
            // Check if the player disconnected
            else if (VRCPlayerApi.GetPlayerById(player.playerId) != null)
            {
                player = VRCPlayerApi.GetPlayerById(parent_teampanel.gameController.ply_tracking_dict_keys_arr[array_id]);
                Refresh(); // Usually don't like recursion, but this should only ever fire off once
            }
        }
        else if (array_id >= 0 && parent_teampanel.gameController.ply_tracking_dict_keys_arr != null && array_id < parent_teampanel.gameController.ply_tracking_dict_keys_arr.Length)
        {
            player = VRCPlayerApi.GetPlayerById(parent_teampanel.gameController.ply_tracking_dict_keys_arr[array_id]);
        }

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

        if (parent_teampanel.gameController.option_teamplay)
        {
            image_front.color = parent_teampanel.gameController.team_colors[current_value];
            if (parent_teampanel.gameController.team_colors_bright != null
                && current_value < parent_teampanel.gameController.team_colors_bright.Length) 
            { caption.color = parent_teampanel.gameController.team_colors_bright[current_value]; }
        }
        else
        {
            image_front.color = Color.white;
            caption.color = Color.white;
        }

        image_cb.sprite = parent_teampanel.gameController.team_sprites[current_value];
        image_cb.color = image_front.color;
        if (parent_teampanel.gameController.local_ppp_options != null && parent_teampanel.gameController.local_ppp_options.colorblind) { image_cb.enabled = true; }
        else { image_cb.enabled = false; }
        image_front.enabled = !image_cb.enabled;
        image_back.enabled = !image_cb.enabled;

        //else if (!is_template) { Destroy(gameObject); }

        UpdateOwnership();
        }

    public void UpdateOwnership()
    {
        bool toggle_should_be_on = Networking.IsOwner(parent_teampanel.gameController.gameObject);
        if (parent_teampanel != null && parent_teampanel.gameController.round_state != (int)round_state_name.Start) { toggle_should_be_on = false; }
        else if (parent_teampanel != null && parent_teampanel.gameController.option_personal_teams && player == Networking.LocalPlayer) { toggle_should_be_on = true; }

        if (parent_teampanel.gameController.team_count <= 1 || !toggle_should_be_on)
        {

            button_increment.gameObject.SetActive(false);
            button_decrement.gameObject.SetActive(false);
        }
        else if (parent_teampanel.gameController.team_count > 1 && toggle_should_be_on)
        {
            button_increment.gameObject.SetActive(true);
            button_decrement.gameObject.SetActive(true);
        }

        float text_xoffset = local_xoffset_init;
        float text_width = local_width_init;
        button_make_host.gameObject.SetActive(false);
        if (parent_teampanel != null && parent_teampanel.gameController != null && Networking.IsOwner(parent_teampanel.gameController.gameObject) && player != Networking.LocalPlayer && player != null)
        {
            button_make_host.gameObject.SetActive(true);
            
            text_xoffset -= (((RectTransform)button_make_host.gameObject.transform).sizeDelta.x) * 0.5f;
            text_width -= (((RectTransform)button_make_host.gameObject.transform).sizeDelta.x);
        }
        caption_transform.localPosition = new Vector3(text_xoffset, caption_transform.localPosition.y, caption_transform.localPosition.z);
        caption_transform.sizeDelta = new Vector2(text_width, caption_transform.sizeDelta.y);

    }

    public void SignalToUpdateFromPanel()
    {
        if (player != null)
        {
            parent_teampanel.gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", player.playerId, current_value, false);
        }
    }

    public void SignalToUpdateHost()
    {
        if (player != null)
        {
            parent_teampanel.HostChangeRequest(player.playerId);
        }
    }
}
