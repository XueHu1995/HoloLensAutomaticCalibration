using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OpenCVForUnity;


public class MarkerDetection : MonoBehaviour
{
    public float markerSize = 0.1f;

    public ArUcoDictionary dictionaryId;

    [HideInInspector]
    public static bool success = false; // default detection status

    [HideInInspector]
    public Mat camMatrix; // formatted camera intrinsic

    [HideInInspector]
    public MatOfDouble distCoeffs; // formatted camera distortion

    double[] distortion = { -0.273471973768806f, 0.594274000317825f, 0f, 0f }; // radial dis (2) tangential dis (2)

    // Use this for initialization
    private void Start()
    {
        Debug.Log("initialised");

        // web camera intrinsics
        //camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        //camMatrix.put(0, 0, 1006.224f);
        //camMatrix.put(0, 1, 0);
        //camMatrix.put(0, 2, 448f);
        //camMatrix.put(1, 0, 0);
        //camMatrix.put(1, 1, 1006.224f);
        //camMatrix.put(1, 2, 252f);
        //camMatrix.put(2, 0, 0);
        //camMatrix.put(2, 1, 0);
        //camMatrix.put(2, 2, 1.0f);

        camMatrix.put(0, 0, 1040.4576f);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, 488.86656f);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, 1039.5907f);
        camMatrix.put(1, 2, 228.46068f);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);
        distCoeffs = new MatOfDouble(distortion);

    }    

    public Vector4 DetectMarkers(Mat rgbMat)
    {
        Mat ids = new Mat();
        List<Mat> corners = new List<Mat>();
        List<Mat> rejectedCorners = new List<Mat>();
        Mat rvecs = new Mat();
        //Mat rotMat = new Mat(3, 3, CvType.CV_64FC1);
        Mat tvecs = new Mat();
        DetectorParameters detectorParams = DetectorParameters.create();
        Dictionary dictionary = Aruco.getPredefinedDictionary((int)dictionaryId);
        Vector4 ARM = new Vector3();

        // detect markers
        Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejectedCorners, camMatrix, distCoeffs);

        // if at least one marker detected
        if (ids.total() > 0)
        {
            // estimate pose, from marker to camera
            Aruco.estimatePoseSingleMarkers(corners, markerSize, camMatrix, distCoeffs, rvecs, tvecs);

            // Marker centre location
            double[] tvecArr = tvecs.get(0, 0);
            
            // image flip + coordinates transformation (OpenCV to Unity)           
            ARM = new Vector4((float)tvecArr[0] + 0.005f, (float)tvecArr[1] + 0.005f, -(float)tvecArr[2], 1f);

            Debug.Log("raw ARM " + ARM.ToString("f5"));

            //// Rotation and convert to Unity matrix format
            //double[] rvecArr = rvecs.get(0, 0);
            //Mat rvec = new Mat(3, 1, CvType.CV_64FC1);
            //rvec.put(0, 0, rvecArr);
            //Calib3d.Rodrigues(rvec, rotMat);
            //double[] rotMatArr = new double[rotMat.total()];
            //rotMat.get(0, 0, rotMatArr);

            //// Transformation matrix
            //Matrix4x4 transformationM = new Matrix4x4();
            //transformationM.SetRow(0, new Vector4((float)rotMatArr[0], (float)rotMatArr[1], (float)rotMatArr[2], (float)tvecArr[0]));
            //transformationM.SetRow(1, new Vector4((float)rotMatArr[3], (float)rotMatArr[4], (float)rotMatArr[5], (float)tvecArr[1]));
            //transformationM.SetRow(2, new Vector4((float)rotMatArr[6], (float)rotMatArr[7], (float)rotMatArr[8], (float)tvecArr[2]));
            //transformationM.SetRow(3, new Vector4(0, 0, 0, 1));
            //Debug.Log("transformationM " + transformationM.ToString());

            success = true;
        }
        else
        {
            Debug.Log("not detected");
            success = false;
        }
            
        return ARM;
    }

    public enum ArUcoDictionary
    {
        DICT_4X4_50 = Aruco.DICT_4X4_50,
        DICT_4X4_100 = Aruco.DICT_4X4_100,
        DICT_4X4_250 = Aruco.DICT_4X4_250,
        DICT_4X4_1000 = Aruco.DICT_4X4_1000,
        DICT_5X5_50 = Aruco.DICT_5X5_50,
        DICT_5X5_100 = Aruco.DICT_5X5_100,
        DICT_5X5_250 = Aruco.DICT_5X5_250,
        DICT_5X5_1000 = Aruco.DICT_5X5_1000,
        DICT_6X6_50 = Aruco.DICT_6X6_50,
        DICT_6X6_100 = Aruco.DICT_6X6_100,
        DICT_6X6_250 = Aruco.DICT_6X6_250,
        DICT_6X6_1000 = Aruco.DICT_6X6_1000,
        DICT_7X7_50 = Aruco.DICT_7X7_50,
        DICT_7X7_100 = Aruco.DICT_7X7_100,
        DICT_7X7_250 = Aruco.DICT_7X7_250,
        DICT_7X7_1000 = Aruco.DICT_7X7_1000,
        DICT_ARUCO_ORIGINAL = Aruco.DICT_ARUCO_ORIGINAL,
    }
}
