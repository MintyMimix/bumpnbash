
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum item_spawn_state_name
{
    Disabled, Spawnable, InWorld, ENUM_LENGTH
}

public class ItemSpawner : UdonSharpBehaviour
{
    [Header("References")]
    [SerializeField] public GameController gameController;
    [SerializeField] public ItemPowerup child_powerup;
    [SerializeField] public ItemWeapon child_weapon;
    [SerializeField] public GameObject child_marker;
    [Header("Spawner Options")]
    [SerializeField] public bool show_marker_in_game = true;
    [Tooltip("How long an item should linger in the world after being spawned, in seconds")]
    [SerializeField] [UdonSynced] public float item_spawn_linger;
    [Tooltip("The number of seconds that need to pass before the chance for an item to be spawned is rolled")]
    [SerializeField] [UdonSynced] public float item_spawn_impulse;
    [Tooltip("The odds of an item being spawned every impulse, measured between 0.0001 and 1.0")]
    [SerializeField] [UdonSynced] public float item_spawn_chance_global = 1.0f; 
    [Tooltip("How long a powerup should last when picked up from this spawner, in seconds")]
    [SerializeField] [UdonSynced] public float item_spawn_powerup_duration = 10.0f;
    [Tooltip("Which team # should this be assigned to? (-1: all, -2: FFA-only)")]
    [SerializeField][UdonSynced] public sbyte item_spawn_team = -1;
    [Tooltip("How many players must be in the game before this spawner is active? (Must be a positive value!)")]
    [SerializeField][UdonSynced] public byte item_spawn_min_players = 0;

    [NonSerialized][UdonSynced] public int item_spawn_global_index = -1;
    [NonSerialized][UdonSynced] public int item_spawn_state = (int)item_spawn_state_name.Disabled; // See: item_spawn_state_name

    [NonSerialized] public int[] item_spawn_chances; // This will be ints from 0 to 10000 for the purposes of syncing, representing a decimal of precision 2 (0.0 to 100.0)
    [Header("Spawn Chances")]
    [NonSerialized][UdonSynced] public string item_spawn_chances_str;
    [Tooltip("The name of the item that has a chance of being spawned. MUST match keys in enumerator powerup_type_name or weapon_type_name.")]
    [SerializeField] public string[] item_spawn_chances_config_keys; // Assign in inspector. Must be equal to strings in the powerup_type_name and weapon_type_name enumerators respectively.
    [Tooltip("The chance an item has of being spawned. Will automatically normalize if odds add up above 100% for all items. MUST match length of item_spawn_chances_config_keys.")]
    [SerializeField] public float[] item_spawn_chances_config_values;
    [NonSerialized][UdonSynced] public int item_spawn_index = 0;

