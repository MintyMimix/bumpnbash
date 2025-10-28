
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LocalMotionSicknessHelper : GlobalTickReceiver
{
    [SerializeField] public Transform helper_cube_transform;
    [SerializeField] public Transform helper_capsule_transform;
    //[SerializeField] public Renderer helper_cube_renderer;
    //[SerializeField] public Renderer helper_capsule_renderer;
    //[SerializeField] public float linger_duration = 0.25f;
    //[SerializeField] public float fade_duration = 0.25f;
    //[SerializeField] public float speed_mul_threshold = 1.5f;
    [NonSerialized] public float local_tick_timer = 0.0f;
    //[NonSerialized] public float linger_timer = 0.0f;
    //[NonSerialized] public float fade_timer = 0.0f;
    [NonSerialized] public Vector3 helper_cube_default_size = Vector3.zero;
    [NonSerialized] public Vector3 helper_capsule_default_size = Vector3.zero;
    [NonSerialized] public VRCPlayerApi owner;

    //[NonSerialized] private Rigidbody rb;

    public override void Start()
    {
        base.Start();
        helper_cube_default_size = helper_cube_transform.localScale;
        helper_capsule_default_size = helper_capsule_transform.localScale;
        owner = Networking.LocalPlayer;
    }

    public override void OnHyperTick(float tickDeltaTime)
    {
        local_tick_timer += tickDeltaTime;
        if (local_tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            LocalPerTickUpdate();
            local_tick_timer = 0.0f;
        }

        /*if (linger_timer < linger_duration)
        {
            linger_timer += tickDeltaTime;
            helper_capsule_renderer.enabled = true;
        }
        else
        {
            if (fade_timer < fade_duration)
            {
                fade_timer += tickDeltaTime;
                helper_capsule_renderer.material.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 1.0f - (fade_timer / fade_duration)));
            }
            else
            {
                helper_capsule_renderer.enabled = false;
            }
        }*/
    }

    private void LocalPerTickUpdate()
    {
        /*if (Mathf.Abs(owner.GetVelocity().x) > owner.GetRunSpeed() * speed_mul_threshold || Mathf.Abs(owner.GetVelocity().z) > owner.GetRunSpeed() * speed_mul_threshold
            || Mathf.Abs(owner.GetVelocity().y) > owner.GetJumpImpulse()
            )
        {
            linger_timer = 0.0f;
            fade_timer = 0.0f;
            helper_capsule_renderer.material.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        }*/

        helper_capsule_transform.localScale = helper_capsule_default_size * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        helper_cube_transform.localScale = helper_cube_default_size * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
    }

    public override void PostLateUpdate()
    {
        if (owner == null) { return; }
        helper_capsule_transform.position = owner.GetPosition() + new Vector3(0.0f, owner.GetAvatarEyeHeightAsMeters() * 0.5f, 0.0f);
        helper_cube_transform.position = owner.GetPosition();
    }



}

