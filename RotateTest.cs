
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RotateTest : UdonSharpBehaviour
{
    [SerializeField] public Transform faceTowards;
    [SerializeField] public Quaternion quatOut;
    void Update()
    {
        Quaternion faceScoreboard = new Quaternion();
        faceScoreboard = Quaternion.LookRotation((faceTowards.position - transform.position).normalized, Vector3.up);
        transform.rotation = faceScoreboard;
        quatOut = faceScoreboard;
        //
    }
}
