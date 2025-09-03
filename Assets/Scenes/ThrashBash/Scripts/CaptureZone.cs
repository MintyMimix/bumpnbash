
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CaptureZone : UdonSharpBehaviour
{

    [Tooltip("How long should this point be locked before it can be captured?")]
    [SerializeField] public float initial_lock_duration = 30.0f;
    [Tooltip("How often should this capture point perform a Physics check for player hitboxes?")]
    [SerializeField] public float check_players_impulse = 0.5f;
    [Tooltip("How long should the contestor be allowed to remain outside of the capture zone before their progress fades? (MUST be > check_players_impulse)")]
    [SerializeField] [UdonSynced] public float contest_pause_duration = 4.0f;
    [SerializeField] public GameController gameController;
    [SerializeField] public Sprite child_sprite;
    [NonSerialized] [UdonSynced] public float check_players_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float contest_pause_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float initial_lock_timer = 0.0f;
    [NonSerialized] [UdonSynced] public float hold_timer = 0.0f;
    [NonSerialized] [UdonSynced] public bool is_locked = true;
    [NonSerialized] [UdonSynced] public int hold_id = -1;
    [NonSerialized] [UdonSynced] public int contest_id = -1;
    [NonSerialized] [UdonSynced] public float contest_progress = 0.0f;
    [NonSerialized] [UdonSynced] public bool overtime_enabled = false; // This will only be true if there is both a holder and a contestor.
    [NonSerialized] public int[] players_on_point;
    [NonSerialized] public int global_index;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

    }

    private void OnEnable()
    {
        hold_id = -1;
        hold_timer = 0.0f;
        contest_id = -1;
        contest_progress = 0.0f;
        check_players_timer = 0.0f;
        initial_lock_timer = 0.0f;
        contest_pause_timer = 0.0f;
        is_locked = true;
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
    }

    private void Update()
    {
        if (check_players_timer >= check_players_impulse) 
        {
            players_on_point = CheckPlayersOnPoint();
            CheckPointContest();
            check_players_timer = 0.0f;
        }
        else
        {
            check_players_timer += Time.deltaTime;
        }

        if (is_locked && initial_lock_timer >= initial_lock_duration)
        {
            is_locked = false;
        }
        else if (is_locked && gameController.round_state == (int)round_state_name.Ongoing)
        {
            initial_lock_timer += Time.deltaTime;
        }

        // If the contestor exists but is not present on the point, drain contestor's progress.
        if (contest_id >= 0 && contest_progress >= 0.0f)
        {
            if (contest_pause_timer >= contest_pause_duration) { contest_progress -= Time.deltaTime; }
            else { contest_pause_timer += Time.deltaTime; }
        }

        // If the contest_progress exceeds duration, then contestor becomes the new holder; reset contestor and contest_progress.
        if (contest_progress >= gameController.option_gm_config_a)
        {
            hold_id = contest_id;
            contest_id = -1;
            contest_progress = 0.0f;
            // When assigning a new holder, make sure to set the hold_timer = ply_points / koth divisor
            if (gameController.option_teamplay)
            {
                // Get points from 1st player in team to set as timer
                int[] ply_id_in_team = gameController.DictFindAllWithValue(hold_id, gameController.ply_tracking_dict_keys_arr, gameController.ply_tracking_dict_values_arr, (int)dict_compare_name.Equals)[0];
                PlayerAttributes[] plyAttrList = new PlayerAttributes[ply_id_in_team.Length];
                if (GetAttrFromID(hold_id, ref plyAttrList[0])) { hold_timer = (float)plyAttrList[0].ply_points / gameController.koth_decimal_division; }
            }
            else
            {
                // In FFA, just set from the player directly
                PlayerAttributes plyAttr = null;
                if (GetAttrFromID(hold_id, ref plyAttr)) { hold_timer = (float)plyAttr.ply_points / gameController.koth_decimal_division; }
            }
        }
        else if (contest_progress <= 0.0f && contest_id >= 0)
        {
            // If the contestor exists and contesting contest_progress drops below 0.0f, set to 0.0f and nullify the contestor.
            contest_id = -1; contest_progress = 0.0f; contest_pause_timer = 0.0f;
        }

    }

    public void CheckPointContest()
    {
        bool holder_on_point = false; bool contestor_on_point = false; int others_unique_count = 0; int other_id = 0;
        for (int i = 0; i < players_on_point.Length; i++)
        {
            // If all three conditions are already true, we do not need to search through the players on point any further; we have the information we need.
            if (holder_on_point && contestor_on_point && others_unique_count > 1) { break; }
            // Ignore invalid player entries
            if (players_on_point[i] < 0 || VRCPlayerApi.GetPlayerById(players_on_point[i]) == null) { continue; }
            int compare_id = players_on_point[i];
            // If we are in teamplay, use the team ID instead of the player ID
            if (gameController.option_teamplay) { compare_id = gameController.GetGlobalTeam(players_on_point[i]); }

            if (compare_id == hold_id) { holder_on_point = true; }
            else if (compare_id == contest_id) { contestor_on_point = true; }
            else if (compare_id == other_id) { others_unique_count++; }
            else { others_unique_count++; other_id = compare_id; }
        }

        // If the holder is on the point, enable overtime if there are others trying to contest it. Otherwise, disable overtime.
        // Overtime prevents a win, but allows progress.
        if (holder_on_point) { overtime_enabled = (contestor_on_point || others_unique_count > 0); }
        // If no one other than the contestors are on point, give them progress.
        else if (contestor_on_point && others_unique_count == 0) { contest_progress += check_players_impulse; contest_pause_timer = 0.0f; }

        // If there is no contestor AND there is only one team/player on the point AND they are not the holder, assign the contestor to them.
        if (others_unique_count == 1 && (contest_id < 0 || (contest_id >= 0 && !contestor_on_point && contest_progress < 0.0f))) { contest_id = other_id; contest_progress = 0.0f; contest_pause_timer = 0.0f; }
    }

    public int[] CheckPlayersOnPoint()
    {
        LayerMask layers_to_hit = LayerMask.GetMask("PlayerHitbox", "LocalPlayerHitbox");
        Bounds BoxBounds = transform.GetComponent<Collider>().bounds;
        Vector3 capsuleHeight = new Vector3(0.0f, (BoxBounds.max.y - BoxBounds.min.y) / 2.0f, 0.0f);
        float capsuleRadius = ((BoxBounds.max.z - BoxBounds.min.z) + (BoxBounds.max.x - BoxBounds.min.x)) / 4.0f;
        Collider[] hitColliders = Physics.OverlapCapsule(transform.position - capsuleHeight, transform.position + capsuleHeight, capsuleRadius, layers_to_hit, QueryTriggerInteraction.Collide);

        int[] players_temp = new int[hitColliders.Length]; ushort players_count = 0;
        foreach (Collider collider in hitColliders)
        {
            if (CheckCollider(collider))
            {
                players_temp[players_count] = Networking.GetOwner(collider.gameObject).playerId;
                players_count++;
            }
        }
        int[] players = new int[players_count];
        for (int i = 0; i < players_count; i++)
        {
            players[i] = players_temp[i];
        }

        return players;
    }

    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy) { return false; }
        // Did we hit a hitbox?
        if (other.gameObject.GetComponent<PlayerHitbox>() != null && Networking.GetOwner(other.gameObject) != null)
        {
            return true;
        }
        return false;
    }

    // To-do: Set this as a networked message instead 
    public void UpdateHolderPoints()
    {
        // Should only be called occasionally so as not to spam net messages (maybe every second?)
        ushort points_granted = (ushort)Mathf.FloorToInt(hold_timer * gameController.koth_decimal_division);
        if (gameController.option_teamplay) 
        {
            PlayerAttributes[] plyAttrList = GetAttrFromTeam(hold_id);
            foreach (PlayerAttributes plyAttr in plyAttrList)
            {
                plyAttr.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SetPoints", points_granted);
            }
        }
        else if (hold_id >= 0)
        {
            PlayerAttributes plyAttr = null;
            if (GetAttrFromID(hold_id, ref plyAttr)) { plyAttr.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SetPoints", points_granted); }
        }
    }

    public bool GetAttrFromID(int ply_id, ref PlayerAttributes plyAttr)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(ply_id);
        if (player == null) { return false; }
        PlayerAttributes tempAttr = gameController.FindPlayerAttributes(player);
        if (tempAttr == null) { return false; }
        plyAttr = tempAttr;
        return true;
    }

    public PlayerAttributes[] GetAttrFromTeam(int team_id)
    {
        if (team_id < gameController.ply_tracking_dict_values_arr.Length && gameController.ply_tracking_dict_keys_arr.Length == gameController.ply_tracking_dict_values_arr.Length)
        {
            int[] ply_id_in_team = gameController.DictFindAllWithValue(team_id, gameController.ply_tracking_dict_keys_arr, gameController.ply_tracking_dict_values_arr, (int)dict_compare_name.Equals)[0];
            PlayerAttributes[] plyAttrList = new PlayerAttributes[ply_id_in_team.Length];
            for (int i = 0; i < ply_id_in_team.Length; i++)
            {
                GetAttrFromID(ply_id_in_team[i], ref plyAttrList[i]);
            }
            return plyAttrList;
        }
        else { return null; }
    }
}
