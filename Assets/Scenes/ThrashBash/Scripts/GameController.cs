
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum game_sfx_index
{
    Death, Kill, HitSend, HitReceive
}

public class GameController : UdonSharpBehaviour
{

    [UdonSynced] public int round_state = 0;
    [UdonSynced] public float round_length = 120.0f;

    [UdonSynced] public float plysettings_dp = 0.0f;
    [UdonSynced] public int plysettings_lives = 3;

    public float round_timer = 0.0f;

    [SerializeField] public Collider[] room_game_spawnzones;
    [SerializeField] public TextMeshProUGUI room_ready_txt;
    [SerializeField] public AudioSource snd_game_music_source;
    [SerializeField] public AudioClip[] snd_game_music_clips;
    [SerializeField] public AudioSource snd_ready_music_source;
    [SerializeField] public AudioSource[] snd_game_sfx_sources;
    [SerializeField] public GameObject template_PlayerHitbox;

    private GameObject[] arr_PlayerHitbox;

    void Start()
    {
        arr_PlayerHitbox = new GameObject[0];
    }

    public PlayerAttributes FindPlayerAttributes(VRCPlayerApi player)
    {
        //UnityEngine.Debug.Log("FIND PLAYER HANDLER: " + player.displayName);
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains("PlayerAttributes")) continue;
            PlayerAttributes foundScript = objects[i].GetComponentInChildren<PlayerAttributes>();
            if (Utilities.IsValid(foundScript))
            {
                //UnityEngine.Debug.Log("FOUND SCRIPT: " + foundScript.name);
                return foundScript;
            }
        }
        return null;
    }

    public GameObject FindOwnedObject(VRCPlayerApi player, string objName)
    {
        //UnityEngine.Debug.Log("FIND " + objName + "FROM: " + player.displayName);
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains(objName)) continue;
            if (Utilities.IsValid(objects[i]))
            {
                //UnityEngine.Debug.Log("FOUND OBJECT: " + objects[i].name);
                return objects[i];
            }
        }
        return null;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        base.OnPlayerJoined(player);
        var plyWeaponObj = FindOwnedObject(player, "PlayerWeapon");
        var plyAttributesObj = FindOwnedObject(player, "PlayerAttributes");
        var plyUIToOthers = FindOwnedObject(player, "UIPlyToOthers");
        Networking.SetOwner(player, plyWeaponObj);
        Networking.SetOwner(player, plyAttributesObj);
        Networking.SetOwner(player, plyUIToOthers);
        // NOTE: THIS MEANS THAT HITBOXES JUST FLOAT AROUND, AND WILL PERSIST UPON DISCONNECT. NEED WAY TO DISPOSE.
        var plyHitboxObj = Instantiate(template_PlayerHitbox);
        plyHitboxObj.GetComponent<PlayerHitbox>().owner_id = player.playerId;
        plyHitboxObj.layer = LayerMask.NameToLayer("PlayerHitbox");
        var arr_plyhitbox_ext = new GameObject[arr_PlayerHitbox.Length + 1];
        for (int i = 0; i < arr_PlayerHitbox.Length; i++)
        {
            arr_plyhitbox_ext[i] = arr_PlayerHitbox[i];
        }
        arr_plyhitbox_ext[arr_PlayerHitbox.Length] = plyHitboxObj;
        arr_PlayerHitbox = arr_plyhitbox_ext;
        //plyAttributesObj.GetComponent<PlayerAttributes>().ply_state = 
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        base.OnPlayerLeft(player);
        var arr_plyhitbox_reduce = new GameObject[arr_PlayerHitbox.Length - 1];
        var player_index = -1;
        for (int i = 0; i < arr_PlayerHitbox.Length; i++)
        {
            if (arr_PlayerHitbox[i].GetComponent<PlayerHitbox>().owner_id == player.playerId) 
            { 
                player_index = i;
                Destroy(arr_PlayerHitbox[i]);
                continue; 
            }
            if (player_index == -1) { arr_plyhitbox_reduce[i] = arr_PlayerHitbox[i]; }
            else { arr_plyhitbox_reduce[i - 1] = arr_PlayerHitbox[i]; }
        }
        arr_PlayerHitbox = arr_plyhitbox_reduce;
    }

    private void Update()
    {
        if (round_state != 0 && round_timer < round_length)
        {
            round_timer += Time.deltaTime;
        }
    }
}
