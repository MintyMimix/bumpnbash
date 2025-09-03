
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using static VRC.SDKBase.VRCPlayerApi;

public enum hurtbox_state_name
{
    Inactive, Active, Waiting
}

public class WeaponHurtbox : UdonSharpBehaviour
{
    public GameController gameController;
    public int hurtbox_state;
    public float hurtbox_lifetime, hurtbox_timer;
    public float hurtbox_damage;
    public int[] players_struck;
    //public bool struck_local = false;
    public int owner_id;

    private void Update()
    {
        if (hurtbox_state == (int)hurtbox_state_name.Active && hurtbox_timer < hurtbox_lifetime)
        {
            hurtbox_timer += Time.deltaTime;
        }
        else if (hurtbox_state == (int)hurtbox_state_name.Active && hurtbox_timer >= hurtbox_lifetime)
        {
            hurtbox_state = (int)hurtbox_state_name.Waiting;
            gameController.DestroyInstanceWithArray(gameObject.GetInstanceID(), gameController.hurtboxes);
        }

    }

    // Attacker-focused code 
    private void OnTriggerStay(Collider other)
    {
        // Run this only if we are the owner of the hurtbox
        if (owner_id != Networking.LocalPlayer.playerId) { return; }
        var plyHitbox = other.gameObject.GetComponent<PlayerHitbox>();
        if (plyHitbox == null) { return; }
        var colliderOwner = Networking.GetOwner(plyHitbox.gameObject);
        // And that we're not colliding with our own hurtbox
        if (colliderOwner.playerId == Networking.LocalPlayer.playerId) { return;  }
        var players_ext = new int[players_struck.Length + 1];
        for (int i = 0; i < players_struck.Length; i++)
        {
            players_ext[i] = players_struck[i];
            if (players_ext[i] == colliderOwner.playerId) { return; }
        }
        players_ext[players_struck.Length] = colliderOwner.playerId;
        players_struck = players_ext;
        //var player_attributes = gameController.FindPlayerAttributes(colliderOwner);
        var force_dir = Vector3.Normalize(colliderOwner.GetPosition() - transform.position);
        force_dir = new Vector3(force_dir.x, 0, force_dir.z);
        gameController.FindPlayerAttributes(colliderOwner).SendCustomNetworkEvent(NetworkEventTarget.Owner, "ReceiveDamage", hurtbox_damage, force_dir, owner_id);
        //UnityEngine.Debug.Log("PLAYER STRUCK: " + colliderOwner.displayName + " BY " + gameObject.name);

    }

}
