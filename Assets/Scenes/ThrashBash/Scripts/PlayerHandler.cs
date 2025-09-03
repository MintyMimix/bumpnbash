
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using static VRC.SDKBase.VRCPlayerApi;

public class PlayerHandler : UdonSharpBehaviour
{
    [UdonSynced] public float plyDP; // Damage Points (e.g. 0%, 40%, 120%, 999%)
    [UdonSynced] public float plyBaseDP; // Base Damage Points (increased if handicapped)
    [UdonSynced] public float plyAtkMul; // Additional attacking damage applied due to powerups
    //[UdonSynced] public float plyMaxAtk;
    //[UdonSynced] public float plyMinAtk;
    [UdonSynced] public float plyDefMul; // Final push force mitigated due to powerups
    //[UdonSynced] public float plyMaxDef;
    //[UdonSynced] public float plyMinDef;
    [UdonSynced] public float plySizeMul; // Size multiplier
    [UdonSynced] public int plyLives; // # of lives
    //public VRCPlayerApi plyOwner;
    [UdonSynced] public bool plyActive; // Player is participating in the game
    public float localPlayerState;
    private float sizeFactor; // The ratio between plySizeMul (damage multpilier) and actual player scale (default vs current eye height)
    private float plyDefaultEyeHeight;
    private float plyDesiredEyeHeight;
    private float plyEyeHeightLerpInterval;
    public VRCPlayerApi lastHitByPly;
    private float lastHitByTimer;
    private float lastHitByDuration;
    public GameObject HitReceiveSound;
    public GameObject HitSendSound;
    public GameObject KillSound;
    public GameObject DeathSound;
    public GameHandler gameHandler;
    public GameObject playerHitBox;
    public float HUDLingerTimer, HUDLingerDuration;

    // BIG TO-DO: Instance Master can't seem to hit anything (because they are the owner of every object on their client)

    // To-Do: Implement powerups as an array or dict of timers (these are not network synced; only the actual stats are)

    void Start()
    {
        plyAtkMul = 1.0f;
        plyDefMul = 1.0f;
        plySizeMul = 1.0f;

        localPlayerState = 0;
        HUDLingerDuration = 4.0f;
        lastHitByDuration = 4.0f;

        plyDefaultEyeHeight = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        plyDesiredEyeHeight = plyDefaultEyeHeight;
        plyEyeHeightLerpInterval = 0.2f; // To-Do: Set this interval to 1/5 of the difference between plyDefaultEyeHeight and = plyDesiredEyeHeight in size change function
        playerHitBox = gameHandler.FindOwnedObject(Networking.GetOwner(gameObject), "PlayerHitbox");
    }

    private void Update()
    {
        
        // Update size
        var plyCurrentEyeHeight = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        if (plyCurrentEyeHeight != plyDesiredEyeHeight && plyEyeHeightLerpInterval != 0) 
        { 
            if (plyEyeHeightLerpInterval > 0 && plyCurrentEyeHeight + plyEyeHeightLerpInterval <= plyDesiredEyeHeight) { plyCurrentEyeHeight += plyEyeHeightLerpInterval; }
            if (plyEyeHeightLerpInterval > 0 && plyCurrentEyeHeight + plyEyeHeightLerpInterval > plyDesiredEyeHeight) { plyCurrentEyeHeight = plyDesiredEyeHeight; }
            if (plyEyeHeightLerpInterval < 0 && plyCurrentEyeHeight + plyEyeHeightLerpInterval >= plyDesiredEyeHeight) { plyCurrentEyeHeight += plyEyeHeightLerpInterval; }
            if (plyEyeHeightLerpInterval < 0 && plyCurrentEyeHeight + plyEyeHeightLerpInterval < plyDesiredEyeHeight) { plyCurrentEyeHeight = plyDesiredEyeHeight; }
            //Debug.Log("I DESIRE THE HEIGHT: " + plyDesiredEyeHeight + " (FROM " + plyDefaultEyeHeight + ") AND WILL CHANGE TO " + plyCurrentEyeHeight);
            //Networking.LocalPlayer.SetAvatarEyeHeightByMeters(plyCurrentEyeHeight);
        }


        if (playerHitBox != null && gameHandler.roundState == 2)
        {
            if (playerHitBox.transform.parent != null) { playerHitBox.transform.parent = null; }
            var scaleHitbox = 2.0f * (Networking.GetOwner(gameObject).GetAvatarEyeHeightAsMeters() / 1.6f);
            playerHitBox.transform.localScale = new Vector3(1.0f, scaleHitbox, 1.0f);
            playerHitBox.transform.SetPositionAndRotation(Networking.GetOwner(gameObject).GetPosition() + new Vector3(0.0f, scaleHitbox / 2.0f, 0.0f), Networking.GetOwner(gameObject).GetRotation());

        }

        // Only display HUD element for a short time
        if (localPlayerState == 2 && HUDLingerTimer < HUDLingerDuration)
        {
            HUDLingerTimer += Time.deltaTime;
        }
        else if (localPlayerState == 2 && HUDLingerTimer >= HUDLingerDuration)
        {
            localPlayerState = 1;
        }

    }

