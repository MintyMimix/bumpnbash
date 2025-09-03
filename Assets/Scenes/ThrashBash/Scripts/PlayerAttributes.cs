
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class PlayerAttributes : UdonSharpBehaviour
{
    [UdonSynced] public int ply_state;
    [UdonSynced] public float ply_dp;
    [UdonSynced] public int ply_lives;
    public GameController gameController;

    void Start()
    {
        
    }

    [NetworkCallable]
    public void HitOtherPlayer(int attacker_id) 
    {
        // Only the attacker should have a registered hit
        if (attacker_id != Networking.LocalPlayer.playerId) { return; }
        gameController.snd_game_sfx_sources[(int)game_sfx_index.HitSend].Play();
    }

    [NetworkCallable]
    public void ReceiveDamage(float damage, Vector3 forceDirection, int attacker_id)
    {
        if (attacker_id != Networking.LocalPlayer.playerId) { return; }
        // To-Do: Add defense/offense stats
        var calcDmg = damage;
        var modForceDirection = forceDirection;

        if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, 1.0f, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, 0.33f, 0.0f); }
        var calcForce = (modForceDirection + new Vector3(0.0f, 0.33f, 0.0f));
        calcForce *= Mathf.Pow((calcDmg + ply_dp) / 10.0f, 1.333f);

        gameController.snd_game_sfx_sources[(int)game_sfx_index.HitReceive].Play();
        //HitReceiveSound.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
        //HitReceiveSound.GetComponent<AudioSource>().Play();

        Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);

        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        //lastHitByPly = attackerPly;
        //lastHitByDmg = calcDmg;
        ply_dp += calcDmg;

        if (attacker_id >= 0) { 
            SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attacker_id);
        }
    }

}
