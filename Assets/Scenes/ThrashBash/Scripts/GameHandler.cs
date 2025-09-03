
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class GameHandler : UdonSharpBehaviour
{

    [UdonSynced] public int roundState = 0; // To-do: enumerate READY, ONGOING, OVERTIME, END
    [UdonSynced] public float roundLength = 20.0f;
    [UdonSynced] public float roundTimer = 0.0f;
    public int[] plyActive;

    public float defaultPlyDP = 0.0f;
    public float defaultPlyAtkMul = 1.0f;
    public float defaultPlyDefMul = 1.0f;
    public float defaultPlySizeMul = 1.0f;
    public int defaultPlyLives = 3;

    public Transform roomReady;
    public Transform roomGame;
    [SerializeField] public Collider[] gameSpawnZones;
    [SerializeField] public TextMeshProUGUI RoundPlyCount_txt;
    [SerializeField] public AudioSource gameMusic;
    [SerializeField] public AudioClip[] gameMusicClips;
    [SerializeField] public AudioSource readyMusic;

    void Start()
    {
        //var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        //foreach(VRCPlayerApi player in VRCPlayerApi.GetPlayers(players)) { 
        //   //
        //} 
        readyMusic.Play();
    }

    private void Update()
    {
        if (roundState == 1 && roundTimer < 5.0f)
        {
            roundTimer += Time.deltaTime;
        }
        else if (roundState == 1 && roundTimer >= 5.0f)
        {
            roundTimer = 0;
            roundState = 2;
        }
        else if (roundState == 2 && roundTimer < roundLength)
        {
            roundTimer += Time.deltaTime;
        }
        else if (roundState == 2 && roundTimer >= roundLength)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetGame");
            roundState = 3;
        }
    }

    public PlayerHandler FindPlayerHandler(VRCPlayerApi player)
    {
        //UnityEngine.Debug.Log("FIND PLAYER HANDLER: " + player.displayName);
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains("PlayerHandler")) continue;
            PlayerHandler foundScript = objects[i].GetComponentInChildren<PlayerHandler>();
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
        //UnityEngine.Debug.Log("NEW PLAYER JOINED: " + player.displayName);
        // To-do: have this be based on an area rather than OnPlayerJoined
        player.SetPlayerTag("PlayerType", "Player"); // States: Player, Spectator
        player.SetPlayerTag("PlayerState", "Alive"); // States: Alive, Dead
        //player.SetPlayerTag("PlayerHP", "Alive"); We can handle HP, Attack, and Defense locally
        //readyPlayerCount = CalculateReadyPlayers();
        var weaponBaseObj = FindOwnedObject(player, "WeaponBase");
        var plyHandlerObj = FindOwnedObject(player, "PlayerHandler");
        var plyHitboxObj = FindOwnedObject(player, "PlayerHitbox");
        Networking.SetOwner(player, weaponBaseObj);
        Networking.SetOwner(player, plyHandlerObj);
        Networking.SetOwner(player, plyHitboxObj);
        FindPlayerHandler(player).playerHitBox = plyHitboxObj;
        //Debug.Log("THE HITBOX SHOULD BE OWNED BY " + player.displayName + ", BUT IS ACTUALLY OWNED BY " + Networking.GetOwner(plyHitboxObj).displayName);
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateActivePlayers"); // To-Do: Update this on ready room enter/exit instead of on player join
        weaponBaseObj.SetActive(false);
        plyHitboxObj.SetActive(false);
    }

    public int[] GetActivePlayers()
    {
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        var playerActiveArr = new int[VRCPlayerApi.GetPlayerCount()];
        var activePlyCount = 0;
        //UnityEngine.Debug.Log("LOOK THROUGH PLAYER ARRAY OF SIZE " + VRCPlayerApi.GetPlayerCount().ToString());
        for (var i = 0; i < VRCPlayerApi.GetPlayers(players).Length; i++) {
            var player = VRCPlayerApi.GetPlayers(players)[i];
            //UnityEngine.Debug.Log("ITERATE PLAYER: " + player.displayName);
            if (player.GetPlayerTag("PlayerType") == "Player")
            {
                playerActiveArr[i] = VRCPlayerApi.GetPlayerId(player);
                activePlyCount++;
                //UnityEngine.Debug.Log("PLAYER IS ACTIVE: " + player.displayName);
            }
            else 
            {
                playerActiveArr[i] = -1;
                //UnityEngine.Debug.Log("PLAYER IS INACTIVE: " + player.displayName);
            }
        }
        RoundPlyCount_txt.text = "Players: " + activePlyCount;
        return playerActiveArr;
    }

    public void UpdateActivePlayers()
    {
        //UnityEngine.Debug.Log("NETWORK EVENT: UPDATE ACTIVE PLAYERS");
        plyActive = GetActivePlayers();
    }

    // Networking question: if this event is sent locally, and UdonSynced is active, does it then apply to all other players, despite being a PlayerObject?
    // If not, then we want to have the round start in two steps: one in which has all players synced up at once, then again for local
    /*public void RoundStart()
    {
        // Set all player object data to the values above
        for (var i = 0; i <= plyActive.Length; i++)
        {
            if (plyActive[i] < 0) { continue; }
            var player = VRCPlayerApi.GetPlayerById(plyActive[i]);
            var playerData = FindPlayerHandler(player);
            playerData.plyHP = defaultPlyHP;
            playerData.plyMaxHP = defaultPlyHP;
            playerData.plyAtk = defaultPlyAtk;
            playerData.plyDef = defaultPlyDef;
            playerData.plySizeMul = defaultPlySizeMul;
            playerData.plyLives = defaultPlyLives;
            playerData.plyActive = true;

        }
    }*/

    public void TeleportLocalPlayerToGameSpawnZone(int spawnZoneIndex = -1) {
        // If no spawnzone is specified, just use a random one
        if (spawnZoneIndex == -1) { spawnZoneIndex = Random.Range(0, gameSpawnZones.Length-1);  }

        var spawnZoneBounds = gameSpawnZones[spawnZoneIndex].bounds;
        var rx = Random.Range(spawnZoneBounds.min.x, spawnZoneBounds.min.x);
        var rz = Random.Range(spawnZoneBounds.min.z, spawnZoneBounds.min.z);

        Networking.LocalPlayer.TeleportTo(new Vector3(rx, spawnZoneBounds.center.y, rz), Networking.LocalPlayer.GetRotation());
    }

    public void TeleportLocalPlayerToReadyRoom()
    {
        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        Networking.LocalPlayer.TeleportTo(roomReady.transform.position, Networking.LocalPlayer.GetRotation());
    }

    public void LocalRoundStart()
    {
        //UnityEngine.Debug.Log("NETWORK EVENT: LOCAL ROUND START");
        //UnityEngine.Debug.Log("array length: " + plyActive.Length.ToString());
        for (var i = 0; i < plyActive.Length; i++)
        {
            //UnityEngine.Debug.Log("iter: " + i.ToString());
            if (plyActive[i] < 0) { continue; }
            //UnityEngine.Debug.Log("PLAYER " + plyActive[i].ToString() + " IS ACTIVE");
            var player = VRCPlayerApi.GetPlayerById(plyActive[i]);
            var weaponBaseObj = FindOwnedObject(player, "WeaponBase");
            var plyHitboxObj = FindOwnedObject(player, "PlayerHitbox");
            weaponBaseObj.SetActive(true);
            plyHitboxObj.SetActive(true);

            if (!player.isLocal) { continue; }

            //UnityEngine.Debug.Log("PLAYER " + player.displayName + "IS LOCAL");
            PlayerHandler playerData = FindPlayerHandler(player);
            //UnityEngine.Debug.Log("SETTING PLAYERDATA");
            playerData.plyDP = defaultPlyDP;
            playerData.plyBaseDP = defaultPlyDP;
            playerData.plyAtkMul = defaultPlyAtkMul;
            playerData.plyDefMul = defaultPlyDefMul;
            playerData.plySizeMul = defaultPlySizeMul;
            playerData.plyLives = defaultPlyLives;
            playerData.plyActive = true;
            playerData.localPlayerState = 1;
            weaponBaseObj.GetComponent<WeaponBase>().localPlayerHandler = playerData;
            //UnityEngine.Debug.Log("TELEPORTING PLAYER");
            roundState = 1;
            TeleportLocalPlayerToGameSpawnZone(i % gameSpawnZones.Length);
            readyMusic.Stop();
            var randMusic = Random.Range(0, gameMusicClips.Length-1);
            gameMusic.clip = gameMusicClips[randMusic];
            gameMusic.Play();

            // To-Do: Function to validate spawn zones

        }
    }

    public void NetworkRoundStart()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LocalRoundStart");
    }

    public void CheckAllPlayerLives()
    {
        if (roundState != 2) { return; }
        // To-Do: Add teamplay support
        var playersAlive = 0;
        for (var i = 0; i < plyActive.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(plyActive[i]);
            var plyHandler = FindPlayerHandler(player);
            if (plyHandler.plyLives > 0) { playersAlive++;  }
            UnityEngine.Debug.Log("Player " + player.displayName + " has " + plyHandler.plyLives + " lives left!");
        }
        if ((playersAlive <= 1 && plyActive.Length > 1) || (playersAlive <= 0 && plyActive.Length == 1) || plyActive.Length == 0)
        {
            roundState = 3;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetGame");
        }
    }

    public void ResetGame()
    {
        for (var i = 0; i < plyActive.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(plyActive[i]);
            var weaponBaseObj = FindOwnedObject(player, "WeaponBase");
            var plyHitboxObj = FindOwnedObject(player, "PlayerHitbox");
            weaponBaseObj.SetActive(false);
            plyHitboxObj.SetActive(false);
            TeleportLocalPlayerToReadyRoom();
        }
        readyMusic.Play();
        gameMusic.Stop();
        roundTimer = 0;
        //roundState = 0;
    }
}
