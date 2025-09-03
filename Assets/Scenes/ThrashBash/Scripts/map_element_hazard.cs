
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum damage_mode_name
{
    Constant, Linear, Series, Quadratic, ENUM_LENGTH
}

public class map_element_hazard : BouncePad
{
    [SerializeField] public float base_damage = 10.0f;
    [SerializeField] public byte damage_mode;
    [SerializeField] public float damage_cooldown_duration = 5.0f;
    [NonSerialized] public float damage_cooldown_timer = 0.0f;
    [NonSerialized] public float current_damage = 0.0f;
    [NonSerialized] public uint damage_ticks = 0;


    private void FixedUpdate()
    {
        CooldownTick();
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

        if (gameController == null) { return; }
        var plyAttr = gameController.FindPlayerAttributes(player);
        if (plyAttr == null) { return; }
        plyAttr.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ReceiveDamage", current_damage, Vector3.zero, -1, (int)damage_type_name.Burn, false);
        
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        Bounce(player);
        DamagePlayer(player);
    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
    {
        Bounce(player);
        DamagePlayer(player);
    }

}
