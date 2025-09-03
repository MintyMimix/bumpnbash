
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

public class PlayerHitbox : UdonSharpBehaviour
{
    public int owner_id;
    private VRCPlayerApi owner_ply;

    void Start()
    {
        
    }

    private void Update()
    {
        if (owner_id > 0)
        {
            owner_ply = VRCPlayerApi.GetPlayerById(owner_id);
            var scaleHitbox = 2.0f * (owner_ply.GetAvatarEyeHeightAsMeters() / 1.6f);
            transform.localScale = new Vector3(1.0f, scaleHitbox, 1.0f);
            transform.SetPositionAndRotation(owner_ply.GetPosition() + new Vector3(0.0f, scaleHitbox / 2.0f, 0.0f), owner_ply.GetRotation());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<WeaponProjectile>() != null)
        {
            var projectile = other.gameObject.GetComponent<WeaponProjectile>();
            if (projectile.owner_id != owner_id)
            {
                projectile.OnProjectileHit(projectile.transform.position);
            }
        }
    }

}
