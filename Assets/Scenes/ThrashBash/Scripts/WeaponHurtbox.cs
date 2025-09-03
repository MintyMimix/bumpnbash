
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.RPC;

public class WeaponHurtbox : UdonSharpBehaviour
{
    public int hurtboxState, hurtboxID;
    public float hurtboxSize, hurtboxSizeLerp;
    public float hurtboxTransitionDuration, hurtboxLingerDuration, hurtboxTransitionTimer, hurtboxLingerTimer;
    public float hurtboxDamage;
    private bool excludeLocalPly = false;
    public GameHandler gameHandler;

    void Start()
    {

    }

    public void ReceiveHit(VRCPlayerApi player) {
        // You've been hit!
        // Do not apply remotely; this should be handled locally
        Debug.Log("PLAYER HAS BEEN HIT, AND THEY ARE " + excludeLocalPly.ToString() + " TO BE EXCLUDED AND ARE " + player.isLocal.ToString() + " LOCAL");
        if (!player.isLocal || excludeLocalPly) { return; }
        // Find the defender's player handler
        var defenderHandler = gameHandler.FindPlayerHandler(player);
        // Find the attacking player
        var attackerPly = Networking.GetOwner(gameObject);
        // Calculate the force directional vector (vSourceToDestination = vDestination - vSource;)
        var forceDirection = Vector3.Normalize(player.GetPosition() - transform.position);
        // Tell the player they've been hit and who they've been hit by (network owner)
        defenderHandler.takeDamageFromTarget(hurtboxDamage, attackerPly, forceDirection);
        //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "PlaySentDamageSound");
        // Add the player to the list of players struck so we don't hit them multiple times
        excludeLocalPly = true;
        
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal || excludeLocalPly || Networking.GetOwner(gameObject) == Networking.LocalPlayer) { return; }
        ReceiveHit(player);
    }

    public void OnTriggerEnter(Collider other)
    {
        var colliderOwner = Networking.GetOwner(other.gameObject);
        if ((colliderOwner == Networking.LocalPlayer && other.gameObject.layer != 11) || excludeLocalPly) { return; }
        ReceiveHit(colliderOwner);
    }

    private void Update()
    {
        // [OBSOLETE] To-do: transfer all hurtbox behavior to a child template object, which can be either instantiated (risky, network heavy) or handled as an array of preset (maybe up to 20 per player?)
        // [OBSOLETE] To-do: create OnPlayerCollisionEnter that triggers on local player to: (1) identify the owner's playerdata attack, (2) apply force with scalar attack - defense, (3) increase own damage, (4) recalculate defense, (5) send networked event to update all playerdata values[?]
        if (hurtboxState == 1 && hurtboxTransitionTimer < hurtboxTransitionDuration)
        {
            hurtboxTransitionTimer += Time.deltaTime;
            hurtboxSizeLerp = hurtboxSize * (hurtboxTransitionTimer / hurtboxTransitionDuration);
            transform.localScale = new Vector3(hurtboxSizeLerp, hurtboxSizeLerp, hurtboxSizeLerp);
        }
        else if (hurtboxState == 1 && hurtboxTransitionTimer >= hurtboxTransitionDuration)
        {
            hurtboxLingerTimer = 0;
            hurtboxState = 2;
        }
        else if (hurtboxState == 2 && hurtboxLingerTimer < hurtboxLingerDuration)
        {
            hurtboxLingerTimer += Time.deltaTime;
        }
        else if (hurtboxState == 2 && hurtboxLingerTimer >= hurtboxLingerDuration)
        {
            transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
            hurtboxState = 3;
            Destroy(gameObject);
            //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartDestroy");
        }
    }
}
