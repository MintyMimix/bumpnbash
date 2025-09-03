
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum game_sfx_index
{
    Death, Kill, HitSend, HitReceive
}

public enum round_state_name
{
    Start, Ready, Ongoing, Over
}

public class GameController : UdonSharpBehaviour
{

    [SerializeField] public GameObject template_WeaponProjectile;
    [SerializeField] public GameObject template_WeaponHurtbox;
    [SerializeField] public GameObject room_ready_spawn;
    [SerializeField] public Collider[] room_game_spawnzones;
    [SerializeField] public TextMeshProUGUI room_ready_txt;
    [SerializeField] public AudioSource snd_game_music_source;
    [SerializeField] public AudioClip[] snd_game_music_clips;
    [SerializeField] public AudioSource snd_ready_music_source;
    [SerializeField] public AudioSource[] snd_game_sfx_sources;

    [UdonSynced] public int round_state = 0;
    [UdonSynced] public float round_length = 120.0f;
    [UdonSynced] public float ready_length = 5.0f;
    [UdonSynced] public float over_length = 10.0f;

    [UdonSynced] public float plysettings_dp = 0.0f;
    [UdonSynced] public float plysettings_respawn_duration = 3.0f;
    [UdonSynced] public int plysettings_lives = 3;

    public float round_timer = 0.0f;

    public GameObject[] projectiles;
    public GameObject[] hurtboxes;
    public int[] players_active;
    [UdonSynced] public string players_active_str = "";


    private void Start()
    {
        projectiles = new GameObject[0];
        hurtboxes = new GameObject[0];
        snd_ready_music_source.Play();
    }

    private void Update()
    {
        if (round_state == (int)round_state_name.Ready && round_timer < ready_length)
        {
            round_timer += Time.deltaTime;
        }
        else if (round_state == (int)round_state_name.Ready && round_timer >= ready_length)
        {
            round_timer = 0;
            round_state = (int)round_state_name.Ongoing;
            if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer < round_length)
        {
            round_timer += Time.deltaTime;
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer >= round_length)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetGame");
            round_state = (int)round_state_name.Over;
            if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
        }
        else if (round_state == (int)round_state_name.Over && round_timer < ready_length)
        {
            round_timer += Time.deltaTime;
        }
        else if (round_state == (int)round_state_name.Over && round_timer >= ready_length)
        {
            round_timer = 0;
            round_state = (int)round_state_name.Start;
            if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
        }

