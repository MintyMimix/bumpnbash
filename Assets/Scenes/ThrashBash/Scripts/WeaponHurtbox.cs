
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
    Strike, ForceExplosion, Kapow, ENUM_LENGTH
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

    private void Start()
    {
        players_struck = new int[0];
    }

    private void OnEnable()
    {
        if (weapon_parent != null) { local_offset = Vector3.Distance(transform.position, weapon_parent.transform.position); }
    }

    private void FixedUpdate()
    {
        if (weapon_parent != null) {
            var layers_to_hit = LayerMask.GetMask("Player", "PlayerLocal", "PlayerHitbox", "Environment");
            Vector3 next_pos = weapon_parent.transform.position + (weapon_parent.transform.right * local_offset);
            var ray_cast = Physics.Linecast(weapon_parent.transform.position, next_pos, out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
            if (hitInfo.collider != null) { transform.position = hitInfo.point; }
            else { transform.position = next_pos; }
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


        var playerAttributes = gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(owner_id));
        var m_Renderer = GetComponent<Renderer>();
        if (playerAttributes != null && m_Renderer != null && playerAttributes.gameController.team_colors != null && playerAttributes.ply_team >= 0)
        {
            if (playerAttributes.gameController.option_teamplay)
            {
                m_Renderer.material.SetColor("_Color",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].b)),
                    (byte)0));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].b)),
                    (byte)playerAttributes.gameController.team_colors[playerAttributes.ply_team].a));
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
    private void OnTriggerStay(Collider other)
    {
        if (other == null) { return; }
        // Run this only if we are the owner of the hurtbox
        if (owner_id != Networking.LocalPlayer.playerId) { return; }
        // And that we're not colliding with our own hurtbox
        var plyHitbox = other.gameObject.GetComponent<PlayerHitbox>();
        if (plyHitbox == null) { return; }
        var colliderOwner = Networking.GetOwner(plyHitbox.gameObject);
        if (colliderOwner.playerId == Networking.LocalPlayer.playerId) { return; }

        // Then, add the player struck to the exclusion list
        var players_ext = new int[players_struck.Length + 1];
        for (int i = 0; i < players_struck.Length; i++)
        {
            players_ext[i] = players_struck[i];
            if (players_ext[i] == colliderOwner.playerId) { return; }
        }
        players_ext[players_struck.Length] = colliderOwner.playerId;
        players_struck = players_ext;

        // Finally, calculate the force direction and tell the player they've been hit
        var force_dir = Vector3.Normalize(colliderOwner.GetPosition() - transform.position);
        force_dir = new Vector3(force_dir.x, 0, force_dir.z);

        // Check teams as well
        if (
            gameController.GetGlobalTeam(colliderOwner.playerId)
            == gameController.GetGlobalTeam(owner_id)
            && gameController.option_teamplay
            ) { return; }

        gameController.FindPlayerAttributes(colliderOwner).SendCustomNetworkEvent(NetworkEventTarget.Owner, "ReceiveDamage", hurtbox_damage, force_dir, owner_id, damage_type);

    }

}
