
using UdonSharp;
using UnityEngine;
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
    public int hurtbox_state;
    public float hurtbox_lifetime, hurtbox_timer;
    public float hurtbox_damage;
    public int owner_id;
    public int[] players_struck;
    public GameController gameController;

    void Start()
    {
        
    }

    private void Update()
    {
        if (hurtbox_state == (int)hurtbox_state_name.Active && hurtbox_timer < hurtbox_lifetime)
        {
            hurtbox_timer += Time.deltaTime;
        }
        else if (hurtbox_state == (int)hurtbox_state_name.Active && hurtbox_timer >= hurtbox_lifetime)
        {
            hurtbox_state = (int)hurtbox_state_name.Waiting;
            if (Networking.GetOwner(gameObject) == Networking.LocalPlayer)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DestroyHurtbox");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Run this only if we are the owner of the hurtbox
        if (VRCPlayerApi.GetPlayerById(owner_id) != Networking.LocalPlayer) { return; }
        var plyHitbox = other.gameObject.GetComponent<PlayerHitbox>();
        if (plyHitbox == null) { return; }
        var colliderOwner = VRCPlayerApi.GetPlayerById(plyHitbox.owner_id);
        var players_ext = new int[players_struck.Length + 1];
        for (int i = 0; i < players_struck.Length; i++)
        {
            players_ext[i] = players_struck[i];
            if (players_ext[i] == colliderOwner.playerId) { return; }
        }
        players_ext[players_struck.Length] = colliderOwner.playerId;
        var player_attributes = gameController.FindPlayerAttributes(colliderOwner);
        var force_dir = Vector3.Normalize(colliderOwner.GetPosition() - transform.position);
        force_dir = new Vector3(force_dir.x, 0, force_dir.z);
        player_attributes.SendCustomNetworkEvent(NetworkEventTarget.Owner, "ReceiveDamage", hurtbox_damage, force_dir, owner_id);
        UnityEngine.Debug.Log("PLAYER STRUCK: " + colliderOwner.displayName + " BY " + gameObject.name);

    }

    // Only the owner can call this
    [NetworkCallable]
    public void DestroyHurtbox()
    {
        Destroy(gameObject);
    }
}
