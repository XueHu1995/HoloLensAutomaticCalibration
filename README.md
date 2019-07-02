# HoloLensAutomaticCalibration
Improve the virtual-to-real alignment of HoloLens by automating calibration and reducing the parallax with an eye tracker.\<br>  
Tested with Pupil Lab eye camera. 
+ Real-time target (Aruco marker) tracking: by OpenCV library; 
+ Coordinate transformation within HoloLens: with Webcam intrinsic and WebcamToWorld sourced from MediaCapture class
+ Pixel localisation by 3D modelled gaze from eye tracking data
+ Revise display: modify the high-level 3D target location in world by reversing shader transformation.

## Structure:
+ TrackerManager: handles tracking and revision
+ UDPCommunication: used for data transfer from PC to HoloLens
+ ShaderDebug: store and debug the displayed pixel location in window frame  

## Required library: 
+ OpenCVForUnity https://github.com/EnoxSoftware/HoloLensWithOpenCVForUnityExample, 
+ HoloLensCameraStream https://github.com/VulcanTechnologies/HoloLensCameraStream
## Required off-line calibration: 
+ 1-D screen pose
+ Relative eyecam-webcam pose
