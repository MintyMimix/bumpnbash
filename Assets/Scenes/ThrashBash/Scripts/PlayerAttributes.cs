
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum player_state_name
{
    Inactive, Joined, Alive, Respawning, Dead, Spectator
}

public class PlayerAttributes : UdonSharpBehaviour
{

    [UdonSynced] public int ply_state;
    [UdonSynced] public float ply_dp;
    [UdonSynced] public float ply_dp_default;
    [UdonSynced] public int ply_lives;
    [UdonSynced] public float ply_respawn_duration;
    public VRCPlayerApi last_hit_by_ply;
    public int last_kill_ply = -1;
    public float last_kill_duration = 4.0f;
    public float last_kill_timer = 0.0f;

    public GameController gameController;
    public float ply_respawn_timer = 0.0f;

    // To-Do: Have all projectile damage scale to a configurable factor, which is then auto-scaled to the # of players

    void Start()
    {

    }

    private void Update()
    {
        if (ply_respawn_timer < ply_respawn_duration)
        {
            ply_respawn_timer += Time.deltaTime;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            ply_state = (int)player_state_name.Alive;
        }
        else if (ply_state == (int)player_state_name.Dead && gameController.round_state == (int)round_state_name.Ongoing)
        {
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CheckAllPlayerLives");
            ply_respawn_timer = 0.0f;
        }

        if (last_kill_timer < last_kill_duration && last_kill_ply > -1)
        {
            last_kill_timer += Time.deltaTime;
        }
        else if (last_kill_timer >= last_kill_duration && last_kill_ply > -1)
        {
            last_kill_ply = -1;
        }
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
        //if (attacker_id == Networking.LocalPlayer.playerId) { return; }
        if (ply_state != (int)player_state_name.Alive) { return; }
        // To-Do: Add defense/offense stats
        var calcDmg = damage;
        var modForceDirection = forceDirection;

        if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, 1.0f, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, 0.33f, 0.0f); }
        var calcForce = (modForceDirection + new Vector3(0.0f, 0.33f, 0.0f));
        calcForce *= Mathf.Pow((calcDmg + ply_dp) / 4.25f, 1.05f);

        gameController.snd_game_sfx_sources[(int)game_sfx_index.HitReceive].Play();
        //HitReceiveSound.transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());
        //HitReceiveSound.GetComponent<AudioSource>().Play();

        Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);

        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        last_hit_by_ply = VRCPlayerApi.GetPlayerById(attacker_id);
        ply_dp += calcDmg;

        if (attacker_id >= 0) {
            SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attacker_id);
        }
    }

    [NetworkCallable]
    public void KillOtherPlayer(int attackerPlyId, int defenderPlyId)
    {
        if (attackerPlyId != Networking.LocalPlayer.playerId) { return; }
        gameController.snd_game_sfx_sources[(int)game_sfx_index.Kill].Play();
        last_kill_timer = 0.0f;
        last_kill_ply = defenderPlyId;
    }

    [NetworkCallable]
    public void HandleLocalPlayerDeath()
    {
        gameController.snd_game_sfx_sources[(int)game_sfx_index.Death].GetComponent<AudioSource>().Play();
        ply_respawn_timer = 0;
        if (ply_state == (int)player_state_name.Alive && gameController.round_state == (int)round_state_name.Ongoing)
        {
            ply_state = (int)player_state_name.Respawning;
            ply_lives--;
        }
        else if (ply_state == (int)player_state_name.Dead || gameController.round_state == (int)round_state_name.Ready)
        {
            ply_state = (int)player_state_name.Respawning;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            //UnityEngine.Debug.Log("Stop trying to die while respawning!");
        }
        else
        {
            //UnityEngine.Debug.Log("Whoa, you died in an unusual way! Contact a developer!");
        }
        // To-Do: Manage behavior based on GameHandler.LivesMode
        if (ply_lives > 0)
        {
            ply_dp = ply_dp_default;
            // To-Do: Remove all active powerups
            // To-Do: Reset stats based on GameHandler's settings
            // plyAtkMul = 1.0f;
            gameController.TeleportLocalPlayerToGameSpawnZone();
        }
        else
        {
            gameController.TeleportLocalPlayerToReadyRoom();
            ply_state = (int)player_state_name.Dead;

        }
        if (last_hit_by_ply != null) { 
            SendCustomNetworkEvent(NetworkEventTarget.All, "KillOtherPlayer", last_hit_by_ply.playerId, Networking.LocalPlayer.playerId); 
        }
        
    }

}