    [NonSerialized] [UdonSynced] public double item_timer_start_ms;
    [NonSerialized] [UdonSynced] public double item_timer_duration = 0.0f;
    [NonSerialized] public double item_timer_local = 0.0f;
    [NonSerialized] public double item_timer_network = 0.0f;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        item_spawn_chances = ConvertChancesToInt(NormalizeChances(ParseInspectorSpawnChances()));
        item_spawn_chances_str = gameController.ConvertIntArrayToString(item_spawn_chances);
        RequestSerialization();
        StartTimer(item_spawn_impulse);
    }

    private void Update()
    {
        if (item_spawn_state == (int)item_spawn_state_name.Disabled) {
            if (child_powerup.gameObject.activeInHierarchy) { child_powerup.gameObject.SetActive(false); }
            if (child_weapon.gameObject.activeInHierarchy) { child_weapon.gameObject.SetActive(false); }
            if (child_marker.gameObject.activeInHierarchy) { child_marker.gameObject.SetActive(false); }
            return; 
        }
        else
        {
            if (!child_marker.gameObject.activeInHierarchy && show_marker_in_game) { child_marker.gameObject.SetActive(true); }
        }

        // Events which only run when the timer ticks to zero below
        if (!ProcessTimer()) { return; }

        // Only spawn the item if it's in a spawnable state
        if (item_spawn_state == (int)item_spawn_state_name.Spawnable)
        {
            // Roll for global spawn chance
            if (!RollForSpawn()) { StartTimer(item_spawn_impulse); }
            else {
                // Spawn the item
                if (Networking.IsMaster)
                {
                    item_spawn_index = RollForItem(item_spawn_chances);
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SpawnItem", item_spawn_index);
                }
            }
         }
        else if (item_spawn_state == (int)item_spawn_state_name.InWorld)
        {
            if (Networking.IsMaster)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DespawnItem", (int)item_sfx_index.ItemExpire, -1, true);
            }
        }
    }

    public void StartTimer(double duration)
    {
        item_timer_start_ms = Networking.GetServerTimeInSeconds();
        item_timer_local = 0.0f;
        item_timer_network = 0.0f;
        item_timer_duration = duration;
        if (Networking.IsMaster) { RequestSerialization(); }
    }

    [NetworkCallable]
    public void SpawnItem(int item_index)
    {
        // Powerup
        if (item_index < (int)powerup_type_name.ENUM_LENGTH)
        {
            child_powerup.item_owner_id = -1;
            child_powerup.item_team_id = item_spawn_team;
            child_powerup.SetTeamColor(item_spawn_team);
            child_powerup.item_stored_global_index = item_spawn_global_index;
            child_powerup.item_state = (int)item_state_name.InWorld;
            child_powerup.item_type = (int)item_type_name.Powerup;
            child_powerup.item_is_template = false;
            child_powerup.powerup_type = item_index;
            child_powerup.SetPowerupStats(item_index);
            child_powerup.powerup_duration = item_spawn_powerup_duration; // To-do: make this configurable somewhere
            child_powerup.powerup_start_ms = Networking.GetServerTimeInSeconds();
            child_powerup.powerup_timer_local = 0.0f;
            child_powerup.powerup_timer_network = 0.0f;
            child_powerup.gameObject.SetActive(true);
        }
        // Weapon
        else if (item_index - (int)powerup_type_name.ENUM_LENGTH < (int)weapon_type_name.ENUM_LENGTH)
        {
            child_weapon.item_owner_id = -1;
            child_weapon.item_team_id = item_spawn_team;
            child_weapon.SetTeamColor(item_spawn_team);
            child_weapon.item_stored_global_index = item_spawn_global_index;
            child_weapon.item_type = (int)item_type_name.Weapon;
            child_weapon.item_is_template = false;
            // To-do: weapon configuration; make sure to use (item_index - (int)powerup_type_name.ENUM_LENGTH) for type
        }

        item_spawn_state = (int)item_spawn_state_name.InWorld;
        if (show_marker_in_game) { child_marker.SetActive(true); }
        else { child_marker.SetActive(false); }
        StartTimer(item_spawn_linger);
    }

    [NetworkCallable]
    public void DespawnItem(int reason_code, int owner_id, bool playSFX)
    {
        var playSFXfiltered = playSFX;

        // Powerup
        if (item_spawn_index < (int)powerup_type_name.ENUM_LENGTH)
        {
            child_powerup.item_state = (int)item_state_name.Disabled;
            child_powerup.powerup_type = (int)powerup_type_name.Fallback;
            if (owner_id == Networking.LocalPlayer.playerId && reason_code == (int)item_snd_clips_name.PickupOther) { playSFXfiltered = false; }
            if (playSFXfiltered) {
                child_powerup.item_snd_source.transform.position = transform.position;
                gameController.PlaySFXFromArray(child_powerup.item_snd_source, child_powerup.item_snd_clips, reason_code);
            }
            child_powerup.gameObject.SetActive(false);
        }
        // Weapon
        /*else if ((item_spawn_index - (int)powerup_type_name.ENUM_LENGTH) < (int)weapon_type_name.ENUM_LENGTH)
        {
            child_weapon.item_state = (int)item_state_name.Disabled;
            if (owner_id == Networking.LocalPlayer.playerId && reason_code == (int)item_snd_clips_name.PickupOther) { playSFXfiltered = false; }
            if (playSFXfiltered) {
                child_powerup.item_snd_source.transform.position = transform.position;
                gameController.PlaySFXFromArray(child_weapon.item_snd_source, child_weapon.item_snd_clips, reason_code);
            }
            child_weapon.gameObject.SetActive(false);
            // To-do: weapon configuration; make sure to use (item_index - (int)powerup_type_name.ENUM_LENGTH) for type
        }*/
        
        if (item_spawn_state == (int)item_spawn_state_name.Disabled) { return; }

        item_spawn_state = (int)item_spawn_state_name.Spawnable;
        if (show_marker_in_game) { child_marker.SetActive(true); }
        else { child_marker.SetActive(false); }
        StartTimer(item_spawn_impulse);
    }

    // Process the spawn timer. Return true if an event should fire.
    internal bool ProcessTimer()
    {
        item_timer_local += Time.deltaTime;
        item_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), item_timer_start_ms);
        if (item_timer_duration > 0 && (item_timer_local >= item_timer_duration || item_timer_network >= item_timer_duration))
        {
            return true;
        }
        return false;
    }

    public bool RollForSpawn()
    {
        var roll = UnityEngine.Random.Range(0.0f, 1.0f);
        if (roll <= item_spawn_chance_global) { return true; }
        return false;
    }

    public int RollForItem(int[] item_chances)
    {
        if (item_chances == null || item_chances.Length <= 0) { return 0; }
        var roll = UnityEngine.Random.Range((int)0, (int)(10000+1));
        var index_rolled = 0;

        for (int i = 0; i < item_chances.Length; i++)
        {
            if (i == item_chances.Length - 1) { index_rolled = item_chances.Length; break; } // Regardless of our how high our roll is, if we hit the end of the chance table, just assume it's the best outcome
            else if (
                (i > 0 && roll < item_chances[i] && roll >= item_chances[i - 1]) || (i == 0 && roll < item_chances[i])
                )
            {
                index_rolled = i; break;
            }
        }

        return index_rolled;
    }

    private float[] ParseInspectorSpawnChances()
    {
        var outArr = new float[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];
        for (int d = 0; d < outArr.Length; d++)
        {
            outArr[d] = 0; // We set the default value of all chances to zero.
        }

        var minLen = Math.Min(item_spawn_chances_config_keys.Length, item_spawn_chances_config_values.Length);
        if (item_spawn_chances_config_keys.Length != item_spawn_chances_config_values.Length) { Debug.LogWarning(gameObject.name + ": Key-Value pair for item spawn chances do not match! (Keys: " + item_spawn_chances_config_keys.Length.ToString() + ", Values: " + item_spawn_chances_config_values.Length.ToString() + ")"); }
        for (int i = 0; i < minLen; i++)
        {
            if (i < (int)powerup_type_name.ENUM_LENGTH)
            {
                var powerup_type_name_enum_index = gameController.KeyToPowerupType(item_spawn_chances_config_keys[i]);
                outArr[powerup_type_name_enum_index] = item_spawn_chances_config_values[i];
            }
            else if (i - (int)powerup_type_name.ENUM_LENGTH < (int)weapon_type_name.ENUM_LENGTH)
            {
                var weapon_type_name_enum_index = gameController.KeyToWeaponType(item_spawn_chances_config_keys[i]);
                outArr[weapon_type_name_enum_index] = item_spawn_chances_config_values[i];
            }
        }
        //UnityEngine.Debug.Log(gameObject.name + ": Parsed inspector array: " + gameController.DebugPrintFloatArray(outArr));
        return outArr;
    }

    public float[] NormalizeChances(float[] inArr)
    {
        if (inArr.Length <= 0) { Debug.LogError(gameObject.name + ": Attempted to normalize chances for an empty array!"); return inArr; } // if passing a null array, don't bother
        var chance_total = 0.0f;
        for (int i = 0; i < inArr.Length; i++)
        {
            chance_total += inArr[i];
        }
        if (chance_total <= 0.0f) { Debug.LogError(gameObject.name + ": Attempted to normalize chances when the combined value is 0!"); return inArr; } // if passing a null array, don't bother

        var normalized_chances = new float[inArr.Length];
        for (int j = 0; j < inArr.Length; j++)
        {
            if (j == 0) { normalized_chances[j] = (inArr[j] / chance_total); }
            else { normalized_chances[j] = (inArr[j] / chance_total) + normalized_chances[j - 1]; }
        }

        //UnityEngine.Debug.Log(gameObject.name + ": Normalized array array: " + gameController.DebugPrintFloatArray(normalized_chances) + " with total chances of " + chance_total);
        return normalized_chances;
    }

    public int[] ConvertChancesToInt(float[] inArr)
    {
        var outArr = new int[inArr.Length];
        for (int i = 0; i < inArr.Length; i++)
        {
            outArr[i] = (int)Mathf.RoundToInt(inArr[i] * 10000.0f);
        }
        //UnityEngine.Debug.Log(gameObject.name + ": Converted chance array to int " + gameController.DebugPrintIntArray(outArr));
        return outArr;
    }


}
