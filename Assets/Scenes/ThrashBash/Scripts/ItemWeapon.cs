
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ItemWeapon : ItemGeneric
{
    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        CheckForSpawnerParent();
    }
    private void FixedUpdate()
    {
        transform.rotation = Networking.LocalPlayer.GetRotation();
    }

}
