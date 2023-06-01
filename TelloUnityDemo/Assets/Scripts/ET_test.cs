using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Varjo.XR;


/*

public methods:
CalibrateGaze() -- calibrates using set mode
StartLog() -- start logging eye tracking data
EndLog() -- finish logging eye tracking data

may want to call VarjoEyeTracking.IsGaze(Allowed|Available|Calibrated)() before tracking and issue warning to console else
we can get the XR rig with "this"

NOTE: this script does *not* track eye measurements (interpupillary distance, pupil dilation, etc.)
 - we can add this functionality if desired!

*/

// Attach to anything.
public class ET_test : MonoBehaviour
{
    public TC_test TC;

    public float rot_command = 0.1f;
    public float fb_command = 0.1f;


    private float rot_history = 0f;
    private float fb_history = 0f;

    private Vector3 headset_origin = Vector3.zero;
    private Vector3 drone_origin = Vector3.zero;


    [Header("Main camera (under XR Rig)")]
    public Camera xrCamera;

    [Header("Logging toggle key")]
    public KeyCode loggingToggleKey = KeyCode.RightControl;

    // [Header("Log file name (defaults to current date/time)")]
    // public bool useCustomLogFileName = false;
    // public string customLogFileName = "";

    [Header("Logging path (defaults to Logs)")]
    public bool useCustomLogPath = false;
    public string customLogPath = "";

    [Header("Gaze calibration settings")]
    [Tooltip("Legacy - 10 dots without priors; Fast: 5 dots; One Dot: quickest, least accurate")]
    public VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;
    [Tooltip("Keyboard shortcut to request calibration")]
    public KeyCode calibrationKey = KeyCode.Space;

    [Header("Gaze output filter")]
    [Tooltip("Standard: smoothing on gaze data; None: raw data")]
    public VarjoEyeTracking.GazeOutputFilterType gazeFilterType = VarjoEyeTracking.GazeOutputFilterType.None;


    private VarjoEyeTracking.GazeOutputFrequency frequency = VarjoEyeTracking.GazeOutputFrequency.MaximumSupported;
    private List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    private StreamWriter writer = null;
    private bool logging = false;

    private static readonly string[] Columns = { "CaptureTime", "HeadsetPos", "HeadsetRotation", "CombinedGazeForward",
        "FocusDistance", "FocusStability", "CalcXEccentricity", "CalcYEccentricity", "LeftForward", "RightForward", "LeftPosition", "RightPosition" };
    private static string VectorPrecision = "F6"; // preciison after decimal for vector printouts
    private static string DegreePrecision = "F1"; // precision after decimal for calculated eccentricities in degrees


    // Start is called before the first frame update
    void Start()
    {
        VarjoEyeTracking.SetGazeOutputFrequency(frequency);
    }

    // Update is called once per frame
    public void Update()
    {
        if (Input.GetKeyDown(calibrationKey))
            CalibrateGaze();


        if (Input.GetKeyDown(loggingToggleKey))
        {
            if (!logging)
                StartLogging();
            else
                StopLogging();

            return;
        }



        if (logging)
        {
            int dataCount = VarjoEyeTracking.GetGazeList(out dataSinceLastUpdate);

            for (int i = 0; i < dataCount; ++i)
                LogGazeData(dataSinceLastUpdate[i]);
        }
    }

