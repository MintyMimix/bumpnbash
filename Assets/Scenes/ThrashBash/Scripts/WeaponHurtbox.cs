
using System;
using System.Collections.Generic;
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using static UnityEngine.UI.Image;

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
    // Options
    [SerializeField] public bool is_melee = false; // If the weapon is melee, we need to use different behaviors 
    [SerializeField] public LayerMask layers_to_hit;
    // References
    [SerializeField] public GameController gameController;
    [SerializeField] public Rigidbody hurtbox_rb;
    [SerializeField] public GameObject[] hurtbox_meshes; // NEEDS TO MATCH LENGTH OF damage_mesh_type_name
    [SerializeField] public Collider[] hurtbox_colliders; // NEEDS TO MATCH LENGTH OF damage_mesh_type_name
    // Owner Data
    [NonSerialized] public int damage_type;
    [NonSerialized] public float source_weapon_type;
    // Hurtbox Data
    [NonSerialized] public float hurtbox_duration;
    [NonSerialized] public double hurtbox_start_ms;
    [NonSerialized] public double hurtbox_timer_network = 0.0f;
    [NonSerialized] public float hurtbox_damage;
    [NonSerialized] public byte hurtbox_extra_data = 0;
    // Active Data
    [NonSerialized] private float tick_timer = 0.0f;
    [NonSerialized] public Rigidbody weapon_rb;
    [NonSerialized] public Transform active_particle;
    [NonSerialized] public GameObject active_mesh;
    [NonSerialized] public Collider active_collider;
    [NonSerialized] public int[] players_struck_prealloc;
    [NonSerialized] public ushort players_struck_cnt;
    [NonSerialized] public Vector3 start_scale;
    [NonSerialized] public PlayerWeapon weapon_script;
    [NonSerialized] public bool in_reset_state = true;

    private void Start()
    {
        // Because of Weird Networked Shit, we need to ensure the hurtboxes are active at the start, reset, and then are set to be inactive.
        // Otherwise, the first time a ranged hurtbox is created, it will immediately expire, resulting in no strikes.
        in_reset_state = true;
        OnReset(true);
    }

    private void Update()
    {
        if (in_reset_state) { return; }

        tick_timer += Time.deltaTime;

        if (hurtbox_duration > 0.0f && hurtbox_start_ms != 0.0f && gameObject.activeInHierarchy)
        {
            double server_ms = Networking.GetServerTimeInSeconds();
            hurtbox_timer_network = Networking.CalculateServerDeltaTime(server_ms, hurtbox_start_ms);

            if (hurtbox_timer_network > hurtbox_duration)
            {
                // Fire off events for hurtbox expiration here
                //UnityEngine.Debug.Log("[HURTBOX_TEST] " + gameObject.name + " expired. server_ms = " + server_ms + "; start_ms = " + hurtbox_start_ms + "; timer = " + hurtbox_timer_network + "; duration = " + hurtbox_duration);
                OnStop();
            }
        }

        if (tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            UpdatePerActiveTick();
            tick_timer = 0.0f;
        }
    }

    private void OnEnable()
    {
        if (gameController != null && gameController.local_ppp_options != null && active_mesh != null)
        {
            Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
            bool show_renderer = gameController.local_ppp_options.hurtbox_show;
            m_Renderer.enabled = show_renderer || damage_type == (int)damage_type_name.ForceExplosion || damage_type == (int)damage_type_name.ItemHit || (gameController.flag_for_mobile_vr.activeInHierarchy && damage_type == (int)damage_type_name.Laser);
        }
        if (is_melee)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        Physics.SyncTransforms();
        //hurtbox_rb.MovePosition(transform.position);
        //hurtbox_rb.MoveRotation(transform.rotation);
    }

    public void OnReset(bool full_reset)
    {
        in_reset_state = true;
        if (full_reset)
        {
            hurtbox_start_ms = 0.0f;
        }
        tick_timer = 0.0f;
        hurtbox_timer_network = 0.0f;
        if (active_particle != null)
        {
            var main = active_particle.GetComponent<ParticleSystem>().main;
            active_particle.GetComponent<ParticleSystem>().Stop();
            active_particle.gameObject.SetActive(false);
        }
        players_struck_prealloc = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS]; // 80 is the hard limit of players in a VRChat world
        players_struck_cnt = 0;
        in_reset_state = false;
        if (full_reset)
        {
            gameObject.SetActive(false);
        }
    }

    public void OnStop()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePerActiveTick()
    {
        if (is_melee) { UpdateMeleePosition(); }
    }

    private void UpdateMeleePosition()
    {
        if (weapon_rb == null) { return; }
        Transform point_end_tf = null;
        Vector3 origin = weapon_rb.position;

        if (weapon_script != null && weapon_script.weapon_mdl != null && weapon_script.weapon_type >= 0 && weapon_script.weapon_type < weapon_script.weapon_mdl.Length && weapon_script.weapon_mdl[weapon_script.weapon_type] != null)
        {
            point_end_tf = weapon_script.weapon_mdl[weapon_script.weapon_type].transform.GetChild(0);
        }
        if (point_end_tf != null)
        {

            bool ray_cast = Physics.Linecast(origin, point_end_tf.position, out RaycastHit hitInfo, layers_to_hit, QueryTriggerInteraction.Collide);
            Vector3 end_pos;
            if (hitInfo.collider != null && hitInfo.collider.gameObject != null && hitInfo.collider.gameObject.activeInHierarchy) { end_pos = hitInfo.point; }
            else { end_pos = point_end_tf.position; }

            if (weapon_script.weapon_type == (int)weapon_type_name.PunchingGlove || weapon_script.weapon_type == (int)weapon_type_name.BossGlove || weapon_script.weapon_type == (int)weapon_type_name.HyperGlove || weapon_script.weapon_type == (int)weapon_type_name.MegaGlove)
            {
                float calcAnimPct = (float)(hurtbox_timer_network / hurtbox_duration);
                float calcScale = 1.0f;
                //float animThreshold = (50.0f / 60.0f); When it was originally one second long, we mapped to animation, but it makes no sense for the retract to have a hitbox, so let's just make it 100%
                float animThreshold = 1.0f;
                if (calcAnimPct >= animThreshold) { calcScale = 1.0f - (float)((hurtbox_timer_network - animThreshold) / (hurtbox_duration - animThreshold)); }
                //transform.position = Vector3.Lerp(origin, origin + (weapon_rb.transform.right * (Vector3.Distance(origin, end_pos) / 2.0f)), calcScale);
                //hurtbox_rb.MovePosition(Vector3.Lerp(origin, origin + (weapon_rb.transform.right * (Vector3.Distance(origin, end_pos) / 2.0f)), calcScale));
                Vector3 midpoint = (origin + end_pos) * 0.5f;
                // convert midpoint into parent's local space
                Vector3 localMidpoint = weapon_script.transform.InverseTransformPoint(midpoint);
                transform.localPosition = Vector3.Lerp(Vector3.zero, localMidpoint, calcScale);
                GlobalHelperFunctions.SetGlobalScale(this.transform,
                    new Vector3(
                    Mathf.Lerp(start_scale.x, start_scale.x + Vector3.Distance(origin, end_pos), calcScale),
                    start_scale.y,
                    start_scale.z
                    )
                );

                float distanceCompare = Vector3.Distance(origin, end_pos) / Vector3.Distance(origin, point_end_tf.position);
                if (distanceCompare < 1.0f) { weapon_script.animate_pct = 0.5f * (Vector3.Distance(origin, end_pos) / Vector3.Distance(origin, point_end_tf.position)); }
                else { weapon_script.animate_pct = 0.999f; } // For some reason, 1.0f resets the animation
                weapon_script.animate_stored_pct = weapon_script.animate_pct;
                weapon_script.animate_stored_use_timer = weapon_script.use_timer;

            }
            else
            {
                transform.localScale = Vector3.Lerp(start_scale, start_scale * Vector3.Distance(origin, end_pos), 1.0f - (float)(hurtbox_timer_network / hurtbox_duration));
            }
            
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            OnTriggerEnter(collision.collider);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || players_struck_prealloc == null) { return; }
        // Run this only if we are the owner of the hurtbox
        if (Networking.GetOwner(gameObject) != Networking.LocalPlayer) { return; }
        // And that we're striking player hitbox to begin with
        if (other.GetComponent<PlayerHitbox>() == null) { return; }
        // And that we're not colliding with a hitbox we've already processed
        VRCPlayerApi colliderOwner = Networking.GetOwner(other.gameObject);
        if (colliderOwner == null) { return; }
        for (int i = 0; i < players_struck_cnt; i++)
        {
            if (players_struck_prealloc[i] == colliderOwner.playerId) { return; }
        }

        // If all that checks out, we can add the player to the exclusion list
        players_struck_prealloc[players_struck_cnt] = colliderOwner.playerId;
        players_struck_cnt++;

        bool hit_self = false;
        if (colliderOwner == Networking.LocalPlayer)
        {
            // If we are hitting ourselves, the hurtbox must be from an explosive weapon
            hit_self = !is_melee && (damage_type == (int)damage_type_name.ForceExplosion || damage_type == (int)damage_type_name.ItemHit);
            // Otherwise, we should stop processing here. We mark ourselves as excluded from the hurtbox so we don't need to check again
            if (!hit_self) { return; }
        }

        // Ensure hurtbox is not on a teammate (unless it's a throwable item)
        if (
            gameController.GetGlobalTeam(colliderOwner.playerId)
            == gameController.GetGlobalTeam(Networking.LocalPlayer.playerId)
            && gameController.option_teamplay
            && !hit_self
            && damage_type != (int)damage_type_name.ItemHit
            ) { return; }


        // Finally, calculate the force direction and tell the player they've been hit
        Vector3 force_dir = Vector3.zero; Vector3 hitSpot = Vector3.zero;
        if (source_weapon_type == (int)weapon_type_name.Bomb || source_weapon_type == (int)weapon_type_name.Rocket)
        {
            force_dir = (colliderOwner.GetPosition() - transform.position).normalized;
        }
        else if (source_weapon_type == (int)weapon_type_name.SuperLaser)
        {
            force_dir = (active_collider.ClosestPointOnBounds(transform.position)  - colliderOwner.GetPosition()).normalized;
        }
        else
        {
            force_dir = (colliderOwner.GetPosition() - active_collider.ClosestPointOnBounds(transform.position)).normalized;
        }

        hitSpot = active_collider.ClosestPointOnBounds(colliderOwner.GetPosition());

        if (!hit_self) {
            PlayerAttributes ply_attr = gameController.FindPlayerAttributes(colliderOwner);
            if (ply_attr == null) { return; }
            ply_attr.SendCustomNetworkEvent(NetworkEventTarget.Owner, "ReceiveDamage", hurtbox_damage, force_dir, hitSpot, Networking.LocalPlayer.playerId, damage_type, false, hurtbox_extra_data);
        }
        else
        {
            gameController.local_plyAttr.ReceiveDamage(hurtbox_damage, force_dir, hitSpot, Networking.LocalPlayer.playerId, damage_type, true, hurtbox_extra_data);
            if (weapon_script == null) { weapon_script = gameController.local_plyweapon; }
            gameController.PlaySFXFromArray(
                weapon_script.snd_source_weaponcontact, weapon_script.snd_game_sfx_clips_weaponcontact, weapon_script.weapon_type
            );
        }

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
                if (damage_type == (int)damage_type_name.ForceExplosion) { active_particle = GlobalHelperFunctions.GetChildTransformByName(active_mesh.transform, "ParticleExplosion"); }
                else if (damage_type == (int)damage_type_name.ItemHit) { active_particle = GlobalHelperFunctions.GetChildTransformByName(active_mesh.transform, "ParticleItemExplosion"); }
                if (active_particle != null)
                {
                    //active_particle.gameObject.SetActive(true);
                    var main = active_particle.GetComponent<ParticleSystem>().main;
                    main.startLifetime = hurtbox_duration;
                    main.duration = hurtbox_duration;
                    //active_particle.GetComponent<ParticleSystem>().Play();
                    //Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
                    //if (m_Renderer != null) { m_renderer.enabled = false; }

                    if (damage_type == (int)damage_type_name.ItemHit)
                    {
                        int item_index = 0; Sprite sprite_to_render = null;
                        if (hurtbox_extra_data < (int)powerup_type_name.ENUM_LENGTH)
                        {
                            item_index = hurtbox_extra_data;
                            sprite_to_render = GlobalHelperFunctions.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemPowerup").GetComponent<ItemPowerup>().powerup_sprites[item_index];
                        }
                        else
                        {
                            item_index = hurtbox_extra_data - (int)powerup_type_name.ENUM_LENGTH;
                            sprite_to_render = GlobalHelperFunctions.GetChildTransformByName(gameController.template_ItemSpawner.transform, "ItemWeapon").GetComponent<ItemWeapon>().iweapon_sprites[item_index];

                        }
                        if (sprite_to_render != null) { active_particle.GetComponent<Renderer>().material.SetTexture("_MainTex", sprite_to_render.texture); }
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
                    active_particle = GlobalHelperFunctions.GetChildTransformByName(active_mesh.transform, "ParticleLaser");
                    if (active_particle != null)
                    {
                        //active_particle.gameObject.SetActive(true);
                        var main = active_particle.GetComponent<ParticleSystem>().main;
                        main.startLifetime = hurtbox_duration;
                        main.duration = hurtbox_duration;
                        main.startSpeed = transform.localScale.x * 40.0f;
                        //active_particle.GetComponent<ParticleSystem>().Play();
                        //Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
                        //if (m_Renderer != null) { m_renderer.enabled = false; }
                    }
                }
            }
        }
        else { active_mesh = gameObject; }
        SetTeamColor();

    }

    private void SetTeamColor()
    {
        Renderer m_Renderer = active_mesh.GetComponent<Renderer>();
        PlayerAttributes owner_plyAttr = gameController.FindPlayerAttributes(Networking.GetOwner(gameObject));
        if (owner_plyAttr == null || m_Renderer == null || owner_plyAttr.gameController.team_colors == null) { return; }

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
        if (active_particle != null && active_particle.GetComponent<ParticleSystem>() != null)
        {
            var particle_main = active_particle.GetComponent<ParticleSystem>().main;
            particle_main.startColor = new Color(m_Renderer.material.GetColor("_Color").r, m_Renderer.material.GetColor("_Color").g, m_Renderer.material.GetColor("_Color").b, 1.0f);
            var main = active_particle.GetComponent<ParticleSystem>().main;
            if (gameController != null && gameController.local_ppp_options != null)
            {
                var particle_emission = active_particle.GetComponent<ParticleSystem>().emission;
                particle_emission.enabled = gameController.local_ppp_options.particles_on;
            }
            active_particle.gameObject.SetActive(true);
            active_particle.GetComponent<ParticleSystem>().Play();
        }
    }

}
