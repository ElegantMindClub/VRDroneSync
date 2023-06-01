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
public class EyeTracking : MonoBehaviour
{
    public TelloController TC;

    public Vector3 headset_origin = Vector3.zero;
    public Vector3 drone_origin = Vector3.zero;
    public Vector3 drone_origin_rotation = Vector3.zero;

    public double[] headset_position_1 = new double[3] { 0, 0, 0 };
    public double[] headset_position_2 = new double[3] { 0, 0, 0 };

    public Vector3 headset_position = Vector3.zero;
    public double[] pos_diff = { 0, 0 };

    public float rot_command = 0f;
    // public float fb_command = 0f;
    public float[] pos_command = new float[3] { 0, 0, 0 };


    private float rot_history = 0f;
    private float fb_history = 0f;

    public bool calibrated = false;


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


        // ****************** Before 5/31 ********************

        //float temp = 0f;

        ////  Try from here


        //float theta = xrCamera.transform.localRotation.ToEulerAngles()[1];

        //double[,] R = new double[2, 2];
        //R[0, 0] = Math.Cos(theta);
        //R[0, 1] = -Math.Sin(theta);
        //R[1, 0] = Math.Sin(theta);
        //R[1, 1] = Math.Cos(theta);
        //double[] vec_in_S1 = new double[2] { xrCamera.transform.localPosition[0], xrCamera.transform.localPosition[2] }; // replace with your values
        //double[] vec_in_S2 = new double[2];
        //// Matrix-vector multiplication
        //for (int i = 0; i < 2; ++i)
        //{
        //    vec_in_S2[i] = 0;
        //    for (int j = 0; j < 2; ++j)
        //    {
        //        vec_in_S2[i] += R[i, j] * vec_in_S1[j];
        //    }
        //}

        //float sensitivity = 0.4f;   //var to set speed/scaling
        //// sensitivity = 0.4f is control, almost 1:1
        //// sensitivity = 0.6f is larger scaling (more sensitive)
        //// sensitivity = 0.8f is larger larger scaling

        //temp = (float)vec_in_S2[1];
        //if (temp > fb_history + 0.02) { 
        //    temp = sensitivity; 
        //    //print("forward"); 
        //}
        //else if (temp < fb_history - 0.02) { 
        //    temp = -sensitivity; 
        //    //print("backward"); 
        //}
        //else {
        //    temp = 0; 
        //    //print("zero"); 
        //}

        //// if (temp != 0) { Debug.Log(temp.ToString()); }


        //fb_history = (float)vec_in_S2[1];
        //fb_command = temp;

        //// Try end here


        // ****************** Before 5/31 ********************

        ///// ************************* Try on 05/30 ***************************

        float rotation_diff = xrCamera.transform.localRotation.ToEulerAngles()[1] - TC.drone_rotation.z;

        if (rotation_diff > 3.14) { rotation_diff -= 2 * 3.14f; }
        else if (rotation_diff < -3.14) { rotation_diff += 2 * 3.14f; }

        rot_command = 0f;
        if (rotation_diff > 0.05)
        {
            if (rotation_diff < 0.35) { rot_command = 0.3f; }
            else { rot_command = 1f; }
        }
        else if (rotation_diff < -0.05)
        {
            if (rotation_diff > -0.35) { rot_command = -0.3f; }
            else { rot_command = -1f; }
        }

        ///// ************************* Try on 05/30 ***************************


        ///// ************************* Try on 05/31 ***************************
        if (calibrated)
        {
            //int[,] rot_mat = { { 0, -1, 0 }, { 0, 0, -1 }, { -1, 0, 0} };
            //headset_position_1 = new double[3] {headset_position.x, headset_position.y, headset_position.z };

            //for (int i = 0; i < 3; ++i)
            //{
            //    for (int j = 0; j < 3; ++j)
            //    {
            //        headset_position_2[i] = headset_position_1[j] * rot_mat[i, j];
            //    }
            //}

            double[] vec_in_S1 = new double[2] { xrCamera.transform.localPosition[0], xrCamera.transform.localPosition[2] }; // replace with your values
            double[] vec_in_S2 = new double[2];

            float theta = rotation_diff;

            double[,] R = new double[2, 2];
            R[0, 0] = Math.Cos(theta);
            R[0, 1] = -Math.Sin(theta);
            R[1, 0] = Math.Sin(theta);
            R[1, 1] = Math.Cos(theta);
            for (int i = 0; i < 2; ++i)
            {
                vec_in_S2[i] = 0;
                for (int j = 0; j < 2; ++j)
                {
                    vec_in_S2[i] += R[i, j] * vec_in_S1[j];
                }
            }
            pos_diff[0] = vec_in_S2[0];
            pos_diff[1] = vec_in_S2[1];

            for (int i = 0; i<2; ++i)
            {
                if (pos_diff[i] > 0.02)
                {
                    pos_command[i] = 0.4f;
                }
                else if (pos_diff[i] < -0.02)
                {
                    pos_command[i] = -0.4f;
                }
            }

            //headset_position_2 = new double[3] { headset_position.x , headset_position.y, headset_position.z };  ///  CHANGE THIS
            //double[] drone_position_1 = new double[3] { TC.drone_position.x, TC.drone_position.y, TC.drone_position.z };
            //for (int i = 0; i < 3; ++i)
            //{
            //    pos_diff[i] = headset_position_2[i] - drone_position_1[i];
            //}
            //for (int i = 0; i<3; ++i)
            //{
            //    if (pos_diff[i] > 0.02)
            //    {
            //        pos_command[i] = 0.4f;
            //    }
            //    else if (pos_diff[i] < -0.02)
            //    {
            //        pos_command[i] = -0.4f;
            //    }
            //}
        }


        ///// ************************* Try on 05/31 ***************************




        ///// ************************* Before 05/30 ***************************

        //temp = xrCamera.transform.localRotation.ToEulerAngles()[1];
        //if (temp > rot_history + 0.02) { temp = 0.8f; }
        //else if (temp < rot_history - 0.02) { temp = -0.8f; }
        //else { temp = 0; }

        //rot_history = xrCamera.transform.localRotation.ToEulerAngles()[1];
        //rot_command = temp;


        ///// ************************* Before 05/30 ***************************


        string[] logData = new string[Columns.Length];

        // capture time (Unix ms timestamp)
        logData[0] = ((DateTimeOffset)VarjoTime.ConvertVarjoTimestampToDateTime(data.captureTime)).ToUnixTimeMilliseconds().ToString();

        // headset position + rotation
        logData[1] = (xrCamera.transform.localPosition - headset_origin).ToString(VectorPrecision);
        logData[2] = xrCamera.transform.localRotation.ToEulerAngles().ToString(VectorPrecision);
        headset_position = xrCamera.transform.localPosition - headset_origin;

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

        if (Input.GetKeyDown(KeyCode.O))
        {
            if (calibrated) {return;}
            headset_origin = xrCamera.transform.localPosition;
            drone_origin = TC.drone_position;
            drone_origin_rotation = TC.drone_rotation;
            calibrated = true;
        }




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
