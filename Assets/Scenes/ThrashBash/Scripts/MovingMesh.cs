
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MovingMesh : UdonSharpBehaviour
{
    [SerializeField] Rigidbody rb;

    void FixedUpdate()
    {
        rb.AddForce(Vector3.zero);
    }
}
