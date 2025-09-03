
using System;
using System.Collections.Generic;
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum damage_type_name
{
    Strike, ForceExplosion, Kapow, HazardBurn, Laser, ItemHit, ENUM_LENGTH
}
public enum damage_mesh_type_name
{
    Cube, Sphere, ENUM_LENGTH
}

public class WeaponHurtbox : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject[] hurtbox_meshes; // NEEDS TO MATCH LENGTH OF damage_mesh_type_name
    [SerializeField] public Collider[] hurtbox_colliders; // NEEDS TO MATCH LENGTH OF damage_mesh_type_name
	[NonSerialized] public GameObject active_mesh;
	[NonSerialized] public Collider active_collider;
    [NonSerialized] public int global_index = -1;
    [NonSerialized] public int ref_index = -1;
    [NonSerialized] public Vector3 origin;
    [NonSerialized] public float hurtbox_duration;
    //[NonSerialized] public double hurtbox_start_ms;
    [NonSerialized] public double hurtbox_timer_local = 0.0f;
    //[NonSerialized] private double hurtbox_timer_network = 0.0f;
    [NonSerialized] public float hurtbox_damage;
    [NonSerialized] public byte extra_data = 0;
    [NonSerialized] public float weapon_type; // Applies REGARDLESS OF weapon_parent
    //[NonSerialized] public int[] players_struck;
    //[NonSerialized] public List<int> players_struck_list;
    [NonSerialized] public int[] players_struck_prealloc;
    [NonSerialized] public ushort players_struck_cnt;
    [NonSerialized] public Vector3 start_scale;
    //public bool struck_local = false;
    [NonSerialized] public int owner_id;
    [NonSerialized] public int damage_type;
    [NonSerialized] public GameObject weapon_parent; // WARNING: Only use if keep_parent is on from WeaponProjectile
    [NonSerialized] public PlayerWeapon weapon_script; // WARNING: Only use if keep_parent is on from WeaponProjectile

    [NonSerialized] public float local_offset;
    [NonSerialized] public PlayerAttributes owner_plyAttr;
    [SerializeField] public Rigidbody rb;
    [NonSerialized] private Rigidbody weapon_rb;
    [NonSerialized] private Transform particle;
    [NonSerialized] private LayerMask layers_to_hit;

    private void Start()
    {
        //players_struck = new int[0];
        //players_struck_list = new List<int>();
        rb = gameObject.GetComponent<Rigidbody>();

        if (origin == null) { origin = transform.position; }
        layers_to_hit = LayerMask.GetMask("PlayerHitbox", "Environment");

        players_struck_prealloc = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS]; // 80 is the hard limit of players in a VRChat world
        players_struck_cnt = 0;
    }

    public void ResetHurtbox()
    {
        hurtbox_timer_local = 0.0f;
        weapon_parent = null;
        weapon_script = null;
        transform.parent = null;
        transform.SetPositionAndRotation(gameController.template_WeaponHurtbox.transform.position, gameController.template_WeaponHurtbox.transform.rotation);
        if (rb == null) { rb = gameObject.GetComponent<Rigidbody>(); }
        else
        {
            rb.position = transform.position;
            rb.rotation = transform.rotation;
        }
        Start();
    }

    public void SetMesh()
    {
        if (hurtbox_meshes != null)
        {
            for (int i = 0; i < hurtbox_meshes.Length; i++)
            {
                hurtbox_meshes[i].SetActive(false);
                hurtbox_colliders[i].enabled = false;
            }
            if (damage_type == (int)damage_type_name.ForceExplosion || damage_type == (int)damage_type_name.ItemHit)
            {
                hurtbox_meshes[(int)damage_mesh_type_name.Sphere].SetActive(true);
                hurtbox_colliders[(int)damage_mesh_type_name.Sphere].enabled = true;
                active_mesh = hurtbox_meshes[(int)damage_mesh_type_name.Sphere];
                active_collider = hurtbox_colliders[(int)damage_mesh_type_name.Sphere];
                if (damage_type == (int)damage_type_name.ForceExplosion) { particle = gameController.GetChildTransformByName(active_mesh.transform, "ParticleExplosion"); }
                else if (damage_type == (int)damage_type_name.ItemHit) { particle = gameController.GetChildTransformByName(active_mesh.transform, "ParticleItemExplosion"); }
                if (particle != null)
                {
                    particle.gameObject.SetActive(true);
                    var main = particle.GetComponent<ParticleSystem>().main;
                    main.startLifetime = hurtbox_duration;
                    main.duration = hurtbox_duration;
                    if (gameController != null && gameController.local_ppp_options != null)
                    {
                        var particle_emission = particle.GetComponent<ParticleSystem>().emission;
                        particle_emission.enabled = gameController.local_ppp_options.particles_on;
                    }
                    particle.gameObject.SetActive(true);
                    particle.GetComponent<ParticleSystem>().Play();
                    //Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
                    //if (m_Renderer != null) { m_renderer.enabled = false; }

                    if (damage_type == (int)damage_type_name.ItemHit)
                    {
                        int item_index = 0; Sprite sprite_to_render = null;
                        if (extra_data < (int)powerup_type_name.ENUM_LENGTH)
                        {
                            item_index = extra_data;
                            sprite_to_render = gameController.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemPowerup").GetComponent<ItemPowerup>().powerup_sprites[item_index];
                        }
                        else
                        {
                            item_index = extra_data - (int)powerup_type_name.ENUM_LENGTH;
                            sprite_to_render = gameController.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemWeapon").GetComponent<ItemWeapon>().iweapon_sprites[item_index];

                        }
                        if (sprite_to_render != null) { particle.GetComponent<Renderer>().material.SetTexture("_MainTex", sprite_to_render.texture); }
                    }

                }
            }
            else
            {
                hurtbox_meshes[(int)damage_mesh_type_name.Cube].SetActive(true);
                hurtbox_colliders[(int)damage_mesh_type_name.Cube].enabled = true;
                active_mesh = hurtbox_meshes[(int)damage_mesh_type_name.Cube];
                active_collider = hurtbox_colliders[(int)damage_mesh_type_name.Cube];
                if (damage_type == (int)damage_type_name.Laser)
                {
                    particle = gameController.GetChildTransformByName(active_mesh.transform, "ParticleLaser");
                    if (particle != null)
                    {
                        var main = particle.GetComponent<ParticleSystem>().main;
                        main.startLifetime = hurtbox_duration;
                        main.duration = hurtbox_duration;
                        main.startSpeed = transform.localScale.x * 40.0f;
                        particle.gameObject.SetActive(true);
                        particle.GetComponent<ParticleSystem>().Play();
                        //Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
                        //if (m_Renderer != null) { m_renderer.enabled = false; }
                    }
                }
            }
        }
        else { active_mesh = gameObject; }

        UpdateTeamMaterial();

    }
    private void UpdateTeamMaterial()
    {
        Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
        if (owner_plyAttr != null && m_Renderer != null && owner_plyAttr.gameController.team_colors != null)
        {
            int team = Mathf.Max(0, owner_plyAttr.ply_team);
            if (owner_plyAttr.gameController.option_teamplay)
            {
                m_Renderer.material.SetColor("_Color",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + owner_plyAttr.gameController.team_colors[team].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + owner_plyAttr.gameController.team_colors[team].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + owner_plyAttr.gameController.team_colors[team].b)),
                    (byte)0));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + owner_plyAttr.gameController.team_colors[team].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + owner_plyAttr.gameController.team_colors[team].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + owner_plyAttr.gameController.team_colors[team].b)),
                    (byte)owner_plyAttr.gameController.team_colors[team].a));
            }
            else
            {
                m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 0));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor", new Color32(83, 83, 83, 255));
            }
            if (particle != null && particle.GetComponent<ParticleSystem>() != null)
            {
                var particle_main = particle.GetComponent<ParticleSystem>().main;
                particle_main.startColor = new Color(m_Renderer.material.GetColor("_Color").r, m_Renderer.material.GetColor("_Color").g, m_Renderer.material.GetColor("_Color").b, 1.0f);
            }
        }
    }
    private void OnEnable()
    {
        if (weapon_parent != null)
        {
            weapon_rb = weapon_parent.GetComponent<Rigidbody>(); local_offset = Vector3.Distance(transform.position, weapon_rb.position);
            weapon_script = weapon_parent.GetComponent<PlayerWeapon>();
        }
        owner_plyAttr = gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(owner_id));

        if (gameController != null && gameController.local_ppp_options != null && active_mesh != null)
        {
            Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
            m_Renderer.enabled = gameController.local_ppp_options.hurtbox_show;
        }

        SetMesh();

    }

    private void FixedUpdate()
    {
        if (weapon_parent != null)
        {
            //LayerMask layers_to_hit = LayerMask.GetMask("PlayerHitbox", "Environment");
            Transform point_end_tf = null;
            origin = weapon_rb.position;
            if (weapon_script != null && weapon_script.weapon_mdl != null && weapon_script.weapon_mdl.Length > weapon_script.weapon_type && weapon_script.weapon_mdl[weapon_script.weapon_type] != null)
            {
                point_end_tf = weapon_script.weapon_mdl[weapon_script.weapon_type].transform.GetChild(0);
            }
            if (point_end_tf != null)
            {
                bool ray_cast = Physics.Linecast(origin, point_end_tf.position, out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
                Vector3 end_pos;
                if (CheckCollider(hitInfo.collider)) { end_pos = hitInfo.point; }
                else { end_pos = point_end_tf.position; }

                if (weapon_script.weapon_type == (int)weapon_type_name.PunchingGlove || weapon_script.weapon_type == (int)weapon_type_name.BossGlove || weapon_script.weapon_type == (int)weapon_type_name.HyperGlove || weapon_script.weapon_type == (int)weapon_type_name.MegaGlove)
                {
                    float calcAnimPct = (float)(hurtbox_timer_local / hurtbox_duration);
                    float calcScale = 1.0f;
                    //float animThreshold = (50.0f / 60.0f); When it was originally one second long, we mapped to animation, but it makes no sense for the retract to have a hitbox, so let's just make it 100%
                    float animThreshold = 1.0f;
                    if (calcAnimPct >= animThreshold) { calcScale = 1.0f - (float)((hurtbox_timer_local - animThreshold) / (hurtbox_duration - animThreshold)); }
                    rb.position = Vector3.Lerp(origin, origin + (weapon_rb.transform.right * (Vector3.Distance(origin, end_pos) / 2.0f)), calcScale);
                    transform.rotation = weapon_parent.transform.rotation; // We don't use rb or MoveRotation() for this because the result is physics calculations that we don't need
                    rb.velocity = Vector3.zero;
                    SetGlobalScale(this.transform,
                        new Vector3(
                        Mathf.Lerp(start_scale.x, start_scale.x + Vector3.Distance(origin, end_pos), calcScale),
                        start_scale.y,
                        start_scale.z
                        )
                    );

                    if (Networking.GetOwner(weapon_parent) == Networking.LocalPlayer)
                    {
                        //UnityEngine.Debug.Log("Distance comparison: " + Vector3.Distance(weapon_rb.position, end_pos) + " vs " + (Vector3.Distance(weapon_rb.position, point_end_tf.position)));
                        float distanceCompare = Vector3.Distance(origin, end_pos) / Vector3.Distance(origin, point_end_tf.position);
                        if (distanceCompare < 1.0f) { weapon_script.animate_pct = 0.5f * (Vector3.Distance(origin, end_pos) / Vector3.Distance(origin, point_end_tf.position)); }
                        else { weapon_script.animate_pct = 0.999f; } // For some reason, 1.0f resets the animation
                        weapon_script.animate_stored_pct = weapon_script.animate_pct;
                        weapon_script.animate_stored_use_timer = weapon_script.use_timer;
                    }
                }
                else
                {
                    transform.localScale = Vector3.Lerp(start_scale, start_scale * Vector3.Distance(origin, end_pos), 1.0f - (float)(hurtbox_timer_local / hurtbox_duration));
                }
            }

        }
    }

    private void Update()
    {

        hurtbox_timer_local += Time.deltaTime;
        if (hurtbox_timer_local >= hurtbox_duration && hurtbox_duration > 0)
        {
            //Debug.Log("HURTBOX DESTROYED BECAUSE ITS DURATION OF " + hurtbox_duration.ToString() + " WAS EXCEEDED BY LOCAL TIME " + hurtbox_timer_local.ToString() + " OR NETWORK TIME " + hurtbox_timer_network.ToString());
            //Destroy(gameObject);
            if (weapon_script != null) 
            { 
                weapon_script.animate_handled_by_hurtbox = false;
            }

            if (global_index > -1 && ref_index > -1 && gameController.global_hurtbox_refs != null)
            {
                gameController.PreallocClearSlot((int)prealloc_obj_name.WeaponHurtbox, global_index, ref ref_index);
            }

            gameObject.SetActive(false);
        }
        else if (weapon_script != null && Networking.GetOwner(weapon_parent) == Networking.LocalPlayer && (weapon_script.weapon_type == (int)weapon_type_name.PunchingGlove || weapon_script.weapon_type == (int)weapon_type_name.BossGlove || weapon_script.weapon_type == (int)weapon_type_name.HyperGlove || weapon_script.weapon_type == (int)weapon_type_name.MegaGlove))
        {
            weapon_script.animate_handled_by_hurtbox = true;
        }

        rb.AddForce(Vector3.zero); // Add an ever so slight force to the rigidbody just so it gets registered by the player hitbox trigger 
    }

    // Attacker-focused code 
    private bool CheckCollider(Collider other)
    {
        if (other == null || other.gameObject == null || !other.gameObject.activeInHierarchy) { return false; }
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

    private void OnTriggerStay(Collider other)
    {
        if (other == null || players_struck_prealloc == null) { return; }
        // Run this only if we are the owner of the hurtbox
        if (owner_id != Networking.LocalPlayer.playerId) { return; }
        // Check we're not colliding a hitbox we've already processed
        VRCPlayerApi colliderOwner = Networking.GetOwner(other.gameObject);
        if (colliderOwner == null) { return; }
        for (int i = 0; i < players_struck_cnt; i++)
        {
            if (players_struck_prealloc[i] == colliderOwner.playerId) { return; }
        }
        // And that it's a hurtbox to begin with
        PlayerHitbox plyHitbox = other.gameObject.GetComponent<PlayerHitbox>();
        if (plyHitbox == null) { return; }

        // If all that checks out, we can add the player to the exclusion list
        players_struck_prealloc[players_struck_cnt] = colliderOwner.playerId;
        players_struck_cnt++;

        // What if we had rocket jumping punches? (change hit_self = true to return if this breaks something)
        // Make sure that weapon_parent is null for this, otherwise it will always hit outselves
        bool hit_self = false;
        // To-do: check if hit_self will behave now that we have many other checks in place
        if (colliderOwner.playerId == Networking.LocalPlayer.playerId) {

            // Allow hit self only if...
            bool allow_hit_self = false;
            // The hurtbox is from an explosive weapon
            allow_hit_self = (allow_hit_self) || (weapon_parent == null && (damage_type == (int)damage_type_name.ForceExplosion || damage_type == (int)damage_type_name.ItemHit));
            // The hurtbox is a non-laser hitting the ground
            allow_hit_self = (allow_hit_self) || (weapon_parent != null && other.gameObject.layer == LayerMask.NameToLayer("Environment") && damage_type != (int)damage_type_name.Laser);
            // And we are not The Big Boss
            allow_hit_self = (allow_hit_self) && !(gameController.option_gamemode == (int)gamemode_name.BossBash && owner_plyAttr != null && owner_plyAttr.ply_team == 1);
            if (allow_hit_self)
            {
                hit_self = true;
            }
            else { return; }
        }

        // Check teams as well (unless it's a throwable item; then it can hit anyone)
        if (
            gameController.GetGlobalTeam(colliderOwner.playerId)
            == gameController.GetGlobalTeam(owner_id)
            && gameController.option_teamplay
            && !hit_self
            && damage_type != (int)damage_type_name.ItemHit 
            ) { return; }

		// Finally, calculate the force direction and tell the player they've been hit
		//Vector3 force_dir = Vector3.Normalize(colliderOwner.GetPosition() - rb.position);
		//force_dir = new Vector3(force_dir.x, 0, force_dir.z);
		Vector3 force_dir = Vector3.zero; Vector3 hitSpot = Vector3.zero;
        if (weapon_type == (int)weapon_type_name.Bomb || weapon_type == (int)weapon_type_name.Rocket) 
        {
            force_dir = (colliderOwner.GetPosition() - rb.position).normalized; 
        }
        else 
        {
            force_dir = (colliderOwner.GetPosition() - active_collider.ClosestPointOnBounds(origin)).normalized; 
        }

        hitSpot = active_collider.ClosestPointOnBounds(colliderOwner.GetPosition());
        // force_dir = (colliderOwner.GetPosition() - active_collider.ClosestPoint(colliderOwner.GetPosition())).normalized;
        //UnityEngine.Debug.Log("Force direction: " + force_dir.ToString() + " (old: " + Vector3.Normalize(colliderOwner.GetPosition() - rb.position) + ") ");

        if (!hit_self) { gameController.FindPlayerAttributes(colliderOwner).SendCustomNetworkEvent(NetworkEventTarget.Owner, "ReceiveDamage", hurtbox_damage, force_dir, hitSpot, owner_id, damage_type, false, extra_data); }
        else
        {
            gameController.local_plyAttr.ReceiveDamage(hurtbox_damage, force_dir, hitSpot, owner_id, damage_type, true, extra_data);
            var plyWeapon = gameController.local_plyweapon;
            gameController.PlaySFXFromArray(
                plyWeapon.snd_source_weaponcontact, plyWeapon.snd_game_sfx_clips_weaponcontact, plyWeapon.weapon_type
            );
        }
        return;
    }
    public static void SetGlobalScale(Transform transform, Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
    }

}
