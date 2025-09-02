
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MovingMesh : UdonSharpBehaviour
{
    void FixedUpdate()
    {
        GetComponent<Rigidbody>().AddForce(Vector3.zero);
    }
}