        room_ready_txt.text = "Players: " + players_active.Length.ToString() + "\n {" + players_active_str + "}";
    }

    public GameObject FindPlayerOwnedObject(VRCPlayerApi player, string objName)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains(objName)) continue;
            if (Utilities.IsValid(objects[i]))
            {
                return objects[i];
            }
        }
        return null;
    }

    public PlayerAttributes FindPlayerAttributes(VRCPlayerApi player)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains("PlayerAttributes")) continue;
            PlayerAttributes foundScript = objects[i].GetComponentInChildren<PlayerAttributes>();
            if (Utilities.IsValid(foundScript))
            {
                return foundScript;
            }
        }
        return null;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        base.OnPlayerJoined(player);
        var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
        var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
        var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
        var plyUIToOthers = FindPlayerOwnedObject(player, "UIPlyToOthers");
        var plyUIToSelf = FindPlayerOwnedObject(player, "UIPlyToSelf");
        Networking.SetOwner(player, plyWeaponObj);
        Networking.SetOwner(player, plyAttributesObj);
        Networking.SetOwner(player, plyHitboxObj);
        plyHitboxObj.GetComponent<PlayerHitbox>().playerAttributes = plyAttributesObj.GetComponent<PlayerAttributes>();
        Networking.SetOwner(player, plyUIToOthers);
        plyUIToOthers.GetComponent<UIPlyToOthers>().owner = player;
        plyUIToOthers.GetComponent<UIPlyToOthers>().playerAttributes = plyAttributesObj.GetComponent<PlayerAttributes>();
        Networking.SetOwner(player, plyUIToSelf);
        plyUIToSelf.GetComponent<UIPlyToSelf>().owner = player;
        plyUIToSelf.GetComponent<UIPlyToSelf>().playerAttributes = plyAttributesObj.GetComponent<PlayerAttributes>();

        plyWeaponObj.SetActive(false);
        plyHitboxObj.SetActive(false);
        if (Networking.LocalPlayer == player) 
        { 
            plyUIToOthers.SetActive(false);
            plyUIToSelf.SetActive(true);
        }
        else
        {
            plyUIToOthers.SetActive(true);
            plyUIToSelf.SetActive(false);
        }
        if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
    }

    public override void OnDeserialization()
    {
        if (players_active_str.Length > 0) { players_active = ConvertStrToIntArray(players_active_str); }
        CheckAllPlayerLives();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (Utilities.IsValid(objects[i]))
            {
                Destroy(objects[i]);
            }
        }
        RemovePlayerFromActive(player.playerId);
    }

    public void TeleportLocalPlayerToGameSpawnZone(int spawnZoneIndex = -1)
    {
        // If no spawnzone is specified, just use a random one
        if (spawnZoneIndex == -1) { spawnZoneIndex = UnityEngine.Random.Range(0, room_game_spawnzones.Length - 1); }

        var spawnZoneBounds = room_game_spawnzones[spawnZoneIndex].bounds;
        var rx = UnityEngine.Random.Range(spawnZoneBounds.min.x, spawnZoneBounds.max.x);
        var rz = UnityEngine.Random.Range(spawnZoneBounds.min.z, spawnZoneBounds.max.z);

        Networking.LocalPlayer.TeleportTo(new Vector3(rx, spawnZoneBounds.center.y, rz), Networking.LocalPlayer.GetRotation());
    }

    public void TeleportLocalPlayerToReadyRoom()
    {
        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        Networking.LocalPlayer.TeleportTo(room_ready_spawn.transform.position, Networking.LocalPlayer.GetRotation());
    }


    [NetworkCallable]
    public void LocalRoundStart()
    {
        for (var i = 0; i < players_active.Length; i++)
        {
            if (players_active[i] < 0) { continue; }
            var player = VRCPlayerApi.GetPlayerById(players_active[i]);
            var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
            var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
            plyWeaponObj.SetActive(true);
            plyHitboxObj.SetActive(true);

            if (!player.isLocal) { continue; }

            PlayerAttributes playerData = FindPlayerAttributes(player);
            playerData.ply_dp = plysettings_dp;
            playerData.ply_dp_default = plysettings_dp;
            playerData.ply_lives = plysettings_lives;
            playerData.ply_respawn_duration = plysettings_respawn_duration;
            playerData.ply_state = (int)player_state_name.Alive;
            round_state = (int)round_state_name.Ready;
            if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
            // To-Do: Function to validate spawn zones
            TeleportLocalPlayerToGameSpawnZone(i % room_game_spawnzones.Length);
            snd_ready_music_source.Stop();
            var randMusic = UnityEngine.Random.Range(0, snd_game_music_clips.Length - 1);
            snd_game_music_source.clip = snd_game_music_clips[randMusic];
            snd_game_music_source.Play();

        }
    }

    public void NetworkRoundStart()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalRoundStart"); 
    }

    [NetworkCallable]
    public void CheckAllPlayerLives()
    {
        if (round_state != (int)round_state_name.Ongoing) { return; }
        // To-Do: Add teamplay support
        var playersAlive = 0;
        for (var i = 0; i < players_active.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_active[i]);
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes.ply_lives > 0) { playersAlive++; }
        }
        if ((playersAlive <= 1 && players_active.Length > 1) || (playersAlive <= 0 && players_active.Length == 1) || players_active.Length == 0)
        {
            round_state = (int)round_state_name.Over;
            if (Networking.LocalPlayer.isMaster) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetGame"); }
        }
    }

    public void ResetGame()
    {
        for (var i = 0; i < players_active.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_active[i]);
            var plyWeapon = FindPlayerOwnedObject(player, "PlayerWeapon");
            var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
            plyWeapon.SetActive(false);
            plyHitboxObj.SetActive(false);
            TeleportLocalPlayerToReadyRoom();
        }
        snd_ready_music_source.Play();
        snd_game_music_source.Stop();
        round_timer = 0;
        if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
    }

    public int StringToInt(string str)
    {
        int result = -404;
        int.TryParse(str, out result); // UdonSharp supports TryParse
        return result;
    }

    public int[] ConvertStrToIntArray(string str) 
    {
        string[] splitStr = str.Split(',');
        int[] arrOut = new int[splitStr.Length];

        for (int i = 0; i < splitStr.Length; i++)
        {
            var intAttempt = StringToInt(splitStr[i]);
            if (intAttempt != 404) { arrOut[i] = intAttempt; }
        }
        return arrOut;
    }

    public string ConvertIntArrayToString(int[] arrIn, string separator)
    {
        if (arrIn == null || arrIn.Length == 0) return "";

        string result = arrIn[0].ToString();
        for (int i = 1; i < arrIn.Length; i++)
        {
            result += separator;
            result += arrIn[i].ToString();
        }
        return result;
    }

    [NetworkCallable]
    public void AddPlayerToActive(int player_id)
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (players_active_str.Length > 0) {players_active = ConvertStrToIntArray(players_active_str); }
        var players_add = new int[players_active.Length + 1];
        for (var i = 0; i < players_active.Length; i++)
        {
            if (players_active[i] == player_id) { return; }
            players_add[i] = players_active[i];
        }
        players_add[players_active.Length] = player_id;
        players_active = players_add;
        players_active_str = ConvertIntArrayToString(players_add, ",");
        if (VRCPlayerApi.GetPlayerById(player_id) != null) { FindPlayerAttributes(VRCPlayerApi.GetPlayerById(player_id)).ply_state = (int)player_state_name.Joined; }
        RequestSerialization();
    }

    [NetworkCallable]
    public void RemovePlayerFromActive(int player_id)
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (players_active_str.Length > 0) { players_active = ConvertStrToIntArray(players_active_str); }
        else { return; }
        var players_remove = new int[players_active.Length - 1];
        var player_index = -1;
        for (int i = 0; i < players_active.Length; i++)
        {
            if (players_active[i] == player_id)
            {
                player_index = i;
                continue;
            }
            // To-Do: clean up array of nulls
            if (player_index == -1 && players_active[i] > 0) { players_remove[i] = players_active[i]; }
            else { players_remove[i - 1] = players_active[i]; }
        }
        players_active = players_remove;
        players_active_str = ConvertIntArrayToString(players_remove, ",");
        if (VRCPlayerApi.GetPlayerById(player_id) != null) { FindPlayerAttributes(VRCPlayerApi.GetPlayerById(player_id)).ply_state = (int)player_state_name.Inactive; }
        RequestSerialization();
    }

    [NetworkCallable]
    public void NetworkCreateProjectile(Vector3 fire_start_pos, Quaternion fire_angle, float fire_speed, float fire_lifetime, int player_id)
    {
        var newProjectileObj = Instantiate(template_WeaponProjectile, transform);
        newProjectileObj.transform.parent = null;
        var projectile = newProjectileObj.GetComponent<WeaponProjectile>();
        newProjectileObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newProjectileObj.transform.SetPositionAndRotation(fire_start_pos, fire_angle);
        projectile.projectile_type = (int)projectile_type_name.Bullet;
        projectile.projectile_lifetime = fire_lifetime;
        projectile.pos_start = fire_start_pos;
        projectile.projectile_speed = fire_speed;
        projectile.owner_id = player_id;
        projectile.template_WeaponHurtbox = template_WeaponHurtbox;
        projectile.gameController = this;
        newProjectileObj.SetActive(true);

        // Store this in an array
        var projectiles_ext = new GameObject[projectiles.Length + 1];
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] != null)
            {
                projectiles_ext[i] = projectiles[i];
            }
        }
        projectiles_ext[projectiles.Length] = newProjectileObj;
        projectiles = projectiles_ext;
    }

    [NetworkCallable]
    public void NetworkCreateHurtBox(Vector3 position, float damage, float lifetime, int player_id, int projectile_instance_id)
    {
        var newHurtboxObj = Instantiate(template_WeaponHurtbox, transform);

        newHurtboxObj.transform.parent = null;
        var hurtbox = newHurtboxObj.GetComponent<WeaponHurtbox>();

        newHurtboxObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        newHurtboxObj.transform.position = position;
        hurtbox.hurtbox_state = (int)hurtbox_state_name.Active;
        hurtbox.hurtbox_damage = damage;
        hurtbox.hurtbox_lifetime = lifetime;
        hurtbox.owner_id = player_id;
        hurtbox.gameController = this;
        newHurtboxObj.SetActive(true);

        var hurtboxes_ext = new GameObject[hurtboxes.Length + 1];
        for (int i = 0; i < hurtboxes.Length; i++)
        {
            if (hurtboxes[i] != null)
            {
                hurtboxes_ext[i] = hurtboxes[i];
            }
        }
        hurtboxes_ext[hurtboxes.Length] = newHurtboxObj;
        hurtboxes = hurtboxes_ext;

        projectiles = DestroyInstanceWithArray(projectile_instance_id, projectiles);
    }

    public GameObject[] DestroyInstanceWithArray(int inInstanceId, GameObject[] inArr)
    {
        // If the array we are evaluating has no entries, just don't bother
        if (inArr.Length <= 0)
        {
            return new GameObject[0];
        }

        // This method admittedly sucks, but Udon not supporting List<T> / dynamically allocated arrays forces my hand
        var indices_to_remove_str = "";
        for (int i = 0; i < inArr.Length; i++)
        {
            if (inArr[i] == null || inInstanceId == inArr[i].GetInstanceID())
            {
                if (indices_to_remove_str.Length > 0) { indices_to_remove_str += ","; }
                indices_to_remove_str += i.ToString();
            }
        }
        // If we find nothing to remove, just return the original array
        if (indices_to_remove_str.Length == 0) {
            return inArr;
        }

        var indices_to_remove_int = ConvertStrToIntArray(indices_to_remove_str);

        // If we find we're removing everything, just destroy it all and send an empty array
        if (inArr.Length - indices_to_remove_int.Length <= 0)
        {
            for (int i = 0; i < inArr.Length; i++)
            {
                if (inArr[i] != null) { Destroy(inArr[i]); }
            }
            return inArr;
        }

        // Otherwise, we create a new array with the indices listed removed and return that
        var index_iter = 0;
        var arr_reduced = new GameObject[inArr.Length - indices_to_remove_int.Length];
        for (int i = 0; i < inArr.Length; i++)
        {
            var include_entry = true;
            for (int j = 0; j < indices_to_remove_int.Length; j++)
            {
                if (i == indices_to_remove_int[j]) { 
                    include_entry = false;
                    if (inArr[i] != null) { Destroy(inArr[i]); }
                    break; 
                }
            }
            if (!include_entry) { continue; }
            arr_reduced[index_iter] = inArr[i];
            index_iter++;
        }
        return arr_reduced;
    }

    /*
    public GameObject[] DestroyInstanceWithArray(int inInstanceID, GameObject[] inArr)
    {
        //gameObject.GetInstanceID()
        // Make sure we're actually destroying an existing object
        if (inInstanceID == null) { return inArr; }
        // If the array we are evaluating has no entries, don't bother searching. Destroy the object and move on
        if (inArr.Length <= 0)
        {
            Destroy(inInstanceObj);
            return new GameObject[0];
        }

        // This method admittedly sucks, but Udon not supporting List<T> / dynamically allocated arrays forces my hand
        var indices_to_remove_str = "";
        for (int i = 0; i < inArr.Length; i++)
        {
            if (inArr[i] == null || inInstanceObj == inArr[i])
            {
                if (indices_to_remove_str.Length > 0) { indices_to_remove_str += ","; }
                indices_to_remove_str += i.ToString();
            }
        }
        // If we find nothing to remove, just destroy the instance and return the original array
        if (indices_to_remove_str.Length == 0) {
            Destroy(inInstanceObj);
            return inArr;
        }

        var indices_to_remove_int = ConvertStrToIntArray(indices_to_remove_str);

        // If we find we're removing everything, just send an empty array
        if (inArr.Length - indices_to_remove_int.Length <= 0)
        {
            Destroy(inInstanceObj);
            return inArr;
        }

        // Otherwise, we create a new array with the indices listed removed and return that
        var index_iter = 0;
        var arr_reduced = new GameObject[inArr.Length - indices_to_remove_int.Length];
        for (int i = 0; i < inArr.Length; i++)
        {
            var include_entry = true;
            for (int j = 0; j < indices_to_remove_int.Length; j++)
            {
                if (i == indices_to_remove_int[j]) { include_entry = false; break; }
            }
            if (!include_entry) { continue; }
            arr_reduced[index_iter] = inArr[i];
            index_iter++;
        }
        Destroy(inInstanceObj);
        return arr_reduced;
    }*/

}
