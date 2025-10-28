
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GlobalTickController : UdonSharpBehaviour
{
    public float SLOW_TICK_INTERVAL = 0.5f;
    public float FAST_TICK_INTERVAL = 0.1f;
    public float HYPER_TICK_INTERVAL = 0.05f;
    public ushort BATCH_SIZE = 1000;

    [SerializeField] private float slow_tick_timer = 0.0f;
    [SerializeField] private float fast_tick_timer = 0.0f;
    [SerializeField] private float hyper_tick_timer = 0.0f;

    [SerializeField] private GlobalTickReceiver[] recievers;
    [SerializeField] private int receiver_cnt = 0;

    private void Start()
    {
        if (recievers == null || recievers.Length == 0)
        {
            recievers = new GlobalTickReceiver[BATCH_SIZE];
        }
    }


    void Update()
    {
        if (hyper_tick_timer < HYPER_TICK_INTERVAL) { hyper_tick_timer += Time.deltaTime; }
        else
        {
            if (recievers != null)
            {
                for (int i = 0; i < receiver_cnt; i++)
                {
                    if (recievers[i] == null) { continue; } //{ Remove(i); i--; continue; }
                    recievers[i].OnHyperTick(hyper_tick_timer);
                }
            }
            hyper_tick_timer = 0.0f;

        }

        if (fast_tick_timer < FAST_TICK_INTERVAL) { fast_tick_timer += Time.deltaTime; }
        else
        {
            if (recievers != null)
            {
                for (int i = 0; i < receiver_cnt; i++)
                {
                    if (recievers[i] == null) { continue; }// { Remove(i); i--; continue; }
                    recievers[i].OnFastTick(fast_tick_timer);
                }
            }
            fast_tick_timer = 0.0f;

        }

        if (slow_tick_timer < SLOW_TICK_INTERVAL) { slow_tick_timer += Time.deltaTime; }
        else 
        {
            if (recievers != null)
            {
                for (int i = 0; i < receiver_cnt; i++)
                {
                    if (recievers[i] == null) { continue; }// { Remove(i); i--; continue; }
                    recievers[i].OnSlowTick(slow_tick_timer);
                }
            }
            slow_tick_timer = 0.0f;
        }
    }

    public int CheckForReceiver(GlobalTickReceiver reciever)
    {
        if (recievers == null || reciever == null) { return -1; }
        for (int i = 0; i < receiver_cnt; i++)
        {
            if (i >= recievers.Length) { break; }
            if (recievers[i] == null) { continue; }
            if (recievers[i] == reciever) { return i; }
        }
        return -1;
    }

    public void Add(GlobalTickReceiver receiver)
    {
        int rcv_index = CheckForReceiver(receiver);
        if (rcv_index > -1)
        {
            UnityEngine.Debug.Log("[GlobalTickController]: Attempted to add " + receiver.gameObject.name + " but it already exists at index " + rcv_index);
            return; 
        }

        UnityEngine.Debug.Log("[GlobalTickController]: Adding " + receiver.gameObject.name + " to " + receiver_cnt);

        if (recievers == null || receiver_cnt >= recievers.Length) {
            GlobalTickReceiver[] newrecievers = new GlobalTickReceiver[receiver_cnt + BATCH_SIZE];
            for (int i = 0; i < receiver_cnt; i++)
            {
                if (recievers == null || recievers.Length == 0 || i >= recievers.Length) { break; }
                newrecievers[i] = recievers[i];
            }
            newrecievers[receiver_cnt] = receiver;
            recievers = newrecievers;
            receiver_cnt++;
        }
        else
        {
            recievers[receiver_cnt] = receiver;
            receiver_cnt++;
        }
    }

    public void Remove(int rcv_index)
    {
        if (rcv_index == -1) { return; }

        GlobalTickReceiver[] newrecievers = new GlobalTickReceiver[receiver_cnt - 1];
        int newCount = 0;
        for (int i = 0; i < receiver_cnt; i++)
        {
            if (i == rcv_index) { continue; }
            newrecievers[newCount] = recievers[i];
            newCount++;
        }
        recievers = newrecievers;
        receiver_cnt = newCount;
    }

    public void Remove(GlobalTickReceiver receiver)
    {
        int rcv_index = CheckForReceiver(receiver);
        if (rcv_index == -1) { return; }
        Remove(rcv_index);
    }
}
