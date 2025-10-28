
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class map_element_trail_player : GlobalTickReceiver
{
    public GameController gameController;
    public ParticleSystem particle;
    public VRCPlayerApi.TrackingDataType trackingTarget;
    VRCPlayerApi playerApi;
    bool isInEditor;

    public override void Start()
    {
        base.Start();
        playerApi = Networking.LocalPlayer;
        isInEditor = playerApi == null; // PlayerApi will be null in editor
    }

    public override void OnHyperTick(float tickDeltaTime)
    {
        // PlayerApi data will only be valid in game so we don't run the update if we're in editor
        if (isInEditor)
            return;

        VRCPlayerApi.TrackingData trackingData = playerApi.GetTrackingData(trackingTarget);
        //Vector3 eyeHeightAdj = Vector3.up * (0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f));
        transform.SetPositionAndRotation(playerApi.GetPosition() + (playerApi.GetVelocity() / 1.75f), trackingData.rotation);

        if (gameController != null && gameController.local_ppp_options != null && particle != null)
        {
            var particle_emission = particle.emission;
            particle_emission.enabled = !playerApi.IsPlayerGrounded() && gameController.local_ppp_options.particles_on;
        }
    }

}
