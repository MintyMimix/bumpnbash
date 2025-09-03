
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_rotate : UdonSharpBehaviour
{
    [SerializeField] public float rotation_speed = 1.0f;
    [SerializeField] public byte rotation_axis = 0;

    private void Update()
    {
        Quaternion rotAdd = Quaternion.Euler(0, 0, 0);
        if (rotation_axis > 2) { rotation_axis = 0; }
        if (rotation_axis == 0) { rotAdd = Quaternion.Euler(rotation_speed, 0, 0); }
        else if (rotation_axis == 1) { rotAdd = Quaternion.Euler(0, rotation_speed, 0); }
        else if (rotation_axis == 2) { rotAdd = Quaternion.Euler(0, 0, rotation_speed); }
        transform.rotation = transform.rotation * rotAdd;
    }
}
