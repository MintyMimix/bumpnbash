
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GlobalTickReceiver : UdonSharpBehaviour
{
    [SerializeField] public GlobalTickController tickController;

    public virtual void Start()
    {
        if (tickController == null)
        {
            GameObject tcObj = GameObject.Find("GlobalTickController");
            if (tcObj != null) { tickController = tcObj.GetComponent<GlobalTickController>(); }
        }

        if (tickController != null)
        {
            tickController.Add(this);
        }
    }

    public virtual void OnDestroy()
    {
        if (tickController != null)
        {
            tickController.Remove(this);
        }
    }

    public virtual void OnHyperTick(float tickDeltaTime)
    {

    }

    public virtual void OnFastTick(float tickDeltaTime)
    {

    }

    public virtual void OnSlowTick(float tickDeltaTime)
    {

    }
}
