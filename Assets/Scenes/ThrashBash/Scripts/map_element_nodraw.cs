
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class map_element_nodraw : UdonSharpBehaviour
{
    private void Start()
    {
        transform.GetComponent<Renderer>().enabled = false;
    }

}
