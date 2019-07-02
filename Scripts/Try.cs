using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Try : MonoBehaviour

{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(Camera.main.GetStereoViewMatrix(Camera.StereoscopicEye.Left).ToString("f5"));
        Debug.Log(Camera.main.GetStereoViewMatrix(Camera.StereoscopicEye.Right).ToString("f5"));
        Debug.Log(Camera.main.transform.position.ToString("f5"));

    }
}
