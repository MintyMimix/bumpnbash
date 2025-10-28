
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DelayedSync : GlobalTickReceiver
{
    public float local_every_second_timer = 0.0f;
    [UdonSynced] public Vector3 position;
    [UdonSynced] public Quaternion rotation;


    public override void Start()
    {
        base.Start();
        position = transform.position;
        rotation = transform.rotation;
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        local_every_second_timer += tickDeltaTime;
        if (local_every_second_timer >= 0.5f)
        {
            LocalPerIntervalUpdate();
            local_every_second_timer = 0.0f;
        }
    }

    void LocalPerIntervalUpdate()
    {
        position = transform.position;
        rotation = transform.rotation;
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        transform.position = position;
        transform.rotation = rotation;
    }
}
