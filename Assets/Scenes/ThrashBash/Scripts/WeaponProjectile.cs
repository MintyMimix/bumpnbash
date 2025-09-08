
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
    [NonSerialized] public float owner_scale;
    [NonSerialized] public Vector3 pos_start;
    [NonSerialized] public float projectile_distance;
    [NonSerialized] public float projectile_duration;
    [NonSerialized] public double projectile_start_ms;
    [NonSerialized] private double projectile_timer_network = 0.0f;
    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject[] projectile_mdl;
    [NonSerialized] public float actual_distance;
    [NonSerialized] public Vector3 previousPosition;
    [SerializeField] public Rigidbody rb;
    [NonSerialized] private bool projectile_hit_on_this_frame = false;
    [NonSerialized] public Vector3 pos_end;
    [NonSerialized] public bool contact_made = false;
    [NonSerialized] public bool has_physics = false;
    [NonSerialized] public byte extra_data = 0;
    [SerializeField] public LayerMask layers_to_hit;
    [NonSerialized] private Vector3 INVALID_HIT_POS;
    [NonSerialized] private Animator cached_animator;
    [NonSerialized] private float tick_timer = 0.0f;
    [NonSerialized] private Transform cached_trail;
    [NonSerialized] private Transform cached_particle;
    [NonSerialized] public PlayerWeapon weapon_script;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        previousPosition = rb.position;
        layers_to_hit = LayerMask.GetMask("Player", "PlayerLocal", "PlayerHitbox", "Environment");
        INVALID_HIT_POS = new Vector3(-999999, -999999, -999999);
    }

    public void ResetProjectile()
    {
        tick_timer = 0.0f;
        projectile_duration = 0.0f;
        projectile_distance = 0.0f;
        projectile_hit_on_this_frame = false;
        pos_end = Vector3.zero;
        projectile_start_ms = 0.0f;
        projectile_timer_network = 0.0f;
        contact_made = false;
        has_physics = false;
        transform.parent = null;
        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        //transform.SetPositionAndRotation(gameController.template_WeaponProjectile.transform.position, gameController.template_WeaponProjectile.transform.rotation);
        GetComponent<Collider>().isTrigger = true;
        if (rb != null)
        {
            //rb.position = gameController.template_WeaponProjectile.transform.position;
            //rb.rotation = gameController.template_WeaponProjectile.transform.rotation;
            //previousPosition = rb.position;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else { previousPosition = transform.position; }
        if (cached_trail != null) { cached_trail.gameObject.SetActive(false); }
        if (cached_particle != null) { cached_particle.gameObject.SetActive(false); }
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
                    int owner_team = gameController.GetGlobalTeam(Networking.GetOwner(gameObject).playerId);
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
                        cached_trail = GlobalHelperFunctions.GetChildTransformByName(projectile_mdl[i].transform, "Trail");
                        if (cached_trail != null && cached_trail.GetComponent<TrailRenderer>() != null) 
                        {
                            cached_trail.GetComponent<TrailRenderer>().startColor = new Color(mat.GetColor("_Color").r, mat.GetColor("_Color").g, mat.GetColor("_Color").b, 1.0f);
                            cached_trail.GetComponent<TrailRenderer>().endColor = new Color(mat.GetColor("_Color").r, mat.GetColor("_Color").g, mat.GetColor("_Color").b, 1.0f);
                            cached_trail.gameObject.SetActive(true);
                        }
                        cached_particle = GlobalHelperFunctions.GetChildTransformByName(projectile_mdl[i].transform, "Particle");
                        if (cached_particle != null && cached_particle.GetComponent<ParticleSystem>() != null)
                        {
                            cached_particle.gameObject.SetActive(false);
                            var particle_main = cached_particle.GetComponent<ParticleSystem>().main;
                            particle_main.startColor = new Color(mat.GetColor("_Color").r, mat.GetColor("_Color").g, mat.GetColor("_Color").b, 1.0f);
                            particle_main.duration = projectile_duration;
                            particle_main.playOnAwake = true;
                            if (gameController != null && gameController.local_ppp_options != null)
                            {
                                var particle_emission = cached_particle.GetComponent<ParticleSystem>().emission;
                                particle_emission.enabled = gameController.local_ppp_options.particles_on;
                            }
                            cached_particle.gameObject.SetActive(true);
                            cached_particle.GetComponent<ParticleSystem>().Play();
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
        if (projectile_type == (int)projectile_type_name.Bullet || projectile_type == (int)projectile_type_name.Laser)
        {
            outPos = pos_start + (transform.right * CalcDistanceAtTime(time_elapsed));
        }
        return outPos;
    }

    private void Update()
    {
        if (projectile_duration <= 0) { return; }

        projectile_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), projectile_start_ms);

        tick_timer += Time.deltaTime;

        if (tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            UpdatePerActiveTick();
            tick_timer = 0.0f;
        }

        if (projectile_hit_on_this_frame && pos_end != Vector3.zero)
        {
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

    private void UpdatePerActiveTick()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (projectile_duration <= 0) { return; }

        // If this is not a physics object, we calculate the next position, either based on lerp from the distance or raycast if there's an obstacle between our current and next position
        if (!has_physics) {
            Vector3 lerpPos = Vector3.Lerp(rb.position, CalcPosAtTime(projectile_timer_network), Mathf.Min(1.0f, (float)(projectile_timer_network / projectile_duration)));
            Vector3 rayPos = RaycastToNextPos(projectile_timer_network);
            if (Mathf.FloorToInt(rayPos.x) != Mathf.FloorToInt(INVALID_HIT_POS.x) && !projectile_hit_on_this_frame)
            {
                //UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration  " + projectile_duration + " hit target at " + rayPos.ToString() + " which was not invalid: " + rayPos.x + " vs " + INVALID_HIT_POS.x);
                pos_end = rayPos;
                projectile_hit_on_this_frame = true;
                contact_made = true;
            }
            else
            {
                rb.MovePosition(lerpPos);
                if (!projectile_hit_on_this_frame && (projectile_timer_network >= projectile_duration))
                {
                    //UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " expired due to timer " + projectile_timer_network);
                    pos_end = CalcPosAtTime(projectile_duration);
                    projectile_hit_on_this_frame = true;
                    contact_made = false;
                }
            }
        }
        // If this a physics object, all we care about is lifetime
        else
        {
            if (!projectile_hit_on_this_frame && (projectile_timer_network >= projectile_duration))
            {
                //UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " expired due to timer " + projectile_timer_network);
                pos_end = rb.position;
                projectile_hit_on_this_frame = true;
                contact_made = false;
            }
        }
    }

    public void OnProjectileHit(Vector3 position, bool is_contact_made)
    {
        if (Networking.IsOwner(gameObject))
        {

            PlayerAttributes plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
            if (weapon_script == null) { weapon_script = gameController.local_plyweapon; }
            float damage = weapon_script.GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Damage];
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
                    //UnityEngine.Debug.Log("[LASER_TEST]: positioning laser at midpoint between " + pos_start.ToString() + " and " + position.ToString() + " based on distance " + projectile_distance + " (result: " + Vector3.Lerp(pos_start, position, 0.5f).ToString() + ")");
                    position = Vector3.Lerp(pos_start, position, 0.5f); 
                }

            }

            weapon_script.PlayHapticEvent((int)game_sfx_name.ENUM_LENGTH); // ENUM_LENGTH is used for weapon fire

            UnityEngine.Debug.Log("[PROJECTILE_TEST]: creating hurtbox at " + position + " (pos_start = " + pos_start + ")");
            weapon_script.SendCustomNetworkEvent(
                VRC.Udon.Common.Interfaces.NetworkEventTarget.All
                , "NetworkCreateHurtBox"
                , position
                , rb.rotation
                , new Vector4(damage, owner_scale, dist_scale, extra_data)
                , false
                , Networking.LocalPlayer.playerId
                , weapon_type
                , weapon_script.is_secondary
                );
        }

        gameObject.SetActive(false);
    }

    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy || projectile_hit_on_this_frame || projectile_duration <= 0) { return false; }
        // Did we hit a hitbox?
        if (other.gameObject.GetComponent<PlayerHitbox>() != null)
        {
            if (Networking.GetOwner(gameObject) != Networking.GetOwner(other.gameObject))
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
            UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " collided with " + other.name + " while at " + rb.position.ToString());
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
            UnityEngine.Debug.Log("[" + gameObject.name + "] [PROJECTILE_TEST]: Projectile of duration " + projectile_duration + " collided with " + collision.collider.name + " while at " + rb.position.ToString());
            //OnProjectileHit(rb.position, true);
            //Debug.Log("[PROJECTILE] Create projectile at CollisionEter!");
        }
    }

    private Vector3 RaycastToNextPos(double time_elapsed)
    {
        bool ray_cast = Physics.Linecast(previousPosition, CalcPosAtTime(projectile_timer_network), out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
        previousPosition = rb.position;
        // Since vectors are non-nullable, let's just return something impossible
        if (!ray_cast || hitInfo.collider == null || !CheckCollider(hitInfo.collider)) { return INVALID_HIT_POS; }
        //UnityEngine.Debug.Log("Raycast found something! " + hitInfo.point.ToString() + ": " + hitInfo.collider.gameObject.name);
        return hitInfo.point;
    }
}
