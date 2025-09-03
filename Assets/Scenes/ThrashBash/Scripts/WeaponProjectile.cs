
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum projectile_type_name
{
    Bullet, ArcDown, ArcUp, ENUM_LENGTH
}

public class WeaponProjectile : UdonSharpBehaviour
{

    [NonSerialized] public int projectile_type, weapon_type;
    [NonSerialized] public int owner_id;
    [NonSerialized] public bool keep_parent;
    [NonSerialized] public Vector3 pos_start;
    [NonSerialized] public float projectile_distance;
    [NonSerialized] public float projectile_duration;
    [NonSerialized] public double projectile_start_ms;
    [NonSerialized] private double projectile_timer_local = 0.0f;
    [NonSerialized] private double projectile_timer_network = 0.0f;
    [SerializeField] public GameObject template_WeaponHurtbox;
    [SerializeField] public GameController gameController;
    [NonSerialized] private GameObject weapon_parent;

    //To-do: when updating position, perform a ray trace to see if any objects in the Player, PlayerLocal, or PlayerHitbox layers are between current position and current position + speed; if so, make the next position that instead
    void OnEnable()
    {
        //layers_to_hit = LayerMask.GetMask("Player", "PlayerLocal", "PlayerHitbox");
        //Debug.Log("TIME LEFT IN PROJECTILE: " + (1 - (float)(projectile_timer_network / projectile_duration)).ToString());
        //Debug.Log("START POS: " + transform.position.ToString() + "; END POS: " + CalcPosAtTime(projectile_duration).ToString() + "; DISTANCE: " + projectile_distance.ToString());

        // This code is not scalable; run only once
        if (keep_parent)
        {
            weapon_parent = gameController.FindPlayerOwnedObject(VRCPlayerApi.GetPlayerById(owner_id), "PlayerWeapon");
            if (weapon_parent != null) {
                pos_start = weapon_parent.transform.position;
            }
        }
    }

    private Vector3 CalcPosAtTime(double time_elapsed)
    {
        var outPos = transform.position;
        switch (projectile_type)
        {
            case (int)projectile_type_name.Bullet:
                outPos = pos_start + (transform.right * (projectile_distance * (float)(time_elapsed / projectile_duration)));
                if (keep_parent && weapon_parent) { outPos = pos_start + (weapon_parent.transform.right * (projectile_distance * (float)(time_elapsed / projectile_duration))); }
                break;
            default:
                break;
        }
        return outPos;
    }

    private void Update()
    {
        projectile_timer_local += Time.deltaTime;
        projectile_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), projectile_start_ms);
        if (projectile_timer_local >= projectile_duration || projectile_timer_network >= projectile_duration)
        {
            OnProjectileHit(transform.position);
        }
    }

    private void FixedUpdate()
    {
        var rb = this.GetComponent<Rigidbody>();

        var lerpPos = Vector3.Lerp(transform.position, CalcPosAtTime(projectile_timer_network), (float)(projectile_timer_network / projectile_duration));
        //var lerpPos = CalcPosAtTime(projectile_timer_network);
        //Debug.Log(lerpPos.ToString() + "; " + projectile_timer_network.ToString() + " seconds; " + ((float)(projectile_timer_network / projectile_duration)).ToString() + "%" );
        rb.MovePosition(lerpPos);
    }

    public void OnProjectileHit(Vector3 position)
    {
        // To-do: create hurtbox based on weapon_type
        //NetworkCreateHurtBox(Vector3 position, float damage, double start_ms, int player_id, int weapon_type)
        if (owner_id == Networking.LocalPlayer.playerId)
        {
            var plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
            var damage = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Damage];
            damage *= plyAttr.ply_atk * (plyAttr.ply_scale * gameController.scale_damage_factor);
            gameController.SendCustomNetworkEvent(
                VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateHurtBox"
                , position
                , damage
                , Networking.GetServerTimeInSeconds()
                , keep_parent
                , owner_id
                , weapon_type
                );
        }
        // Play the striking sound, if applicable
        switch (weapon_type)
        {
            case (int)weapon_type_name.Bomb:
                gameController.PlaySFXFromArray(
                gameController.snd_game_sfx_sources[(int)game_sfx_index.WeaponFire], gameController.snd_game_sfx_clips[(int)game_sfx_index.WeaponFire], weapon_type
                );
                break;
            case (int)weapon_type_name.Rocket:
                gameController.PlaySFXFromArray(
                gameController.snd_game_sfx_sources[(int)game_sfx_index.WeaponFire], gameController.snd_game_sfx_clips[(int)game_sfx_index.WeaponFire], weapon_type
                );
                break;
            default:
                break;
        }

        Destroy(gameObject);
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
}
