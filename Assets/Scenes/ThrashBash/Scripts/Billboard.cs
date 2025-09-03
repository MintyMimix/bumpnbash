
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Billboard : UdonSharpBehaviour
{
    void Update()
    {
        transform.rotation = Networking.LocalPlayer.GetRotation();
    }
}
