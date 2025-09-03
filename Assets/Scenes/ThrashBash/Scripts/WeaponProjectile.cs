
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
    [NonSerialized] public float owner_scale;
    [NonSerialized] public bool keep_parent;
    [NonSerialized] public Vector3 pos_start;
    [NonSerialized] public float projectile_distance;
    [NonSerialized] public float projectile_duration;
    [NonSerialized] public double projectile_start_ms;
    [NonSerialized] private double projectile_timer_local = 0.0f;
    [NonSerialized] private double projectile_timer_network = 0.0f;
    [SerializeField] public GameObject template_WeaponHurtbox;
    [SerializeField] public GameController gameController;
    [NonSerialized] public GameObject weapon_parent;
    [NonSerialized] public float actual_distance;
    [NonSerialized] public Vector3 previousPosition;
    [NonSerialized] private Rigidbody rb;
    [NonSerialized] private bool projectile_hit_on_this_frame = false;

    private void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        previousPosition = rb.position;
    }

    private float CalcDistanceAtTime(double time_elapsed)
    {
        return projectile_distance * (float)(time_elapsed / projectile_duration);
    }

    private Vector3 CalcPosAtTime(double time_elapsed)
    {
        var outPos = rb.position;
        switch (projectile_type)
        {
            case (int)projectile_type_name.Bullet:
                if (keep_parent && weapon_parent != null) { pos_start = weapon_parent.transform.position; }
                outPos = pos_start + (transform.right * CalcDistanceAtTime(time_elapsed));
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
    }

    private void FixedUpdate()
    {
        
        // Before anything else, if we have a parent, make sure we're positioned and rotated with the parent properly
        if (keep_parent && weapon_parent != null)
        {
            rb.MoveRotation(weapon_parent.transform.rotation);
            rb.MovePosition(weapon_parent.transform.position + (weapon_parent.transform.right * CalcDistanceAtTime(projectile_timer_network)));
        }

        // Then we calculate the next position, either based on lerp from the distance or raycast if there's an obstacle between our current and next position
        var lerpPos = Vector3.Lerp(rb.position, CalcPosAtTime(projectile_timer_network), Mathf.Min(1.0f,(float)(projectile_timer_network / projectile_duration)));
        var rayPos = RaycastToNextPos(projectile_timer_network);
        if (rayPos.x != -999999 && !projectile_hit_on_this_frame) 
        { 
            rb.MovePosition(rayPos);
            projectile_hit_on_this_frame = true;
            OnProjectileHit(rayPos, true);
            //Debug.Log("[PROJECTILE] Create projectile at RayPos!");
        }
        else 
        { 
            rb.MovePosition(lerpPos);
            if (!projectile_hit_on_this_frame && (projectile_timer_local >= projectile_duration || projectile_timer_network >= projectile_duration))
            {
                projectile_hit_on_this_frame = true;
                OnProjectileHit(CalcPosAtTime(projectile_duration), false);
                //Debug.Log("[PROJECTILE] Create projectile because of lifetime!");
            }
        }
    }

    public void OnProjectileHit(Vector3 position, bool contact_made)
    {
        //NetworkCreateHurtBox(Vector3 position, float damage, double start_ms, int player_id, int weapon_type)
        actual_distance = Vector3.Distance(pos_start, this.GetComponent<Rigidbody>().position);
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
                , plyAttr.ply_scale
                , owner_id
                , weapon_type
                , actual_distance
                );
        
            // Play the striking sound, if applicable
            if (weapon_parent != null && weapon_parent.GetComponent<PlayerWeapon>() != null && contact_made && owner_id == Networking.LocalPlayer.playerId)
            {
                var plyWeapon = weapon_parent.GetComponent<PlayerWeapon>();
                gameController.PlaySFXFromArray(
                    plyWeapon.snd_source_weaponcontact, plyWeapon.snd_game_sfx_clips_weaponcontact, weapon_type
                );
                /*
                switch (weapon_type)
                {
                    case (int)weapon_type_name.Bomb:
                        gameController.PlaySFXFromArray(
                        weapon_parent.GetComponent<PlayerWeapon>().weapon_snd_source, gameController.snd_game_sfx_clips[(int)game_sfx_name.WeaponFire], weapon_type
                        );
                        break;
                    case (int)weapon_type_name.Rocket:
                        gameController.PlaySFXFromArray(
                        weapon_parent.GetComponent<PlayerWeapon>().weapon_snd_source, gameController.snd_game_sfx_clips[(int)game_sfx_name.WeaponFire], weapon_type
                        );
                        break;
                    default:
                        break;
                }*/
            }
        }

        Destroy(gameObject);
    }

    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy || projectile_hit_on_this_frame) { return false; }
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

    private void OnTriggerEnter(Collider other)
    {
        if (CheckCollider(other))
        {
            projectile_hit_on_this_frame = true;
            OnProjectileHit(rb.position, true);
            //Debug.Log("[PROJECTILE] Create projectile at TriggerEnter!");
        }
    }

    private void OnCollisionEnter(UnityEngine.Collision collision)
    {
        if (CheckCollider(collision.collider))
        {
            projectile_hit_on_this_frame = true;
            OnProjectileHit(rb.position, true);
            //Debug.Log("[PROJECTILE] Create projectile at CollisionEter!");
        }
    }

    private Vector3 RaycastToNextPos(double time_elapsed)
    {
        var layers_to_hit = LayerMask.GetMask("Player", "PlayerLocal", "PlayerHitbox", "Environment");
        var ray_cast = Physics.Linecast(previousPosition, CalcPosAtTime(projectile_timer_network), out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
        //UnityEngine.Debug.DrawLine(previousPosition, CalcPosAtTime(projectile_timer_network), Color.red, 0.1f);
        previousPosition = rb.position;
        // Since vectors are non-nullable, let's just return something impossible
        if (!ray_cast || hitInfo.collider == null || !CheckCollider(hitInfo.collider)) { return new Vector3(-999999, -999999, -999999); }
        UnityEngine.Debug.Log("Raycast found something! " + hitInfo.point.ToString() + ": " + hitInfo.collider.gameObject.name);
        return hitInfo.point;
    }
}
