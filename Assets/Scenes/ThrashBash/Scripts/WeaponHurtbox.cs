
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using static VRC.SDKBase.VRCPlayerApi;

public enum damage_type_name
{
    Strike, ForceExplosion, Kapow, Burn, ENUM_LENGTH
}

public class WeaponHurtbox : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [NonSerialized] public int hurtbox_state;
    [NonSerialized] public float hurtbox_duration;
    [NonSerialized] public double hurtbox_start_ms;
    [NonSerialized] private double hurtbox_timer_local = 0.0f;
    [NonSerialized] private double hurtbox_timer_network = 0.0f;
    [NonSerialized] public float hurtbox_damage;
    [NonSerialized] public int[] players_struck;
    //public bool struck_local = false;
    [NonSerialized] public int owner_id;
    [NonSerialized] public int damage_type;
    [NonSerialized] public GameObject weapon_parent; // WARNING: Only use if keep_parent is on from WeaponProjectile
    [NonSerialized] public float local_offset;
    [NonSerialized] public PlayerAttributes owner_plyAttr;
    [NonSerialized] private Rigidbody rb;
    [NonSerialized] private Rigidbody weapon_rb;

    private void Start()
    {
        players_struck = new int[0];
        rb = gameObject.GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (weapon_parent != null) { weapon_rb = weapon_parent.GetComponent<Rigidbody>(); local_offset = Vector3.Distance(transform.position, weapon_rb.position);  }
        owner_plyAttr = gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(owner_id));
    }

    private void FixedUpdate()
    {
        if (weapon_parent != null) {
            var layers_to_hit = LayerMask.GetMask("PlayerHitbox", "Environment");
            Vector3 next_pos = weapon_rb.position + (weapon_parent.transform.right * local_offset);
            var ray_cast = Physics.Linecast(weapon_rb.position, next_pos, out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
            if (CheckCollider(hitInfo.collider)) { rb.MovePosition(hitInfo.point); }
            else { rb.MovePosition(next_pos); }
        }
    }

    private void Update()
    {
        hurtbox_timer_local += Time.deltaTime;
        hurtbox_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), hurtbox_start_ms);
        if ((hurtbox_timer_local >= hurtbox_duration || (hurtbox_timer_network >= hurtbox_duration && hurtbox_start_ms != 0)) && hurtbox_duration > 0)
        {
            //Debug.Log("HURTBOX DESTROYED BECAUSE ITS DURATION OF " + hurtbox_duration.ToString() + " WAS EXCEEDED BY LOCAL TIME " + hurtbox_timer_local.ToString() + " OR NETWORK TIME " + hurtbox_timer_network.ToString());
            Destroy(gameObject);
        }

        rb.AddForce(Vector3.zero); // Add an ever so slight force to the rigidbody just so it gets registered by the player hitbox trigger 

        var m_Renderer = GetComponent<Renderer>();
        if (owner_plyAttr != null && m_Renderer != null && owner_plyAttr.gameController.team_colors != null && owner_plyAttr.ply_team >= 0)
        {
            if (owner_plyAttr.gameController.option_teamplay)
            {
                m_Renderer.material.SetColor("_Color",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].b)),
                    (byte)0));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].b)),
                    (byte)owner_plyAttr.gameController.team_colors[owner_plyAttr.ply_team].a));
            }
            else
            {
                m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 0));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor", new Color32(83, 83, 83, 255));
            }
        }
    }

    // Attacker-focused code 

    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy) { return false; }
        // Did we hit a hitbox?
        if (other.gameObject.GetComponent<PlayerHitbox>() != null)
        {
            if (owner_id != Networking.GetOwner(other.gameObject).playerId)
            {
                return true;
            }
        }
        // Did we hit the environment?
        else if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            return true;
        }
        return false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null) { return; }
        // Run this only if we are the owner of the hurtbox
        if (owner_id != Networking.LocalPlayer.playerId) { return; }
        // Check that we are colliding with an actual hitbox
        var plyHitbox = other.gameObject.GetComponent<PlayerHitbox>();
        if (plyHitbox == null) { return; }

        //processing_collider = true;

        // And check we're not colliding with our own hurtbox
        var colliderOwner = Networking.GetOwner(plyHitbox.gameObject);
        // Add the player struck to the exclusion list, regardless of if it is our own
        int[] players_ext = new int[players_struck.Length + 1];
        for (int i = 0; i < players_struck.Length; i++)
        {
            if (players_struck[i] == colliderOwner.playerId) { return; }
            players_ext[i] = players_struck[i];
        }
        players_ext[players_struck.Length] = colliderOwner.playerId;
        players_struck = players_ext;
        //Debug.Log("PLAYERS STRUCK: " + gameController.ConvertIntArrayToString(players_ext) + " WHICH DOES NOT INCLUDE " + colliderOwner.playerId);

        //  What if we had rocket jumping punches? (change hit_self = true to return if this breaks something)

        var hit_self = false;
        if (colliderOwner.playerId == Networking.LocalPlayer.playerId) { 
            if (other.gameObject.layer == LayerMask.NameToLayer("Environment")
                && !(gameController.option_gamemode == (int)round_mode_name.BossBash && owner_plyAttr != null && owner_plyAttr.ply_team == 1)) 
            {
                hit_self = true;
            }
            else { return; }
        }

        // Check teams as well
        if (
            gameController.GetGlobalTeam(colliderOwner.playerId)
            == gameController.GetGlobalTeam(owner_id)
            && gameController.option_teamplay
            && !hit_self
            ) { return; }

        // Finally, calculate the force direction and tell the player they've been hit
        var force_dir = Vector3.Normalize(colliderOwner.GetPosition() - rb.position);
        force_dir = new Vector3(force_dir.x, 0, force_dir.z);

        if (!hit_self) { gameController.FindPlayerAttributes(colliderOwner).SendCustomNetworkEvent(NetworkEventTarget.Owner, "ReceiveDamage", hurtbox_damage, force_dir, owner_id, damage_type, false); }
        else
        {
            gameController.local_plyAttr.ReceiveDamage(hurtbox_damage, force_dir, owner_id, damage_type, true);
            var plyWeapon = weapon_parent.GetComponent<PlayerWeapon>();
            gameController.PlaySFXFromArray(
                plyWeapon.snd_source_weaponcontact, plyWeapon.snd_game_sfx_clips_weaponcontact, plyWeapon.weapon_type
            );
        }
        return;
    }

}
