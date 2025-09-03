
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class map_element_ramp : UdonSharpBehaviour
{
    // To-do: Code is only working for in-air triggers rather than actual ramps
    [SerializeField] public float cooldown_duration = 0.4f;
    [SerializeField] public float minimum_magnitude = 4.0f;
    [NonSerialized] public float cooldown_timer = 0.0f;
    [NonSerialized] public Vector3 stored_velocity;

    private void Start()
    {
        //transform.GetComponent<Renderer>().enabled = false;
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
        if (player.isLocal)
        {
            if (player.GetVelocity().magnitude < minimum_magnitude && stored_velocity.magnitude < minimum_magnitude) { return; }

            LayerMask layers_to_hit = LayerMask.GetMask("Ramp");

            // If we aren't on cooldown, teleport the player to the closest point on the ramp before proceeding. Every tick we are colliding, we reset the cooldown, so this should effectively only apply OnEnter()
            if (cooldown_timer >= cooldown_duration)
            {
                Collider[] hitColliders = Physics.OverlapSphere(player.GetPosition(), player.GetAvatarEyeHeightAsMeters() * (1.0f / 1.6f), layers_to_hit, QueryTriggerInteraction.Collide);
                if (hitColliders.Length > 0)
                {
                    player.TeleportTo(hitColliders[0].ClosestPoint(player.GetPosition()), player.GetRotation());
                }
                stored_velocity = Vector3.zero;
            }

            // We'll store the player's maximum velocity during the timer period
            Vector3 player_velocity = player.GetVelocity();

            /*if (Mathf.Abs(stored_velocity.x) < Mathf.Abs(player_velocity.x))
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
            }*/

            // Then, we adjust our velocity so that it always goes up or down the plane's surface
            RaycastHit slopeHit;
            if (Physics.Raycast(player.GetPosition(), -Vector3.Normalize(player_velocity), out slopeHit, player_velocity.magnitude * player.GetAvatarEyeHeightAsMeters(), layers_to_hit, QueryTriggerInteraction.Collide))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                Vector3 projectedAngle = Vector3.ProjectOnPlane(Vector3.Normalize(player_velocity), slopeHit.normal);
                player.SetVelocity(player_velocity.magnitude * projectedAngle);
            }

            cooldown_timer = 0.0f;


            /*
            LayerMask layers_to_hit = LayerMask.GetMask("Ice");
            Collider[] hitColliders = Physics.OverlapSphere(player.GetPosition(), player.GetAvatarEyeHeightAsMeters() * (1.0f / 1.6f), layers_to_hit, QueryTriggerInteraction.Collide);
            if (hitColliders.Length > 0)
            {
                player.TeleportTo(hitColliders[0].ClosestPoint(player.GetPosition()), player.GetRotation());
            }

            if (Mathf.Abs(stored_velocity.x) < Mathf.Abs(calc_velocity.x))
            { 
                stored_velocity = new Vector3(calc_velocity.x, stored_velocity.y, stored_velocity.z);
            }
            else
            {
                calc_velocity = new Vector3(stored_velocity.x, calc_velocity.y, calc_velocity.z);
            }
            if (Mathf.Abs(stored_velocity.y) < Mathf.Abs(calc_velocity.y))
            {
                stored_velocity = new Vector3(stored_velocity.x, calc_velocity.y, stored_velocity.z);
            }
            else
            {
                calc_velocity = new Vector3(calc_velocity.x, stored_velocity.y, calc_velocity.z);
            }
            if (Mathf.Abs(stored_velocity.z) < Mathf.Abs(calc_velocity.z))
            {
                stored_velocity = new Vector3(stored_velocity.x, stored_velocity.y, calc_velocity.z);
            }
            else
            {
                calc_velocity = new Vector3(calc_velocity.x, calc_velocity.y, stored_velocity.z);
            }

            float calc_x = Mathf.Max(minimum_magnitude, Mathf.Abs(calc_velocity.x));
            float calc_y = Mathf.Max(minimum_magnitude, Mathf.Abs(calc_velocity.y));
            float calc_z = Mathf.Max(minimum_magnitude, Mathf.Abs(calc_velocity.z));
            if (calc_velocity.x < 0) { calc_x = -calc_x; }
            if (calc_velocity.y < 0) { calc_y = -calc_y; }
            if (calc_velocity.z < 0) { calc_z = -calc_z; }

            calc_velocity = new Vector3(calc_x, calc_y, calc_z);

            calc_velocity = new Vector3(calc_velocity.x, Mathf.Abs(calc_velocity.y), calc_velocity.z);
            //calc_velocity += transform.up * calc_velocity.magnitude;
            player.SetVelocity(calc_velocity);
            cooldown_timer = 0.0f;
            */
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
