
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class BouncePad : GlobalTickReceiver
{
    
    [SerializeField] public GameController gameController;
    [SerializeField] public bool should_draw = true;
    [SerializeField] public float minimum_magnitude = 10.0f;
    [SerializeField] public float cooldown_duration = 0.4f;
    [SerializeField] public int min_players = 0;
    [SerializeField] public bool boost_player_momentum = false; // Should the bounce pad boost the player's momentum relative to their upward and forward motion instead this object's normal?
    [SerializeField] public bool bounce_sfx_should_play = true;
    [SerializeField] public AudioClip bounce_sfx_clip;
    [NonSerialized] public float cooldown_timer = 0.0f;
    [NonSerialized] public int bouncepad_global_index = -1;

    public override void Start()
    {
        base.Start();
        if (!should_draw)
            {
                transform.GetComponent<Renderer>().enabled = false;
            }
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        //gameObject.SetActive(false);
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        CooldownTick(tickDeltaTime);
    }

    internal void CooldownTick(float deltaTime)
    {
        if (cooldown_timer < cooldown_duration)
        {
            cooldown_timer += deltaTime;
        }
    }

    public void Bounce(VRCPlayerApi player)
    {
        Vector3 player_velocity = player.GetVelocity();
        if (cooldown_timer >= cooldown_duration)
        {
            if (player.isLocal)
            {
                LayerMask layers_to_hit = LayerMask.GetMask("BouncePad");
                Collider[] hitColliders = Physics.OverlapSphere(player.GetPosition(), player.GetAvatarEyeHeightAsMeters() / 1.6f, layers_to_hit, QueryTriggerInteraction.Collide);
                if (hitColliders.Length > 0)
                {
                    UnityEngine.Debug.Log("[BOUNCE_TEST]: Teleporting using bouncepad " + gameObject.name);
                    player.TeleportTo(hitColliders[0].ClosestPoint(player.GetPosition()), player.GetRotation());
                }
                
                Vector3 calc_velocity = transform.up * Mathf.Max(minimum_magnitude, player_velocity.magnitude);
                if (boost_player_momentum)
                {
                    float calc_x = Mathf.Max(minimum_magnitude, Math.Abs(player_velocity.x)); float calc_z = Mathf.Max(minimum_magnitude, Math.Abs(player_velocity.z));
                    if (player_velocity.x < 0) { calc_x = -calc_x; }
                    if (player_velocity.z < 0) { calc_z = -calc_z; }
                    calc_velocity = new Vector3(calc_x, Mathf.Max(minimum_magnitude * 1.5f, Math.Abs(player_velocity.y)), calc_z);
                }
                player.SetVelocity(calc_velocity);
                cooldown_timer = 0.0f;
            }

            if (bounce_sfx_should_play && bounce_sfx_clip != null && gameController != null)
            {
                AudioClip[] ac = new AudioClip[1];
                ac[0] = bounce_sfx_clip;
                float pitch = Mathf.Clamp(player_velocity.magnitude / (minimum_magnitude / 2.0f), 0.5f, 2.0f);
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitSend], ac, 0, pitch);
            }
        }
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer || cooldown_timer < cooldown_duration) { return; }
        Bounce(player);
    }

    public override void OnPlayerCollisionStay(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer || cooldown_timer < cooldown_duration) { return; }
        Bounce(player);
    }

    public void BounceProp(VRCPlayerApi player)
    {
        Vector3 player_velocity = player.GetVelocity();
        if (cooldown_timer >= cooldown_duration)
        {
            if (player.isLocal)
            {
                LayerMask layers_to_hit = LayerMask.GetMask("BouncePad");
                Collider[] hitColliders = Physics.OverlapSphere(player.GetPosition(), player.GetAvatarEyeHeightAsMeters() / 1.6f, layers_to_hit, QueryTriggerInteraction.Collide);
                if (hitColliders.Length > 0)
                {
                    UnityEngine.Debug.Log("[BOUNCE_TEST]: Teleporting using bounceprop " + gameObject.name);
                    player.TeleportTo(hitColliders[0].ClosestPoint(player.GetPosition()), player.GetRotation());
                }

                Vector3 calc_velocity = transform.up * Mathf.Max(minimum_magnitude, player_velocity.magnitude);
                if (boost_player_momentum)
                {
                    float calc_x = Mathf.Max(minimum_magnitude, Math.Abs(player_velocity.x)); float calc_z = Mathf.Max(minimum_magnitude, Math.Abs(player_velocity.z));
                    if (player_velocity.x < 0) { calc_x = -calc_x; }
                    if (player_velocity.z < 0) { calc_z = -calc_z; }
                    calc_velocity = new Vector3(calc_x, Mathf.Max(minimum_magnitude * 1.5f, Math.Abs(player_velocity.y)), calc_z);
                }
                player.SetVelocity(calc_velocity);
                cooldown_timer = 0.0f;
            }

            if (bounce_sfx_should_play && bounce_sfx_clip != null && gameController != null)
            {
                AudioClip[] ac = new AudioClip[1];
                ac[0] = bounce_sfx_clip;
                float pitch = Mathf.Clamp(player_velocity.magnitude / (minimum_magnitude / 2.0f), 0.5f, 2.0f);
                gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitSend], ac, 0, pitch);
            }
        }
    }

}
