﻿using Amsterdam3D.CameraMotion;
using BruTile.Wms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

   public class CameraManager:MonoBehaviour
    {


    public ICameraExtents CurrentCameraExtends { get; private set; }
    [SerializeField]
    private GameObject currentCamera;
    public Camera currentCameraComponent { get; private set; }
    private StreetViewMoveToPoint streetView;

    [SerializeField]
    private GameObject GodviewCam;

    //idk if public events in a singleton are the best idea
    // Maybe make these ScriptableObject eventhandlers instead?
    public delegate void OnFirstPersonMode();
    public event OnFirstPersonMode OnFirstPersonModeEvent;


    public delegate void OnGodViewMode();
    public event OnGodViewMode OnGodViewModeEvent;


    private Quaternion oldGodViewRotation;



    public CameraMode CameraMode { get; private set; }


    public static CameraManager instance;

    private void Awake()
    {
        streetView = FindObjectOfType<StreetViewMoveToPoint>();
        currentCamera = GodviewCam;
        currentCameraComponent = currentCamera.GetComponent<Camera>();
        CurrentCameraExtends = currentCamera.GetComponent<ICameraExtents>();
        instance = this;
    }


    public void FirstPersonMode(Vector3 position, Quaternion rotation) 
    {
        this.CameraMode = CameraMode.StreetView;

        Vector3 oldPosition = currentCamera.transform.position;
        Quaternion oldRotation = currentCamera.transform.rotation;
        oldGodViewRotation = currentCamera.transform.rotation;
        currentCamera.SetActive(false);
        currentCamera = streetView.EnableFPSCam();
        currentCamera.transform.position = oldPosition;
        currentCamera.transform.rotation = oldRotation;
        currentCameraComponent = currentCamera.GetComponent<Camera>();
        CurrentCameraExtends = currentCamera.GetComponent<ICameraExtents>();
        OnFirstPersonModeEvent?.Invoke();
        MoveCameraToStreet(currentCamera.transform, position, rotation);
    }



    public void MoveCameraToStreet(Transform cameraTransform, Vector3 position, Quaternion rotation, bool reverse = false) 
    {

        if (reverse)
        {
            StartCoroutine(streetView.MoveToPositionReverse(cameraTransform, position, rotation));
        }

        else
        {
            StartCoroutine(streetView.MoveToPosition(cameraTransform, position, rotation));
        }
    }

    

    public void GodViewMode() 
    {
        this.CameraMode = CameraMode.GodView;
        Vector3 currentPosition = currentCamera.transform.position;
        Quaternion rot = currentCamera.transform.rotation;
        currentCamera.SetActive(false);
        currentCamera = GodviewCam;
        currentCamera.transform.position = currentPosition;
        currentCamera.transform.rotation = rot;
        currentCamera.SetActive(true);
        CurrentCameraExtends = currentCamera.GetComponent<ICameraExtents>();
        currentCameraComponent = currentCamera.GetComponent<Camera>();
        OnGodViewModeEvent?.Invoke();
        MoveCameraToStreet(currentCamera.transform, currentPosition + Vector3.up * 400, oldGodViewRotation, true);
    }
}

public enum CameraMode 
{
    GodView,
    StreetView,
    VR
}
