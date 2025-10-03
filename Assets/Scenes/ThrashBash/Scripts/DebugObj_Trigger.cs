
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DebugObj_Trigger : UdonSharpBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        //UnityEngine.Debug.Log("[COLLIDER_TEST]: OnTriggerEnter() = " + other.name);
    }

    void OnCollisionEnter(Collision collision)
    {
        //UnityEngine.Debug.Log("[COLLIDER_TEST]: OnCollisionEnter() = " + collision.collider.name);
    }
}
