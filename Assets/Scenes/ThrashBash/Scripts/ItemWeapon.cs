
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ItemWeapon : ItemGeneric
{
    private void Start()
    {
        CheckForSpawnerParent();
    }
    private void FixedUpdate()
    {
        transform.rotation = Networking.LocalPlayer.GetRotation();
    }

}
