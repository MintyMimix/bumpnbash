
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum hitbox_mat_name
{
    Default, Respawning, Invisible, ENUM_LENGTH
}

public class PlayerHitbox : UdonSharpBehaviour
{
    //public int owner_id;
    [SerializeField] public Material[] hitboxMats;
    [SerializeField] public float default_hitbox_size = 2.0f;

    [NonSerialized] public int material_id;
    [NonSerialized] public VRCPlayerApi owner;
    [NonSerialized] public PlayerAttributes playerAttributes;
    //[NonSerialized] private Rigidbody rb;

    private void Start()
    {
        //rb = this.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (playerAttributes != null && owner != null)
        {
            var scaleHitbox = playerAttributes.ply_scale;
            transform.localScale = new Vector3(scaleHitbox, default_hitbox_size * scaleHitbox, scaleHitbox);
            transform.SetPositionAndRotation(owner.GetPosition() + new Vector3(0.0f, scaleHitbox / default_hitbox_size, 0.0f), owner.GetRotation());
            var m_Renderer = GetComponent<Renderer>();
            if (m_Renderer != null && playerAttributes.gameController.team_colors != null && playerAttributes.ply_team >= 0)
            {
                byte alpha = 255;
                if (material_id == 1) { alpha = 90; }
                else if (material_id == 2) { alpha = 0; }
                if (playerAttributes.gameController.option_teamplay)
                {
                    m_Renderer.material.SetColor("_Color",
                        new Color32(
                        (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].r),
                        (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].g),
                        (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].b),
                        alpha));
                    m_Renderer.material.EnableKeyword("_EMISSION");
                    m_Renderer.material.SetColor("_EmissionColor",
                        new Color32(
                        (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].r),
                        (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].g),
                        (byte)Mathf.Min(255, 80 + playerAttributes.gameController.team_colors[playerAttributes.ply_team].b),
                        255));
                }
                else
                {
                    m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, alpha));
                    m_Renderer.material.EnableKeyword("_EMISSION");
                    m_Renderer.material.SetColor("_EmissionColor", new Color32(180, 180, 180, 255));
                }
            }
        }
    }

    public void Update()
    {
        //rb.AddForce(Vector3.zero); // Add an ever so slight force to the rigidbody just so it gets registered by hurtboxes even when standing still
        if (playerAttributes != null) {
            if (owner == Networking.LocalPlayer && material_id != (int)hitbox_mat_name.Invisible) { SetMaterial((int)hitbox_mat_name.Invisible); }
            else if (owner == Networking.LocalPlayer && gameObject.layer != LayerMask.NameToLayer("LocalPlayerHitbox")) { gameObject.layer = LayerMask.NameToLayer("LocalPlayerHitbox"); }
            else if (owner != Networking.LocalPlayer && playerAttributes.ply_state == (int)player_state_name.Respawning && material_id != (int)hitbox_mat_name.Respawning)
            {
                SetMaterial((int)hitbox_mat_name.Respawning);
            }
            // Could be more sophiscated, such as flashing the material whenever they're hit
            else if (owner != Networking.LocalPlayer && playerAttributes.ply_state != (int)player_state_name.Respawning && material_id != (int)hitbox_mat_name.Default)
            {
                SetMaterial((int)hitbox_mat_name.Default); 
            }
        }
    }

    public void SetMaterial(int index)
    {
        var materialsCopy = gameObject.GetComponent<MeshRenderer>().materials;
        materialsCopy[0] = hitboxMats[index];
        gameObject.GetComponent<MeshRenderer>().materials = materialsCopy;
        material_id = index;
    }

    /*private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<WeaponProjectile>() != null)
        {
            var projectile = other.gameObject.GetComponent<WeaponProjectile>();
            if (projectile.owner_id != Networking.GetOwner(gameObject).playerId)
            {
                projectile.OnProjectileHit(projectile.transform.position);
            }
        }
    }*/

}
