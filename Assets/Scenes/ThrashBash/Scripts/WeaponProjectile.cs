
using System;
using TMPro;
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public enum projectile_type_name
{
    Bullet, ArcDown, ArcUp, Bomb, Laser, ItemProjectile, ENUM_LENGTH
}

public class WeaponProjectile : UdonSharpBehaviour
{

    [NonSerialized] public int projectile_type, weapon_type;
    [NonSerialized] public int owner_id;
    [NonSerialized] public float owner_scale;
    [NonSerialized] public int global_index = -1;
    [NonSerialized] public int ref_index = -1;
    [NonSerialized] public bool keep_parent;
    //[NonSerialized] public int keep_parent_ext;
    [NonSerialized] public Vector3 pos_start;
    [NonSerialized] public float projectile_distance;
    [NonSerialized] public float projectile_duration;
    [NonSerialized] public double projectile_start_ms;
    //[NonSerialized] private double projectile_timer_local = 0.0f;
    [NonSerialized] private double projectile_timer_network = 0.0f;
    [SerializeField] public GameObject template_WeaponHurtbox;
    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject[] projectile_mdl;
    [NonSerialized] public GameObject weapon_parent;
    [NonSerialized] public float actual_distance;
    [NonSerialized] public Vector3 previousPosition;
    [SerializeField] public Rigidbody rb;
    [NonSerialized] private bool projectile_hit_on_this_frame = false;
    [NonSerialized] public Vector3 pos_end;
    [NonSerialized] public bool contact_made = false;
    [NonSerialized] public bool has_physics = false;
    [NonSerialized] public byte extra_data = 0;
    [NonSerialized] public LayerMask layers_to_hit;
    [NonSerialized] private Vector3 INVALID_HIT_POS;
    [NonSerialized] private Animator cached_animator;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        previousPosition = rb.position;
        layers_to_hit = LayerMask.GetMask("Player", "PlayerLocal", "PlayerHitbox", "Environment");
        INVALID_HIT_POS = new Vector3(-999999, -999999, -999999);
    }

    public void ResetProjectile()
    {
        projectile_duration = 0.0f;
        projectile_distance = 0.0f;
        projectile_hit_on_this_frame = false;
        projectile_start_ms = 0.0f;
        projectile_timer_network = 0.0f;
        contact_made = false;
        keep_parent = false;
        has_physics = false;
        transform.parent = null;
        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        transform.SetPositionAndRotation(gameController.template_WeaponProjectile.transform.position, gameController.template_WeaponProjectile.transform.rotation);
        if (rb != null)
        {
            rb.position = gameController.template_WeaponProjectile.transform.position;
            rb.rotation = gameController.template_WeaponProjectile.transform.rotation;
            previousPosition = rb.position;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            GetComponent<Collider>().isTrigger = true;
        }
        else { previousPosition = transform.position; }
        //if (rb == null) { rb = gameObject.GetComponent<Rigidbody>(); }
        //if (rb != null) { previousPosition = rb.position; }
        Start();
    }

    public void UpdateProjectileModel()
    {
        if (weapon_type >= 0 && weapon_type < projectile_mdl.Length && projectile_mdl[weapon_type] != null)
        {
            for (int i = 0; i < projectile_mdl.Length; i++)
            {
                if (i == weapon_type) 
                { 
                    projectile_mdl[i].SetActive(true);
                    // Recolor the model to team
                    if (owner_id >= 0)
                    {
                        int owner_team = gameController.GetGlobalTeam(owner_id);
                        owner_team = Mathf.Max(0, owner_team);
                        //if (owner_team < 0) { continue; }
                        Renderer m_Renderer = projectile_mdl[i].GetComponent<Renderer>();
                        if (m_Renderer != null && gameController.team_colors != null && owner_team >= 0)
                        {
                            Material mat = m_Renderer.materials[0];
                            if (weapon_type == (int)weapon_type_name.Bomb && m_Renderer.materials.Length > 1) { mat = m_Renderer.materials[1]; }
                            if (gameController.option_teamplay)
                            {
                                mat.SetColor("_Color",
                                    new Color32(
                                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_team].r)),
                                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_team].g)),
                                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + gameController.team_colors[owner_team].b)),
                                    (byte)0));
                                mat.EnableKeyword("_EMISSION");
                                mat.SetColor("_EmissionColor",
                                    new Color32(
                                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + gameController.team_colors[owner_team].r)),
                                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + gameController.team_colors[owner_team].g)),
                                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + gameController.team_colors[owner_team].b)),
                                    (byte)gameController.team_colors[owner_team].a));
                            }
                            else
                            {
                                mat.SetColor("_Color", new Color32(255, 255, 255, 0));
                                mat.EnableKeyword("_EMISSION");
                                mat.SetColor("_EmissionColor", new Color32(83, 83, 83, 255));
                            }
                            Transform trail = gameController.GetChildTransformByName(projectile_mdl[i].transform, "Trail");
                            if (trail != null && trail.GetComponent<TrailRenderer>() != null) 
                            {
                                trail.GetComponent<TrailRenderer>().startColor = new Color(mat.GetColor("_Color").r, mat.GetColor("_Color").g, mat.GetColor("_Color").b, 1.0f);
                                trail.GetComponent<TrailRenderer>().endColor = new Color(mat.GetColor("_Color").r, mat.GetColor("_Color").g, mat.GetColor("_Color").b, 1.0f);
                            }
                            Transform particle = gameController.GetChildTransformByName(projectile_mdl[i].transform, "Particle");
                            if (particle != null && particle.GetComponent<ParticleSystem>() != null)
                            {
                                particle.gameObject.SetActive(false);
                                var particle_main = particle.GetComponent<ParticleSystem>().main;
                                particle_main.startColor = new Color(mat.GetColor("_Color").r, mat.GetColor("_Color").g, mat.GetColor("_Color").b, 1.0f);
                                particle_main.duration = projectile_duration;
                                particle_main.playOnAwake = true;
                                if (gameController != null && gameController.local_ppp_options != null)
                                {
                                    var particle_emission = particle.GetComponent<ParticleSystem>().emission;
                                    particle_emission.enabled = gameController.local_ppp_options.particles_on;
                                }
                                particle.gameObject.SetActive(true);
                                particle.GetComponent<ParticleSystem>().Play();
                            }
                        }
                    }
                }
                else { projectile_mdl[i].SetActive(false); }
            }
        }
    }

    private float CalcDistanceAtTime(double time_elapsed)
    {
        return projectile_distance * (float)(time_elapsed / projectile_duration);
    }

    private Vector3 CalcPosAtTime(double time_elapsed)
    {
        Vector3 outPos = rb.position;
        switch (projectile_type)
        {
            case (int)projectile_type_name.Bullet:
                if (keep_parent && weapon_parent != null) { pos_start = weapon_parent.transform.position; }
                outPos = pos_start + (transform.right * CalcDistanceAtTime(time_elapsed));
                break;
            case (int)projectile_type_name.Laser:
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
        if (projectile_duration <= 0) { return; }

        //projectile_timer_local += Time.deltaTime;
        projectile_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), projectile_start_ms);

        if (projectile_hit_on_this_frame && pos_end != null)
        {
            UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " hit a target on this frame at " + pos_end.ToString());
            rb.MovePosition(pos_end);
            OnProjectileHit(pos_end, contact_made);
        }

        if (weapon_type == (int)weapon_type_name.Bomb)
        {
            if (cached_animator == null) { cached_animator = projectile_mdl[weapon_type].GetComponent<Animator>(); }
            if (cached_animator != null)
            {
                cached_animator.SetFloat("AnimationTimer", (float)(projectile_timer_network / projectile_duration));
            }
        }
    }

    private void FixedUpdate()
    {
        if (projectile_duration <= 0) { return; }

        // Before anything else, if we have a parent, make sure we're positioned and rotated with the parent
        if (keep_parent && weapon_parent != null && !projectile_hit_on_this_frame)
        {
            // New behavior: just immediately detonate the projectile and have the hurtbox handle the sizing instead
            rb.MoveRotation(weapon_parent.transform.rotation);
            //rb.MovePosition(weapon_parent.transform.position + (weapon_parent.transform.right * CalcDistanceAtTime(projectile_timer_network)));
            pos_end = weapon_parent.transform.position;
            contact_made = false;
            projectile_hit_on_this_frame = true;
        }

        // If this is not a physics object, we calculate the next position, either based on lerp from the distance or raycast if there's an obstacle between our current and next position
        if (!has_physics) {
            Vector3 lerpPos = Vector3.Lerp(rb.position, CalcPosAtTime(projectile_timer_network), Mathf.Min(1.0f, (float)(projectile_timer_network / projectile_duration)));
            Vector3 rayPos = RaycastToNextPos(projectile_timer_network);
            if (Mathf.FloorToInt(rayPos.x) != Mathf.FloorToInt(INVALID_HIT_POS.x) && !projectile_hit_on_this_frame)
            {
                UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration  " + projectile_duration + " hit target at " + rayPos.ToString() + " which was not invalid: " + rayPos.x + " vs " + INVALID_HIT_POS.x);
                pos_end = rayPos;
                projectile_hit_on_this_frame = true;
                contact_made = true;
                //rb.MovePosition(rayPos);
                //projectile_hit_on_this_frame = true;
                //OnProjectileHit(rayPos, true);
                //Debug.Log("[PROJECTILE] Create projectile at RayPos!");
            }
            else
            {
                rb.MovePosition(lerpPos);
                if (!projectile_hit_on_this_frame && (projectile_timer_network >= projectile_duration))
                {
                    UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " expired due to timer " + projectile_timer_network);
                    pos_end = CalcPosAtTime(projectile_duration);
                    projectile_hit_on_this_frame = true;
                    contact_made = false;
                    //OnProjectileHit(CalcPosAtTime(projectile_duration), false);
                    //Debug.Log("[PROJECTILE] Create projectile because of lifetime!");
                }
            }
        }
        // If this a physics object, all we care about is lifetime
        else
        {
            if (!projectile_hit_on_this_frame && (projectile_timer_network >= projectile_duration))
            {
                UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " expired due to timer " + projectile_timer_network);
                pos_end = rb.position;
                projectile_hit_on_this_frame = true;
                contact_made = false;
            }
        }
    }

    public void OnProjectileHit(Vector3 position, bool is_contact_made)
    {
        //NetworkCreateHurtBox(Vector3 position, float damage, double start_ms, int player_id, int weapon_type)
        //actual_distance = Vector3.Distance(pos_start, this.GetComponent<Rigidbody>().position);
        if (owner_id == Networking.LocalPlayer.playerId)
        {

            PlayerAttributes plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
            float damage = gameController.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Damage];
            damage *= plyAttr.ply_atk * (owner_scale * gameController.scale_damage_factor);
            float dist_scale = 1.0f;
            // For a Laser type projectile, we want to instead position the hurtbox at the midway point between start and current, and scale it according to that distance
            if (projectile_type == (int)projectile_type_name.Laser) 
            {
                if (owner_scale <= 0) 
                {
                    dist_scale = Vector3.Distance(pos_start, position);
                    position = Vector3.Lerp(pos_start, position, 0.5f * owner_scale);
                }
                else 
                {
                    dist_scale = Vector3.Distance(pos_start, position) / owner_scale;
                    position = Vector3.Lerp(pos_start, position, 0.5f); 
                }

            }

            if (!keep_parent) { gameController.local_plyweapon.PlayHapticEvent((int)game_sfx_name.ENUM_LENGTH); } // ENUM_LENGTH is used for weapon fire

            gameController.SendCustomNetworkEvent(
                VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateHurtBox"
                , position
                , pos_start
                , rb.rotation
                , new Vector4(damage, owner_scale, dist_scale, extra_data)
                , keep_parent
                , owner_id
                , weapon_type
                );
        
            // Play the striking sound, if applicable
            if (weapon_parent != null && weapon_parent.GetComponent<PlayerWeapon>() != null && is_contact_made && owner_id == Networking.LocalPlayer.playerId)
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

        //Destroy(gameObject);
        if (global_index > -1 && ref_index > -1 && gameController.global_projectile_refs != null)
        {
            gameController.PreallocClearSlot((int)prealloc_obj_name.WeaponProjectile, global_index, ref ref_index);
        }
        //ResetProjectile();
        gameObject.SetActive(false);
        
    }

    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy || projectile_hit_on_this_frame || projectile_duration <= 0) { return false; }
        // Did we hit a hitbox?
        if (other.gameObject.GetComponent<PlayerHitbox>() != null)
        {
            if (owner_id != Networking.GetOwner(other.gameObject).playerId)
            {
                return true;
            }
        }
        // Did we hit the environment?
        else if (other.gameObject.layer == LayerMask.NameToLayer("Environment")) // && !has_physics
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
            pos_end = rb.position;
            //OnProjectileHit(rb.position, true);
            //Debug.Log("[PROJECTILE] Create projectile at TriggerEnter!");
        }
    }

    private void OnCollisionEnter(UnityEngine.Collision collision)
    {
        if (CheckCollider(collision.collider))
        {
            projectile_hit_on_this_frame = true;
            pos_end = rb.position;
            //OnProjectileHit(rb.position, true);
            //Debug.Log("[PROJECTILE] Create projectile at CollisionEter!");
        }
    }

    private Vector3 RaycastToNextPos(double time_elapsed)
    {
        bool ray_cast = Physics.Linecast(previousPosition, CalcPosAtTime(projectile_timer_network), out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
        //UnityEngine.Debug.DrawLine(previousPosition, CalcPosAtTime(projectile_timer_network), Color.red, 0.1f);
        previousPosition = rb.position;
        // Since vectors are non-nullable, let's just return something impossible
        if (!ray_cast || hitInfo.collider == null || !CheckCollider(hitInfo.collider)) { return INVALID_HIT_POS; }
        //UnityEngine.Debug.Log("Raycast found something! " + hitInfo.point.ToString() + ": " + hitInfo.collider.gameObject.name);
        return hitInfo.point;
    }
}
