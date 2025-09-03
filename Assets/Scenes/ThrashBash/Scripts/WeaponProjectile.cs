
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDKBase;
using VRC.Udon;

public class WeaponProjectile : UdonSharpBehaviour
{
    public int projectileID, projectileType, projectileState;
    public float projectileLifetime, projectileTimer;
    //public Vector3 initDirection;
    public GameObject weaponHurtboxTemplate;
    public GameHandler gameHandler;
    private Rigidbody physbody;
    public WeaponBase parentWeaponBase;
    //public WeaponHurtbox[] weaponHurtboxes;

    // To-Do: Right now, weapon projectile is created and moves based on a desynced player object, which results in hurtboxes that don't align.
    // To resolve this, we can either: (A) have hurtboxes be networked objects of a transmitted position, or (B) have the projectile have a source & destination position (or direction) passed by network
    // (A) is cheaper on resources, while (B) ensures alignment between the projectile's appearance on all clients
    // The most expensive but synced way would be doing both

    void Start()
    {
        physbody = transform.GetComponent<Rigidbody>();
        transform.parent = null;
    }

    private void Update()
    {
        // Handle life timer
        if (projectileState == 1 && projectileTimer < projectileLifetime)
        {
            projectileTimer += Time.deltaTime;
        }
        else if (projectileState == 1 && projectileTimer >= projectileLifetime) 
        {
            // Destroy projectile
            projectileState = 2;
            DetonateProjectileNoContact();
        }

        // Handle movement behavior, as applicable
        if (projectileState == 1 && projectileType == 0) {
            physbody.MovePosition(physbody.position + transform.up * 0.3f);
        }
    }

    public void CreateHurtbox(Vector3 createPosition, Quaternion createRotation)
    {
        projectileState = 2;
        var hurtboxObj = Instantiate(weaponHurtboxTemplate, createPosition, createRotation);
        hurtboxObj.name = "Hurtbox[" + projectileID + "](" + Networking.GetOwner(gameObject).displayName + ")";
        Networking.SetOwner(Networking.GetOwner(gameObject), hurtboxObj);
        SetHurtboxDamage(hurtboxObj);
        parentWeaponBase.DestroyProjectile(projectileID);
    }
        
    public void SetHurtboxDamage(GameObject hurtboxObjToSet)
    {
        var hurtbox = hurtboxObjToSet.GetComponent<WeaponHurtbox>();
        switch (projectileType)
        {
            case 5:
                hurtbox.hurtboxDamage = 100.0f;
                break;
            // Put in exception cases above
            default:
                hurtbox.hurtboxDamage = 10.0f;
                hurtbox.hurtboxTransitionDuration = 0.1f;
                hurtbox.hurtboxLingerDuration = 0.5f;
                hurtbox.hurtboxSize = 1.0f;
                break;
        }
        hurtbox.gameHandler = gameHandler;
        hurtbox.hurtboxState = 1;
        hurtbox.hurtboxID = projectileID;
        hurtboxObjToSet.SetActive(true);
    }

    // Potential flaw: relies on two collisions: one where a player enters the projectile (relative to the attacker), and one where a player enters the hurtbox (relative to the defender)
    // To-do: either have the hurtbox be immediately passed into as damaging X player, or make this local player only, or both
    // >>>Alternatively: have these functions all merged into a single "create hurtbox", as a networked event, with a defined position and rotation passed into as arguments
    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
    {
        // We don't want it to collide with ourselves!
        if (player == Networking.LocalPlayer) { return; }
        CreateHurtbox(player.GetPosition(), player.GetRotation());
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        // We don't want it to collide with ourselves!
        if (player == Networking.LocalPlayer) { return; }
        CreateHurtbox(player.GetPosition(), player.GetRotation());
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 position = contact.point;
        CreateHurtbox(position, rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        var colliderOwner = Networking.GetOwner(other.gameObject);
        Debug.Log("PROJECTILE " + gameObject.name + " COLLIDED WITH " + other.gameObject.name + " OWNED BY " + colliderOwner.displayName);
        if (colliderOwner == Networking.LocalPlayer && other.gameObject.layer != 11) { return; }
        CreateHurtbox(transform.position, transform.rotation);
    }

    private void DetonateProjectileNoContact()
    {
        CreateHurtbox(transform.position, transform.rotation);
    }

    private void HandleMovementRocket()
    {

    }

    private void HandleMovementBomb()
    {

    }

    /*
    public void CreateHurtBox()
    {
        // Create a new array of hurtboxes with length + 1, fill in entries from old array, then last entry contains the new object
        var mergedHurtboxes = new WeaponHurtbox[weaponHurtboxes.Length + 1];
        for (int i = 0; i < weaponHurtboxes.Length; i++)
        {
            mergedHurtboxes[i] = weaponHurtboxes[i];
        }
        var newhurtboxObj = Instantiate(weaponHurtboxTemplate, transform);
        mergedHurtboxes[weaponHurtboxes.Length] = newhurtboxObj.GetComponent<WeaponHurtbox>();
        Networking.SetOwner(Networking.GetOwner(gameObject), newhurtboxObj);
        newhurtboxObj.GetComponent<WeaponHurtbox>().hurtboxID = weaponHurtboxes.Length;
        weaponHurtboxes = mergedHurtboxes;
    }

    public void DestroyHurtbox(int hurtboxID)
    {
        var reducedHurtboxes = new WeaponHurtbox[weaponHurtboxes.Length - 1];
        for (int i = 0; i < weaponHurtboxes.Length; i++)
        {
            if (i == hurtboxID) { continue; }
            else if (i < hurtboxID)
            {
                reducedHurtboxes[i] = weaponHurtboxes[i];
            }
            else
            {
                reducedHurtboxes[i - 1] = weaponHurtboxes[i];
            }
        }
        weaponHurtboxes[hurtboxID].EndDestroy();
    }
    */

}
