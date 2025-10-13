
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class CullObject : UdonSharpBehaviour
{
    [Tooltip("Defines if the culling should just disable the animator & renderer [0], or the object entirely [1]")]
    [SerializeField] public byte cullBehaviorType = 0; 
    [SerializeField] public bool cullChildren = true;
    [SerializeField] public bool excludeObjOwner = false;
    [SerializeField] public float distanceMultiplier = 1.0f;
    [SerializeField] public GameController gameController;
    [NonSerialized] public float CULL_DIST_NEAR = 99999.0f; // Distance to no longer render the weapon model. Quest is 16.5f
    [NonSerialized] public float CULL_DIST_FAR = 99999.0f; // Distance to no longer animate the weapon. Quest is 9.5f.
    [NonSerialized] public bool far_dist_exceeded = false;
    [NonSerialized] public bool near_dist_exceeded = false;
    [NonSerialized] public Animator[] animator_list;
    [NonSerialized] public Renderer[] renderer_list;

    void Start()
    {
        if (gameController.flag_for_mobile_vr.activeInHierarchy)
        {
            CULL_DIST_FAR = ((int)GLOBAL_CONST.RENDER_DISTANCE_FAR_QUEST) / 10.0f;
            CULL_DIST_NEAR = ((int)GLOBAL_CONST.RENDER_DISTANCE_NEAR_QUEST) / 10.0f;
        }
        else
        {
            CULL_DIST_FAR = ((int)GLOBAL_CONST.RENDER_DISTANCE_FAR_PC) / 10.0f;
            CULL_DIST_NEAR = ((int)GLOBAL_CONST.RENDER_DISTANCE_NEAR_PC) / 10.0f;
        }



        if (cullChildren)
        {
            int r_index = 0; int a_index = 0;
            int r_size = 0; int a_size = 0;
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Renderer>() != null)
                {
                    r_size++;
                }
                if (child.GetComponent<Animator>() != null)
                {
                    a_size++;
                }
            }
            renderer_list = new Renderer[r_size];
            animator_list = new Animator[a_size];
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Renderer>() != null)
                {
                    renderer_list[r_index] = child.GetComponent<Renderer>();
                    r_index++;
                }
                if (child.GetComponent<Animator>() != null)
                {
                    animator_list[a_index] = child.GetComponent<Animator>();
                    a_index++;
                }
            }
        }
        else
        {
            if (transform.GetComponent<Renderer>() != null) 
            { 
                renderer_list = new Renderer[1]; 
                renderer_list[0] = transform.GetComponent<Renderer>(); 
            }
            if (transform.GetComponent<Animator>() != null)
            {
                animator_list = new Animator[1]; 
                animator_list[0] = transform.GetComponent<Animator>();
            }
        }


    }

    private void Update()
    {
        if (!excludeObjOwner || (excludeObjOwner && !Networking.IsOwner(gameObject)))
        {
            float dist_far = CULL_DIST_FAR * distanceMultiplier;
            float dist_near = CULL_DIST_NEAR * distanceMultiplier;
            if (gameController != null && ((gameController.local_plyAttr != null && gameController.local_plyAttr.in_ready_room) || gameController.highlight_cameras_active_cnt > 0))
            {
                // If we are in the ready room (for spectating reasons) or are snapping a highlight photo, do not cull
                dist_far = 999999.0f;
                dist_near = 999999.0f;
            }
            else if (gameController != null && gameController.local_plyAttr != null)
            {
                // Otherwise, scale distance with player size
                dist_far *= (1.0f + gameController.local_plyAttr.ply_scale) / 2.0f;
                dist_near *= (1.0f + gameController.local_plyAttr.ply_scale) / 2.0f;
            }

            if (Vector3.Distance(Networking.LocalPlayer.GetPosition(), transform.position) >= dist_far)
            {
                if (cullBehaviorType == 1) { gameObject.SetActive(false); }

                if (!far_dist_exceeded || !near_dist_exceeded)
                {
                    if (renderer_list != null && renderer_list.Length > 0)
                    {
                        for (int i = 0; i < renderer_list.Length; i++)
                        {
                            if (cullBehaviorType == 0) { renderer_list[i].enabled = false; }
                        }
                    }
                    if (animator_list != null && animator_list.Length > 0)
                    {
                        for (int i = 0; i < animator_list.Length; i++)
                        {
                            if (cullBehaviorType == 0) { animator_list[i].enabled = false; }
                        }
                    }
                    far_dist_exceeded = true;
                    near_dist_exceeded = true;
                }

            }
            else if (Vector3.Distance(Networking.LocalPlayer.GetPosition(), transform.position) >= dist_near)
            {
                if (cullBehaviorType == 1) { gameObject.SetActive(true); } // This line is unreachable, but putting here for posterity

                if (!near_dist_exceeded || far_dist_exceeded)
                {
                    if (renderer_list != null && renderer_list.Length > 0)
                    {
                        for (int i = 0; i < renderer_list.Length; i++)
                        {
                            if (cullBehaviorType == 0) { renderer_list[i].enabled = true; }
                        }
                    }
                    if (animator_list != null && animator_list.Length > 0)
                    {
                        for (int i = 0; i < animator_list.Length; i++)
                        {
                            if (cullBehaviorType == 0) { animator_list[i].enabled = false; }
                        }
                    }
                    far_dist_exceeded = false;
                    near_dist_exceeded = true;
                }
            }
            else
            {
                if (cullBehaviorType == 1) { gameObject.SetActive(true); } // This line is unreachable, but putting here for posterity

                if (near_dist_exceeded || far_dist_exceeded)
                {
                    if (renderer_list != null && renderer_list.Length > 0)
                    {
                        for (int i = 0; i < renderer_list.Length; i++)
                        {
                            if (cullBehaviorType == 0) { renderer_list[i].enabled = true; }
                        }
                    }
                    if (animator_list != null && animator_list.Length > 0)
                    {
                        for (int i = 0; i < animator_list.Length; i++)
                        {
                            if (cullBehaviorType == 0) { animator_list[i].enabled = true; }
                        }
                    }
                    near_dist_exceeded = false;
                    far_dist_exceeded = false;
                }
            }
        }
    }
}
