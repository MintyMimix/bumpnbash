
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PlayerLandingCircle : GlobalTickReceiver
{
    [SerializeField] public float default_size = 20.0f; //53.0f in inspector for 1x1x1 cube
    [NonSerialized] public VRCPlayerApi owner;
    [NonSerialized] public PlayerAttributes playerAttributes;
    [NonSerialized] public float local_tick_timer = 0.0f;
    [NonSerialized] public int cached_team = -1;
    [NonSerialized] public bool cached_teamplay = true;

    //[NonSerialized] private Rigidbody rb;
    public override void Start()
    {
        base.Start();
    }

    public override void OnHyperTick(float tickDeltaTime)
    {
        local_tick_timer += Time.deltaTime;
        if (local_tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            LocalPerTickUpdate();
            local_tick_timer = 0.0f;
        }
    }

    private void LocalPerTickUpdate()
    {
        SetScale();
        if (playerAttributes != null && (cached_team != playerAttributes.ply_team || cached_teamplay != playerAttributes.gameController.option_teamplay)) 
        {
            SetTeamColor();
        }
    }

    private void SetScale()
    {
        if (playerAttributes != null && owner != null)
        {
            float scaleCircle = playerAttributes.ply_scale;
            transform.localScale = new Vector3(default_size * scaleCircle, default_size * scaleCircle, scaleCircle);

            LayerMask layers_to_hit = LayerMask.GetMask("Default", "Player", "WeaponProjectile", "Environment");
            Vector3 capsule_pos = owner.GetPosition() + new Vector3(0.0f, owner.GetAvatarEyeHeightAsMeters() * 0.5f, 0.0f);

            if (Physics.Raycast(capsule_pos, Vector3.down, out RaycastHit hit, 300.0f, layers_to_hit))
            {
                transform.SetPositionAndRotation(hit.point + (Vector3.up * 0.01f), Quaternion.LookRotation(hit.normal));
            }
            else
            {
                transform.SetPositionAndRotation(owner.GetPosition() + new Vector3(0.0f, scaleCircle / default_size, 0.0f), owner.GetRotation());
            }
        }
    }

    private void SetTeamColor()
    {
        Renderer m_Renderer = GetComponent<Renderer>();
        if (m_Renderer != null && playerAttributes.gameController.team_colors != null)
        {
            int team = Mathf.Max(0, playerAttributes.ply_team);
            byte alpha = 255;
            if (playerAttributes.gameController.option_teamplay)
            {
                m_Renderer.material.SetColor("_Color",
                    new Color32(
                    (byte)Mathf.Min(255, playerAttributes.gameController.team_colors[team].r),
                    (byte)Mathf.Min(255, playerAttributes.gameController.team_colors[team].g),
                    (byte)Mathf.Min(255, playerAttributes.gameController.team_colors[team].b),
                    alpha));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor",
                    new Color32(
                    (byte)Mathf.Min(255, playerAttributes.gameController.team_colors[team].r),
                    (byte)Mathf.Min(255, playerAttributes.gameController.team_colors[team].g),
                    (byte)Mathf.Min(255, playerAttributes.gameController.team_colors[team].b),
                    255));
            }
            else
            {
                m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, alpha));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor", new Color32(180, 180, 180, 255));
            }

            cached_team = playerAttributes.ply_team;
            cached_teamplay = playerAttributes.gameController.option_teamplay;
        }
    }
}
