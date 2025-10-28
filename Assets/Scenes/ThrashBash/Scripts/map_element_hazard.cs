
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum damage_mode_name
{
    Constant, Linear, Series, Quadratic, ENUM_LENGTH
}
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class map_element_hazard : BouncePad
{
    [SerializeField] public float base_damage = 10.0f;
    [SerializeField] public byte damage_mode;
    [SerializeField] public float damage_cooldown_duration = 5.0f;
    [NonSerialized] public float damage_cooldown_timer = 0.0f;
    [NonSerialized] public float current_damage = 0.0f;
    [NonSerialized] public uint damage_ticks = 0;


    public override void OnFastTick(float tickDeltaTime)
    {
        CooldownTick(tickDeltaTime);
        if (damage_cooldown_timer < damage_cooldown_duration)
        {
            damage_cooldown_timer += Time.deltaTime;
        }
        else
        {
            current_damage = 0.0f;
            damage_ticks = 0;
        }

    }

    public void DamagePlayer(VRCPlayerApi player)
    {
        if (gameController == null) { return; }
        //var plyAttr = gameController.FindPlayerAttributes(player);
        PlayerAttributes plyAttr = gameController.local_plyAttr;
        if (plyAttr == null) { return; }
        if (plyAttr.hazard_timer < plyAttr.hazard_cooldown) { return; }

        damage_ticks++;
        damage_cooldown_timer = 0.0f;

        switch (damage_mode)
        {
            case (int)damage_mode_name.Constant:
                current_damage = base_damage;
                break;
            case (int)damage_mode_name.Linear:
                current_damage += base_damage;
                break;
            case (int)damage_mode_name.Series:
                current_damage += (base_damage * damage_ticks);
                break;
            case (int)damage_mode_name.Quadratic:
                if (current_damage == 0) { current_damage = base_damage; }
                current_damage *= 2;
                break;
            default:
                break;
        }

        Vector3 hitSpot = GetComponent<Collider>().ClosestPointOnBounds(player.GetPosition());
        // Since we are now only processing these events locally, just call your own plyAttr directly
        //plyAttr.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ReceiveDamage", current_damage, Vector3.zero, hitSpot, -1, (int)damage_type_name.HazardBurn, false);
        plyAttr.ReceiveDamage(current_damage, Vector3.zero, hitSpot, -1, (int)damage_type_name.HazardBurn, false, 0);
        Bounce(player);
    }

    public override void OnPlayerTriggerStay(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        DamagePlayer(player);
    }

    public override void OnPlayerCollisionStay(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) { return; }
        DamagePlayer(player);
    }

}
