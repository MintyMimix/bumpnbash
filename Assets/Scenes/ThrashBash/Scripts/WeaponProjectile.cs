
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;

public enum projectile_type_name
{
    Bullet, PunchingGlove, Rocket
}

public class WeaponProjectile : UdonSharpBehaviour
{
    
    public Vector3 pos_start;
    public float projectile_speed;
    public float projectile_lifetime;
    private float projectile_timer;
    public int projectile_type;
    public int owner_id;
    public GameObject template_WeaponHurtbox;
    public GameController gameController;

    //To-do: when updating position, perform a ray trace to see if any objects in the Player, PlayerLocal, or PlayerHitbox layers are between current position and current position + speed; if so, make the next position that instead
    void Start()
    {
        //layers_to_hit = LayerMask.GetMask("Player", "PlayerLocal", "PlayerHitbox");
    }

    private void Update()
    {
        if (projectile_timer < projectile_lifetime)
        {
            projectile_timer += Time.deltaTime;
        }
        else if (projectile_timer >= projectile_lifetime)
        {
            if (owner_id == Networking.LocalPlayer.playerId)
            {
                //gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkTest", projectile_id);
                OnProjectileHit(transform.position);
            }
        }
    }

    private void FixedUpdate()
    {
        // To-Do: change behavior based on projectile_type
        var rb = this.GetComponent<Rigidbody>();
        switch (projectile_type)
        {
            default:
                rb.MovePosition(rb.position + transform.forward * projectile_speed);
                break;
        }
    }


    public void OnProjectileHit(Vector3 position)
    {
        if (owner_id != Networking.LocalPlayer.playerId) { return; }
        gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkCreateHurtBox", position, 10.0f, 2.0f, owner_id, gameObject.GetInstanceID());
        //if (owner_id != Networking.LocalPlayer.playerId) { return; }
        //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CreateHurtBox", position, 10.0f, 2.0f, owner_id);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Did we hit a hitbox?
        if (other.gameObject.GetComponent<PlayerHitbox>() != null)
        {
            if (owner_id != Networking.GetOwner(other.gameObject).playerId)
            {
                OnProjectileHit(transform.position);
            }
        }
        // Did we hit the environment?
        else if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            OnProjectileHit(transform.position);
        }

    }

    /*
    [NetworkCallable]
    public void CreateHurtBox(Vector3 position, float damage, float lifetime, int player_id)
    {
        var newHurtboxObj = Instantiate(template_WeaponHurtbox, transform);

        newHurtboxObj.transform.parent = null;
        var hurtbox = newHurtboxObj.GetComponent<WeaponHurtbox>();

        newHurtboxObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        newHurtboxObj.transform.position = position;
        hurtbox.hurtbox_state = (int)hurtbox_state_name.Active;
        hurtbox.hurtbox_damage = damage;
        hurtbox.hurtbox_lifetime = lifetime;
        hurtbox.owner_id = player_id;

        // Set velocity, size, etc. of projectile here
        //if (weaponType == 0)
        //{

        Destroy(gameObject);
     }

    [NetworkCallable]
    public void TestNetwork()
    {
        testNetwork = 1;
    }


    private void Update()
    {
        if (testNetwork == 1) { DebugTxt.text = "Hi, I'm a networked event!";  }
        else if (owner_id > 0 && VRCPlayerApi.GetPlayerById(owner_id) != null)
        {
            DebugTxt.text = projectile_state.ToString() + "\n" + VRCPlayerApi.GetPlayerById(owner_id).displayName + " {" + owner_id.ToString() + "} vs yours {" + Networking.LocalPlayer.playerId.ToString() + "}";
            if (owner_id == Networking.LocalPlayer.playerId) { DebugTxt.text += "\nI will trigger events"; }
        }

        // Update position based on:
        // (1) The lerp between startPos & endPos using projectile_speed, maxing at endPos
        // (2) If any objects are in the Player, PlayerLocal, or PlayerHitbox layers are between current position and current position + speed
        if (projectile_state == (int)projectile_state_name.Active)
        {
            var pos_current = transform.position;
            //var ray_cast = Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, projectile_speed, layers_to_hit, QueryTriggerInteraction.Collide);
            var rb = this.GetComponent<Rigidbody>();
            rb.MovePosition(rb.position + transform.forward * projectile_speed);
        }
    }*/
}
