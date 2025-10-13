
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public enum item_spawn_state_name
{
    Disabled, Spawnable, InWorld, ENUM_LENGTH
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ItemSpawner : UdonSharpBehaviour
{
    [Header("References")]
    [SerializeField] public GameController gameController;
    [SerializeField] public ItemPowerup child_powerup;
    [SerializeField] public ItemWeapon child_weapon;
    [SerializeField] public GameObject child_marker;
    [SerializeField] public TMP_Text training_tutorial_name_txt; // ONLY used if training_spawner = true
    [SerializeField] public TMP_Text training_tutorial_desc_text; // ONLY used if training_spawner = true
    [Header("Spawner Options")]
    [Tooltip("Should the marker object be visible?")]
    [SerializeField] public bool show_marker_in_game = true;
    [Tooltip("Will this spawn be used for the training room? (Always active)")]
    [SerializeField] public bool training_spawner = false;
    [Tooltip("Is this spawner a local-only template?")]
    [SerializeField] public bool is_template = false;
    [Tooltip("How long an item should linger in the world after being spawned, in seconds")]
    [SerializeField] [UdonSynced] public float item_spawn_linger;
    [Tooltip("The number of seconds that need to pass before the chance for an item to be spawned is rolled")]
    [SerializeField] [UdonSynced] public float item_spawn_impulse;
    [Tooltip("The odds of an item being spawned every impulse, measured between 0.0001 and 1.0")]
    [SerializeField] [UdonSynced] public float item_spawn_chance_global = 1.0f; 
    [Tooltip("How long a powerup should last when picked up from this spawner, in seconds")]
    [SerializeField] [UdonSynced] public float item_spawn_powerup_duration = 10.0f;
    [Tooltip("Which team # should this be assigned to? (-1: all, -2: FFA-only)")]
    [SerializeField] [UdonSynced] public sbyte item_spawn_team = -1;
    [Tooltip("How many players must be in the game before this spawner is active? (Must be a positive value!)")]
    [SerializeField] [UdonSynced] public byte item_spawn_min_players = 0;


    [NonSerialized][UdonSynced] public float item_spawn_frequency_mul = 1.0f; // Setup in the in-game advanced options menu; affects both impulse time and global spawn chance
    [NonSerialized][UdonSynced] public float item_spawn_duration_mul = 1.0f; // Setup in the in-game advanced options menu; affects both powerups and weapons

    //[NonSerialized] [UdonSynced] public int item_spawn_global_index = -1;
    [NonSerialized] [UdonSynced] public int item_spawn_state = (int)item_spawn_state_name.Disabled; // See: item_spawn_state_name
    [NonSerialized] [UdonSynced] public bool item_spawn_powerups_enabled = true; // Should powerups be allowed to spawn?
    [NonSerialized] [UdonSynced] public bool item_spawn_weapons_enabled = true; // Should weapons be allowed to spawn?
    [NonSerialized] [UdonSynced] public float item_spawn_chance_total = 0.0f;
    
    [NonSerialized] public int[] item_spawn_chances; // This will be ints from 0 to 10000 for the purposes of syncing, representing a decimal of precision 2 (0.0 to 100.0)
    [Header("Spawn Chances")]
    [NonSerialized] [UdonSynced] public string item_spawn_chances_str;
    [Tooltip("The name of the item that has a chance of being spawned. MUST match keys in enumerator powerup_type_name or weapon_type_name.")]
    [SerializeField] public string[] item_spawn_chances_config_keys; // Assign in inspector. Must be equal to strings in the powerup_type_name and weapon_type_name enumerators respectively.
    [Tooltip("The chance an item has of being spawned. Will automatically normalize if odds add up above 100% for all items. MUST match length of item_spawn_chances_config_keys.")]
    [SerializeField] public float[] item_spawn_chances_config_values;
    [NonSerialized] [UdonSynced] public int item_spawn_index = 0;

    [NonSerialized] [UdonSynced] public double item_timer_start_ms;
    [NonSerialized] [UdonSynced] public double item_timer_duration = 0.0f;
    [NonSerialized] public double item_timer_network = 0.0f;

    [NonSerialized] public bool wait_for_join_sync = false;

    [NonSerialized] public bool training_tutorial_ui_ready = false;

    [NonSerialized] public int cached_language_type = -1;

    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        if (child_powerup.gameObject != null && child_powerup.gameObject.activeInHierarchy) { child_powerup.gameObject.SetActive(false); }
        if (child_weapon.gameObject != null && child_weapon.gameObject.activeInHierarchy) { child_weapon.gameObject.SetActive(false); }
        if (child_marker.gameObject != null && child_marker.gameObject.activeInHierarchy) { child_marker.gameObject.SetActive(false); }

        SetSpawnChances();
        StartTimer(item_spawn_impulse * (1.0f/item_spawn_frequency_mul));

    }

    private void OnEnable()
    {
        SyncSpawns();
    }

    public void UpdateTrainingTutorialText()
    {
        if (training_spawner && !training_tutorial_ui_ready && training_tutorial_name_txt != null && training_tutorial_desc_text != null && gameController != null && gameController.local_plyAttr != null)
        {
            int spawn_index = RollForItem(item_spawn_chances);
            string full_desc = gameController.local_plyAttr.GetTutorialMessage(spawn_index);
            string[] split_desc = full_desc.Split(": ");
            if (split_desc.Length >= 2)
            {
                training_tutorial_name_txt.text = split_desc[0].Replace(": ", "");
                training_tutorial_desc_text.text = split_desc[1].Replace(": ", "");
                training_tutorial_ui_ready = !training_tutorial_name_txt.text.Contains("$NAME");
            }
        }
    }

    public void SetSpawnChances(byte gamemode)
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        float[] parsed_spawn_chances = ParseInspectorSpawnChances();
        for (int i = 0; i < parsed_spawn_chances.Length; i++)
        {
            // If powerups are disabled, set chances to zero
            if (!item_spawn_powerups_enabled && i < (int)powerup_type_name.ENUM_LENGTH) { parsed_spawn_chances[i] = 0.0f; }
            // If weapons are disabled, set chances to zero
            if (!item_spawn_weapons_enabled && i >= (int)powerup_type_name.ENUM_LENGTH && i - (int)powerup_type_name.ENUM_LENGTH < (int)powerup_type_name.ENUM_LENGTH) { parsed_spawn_chances[i] = 0.0f; }
            // If we're in Fitting In or Infection, disable size-changing powerups and half the chances of atk/def stat ups
            if (gamemode == (int)gamemode_name.FittingIn || gamemode == (int)gamemode_name.Infection)
            {
                if (i == (int)powerup_type_name.SizeUp || i == (int)powerup_type_name.SizeDown) { parsed_spawn_chances[i] = 0.0f; }
                else if (parsed_spawn_chances[i] > 0 && (i == (int)powerup_type_name.AtkUp || i == (int)powerup_type_name.DefUp)) { parsed_spawn_chances[i] *= 0.5f; }
            }
            // If we're in Boss Bash, half the chances of size-changing powerups and atk/def stat ups and don't allow boss glove spawns
            if (gamemode == (int)gamemode_name.FittingIn || gamemode == (int)gamemode_name.BossBash)
            {
                if (parsed_spawn_chances[i] > 0 && (i == (int)powerup_type_name.SizeUp || i == (int)powerup_type_name.SizeDown || i == (int)powerup_type_name.AtkUp || i == (int)powerup_type_name.DefUp)) { parsed_spawn_chances[i] *= 0.5f; }
                else if (parsed_spawn_chances[i] > 0 && i == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.BossGlove) { parsed_spawn_chances[i] *= 0.0f; }
            }
            // If we are NOT in Infection or Boss Bash, disable debuffs (unless it's a training spawner)
            if (gamemode != (int)gamemode_name.Infection && gamemode != (int)gamemode_name.BossBash && !training_spawner)
            {
                if (parsed_spawn_chances[i] > 0 && (i == (int)powerup_type_name.AtkDown || i == (int)powerup_type_name.DefDown || i == (int)powerup_type_name.HighGrav)) { parsed_spawn_chances[i] *= 0.0f; }
            }
        } 
        item_spawn_chances = ConvertChancesToInt(NormalizeChances(parsed_spawn_chances));
        item_spawn_chances_str = GlobalHelperFunctions.ConvertIntArrayToString(item_spawn_chances);
        RequestSerialization(); 
    }

    public void SetSpawnChances()
    {
        SetSpawnChances(gameController.option_gamemode);
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject)) { training_tutorial_ui_ready = false; UpdateTrainingTutorialText(); RequestSerialization(); }
        else if (player == Networking.LocalPlayer) { wait_for_join_sync = true; }
    }
    
    public override void OnDeserialization()
    {
        if (wait_for_join_sync)
        {
            SyncSpawns();
            UpdateTrainingTutorialText();
            wait_for_join_sync = false;
        }
        if (gameController != null) { item_spawn_chances = GlobalHelperFunctions.ConvertStrToIntArray(item_spawn_chances_str); }
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        if (wait_for_join_sync && result.success) { wait_for_join_sync = false; UpdateTrainingTutorialText(); }
    }

    public void SyncSpawns()
    {
        if (item_spawn_state == (int)item_spawn_state_name.InWorld)
        {
            DespawnItem((int)item_sfx_index.ItemExpire, -1, false);
            SpawnItem(item_spawn_index, false);
        }
        else 
        { 
            DespawnItem((int)item_sfx_index.ItemExpire, -1, false); 
        }
    }

    private void Update()
    {
        if (item_spawn_state == (int)item_spawn_state_name.Disabled) {
            if (child_powerup.gameObject != null && child_powerup.gameObject.activeInHierarchy) { child_powerup.gameObject.SetActive(false); }
            if (child_weapon.gameObject != null && child_weapon.gameObject.activeInHierarchy) { child_weapon.gameObject.SetActive(false); }
            if (child_marker.gameObject != null && child_marker.gameObject.activeInHierarchy) { child_marker.gameObject.SetActive(false); }
            return; 
        }
        else
        {
            if (!child_marker.gameObject.activeInHierarchy && show_marker_in_game) { child_marker.gameObject.SetActive(true); }
        }

        // Events which only run when the timer ticks to zero below
        if (!ProcessTimer()) { return; }

        // -- Master Only Below --
        if (!Networking.IsOwner(gameObject)) { return; }
        // Only spawn the item if it's in a spawnable state
        if (item_spawn_state == (int)item_spawn_state_name.Spawnable)
        {
            // Roll for global spawn chance
            if (!RollForSpawn()) { StartTimer(item_spawn_impulse * (1.0f / item_spawn_frequency_mul)); }
            else {
                // Spawn the item
                item_spawn_index = RollForItem(item_spawn_chances);
                if (is_template) { SpawnItem(item_spawn_index, true); }
                else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SpawnItem", item_spawn_index, false); }
            }
        }
        else if (item_spawn_state == (int)item_spawn_state_name.InWorld)
        {
            if (is_template) { DespawnItem((int)item_sfx_index.ItemExpire, -1, false); }
            else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DespawnItem", (int)item_sfx_index.ItemExpire, -1, true); }
        }
    }

    private void LateUpdate()
    {
        if (training_spawner && training_tutorial_name_txt != null && training_tutorial_desc_text != null && gameController != null && gameController.local_plyAttr != null)
        {
            if (!training_tutorial_ui_ready) { UpdateTrainingTutorialText(); }
            else if (gameController.localizer != null && cached_language_type != gameController.localizer.language_type) { training_tutorial_ui_ready = false; cached_language_type = gameController.localizer.language_type; }
        }
    }

    public void StartTimer(double duration)
    {
        item_timer_start_ms = Networking.GetServerTimeInSeconds();
        //item_timer_local = 0.0f;
        item_timer_network = 0.0f;
        item_timer_duration = duration;
        if (Networking.IsOwner(gameObject)) { RequestSerialization(); }
    }

    [NetworkCallable]
    public void SpawnItem(int item_index, bool apply_after_spawn)
    {
        //if (apply_after_spawn) { DespawnItem((int)item_sfx_index.ItemExpire, -1, false); }

        // Powerup
        if (item_index < (int)powerup_type_name.ENUM_LENGTH)
        {
            child_powerup.item_owner_id = -1;
            child_powerup.allow_effects_to_apply = apply_after_spawn;
            if (gameController.option_gamemode == (int)gamemode_name.Infection && !training_spawner && !apply_after_spawn) { child_powerup.item_team_id = 0; } // On Infected, only Survivors may get powerups
            else if (gameController.option_teamplay) 
            { 
                if (item_spawn_team < gameController.team_count) { child_powerup.item_team_id = item_spawn_team; }
                else { child_powerup.item_team_id = -1; }
            }
            else { child_powerup.item_team_id = -1; }
            //child_powerup.item_stored_global_index = item_spawn_global_index;
            child_powerup.item_type = (int)item_type_name.Powerup;
            child_powerup.item_is_template = false;
            child_powerup.powerup_type = item_index;
            child_powerup.SetPowerupStats(item_index);
            child_powerup.powerup_duration = item_spawn_powerup_duration * item_spawn_duration_mul;
            if (gameController.option_gamemode == (int)gamemode_name.Infection && !training_spawner && !apply_after_spawn) { child_powerup.powerup_duration *= 0.5f; } // All pickups are halved duration during Infection 
            child_powerup.powerup_start_ms = Networking.GetServerTimeInSeconds();
            //child_powerup.powerup_timer_local = 0.0f;
            child_powerup.powerup_timer_network = 0.0f;
            //child_powerup.allow_multiple_owners = false;
            child_powerup.apply_after_spawn = apply_after_spawn;
            child_powerup.gameObject.SetActive(true);
            // This must be the last step, in case we try to destroy it on the same frame we are spawning it in
            child_powerup.item_state = (int)item_state_name.InWorld;
        }
        // Weapon
        else if (item_index - (int)powerup_type_name.ENUM_LENGTH < (int)weapon_type_name.ENUM_LENGTH)
        {
            child_weapon.item_owner_id = -1;
            child_weapon.allow_effects_to_apply = apply_after_spawn;
            child_weapon.item_type = (int)item_type_name.Weapon;
            child_weapon.item_is_template = false;
            child_weapon.iweapon_type = item_index - (int)powerup_type_name.ENUM_LENGTH;
            if (gameController.option_gamemode == (int)gamemode_name.Infection && !training_spawner && !apply_after_spawn) { child_weapon.item_team_id = 0; } // On Infected, only Survivors may get weapons
            else if (gameController.option_gamemode == (int)gamemode_name.BossBash && !training_spawner && !apply_after_spawn) 
            {
                // On Boss Bash, only Tiny Troopers may get punching glove weapons (powerups & projectiles are fine)
                if (child_weapon.iweapon_type == (int)weapon_type_name.PunchingGlove || child_weapon.iweapon_type == (int)weapon_type_name.HyperGlove || child_weapon.iweapon_type == (int)weapon_type_name.MegaGlove)
                { child_weapon.item_team_id = 0; }
                else { child_weapon.item_team_id = item_spawn_team; }
            } 
            else if (gameController.option_teamplay)
            {
                if (item_spawn_team < gameController.team_count) { child_weapon.item_team_id = item_spawn_team; }
                else { child_weapon.item_team_id = -1; }
            }
            else { child_weapon.item_team_id = -1; }
            //child_weapon.item_stored_global_index = item_spawn_global_index;
            child_weapon.SetiWeaponStats();
            // If the ammo or duration is -2, that means set the stat to the spawner's powerup duration
            if (child_weapon.iweapon_type >= 0)
            {
                if (child_weapon.iweapon_type < child_weapon.iweapon_ammo_list.Length && child_weapon.iweapon_ammo_list[child_weapon.iweapon_type] == -2) 
                { 
                    child_weapon.iweapon_ammo = Mathf.RoundToInt(item_spawn_powerup_duration);
                }
                if (child_weapon.iweapon_type < child_weapon.iweapon_duration_list.Length && child_weapon.iweapon_duration_list[child_weapon.iweapon_type] == -2) 
                { 
                    child_weapon.iweapon_duration = item_spawn_powerup_duration;
                }
                if (child_weapon.iweapon_type == (int)weapon_type_name.ThrowableItem && gameController != null && gameController.local_plyweapon != null) 
                {
                    child_weapon.iweapon_extra_data = gameController.local_plyweapon.RollForPowerupBombExtraData();
                }
            }
            child_weapon.iweapon_ammo = (int)Mathf.RoundToInt(child_weapon.iweapon_ammo * item_spawn_duration_mul);
            child_weapon.iweapon_duration *= item_spawn_duration_mul;
            // All pickups are halved duration during Infection 
            if (gameController.option_gamemode == (int)gamemode_name.Infection && !training_spawner && !apply_after_spawn) 
            {
                if ((child_weapon.iweapon_ammo * 0.5f) > 0.0f && (child_weapon.iweapon_ammo * 0.5f) < 1.0f) { child_weapon.iweapon_ammo = 1; }
                else if (child_weapon.iweapon_ammo > 0) { child_weapon.iweapon_ammo = Mathf.RoundToInt(child_weapon.iweapon_ammo * 0.5f); }
                if ((child_weapon.iweapon_duration * 0.5f) > 0.0f) { child_weapon.iweapon_duration *= 0.5f; }
            } 
            //child_weapon.allow_multiple_owners = false;
            child_weapon.apply_after_spawn = apply_after_spawn;
            child_weapon.gameObject.SetActive(true);
            child_weapon.item_state = (int)item_state_name.InWorld;
        }

        item_spawn_state = (int)item_spawn_state_name.InWorld;
        if (show_marker_in_game) { child_marker.SetActive(true); }
        else { child_marker.SetActive(false); }
        StartTimer(item_spawn_linger);

        //if (apply_after_spawn) { ForceApplyItem(); }
    }

    [NetworkCallable]
    public void DespawnItem(int reason_code, int owner_id, bool playSFX)
    {
        bool playSFXfiltered = playSFX;
        if (training_spawner) { playSFXfiltered = false; }
        // Powerup
        if (item_spawn_index < (int)powerup_type_name.ENUM_LENGTH && child_powerup != null)
        {
            child_powerup.item_state = (int)item_state_name.Disabled;
            //child_powerup.powerup_type = (int)powerup_type_name.Fallback;
            if (owner_id == Networking.LocalPlayer.playerId && reason_code == (int)item_snd_clips_name.PickupOther) { playSFXfiltered = false; }
            if (playSFXfiltered && child_powerup != null && child_powerup.item_snd_source != null) {
                child_powerup.item_snd_source.transform.position = transform.position;
                //gameController.PlaySFXFromArray(child_powerup.item_snd_source, child_powerup.item_snd_clips, reason_code);
            }
            child_powerup.gameObject.SetActive(false);
        }
        // Weapon
        else if ((item_spawn_index - (int)powerup_type_name.ENUM_LENGTH) < (int)weapon_type_name.ENUM_LENGTH && child_weapon != null)
        {
            child_weapon.item_state = (int)item_state_name.Disabled;
            child_weapon.iweapon_type = 0;
            if (owner_id == Networking.LocalPlayer.playerId && reason_code == (int)item_snd_clips_name.PickupOther) { playSFXfiltered = false; }
            if (playSFXfiltered && child_weapon != null && child_weapon.item_snd_source != null) {
                child_powerup.item_snd_source.transform.position = transform.position;
                //gameController.PlaySFXFromArray(child_weapon.item_snd_source, child_weapon.item_snd_clips, reason_code);
            }
            child_weapon.gameObject.SetActive(false);
        }
        
        if (item_spawn_state == (int)item_spawn_state_name.Disabled) { return; }

        item_spawn_state = (int)item_spawn_state_name.Spawnable;
        if (show_marker_in_game) { child_marker.SetActive(true); }
        else { child_marker.SetActive(false); }
        StartTimer(item_spawn_impulse * (1.0f / item_spawn_frequency_mul));
    }

    // Forcibly applies a copy of the item to the local player as if they picked it up without triggering a networked event. Only used for the throwable weapon.
    /*public void ForceApplyItem()
    {
        if (item_spawn_index < (int)powerup_type_name.ENUM_LENGTH && child_powerup != null)
        {
            //child_powerup.LocalApplyPowerup();
            child_powerup.OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>());
        }
        else if ((item_spawn_index - (int)powerup_type_name.ENUM_LENGTH) < (int)weapon_type_name.ENUM_LENGTH && child_weapon != null)
        {
            //child_weapon.LocalApplyWeapon();
            child_weapon.OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>());
        }
        //DespawnItem((int)item_snd_clips_name.PickupOther, Networking.LocalPlayer.playerId, false);
    }*/

    // Process the spawn timer. Return true if an event should fire.
    internal bool ProcessTimer()
    {
        item_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), item_timer_start_ms);
        if (item_timer_duration > 0 && item_timer_network >= item_timer_duration)
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
            if (i == item_chances.Length - 1) { index_rolled = item_chances.Length - 1; break; } // Regardless of our how high our roll is, if we hit the end of the chance table, just assume it's the best outcome
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
        if (item_spawn_chances_config_keys.Length != item_spawn_chances_config_values.Length) { Debug.LogWarning(transform.parent.gameObject.name + " of " + gameObject.name + ": Key-Value pair for item spawn chances do not match! (Keys: " + item_spawn_chances_config_keys.Length.ToString() + ", Values: " + item_spawn_chances_config_values.Length.ToString() + ")"); }
        for (int i = 0; i < minLen; i++)
        {
            if (i < (int)powerup_type_name.ENUM_LENGTH)
            {
                var powerup_type_name_enum_index = GlobalHelperFunctions.KeyToPowerupType(item_spawn_chances_config_keys[i]);
                outArr[powerup_type_name_enum_index] = item_spawn_chances_config_values[i];
                //Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + ": PARSED POWERUP " + powerup_type_name_enum_index + " from key of " + item_spawn_chances_config_keys[i] + " and value of " + item_spawn_chances_config_values[i]);
            }
            else if (i - (int)powerup_type_name.ENUM_LENGTH < (int)weapon_type_name.ENUM_LENGTH)
            {
                var weapon_type_name_enum_index = GlobalHelperFunctions.KeyToWeaponType(item_spawn_chances_config_keys[i]);
                outArr[weapon_type_name_enum_index + (int)powerup_type_name.ENUM_LENGTH] = item_spawn_chances_config_values[i];
                //Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + ": PARSED WEAPON " + weapon_type_name_enum_index + " from key of " + item_spawn_chances_config_keys[i] + " and value of " + item_spawn_chances_config_values[i]);
            }
        }
        //UnityEngine.Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + ": Parsed inspector array: " + gameController.
        //(outArr));
        return outArr;
    }

    public float[] NormalizeChances(float[] inArr)
    {
        item_spawn_chance_total = 0.0f;
        if (inArr.Length <= 0) { item_spawn_state = (int)item_spawn_state_name.Disabled; Debug.LogError(transform.parent.gameObject.name + " of " + gameObject.name + ": Attempted to normalize chances for an empty array!"); return inArr; } // if passing a null array, don't bother
        for (int i = 0; i < inArr.Length; i++)
        {
            item_spawn_chance_total += inArr[i];
        }
        if (item_spawn_chance_total <= 0.0f) { item_spawn_state = (int)item_spawn_state_name.Disabled; Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + ": Disabled due to combined chances = 0"); return inArr; }

        //UnityEngine.Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + " in array: " + gameController.DebugPrintFloatArray(inArr));
        //UnityEngine.Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + " total chance calculation: " + chance_total);

        var normalized_chances = new float[inArr.Length];
        for (int j = 0; j < inArr.Length; j++)
        {
            if (j == 0) { normalized_chances[j] = (inArr[j] / item_spawn_chance_total); }
            else { normalized_chances[j] = (inArr[j] / item_spawn_chance_total) + normalized_chances[j - 1]; }
        }

        //UnityEngine.Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + ": Normalized array: " + gameController.DebugPrintFloatArray(normalized_chances) + " with total chances of " + chance_total);
        return normalized_chances;
    }

    public int[] ConvertChancesToInt(float[] inArr)
    {
        var outArr = new int[inArr.Length];
        for (int i = 0; i < inArr.Length; i++)
        {
            outArr[i] = (int)Mathf.RoundToInt(inArr[i] * 10000.0f);
        }
        //UnityEngine.Debug.Log(transform.parent.gameObject.name + " of " + gameObject.name + ": Converted chance array to int " + gameController.ConvertIntArrayToString(outArr));
        return outArr;
    }


}
