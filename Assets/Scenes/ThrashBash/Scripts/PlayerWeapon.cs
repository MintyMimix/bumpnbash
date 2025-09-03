
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDK3.Components;

public class PlayerWeapon : UdonSharpBehaviour
{
    public float use_cooldown;
    private float use_timer;
    private bool use_ready;
    [SerializeField] public GameObject template_WeaponProjectile;
    [SerializeField] public GameObject template_WeaponHurtbox;


    void Start()
    {
        
    }

    private void Update()
    {
        // Make sure you can only fire when off cooldown
        if (use_timer < use_cooldown)
        {
            use_timer += Time.deltaTime;
            use_ready = false;
        }
        else
        {
            use_ready = true;
        }

        /*if (!gameObject.GetComponent<VRC_Pickup>().IsHeld) {
            transform.SetPositionAndRotation(
                    Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward) + new Vector3(0.0f, -0.5f, 0.0f)
                    , Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation
                );
        }*/

        if (!transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) == Networking.LocalPlayer) {
            transform.GetComponent<VRCPickup>().pickupable = true;
        }
        else if (transform.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) != Networking.LocalPlayer)
        {
            transform.GetComponent<VRCPickup>().pickupable = false;
        }
    }

    public override void OnPickupUseUp()
    {
        if (!use_ready) { return; }
        use_ready = false;
        use_timer = 0;
        if (transform.GetComponent<VRCPickup>().currentPlayer == Networking.LocalPlayer)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkCreateProjectile", transform.position, Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation, 0.004f, Networking.LocalPlayer.playerId);
        }
    }

    [NetworkCallable]
    public void NetworkCreateProjectile(Vector3 fire_start_pos, Quaternion fire_angle, float fire_speed, int player_id)
    {
        var newProjectileObj = Instantiate(template_WeaponProjectile, transform);

        newProjectileObj.transform.parent = null;
        //if (Networking.GetOwner(newProjectileObj) == Networking.LocalPlayer)
        //{
        //    Networking.SetOwner(VRCPlayerApi.GetPlayerById(player_id), newProjectileObj);
        //}
        var projectile = newProjectileObj.GetComponent<WeaponProjectile>();

        // Set velocity, size, etc. of projectile here
        //if (weaponType == 0)
        //{
        newProjectileObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newProjectileObj.transform.SetPositionAndRotation(fire_start_pos, fire_angle);
        projectile.projectile_state = (int)projectile_state_name.Active;
        projectile.projectile_lifetime = 10.0f;
        projectile.pos_start = fire_start_pos;
        projectile.projectile_speed = fire_speed;
        projectile.owner_id = player_id;
        projectile.template_WeaponHurtbox = template_WeaponHurtbox;

        // if (WeaponType = ...) {}

        // placeholder below
        //weaponHurtbox.hurtboxTransitionTimer = 0;
        //weaponHurtbox.hurtboxState = 1;

    }
}
