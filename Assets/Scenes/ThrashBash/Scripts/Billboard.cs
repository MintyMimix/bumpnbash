
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Billboard : GlobalTickReceiver
{
    public override void Start()
    {
        base.Start();
    }

    public override void OnFastTick(float tickDeltaTime)
    {
        transform.rotation = Networking.LocalPlayer.GetRotation();
    }
}
