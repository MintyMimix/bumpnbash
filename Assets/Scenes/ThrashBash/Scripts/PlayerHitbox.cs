
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

public enum hitbox_mat_name
{
    Default = 0, Respawning = 1, Invisible = 2
}

public class PlayerHitbox : UdonSharpBehaviour
{
    //public int owner_id;
    private VRCPlayerApi owner_ply;
    public Material[] hitboxMats;
    public int material_id;
    public PlayerAttributes playerAttributes;

    private void FixedUpdate()
    {
        owner_ply = Networking.GetOwner(gameObject);
        var scaleHitbox = 2.0f * (owner_ply.GetAvatarEyeHeightAsMeters() / 1.6f);
        transform.localScale = new Vector3(1.0f, scaleHitbox, 1.0f);
        transform.SetPositionAndRotation(owner_ply.GetPosition() + new Vector3(0.0f, scaleHitbox / 2.0f, 0.0f), owner_ply.GetRotation());
    }

    public void Update()
    {
        if (playerAttributes != null) { 
            if (playerAttributes.ply_state == (int)player_state_name.Respawning && material_id != (int)hitbox_mat_name.Respawning)
            {
                SetMaterial((int)hitbox_mat_name.Respawning);
            }
            else if (playerAttributes.ply_state != (int)player_state_name.Respawning && material_id == (int)hitbox_mat_name.Respawning)
            {
                if (owner_ply == Networking.LocalPlayer) { SetMaterial((int)hitbox_mat_name.Invisible); }
                else { SetMaterial((int)hitbox_mat_name.Default); }
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
