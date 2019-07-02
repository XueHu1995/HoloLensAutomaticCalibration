using System;
using System.Collections.Generic;

using HoloLensCameraStream;

using OpenCVForUnity;

using UnityEngine;
using UnityEngine.XR.WSA;

using Application = UnityEngine.WSA.Application;
using Resolution = HoloLensCameraStream.Resolution;
using VideoCapture = HoloLensCameraStream.VideoCapture;

public class TrackerManager : MonoBehaviour
{
    [Tooltip("calibrated eyecam to webcam pose")]
    public Matrix4x4 _eyecamTowebcam;

    public MarkerDetection markerDetection;

    // Game objects
    public GameObject Target;
    public GameObject Revised;
    public GameObject Collider;

    //// Camera intrinsics
    //public static float focal_x, focal_y, cx, cy;

    // Variable for Camera
    private Matrix4x4 _cameraToWorldMatrix = Matrix4x4.zero;
    private byte[] _latestImageBytes;
    private Matrix4x4 _projectionMatrix = Matrix4x4.zero;
    private Resolution _resolution;
    private IntPtr _spatialCoordinateSystemPtr;
    private VideoCapture _videoCapture;


    private void Start()
    {
        _spatialCoordinateSystemPtr = WorldManager.GetNativeISpatialCoordinateSystemPtr();
        // careful about the unit, in [m]
        _eyecamTowebcam.SetRow(0, new Vector4(0.982132575823016f, 0.180790274679324f, 0.0507917048834135f, 0.0257843283223507f));
        _eyecamTowebcam.SetRow(1, new Vector4(-0.187049661233720f, 0.942641262567177f, 0.276404692989064f, -0.0505617089471696f));
        _eyecamTowebcam.SetRow(2, new Vector4(0.00194724308149649f, -0.280414331142444f, 0.960036305051964f, 0.0258339695125850f));
        _eyecamTowebcam.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
    }

    private void OnDestroy()
    {
        Destroy();
    }

    private void OnApplicationQuit()
    {
        Destroy();
    }

    private void Destroy()
    {
        if (_videoCapture != null)
        {
            _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
            _videoCapture.Dispose();
        }
    }

    private void OnVideoCaptureCreated(VideoCapture videoCapture)
    {
        if (videoCapture == null)
        {
            Debug.LogError("Did not find a video capture object. You may not be using the HoloLens.");
            return;
        }

        _videoCapture = videoCapture;

        //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(_spatialCoordinateSystemPtr);

        _resolution = CameraStreamHelper.Instance.GetLowestResolution();
        float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);

        Debug.Log("Frame rate: " + Mathf.RoundToInt(frameRate));
        Debug.Log("_resolution: " + _resolution.height + " x " + _resolution.width);

        videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

        CameraParameters cameraParams = new CameraParameters();
        cameraParams.cameraResolutionHeight = _resolution.height;
        cameraParams.cameraResolutionWidth = _resolution.width;
        cameraParams.frameRate = Mathf.RoundToInt(frameRate);
        cameraParams.pixelFormat = CapturePixelFormat.BGRA32;
        cameraParams.rotateImage180Degrees = false;
        cameraParams.enableHolograms = true;

        videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);

    }

    private void OnVideoModeStarted(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            Debug.LogWarning("Could not start video mode.");
            return;
        }

        Debug.Log("Video capture started.");
    }



    private void OnFrameSampleAcquired(VideoCaptureSample sample)
    {
        // upload image bytes
        if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength) _latestImageBytes = new byte[sample.dataLength];
        sample.CopyRawImageDataIntoBuffer(_latestImageBytes);

        // transform matrix
        float[] cameraToWorldMatrixAsFloat;
        float[] projectionMatrixAsFloat;
        if (sample.TryGetCameraToWorldMatrix(out cameraToWorldMatrixAsFloat) == false || sample.TryGetProjectionMatrix(out projectionMatrixAsFloat) == false)
        {
            Debug.Log("Failed to get camera to world or projection matrix");
            return;
        }

        _cameraToWorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(cameraToWorldMatrixAsFloat);
        _projectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(projectionMatrixAsFloat);

        //focal_x = _projectionMatrix[0, 0] * _resolution.width / 2;
        //focal_y = _projectionMatrix[1, 1] * _resolution.height / 2;
        //cx = _resolution.width / 2 + _resolution.width / 2 * _projectionMatrix[0, 2];
        //cy = _resolution.height / 2 + _resolution.height / 2 * _projectionMatrix[1, 2];
        // Debug.Log("focal_x: " + focal_x.ToString("f5") + " focal_y: " + focal_y.ToString("f5") + " cx: " + cx.ToString("f5") + " cy: " + cy.ToString("f5"));

        sample.Dispose();

        // Opencv mat conversion: to RGB mat
        Mat frameBGRA = new Mat(_resolution.height, _resolution.width, CvType.CV_8UC4);
        //Array.Reverse(_latestImageBytes);
        frameBGRA.put(0, 0, _latestImageBytes);
        //Core.flip(frameBGRA, frameBGRA, 0);

        Mat frameBGR = new Mat(_resolution.height, _resolution.width, CvType.CV_8UC3);
        Imgproc.cvtColor(frameBGRA, frameBGR, Imgproc.COLOR_BGRA2BGR);
        Mat RGB = new Mat();
        Imgproc.cvtColor(frameBGR, RGB, Imgproc.COLOR_BGR2RGB);
        // Target detection: marker location in webcam
        Vector4 location = markerDetection.DetectMarkers(RGB);

        // Application thread. Because some operations are only allowed in main thread, not event thread
        Application.InvokeOnAppThread(() =>
        {
            if (MarkerDetection.success == true)// if detected
            {
                Debug.Log("catch!");
                // target: marker -> webcam -> world.      
                Vector3 target = _cameraToWorldMatrix * location;
                Debug.Log("target: " + target.ToString("f5"));
                Target.transform.position = target;

                // render with the right matrix
                Matrix4x4 V = Camera.main.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
                Matrix4x4 P = GL.GetGPUProjectionMatrix(Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), false); // openGL to DX
                Matrix4x4 M = Target.transform.localToWorldMatrix;
                Target.GetComponent<Renderer>().material.SetMatrix("MATRIX_MVP", P * V * M); // render with custom pipeline
                Target.GetComponent<Renderer>().enabled = true; // show now
                Vector4 vertex = P * V * M * new Vector4(0f, 0f, 0f, 1f); // vertex location on the rendering plane

                //Vector3 cam = V.GetColumn(3);

                //// ----------------------------------------------------
                //// collide a cam-target ray with display collider
                //Vector3 camToTarget = target - cam;
                //Ray ray = new Ray(cam, camToTarget);
                //RaycastHit hit;

                //if (Physics.Raycast(ray, out hit, 2f))
                //{
                //    Vector3 pos = hit.point; // physical hitted point
                //    Target.transform.position = pos;
                //    Debug.Log("hit pos: " + pos.ToString("f5"));

                //    Matrix4x4 M = Target.transform.localToWorldMatrix;
                //    Target.GetComponent<Renderer>().material.SetMatrix("MATRIX_MVP", P * V * M); // render with custom pipeline
                //    Target.GetComponent<Renderer>().enabled = true; // show now
                //    // Vector4 vertex = P * V * M * new Vector4(0f, 0f, 0f, 1f); // vertex location on the rendering plane
                //}
                  
                // ---------------------------------------------------
                // get eye position in eyecam
                string[] eyedata = UDPCommunication.message.Split(',');
                Vector4 eye_pos_e = new Vector4(float.Parse(eyedata[0]) / 1000, float.Parse(eyedata[1]) / 1000, float.Parse(eyedata[2]) / 1000, 1.0f); // in [m]
                Debug.Log("eye in eyecam: " + eye_pos_e.ToString("f5"));

                // eye: eyecam -> webcam -> world.
                Vector3 eye_pos_w = _cameraToWorldMatrix * _eyecamTowebcam * eye_pos_e;
                Debug.Log("eye in world: " + eye_pos_w.ToString("f5"));

                // ----------------------------------------------------
                // collide a eye-target ray with display collider
                Vector3 eyeToTarget = target - eye_pos_w;
                Ray ray_revised = new Ray(eye_pos_w, eyeToTarget);
                RaycastHit hit_revised;

                if (Physics.Raycast(ray_revised, out hit_revised, 2f))
                {
                    Vector4 pos_revised = hit_revised.point; // physical hitted point
                    //Revised.transform.position = pos_revised;
                    Debug.Log("hit_revised pos: " + pos_revised.ToString("f5"));
                    pos_revised.w = 1.0f;
                    // calculate hitted vertex, scale w to the same depth
                    Vector4 vertex_hit = P * V * pos_revised;
                    float scale = vertex.w / vertex_hit.w;
                    Vector4 vertex_hit_scaled = new Vector4(vertex_hit.x * scale, vertex_hit.y * scale, vertex_hit.z, vertex_hit.w * scale);

                    // retrieve the world location
                    Vector3 pos_scaled = V.inverse * P.inverse * vertex_hit_scaled;
                    Revised.transform.position = pos_scaled;
                    // position the revised target and render it
                    Matrix4x4 M_revised = Revised.transform.localToWorldMatrix;
                    Revised.GetComponent<Renderer>().material.SetMatrix("MATRIX_MVP", P * V * M_revised);
                    Revised.GetComponent<Renderer>().enabled = true;

                    Debug.Log("webcameraToWorldMatrix:\n" + _cameraToWorldMatrix.ToString("f5"));
                    Debug.Log("WorldToRightMatrix:\n" + V.ToString("f5"));
                    Debug.Log("RightGLProjectionMatrix:\n" + P.ToString("f5"));

                    Debug.Log("detected target location: " + Target.transform.position.ToString("f5"));
                    Debug.Log("revised target location: " + Revised.transform.position.ToString("f5"));

                    Debug.Log("rendering vertex: " + vertex.ToString("f5"));
                    Debug.Log("hit vertex: " + vertex_hit.ToString("f5"));
                    Debug.Log("revised rendering vertex: " + vertex_hit_scaled.ToString("f5"));
                }
            }
            else
            {
                Revised.GetComponent<Renderer>().enabled = false;
                Target.GetComponent<Renderer>().enabled = false; // hide 
            }
        }, false);

    }
}


  