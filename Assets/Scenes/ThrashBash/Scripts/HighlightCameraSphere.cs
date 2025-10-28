
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;

public class HighlightCameraSphere : GlobalTickReceiver
{
    [SerializeField] public GameController gameController;
    [SerializeField] public GameObject cameraPlane;
    [SerializeField] public SphereCollider bubbleCollider;
    [NonSerialized] public float scale_from_prefab;
    [NonSerialized] public Vector3 cameraplane_localscale_from_prefab;

    public override void Start()
    {
        base.Start();
        scale_from_prefab = transform.lossyScale.x;
        cameraplane_localscale_from_prefab = cameraPlane.transform.localScale;
    }

    public override void OnHyperTick(float tickDeltaTime)
    {
        // Only process if the photo camera is out
        VRCCameraSettings photoCam = VRCCameraSettings.PhotoCamera;
        if (photoCam == null) { return; }
        ResetPhotoPos(photoCam);
        if (!photoCam.Active) { return; }
        // And that the camera is within our sphere's collider radius
        float distToCollider = Vector3.Distance(photoCam.Position, bubbleCollider.transform.position);
        //UnityEngine.Debug.Log("[CAMERA_TEST]: Dist to camera: " + distToCollider + " versus radius " + collider.radius);

        if (distToCollider <= (0.05f + bubbleCollider.radius) * scale_from_prefab)
        {
            // We'll position our plane according to the camera frame
            //cameraPlane.transform.localScale = new Vector3(photoCam.PixelWidth * 0.00012f, 0.05f, photoCam.PixelHeight * 0.00012f);
            //cameraPlane.transform.position = photoCam.Position + photoCam.Forward * scale_from_prefab;

            cameraPlane.transform.localScale = new Vector3(photoCam.PixelWidth * 0.000121f * 0.15f, 0.05f, photoCam.PixelHeight * 0.000121f * 0.15f);
            cameraPlane.transform.position = photoCam.Position + photoCam.Forward * scale_from_prefab * 0.15f;
            cameraPlane.transform.rotation = photoCam.Rotation * Quaternion.Euler(90.0f, 180.0f, 0.0f);
        }

    }

    public void ResetPhotoPos(VRCCameraSettings photoCam)
    {
        // Otherwise, reset the position
        //cameraPlane.transform.localScale = new Vector3(photoCam.PixelWidth * 0.00005f, 0.05f, photoCam.PixelHeight * 0.00005f);
        cameraPlane.transform.localScale = cameraplane_localscale_from_prefab;
        cameraPlane.transform.position = transform.position;
        cameraPlane.transform.rotation = transform.rotation * Quaternion.Euler(90.0f, 180.0f, 0.0f);
    }

}