    public void takeDamageFromTarget(float damage, VRCPlayerApi attackerPly, Vector3 forceDirection)
    {
        //every hit, gain +dmg
        //force applied is proportional to damage
        //but is then divided/multiplied by SizeMul
        //additionally divided/multiplied by attacking and defending modifiers
        //certain weapons may delta more damage than others
        var attackerPlayerHandler = gameHandler.FindPlayerHandler(attackerPly);
        var calcDmg = damage;
        if (attackerPlayerHandler.plyAtkMul != 0) { calcDmg *= attackerPlayerHandler.plyAtkMul; }
        var modForceDirection = forceDirection; 

        if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, 1.0f, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, 0.33f, 0.0f); }
        var calcForce = (modForceDirection + new Vector3(0.0f, 0.33f, 0.0f));
        // To-Do: Create GameSettings object that is only discretely networked, and contains customizations for things like below (i.e. pushforce multiplier scaling with damage)
        calcForce *= Mathf.Pow((calcDmg + plyDP)/ 10.0f, 1.333f);
        UnityEngine.Debug.Log("== FORCE SCALE DUE TO DAMAGE TAKEN " + Mathf.Pow((calcDmg + plyDP) / 10.0f, 2.0f).ToString() + " ==");
        if (plyDefMul != 0) { calcForce *= (1.0f / plyDefMul); }
        if (plySizeMul != 0) { calcForce *= (1.0f / plySizeMul); }
        

        HitReceiveSound.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
        HitReceiveSound.GetComponent<AudioSource>().Play();

        Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);

        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        lastHitByPly = attackerPly;
        //lastHitByDmg = calcDmg;
        plyDP += calcDmg;

        SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attackerPly.playerId);
    }

    [NetworkCallable]
    public void HitOtherPlayer(int attackerPlyId)
    {
        if (VRCPlayerApi.GetPlayerById(attackerPlyId) != Networking.LocalPlayer) { return; }
        HitSendSound.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
        HitSendSound.GetComponent<AudioSource>().Play();
    }

    [NetworkCallable]
    public void KillOtherPlayer(int attackerPlyId, int defenderPlyId)
    {
        if (VRCPlayerApi.GetPlayerById(attackerPlyId) != Networking.LocalPlayer) { return; }
        KillSound.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
        KillSound.GetComponent<AudioSource>().Play();
        // To-Do: Make this a HUD element
        UnityEngine.Debug.Log("You knocked out " + VRCPlayerApi.GetPlayerById(defenderPlyId).displayName + "!");
    }

    [NetworkCallable]
    public void HandleOwnDeath()
    {
        // If no player ID is passed, assume self
        //if (attackerPlyId == - 1) { attackerPlyId = Networking.LocalPlayer.playerId; }
        DeathSound.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
        DeathSound.GetComponent<AudioSource>().Play();
        // To-Do: Make this a HUD element
        if (lastHitByPly == null) { UnityEngine.Debug.Log("You were knocked out!"); }
        else { UnityEngine.Debug.Log("You were knocked out by " + lastHitByPly.displayName + "!"); }
        HUDLingerTimer = 0;
        localPlayerState = 2;
        plyLives--;
        UnityEngine.Debug.Log("You have " + plyLives + "lives left!");
        // To-Do: Manage behavior based on GameHandler.LivesMode
        if (plyLives > 0) 
        {
            plyDP = plyBaseDP;
            // To-Do: Remove all active powerups
            // To-Do: Reset stats based on GameHandler's settings
            // plyAtkMul = 1.0f;
            // To-Do: Teleport player to field
            gameHandler.TeleportLocalPlayerToGameSpawnZone();
        }
        else 
        {
            UnityEngine.Debug.Log("You are dead!");
            // To-Do: Send networked event to everyone to declare you are dead
            // To-Do: Teleport player to spectator zone
            gameHandler.TeleportLocalPlayerToReadyRoom();
            localPlayerState = 3;

        }
        gameHandler.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CheckAllPlayerLives");
    }

}