    public void CalibrateGaze()
    {
        VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);
    }

    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("StartLogging was called when already logging. No new log started.");
            return;
        }

        logging = true;

        string logDir = useCustomLogPath ? customLogPath : Application.dataPath + "/Logs/";
        Directory.CreateDirectory(logDir);

        DateTime now = DateTime.Now;
        string fileName = string.Format("ET-{0}-{1:00}-{2:00}-{3:00}-{4:00}", now.Year, now.Month, now.Day, now.Hour, now.Minute);

        string logPath = logDir + fileName + ".csv";
        writer = new StreamWriter(logPath);

        Log(Columns);
        Debug.Log("Log file started at " + logPath);
    }

    public void StopLogging()
    {
        if (!logging)
            return;

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }

        logging = false;
        Debug.Log("Logging ended.");
    }

    void LogGazeData(VarjoEyeTracking.GazeData data)
    {

        if (Input.GetKeyDown(KeyCode.O))
        {
            headset_origin = xrCamera.transform.localPosition;
            drone_origin = TC.drone_position;
            Debug.Log(headset_origin.ToString());
            Debug.Log(drone_origin.ToString());
        }

        float temp = 0f;

        //  Try from here


        float theta = xrCamera.transform.localRotation.ToEulerAngles()[1];

        double[,] R = new double[2, 2];
        R[0, 0] = Math.Cos(theta);
        R[0, 1] = -Math.Sin(theta);
        R[1, 0] = Math.Sin(theta);
        R[1, 1] = Math.Cos(theta);
        double[] vec_in_S1 = new double[2] { xrCamera.transform.localPosition[0], xrCamera.transform.localPosition[2] }; // replace with your values
        double[] vec_in_S2 = new double[2];
        // Matrix-vector multiplication
        for (int i = 0; i < 2; ++i)
        {
            vec_in_S2[i] = 0;
            for (int j = 0; j < 2; ++j)
            {
                vec_in_S2[i] += R[i, j] * vec_in_S1[j];
            }
        }

        float sensitivity = 0.4f;   //var to set speed/scaling
        // sensitivity = 0.4f is control, almost 1:1
        // sensitivity = 0.6f is larger scaling (more sensitive)
        // sensitivity = 0.8f is larger larger scaling

        temp = (float)vec_in_S2[1];
        if (temp > fb_history + 0.01) { 
            temp = sensitivity; 
            //print("forward"); 
        }
        else if (temp < fb_history - 0.01) { 
            temp = -sensitivity; 
            //print("backward"); 
        }
        else {
            temp = 0; 
            //print("zero"); 
        }

        // if (temp != 0) { Debug.Log(temp.ToString()); }


        fb_history = (float)vec_in_S2[1];
        fb_command = temp;

        // Try end here




        temp = xrCamera.transform.localRotation.ToEulerAngles()[1];
        if (temp > rot_history + 0.01) { temp = 0.7f; }
        else if (temp < rot_history - 0.01) { temp = -0.7f; }
        else { temp = 0; }

        rot_history = xrCamera.transform.localRotation.ToEulerAngles()[1];
        rot_command = temp;



        //temp = xrCamera.transform.localPosition[2];
        //if (temp > fb_history + 0.01) { temp = 0.5f; }
        //else if (temp < fb_history - 0.01) { temp = -0.5f; }
        //else { temp = 0; }

        //fb_history = xrCamera.transform.localPosition[2];
        //fb_command = temp;



        // if data isn't valid, don't log
        //if (data.status == varjoeyetracking.gazestatus.invalid)
        //    return;

        string[] logData = new string[Columns.Length];

        // capture time (Unix ms timestamp)
        logData[0] = ((DateTimeOffset)VarjoTime.ConvertVarjoTimestampToDateTime(data.captureTime)).ToUnixTimeMilliseconds().ToString();

        // headset position + rotation
        logData[1] = xrCamera.transform.localPosition.ToString(VectorPrecision);
        logData[2] = xrCamera.transform.localRotation.ToEulerAngles().ToString(VectorPrecision);


        // combined gaze forward
        logData[3] = data.gaze.forward.ToString(VectorPrecision);

        // focus distance
        logData[4] = data.focusDistance.ToString();
        logData[5] = data.focusStability.ToString();

        // calculated X, Y eccentricities
        float x = data.gaze.forward.x;
        float y = data.gaze.forward.y;
        float z = data.gaze.forward.z;
        logData[6] = (Math.Atan(x / z) / Math.PI * 180).ToString(DegreePrecision);
        logData[7] = (Math.Atan(y / z) / Math.PI * 180).ToString(DegreePrecision);

        // left and right eye forward
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[8] = leftInvalid ? "" : data.left.forward.ToString(VectorPrecision);

        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[9] = rightInvalid ? "" : data.right.forward.ToString(VectorPrecision);

        // left and right eye position
        logData[10] = leftInvalid ? "" : data.left.origin.ToString("F3");
        logData[11] = leftInvalid ? "" : data.right.origin.ToString("F3");

        Log(logData);


    }

    void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", "");
            line += values[i] + (i == values.Length - 1 ? "" : ";");
        }

        writer.WriteLine(line);
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }
}
