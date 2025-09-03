
using UdonSharp;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;

public class WeaponBase : UdonSharpBehaviour
{
    public int weaponType; // enum int?
    public float useTimer, useCooldown;
    public bool useReady;
    public GameObject weaponProjectileTemplate, weaponHurtboxTemplate;
    public WeaponProjectile[] weaponProjectiles;
    public int[] weaponProjectileIDs;
    public float shotRange;
    public GameHandler gameHandler;
    public PlayerHandler localPlayerHandler;

    //public GameObject weaponHurtbox; //Transform, Collider

    // Huge note: if instance master, they own everything, including the world. This makes for desync for the master

    void Start()
    {
        //weaponHurtboxTemplate = transform.GetChild(2).gameObject;
        //weaponHurtbox.GetComponent<Transform>().localScale = new Vector3(0.0f, 0.0f, 0.0f);
    }

    public override void OnPickup()
    {
        //Networking.SetOwner(Networking.LocalPlayer, gameObject);

    }

    private void Update()
    {
        // Make sure you can only fire when off cooldown
        if (useTimer < useCooldown) 
        { 
            useTimer += Time.deltaTime;
            useReady = false;
        }
        else if (gameHandler.roundState == 2)
        {
            useReady = true;
        }
        else if (gameHandler.roundState != 2)
        {
            useReady = false;
        }

        if (gameHandler.roundState == 2 && !gameObject.GetComponent<VRCPickup>().IsHeld && Networking.GetOwner(gameObject) == Networking.LocalPlayer && (localPlayerHandler.localPlayerState == 1 || localPlayerHandler.localPlayerState == 2) )
        {
            transform.SetPositionAndRotation(
                Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward)
                , Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation
            );
        }

        // Ensure this can only be picked up by the owner
        if (!gameObject.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) == Networking.LocalPlayer)
        {
            gameObject.GetComponent<VRCPickup>().pickupable = true;
        }
        else if (gameObject.GetComponent<VRCPickup>().pickupable && Networking.GetOwner(gameObject) != Networking.LocalPlayer)
        {
            gameObject.GetComponent<VRCPickup>().pickupable = false;
        }
    }

    public int[] getMaxInArray(int[] inArray) {
        if (inArray == null || inArray.Length < 1) {
            var nullArr = new int[2];
            nullArr[0] = 0;
            nullArr[1] = -1;
            return nullArr;
        }
        //int? maxVal = null; //nullable so this works even if you have all super-low negatives
        int maxVal = inArray[0];
        int index = 0;
        for (int i = 0; i < inArray.Length; i++)
        {
            int thisNum = inArray[i];
            if (thisNum > maxVal)
            {
                maxVal = thisNum;
                index = i;
            }
        }
        var outArr = new int[2];
        outArr[0] = maxVal;
        outArr[1] = index;
        return outArr;
    }

    // To-Do: Replace with Interact for VR Mode, keep as-is for Desktop mode
    public override void OnPickupUseUp()
    {
        if (!useReady) { return; }
        useReady = false;
        useTimer = 0;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkFireProjectile", transform.position, Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation, Networking.LocalPlayer.playerId);
    }

    public void DestroyProjectile(int projectileID)
    {
        // We shouldn't ever have an instance where our array size is 0, but...
        if (weaponProjectiles.Length <= 0) { return; }
        var reducedProjectiles = new WeaponProjectile[weaponProjectiles.Length - 1];
        var reducedProjectileIDs = new int[weaponProjectileIDs.Length - 1];
        var projectileIndexRemove = -1;
        for (int i = 0; i < weaponProjectiles.Length; i++)
        {
            
            if (weaponProjectileIDs[i] == projectileID) { projectileIndexRemove = i; }
            else if (projectileIndexRemove == -1)
            {
                reducedProjectiles[i] = weaponProjectiles[i];
                reducedProjectileIDs[i] = weaponProjectileIDs[i];
            }
            else
            {
                reducedProjectiles[i - 1] = weaponProjectiles[i];
                reducedProjectileIDs[i - 1] = weaponProjectileIDs[i];
            }
        }
        Debug.Log("REMOVE PROJECTILE OF INDEX " + projectileIndexRemove + " WITH VALUE " + weaponProjectileIDs[projectileIndexRemove]);
        Destroy(weaponProjectiles[projectileIndexRemove].gameObject);
        weaponProjectiles = reducedProjectiles;
        weaponProjectileIDs = reducedProjectileIDs;
    }

    [NetworkCallable]
    public void NetworkFireProjectile(Vector3 firePosition, Quaternion fireRotation, int firingPlayerID)
    {
        // Create a new array of projectiles with length + 1, fill in entries from old array, then last entry contains the new object
        var mergedProjectiles = new WeaponProjectile[weaponProjectiles.Length + 1];
        var mergedProjectileIDs = new int[weaponProjectileIDs.Length + 1];
        for (int i = 0; i < weaponProjectiles.Length; i++)
        {
            mergedProjectiles[i] = weaponProjectiles[i];
            mergedProjectileIDs[i] = weaponProjectileIDs[i];
        }

        var newProjectileObj = Instantiate(weaponProjectileTemplate, transform);
        Networking.SetOwner(VRCPlayerApi.GetPlayerById(firingPlayerID), newProjectileObj);
        var projectile = newProjectileObj.GetComponent<WeaponProjectile>();

        mergedProjectiles[weaponProjectiles.Length] = projectile;
        mergedProjectileIDs[weaponProjectileIDs.Length] = getMaxInArray(weaponProjectileIDs)[0] + 1;
        projectile.projectileID = mergedProjectileIDs[weaponProjectileIDs.Length];
        newProjectileObj.name = "Projectile[" + projectile.projectileID + "](" + Networking.GetOwner(gameObject).displayName + ")";

        weaponProjectiles = mergedProjectiles;
        weaponProjectileIDs = mergedProjectileIDs;

        projectile.weaponHurtboxTemplate = weaponHurtboxTemplate;
        projectile.gameHandler = gameHandler;
        projectile.parentWeaponBase = this;
        // Set velocity, size, etc. of projectile here
        if (weaponType == 0)
        {
            projectile.projectileState = 1;
            projectile.projectileType = weaponType;
            projectile.projectileLifetime = 0.1f;
            newProjectileObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            newProjectileObj.GetComponent<Rigidbody>().MovePosition(firePosition);
            newProjectileObj.GetComponent<Rigidbody>().MoveRotation(fireRotation);
        }

        Networking.SetOwner(Networking.GetOwner(gameObject), newProjectileObj);
        newProjectileObj.SetActive(true);
        // if (WeaponType = ...) {}

        // placeholder below
        //weaponHurtbox.hurtboxTransitionTimer = 0;
        //weaponHurtbox.hurtboxState = 1;

    }

}
