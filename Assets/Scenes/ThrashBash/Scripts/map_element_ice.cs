
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_ice : UdonSharpBehaviour
{
    [SerializeField] public float cooldown_duration = 0.4f;
    [SerializeField] public float minimum_magnitude = 8.0f;
    [NonSerialized] public float cooldown_timer = 0.0f;
    [NonSerialized] public Vector3 stored_velocity;
    private void Start()
    {
        if (transform.GetComponent<Renderer>() != null) { transform.GetComponent<Renderer>().enabled = false; }
        stored_velocity = Vector3.zero;
    }

    private void Update()
    {
        if (cooldown_timer < cooldown_duration)
        {
            cooldown_timer += Time.deltaTime;
        }

    }

    public void Slide(VRCPlayerApi player)
    {
        Vector3 player_velocity = player.GetVelocity();
        if (player.isLocal && player_velocity.magnitude >= minimum_magnitude)
        {
            // We'll store the player's maximum velocity during the timer period
            
            if (Mathf.Abs(stored_velocity.x) < Mathf.Abs(player_velocity.x))
            {
                stored_velocity = new Vector3(player_velocity.x, stored_velocity.y, stored_velocity.z);
            }
            else
            {
                player_velocity = new Vector3(stored_velocity.x, player_velocity.y, player_velocity.z);
            }
            if (Mathf.Abs(stored_velocity.y) < Mathf.Abs(player_velocity.y))
            {
                stored_velocity = new Vector3(stored_velocity.x, player_velocity.y, stored_velocity.z);
            }
            else
            {
                player_velocity = new Vector3(player_velocity.x, stored_velocity.y, player_velocity.z);
            }
            if (Mathf.Abs(stored_velocity.z) < Mathf.Abs(player_velocity.z))
            {
                stored_velocity = new Vector3(stored_velocity.x, stored_velocity.y, player_velocity.z);
            }
            else
            {
                player_velocity = new Vector3(player_velocity.x, player_velocity.y, stored_velocity.z);
            }

            // Then, we adjust our velocity so that it always goes up or down the plane's surface
            LayerMask layers_to_hit = LayerMask.GetMask("Ice");

            cooldown_timer = 0.0f;
                       
            Collider[] hitColliders = Physics.OverlapSphere(player.GetPosition(), player.GetAvatarEyeHeightAsMeters() * (1.0f / 1.6f), layers_to_hit, QueryTriggerInteraction.Collide);
            if (hitColliders.Length > 0)
            {
                player.TeleportTo(hitColliders[0].ClosestPoint(player.GetPosition()), player.GetRotation());
            }

            if (Mathf.Abs(stored_velocity.x) < Mathf.Abs(player_velocity.x))
            { 
                stored_velocity = new Vector3(player_velocity.x, stored_velocity.y, stored_velocity.z);
            }
            else
            {
                player_velocity = new Vector3(stored_velocity.x, player_velocity.y, player_velocity.z);
            }
            if (Mathf.Abs(stored_velocity.y) < Mathf.Abs(player_velocity.y))
            {
                stored_velocity = new Vector3(stored_velocity.x, player_velocity.y, stored_velocity.z);
            }
            else
            {
                player_velocity = new Vector3(player_velocity.x, stored_velocity.y, player_velocity.z);
            }
            if (Mathf.Abs(stored_velocity.z) < Mathf.Abs(player_velocity.z))
            {
                stored_velocity = new Vector3(stored_velocity.x, stored_velocity.y, player_velocity.z);
            }
            else
            {
                player_velocity = new Vector3(player_velocity.x, player_velocity.y, stored_velocity.z);
            }

            float calc_x = Mathf.Max(minimum_magnitude, Mathf.Abs(player_velocity.x));
            float calc_y = Mathf.Max(minimum_magnitude, Mathf.Abs(player_velocity.y));
            float calc_z = Mathf.Max(minimum_magnitude, Mathf.Abs(player_velocity.z));
            if (player_velocity.x < 0) { calc_x = -calc_x; }
            if (player_velocity.y < 0) { calc_y = -calc_y; }
            if (player_velocity.z < 0) { calc_z = -calc_z; }

            player_velocity = new Vector3(calc_x, calc_y, calc_z);

            //player_velocity = new Vector3(player_velocity.x, Mathf.Abs(player_velocity.y), player_velocity.z);
            //player_velocity += transform.up * player_velocity.magnitude;
            player.SetVelocity(player_velocity);
            cooldown_timer = 0.0f;
            
        }
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        Slide(player);
    }

    public override void OnPlayerCollisionStay(VRCPlayerApi player)
    {
        Slide(player);
    }


}
