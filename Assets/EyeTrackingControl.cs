using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Varjo.XR;
using UnityEngine.XR;
using System.Linq;

/*
public methods:
CalibrateGaze() -- calibrates using set mode
StartLog() -- start logging eye tracking data
EndLog() -- finish logging eye tracking data
*/

public class EyeTrackingControl : MonoBehaviour
{

    [Header("Main camera (under XR Rig)")]
    public Camera xrCamera;

    // lets you see how many eye tracking datapoints are logged per second. Runs once
    // every second and prints to debug console if true
    public bool printFramerate = false;
    int gazeDataCount = 0;
    float gazeTimer = 0f;

    // calibration mode fast is the most accurate for now, unfortunately the legacy mode is deprecated
    [Header("Gaze calibration settings")]
    [Tooltip("Legacy - 10 dots without priors; Fast: 5 dots; One Dot: quickest, least accurate")]
    public VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;

    // keys for calibration and logging
    [Tooltip("Keyboard shortcut to request calibration")]
    public KeyCode calibrationKey = KeyCode.Backslash;
    public KeyCode loggingToggleKey = KeyCode.RightControl;

    // we don't want to filter our data to look smoother since we lose accuracy
    [Header("Gaze output filter")]
    [Tooltip("Standard: smoothing on gaze data; None: raw data")]
    public VarjoEyeTracking.GazeOutputFilterType gazeFilterType = VarjoEyeTracking.GazeOutputFilterType.None;
    
    // frequency of eye tracking logging
    private VarjoEyeTracking.GazeOutputFrequency frequency = VarjoEyeTracking.GazeOutputFrequency.MaximumSupported;

    // stuff for logging data
    private static readonly string[] Columns = { "CaptureTime", "CalcXEccentricity", 
        "CalcYEccentricity", "CombinedGazeForward", "CombinedGazeOrigin", "Valid", 
        "LeftForward", "RightForward", "LeftPosition", "RightPosition" };
    private static string calcPrecision = "F6"; // precision after decimal for calculations and tracking
    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";
    [Header("Logging path (defaults to Logs)")]
    public bool useCustomLogPath = false;
    public string customLogPath = "";
    private List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    private StreamWriter writer = null;
    private bool logging = false;

    // Start is called before the first frame update
    private void Start()
    {
        // sets eye tracking parameters
        VarjoEyeTracking.SetGazeOutputFrequency(frequency);
        VarjoEyeTracking.SetGazeOutputFilterType(gazeFilterType);
        // starts calibration
        CalibrateGaze();
    }

    // Update is called once per frame
    void Update()
    {
        // prints framerate if set to true
        if (logging && printFramerate)
        {
            gazeTimer += Time.deltaTime;
            if (gazeTimer >= 1.0f)
            {
                Debug.Log("Gaze data rows per second: " + gazeDataCount);
                gazeDataCount = 0;
                gazeTimer = 0f;
            }
        }

        // runs calibration if calibration key is pressed
        if (Input.GetKeyDown(calibrationKey))
        {
            CalibrateGaze();
        }

        // starts or stops logging if logging key is pressed
        if (Input.GetKeyDown(loggingToggleKey))
        {
            // Check if gaze is calibrated
            if (VarjoEyeTracking.IsGazeCalibrated())
            {
                Debug.Log("GAZE IS NOT CALIBRATED - isgazecalibrated:" + VarjoEyeTracking.IsGazeCalibrated());
            }
            else if (!logging)
                StartLogging();
            else
                StopLogging();
            return;
        }

        // logs every loop if logging has started
        if (logging)
        {
            int dataCount = VarjoEyeTracking.GetGazeList(out dataSinceLastUpdate);//, out eyeMeasurementsSinceLastUpdate);
            for (int i = 0; i < dataCount; ++i)
            {
                LogGazeData(dataSinceLastUpdate[i]);
            }
            if (printFramerate)
            {
                gazeDataCount += dataCount;
            }
        }
    }

    void LogGazeData(VarjoEyeTracking.GazeData data)
    {
        // if data isn't valid, warn user
        if (data.status == VarjoEyeTracking.GazeStatus.Invalid)
        {
            Debug.Log("GAZE IS INVALID");
        }
        string[] logData = new string[Columns.Length];
    
        // capture time (Unix ms timestamp)
        logData[0] = ((DateTimeOffset)VarjoTime.ConvertVarjoTimestampToDateTime(data.captureTime)).ToUnixTimeMilliseconds().ToString();

        // calculated X, Y eccentricities
        float x = data.gaze.forward.x;
        float y = data.gaze.forward.y;
        float z = data.gaze.forward.z;
        logData[1] = (Math.Atan(x / z) / Math.PI * 180).ToString(calcPrecision);
        logData[2] = (Math.Atan(y / z) / Math.PI * 180).ToString(calcPrecision);
        
        // Combined gaze
        bool invalid = data.status == VarjoEyeTracking.GazeStatus.Invalid;
        logData[3] = invalid ? "" : data.gaze.forward.ToString(calcPrecision);
        logData[4] = invalid ? "" : data.gaze.origin.ToString(calcPrecision);
        logData[5] = invalid ? InvalidString : ValidString;

        // left and right eye forward
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[6] = leftInvalid ? "" : data.left.forward.ToString(calcPrecision);

        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[7] = rightInvalid ? "" : data.right.forward.ToString(calcPrecision);

        // left and right eye position
        logData[8] = leftInvalid ? "" : data.left.origin.ToString("F3");
        logData[9] = rightInvalid ? "" : data.right.origin.ToString("F3");

        Log(logData);
    }

    // Write given values in the log file
    void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        writer.WriteLine(line);
    }

    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        logging = true;

        string logPath = useCustomLogPath ? customLogPath : Application.dataPath + "/Logs/";
        Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}-{4:00}", now.Year, now.Month, now.Day, now.Hour, now.Minute);

        string path = logPath + fileName + ".csv";
        writer = new StreamWriter(path);

        Log(Columns);
        Debug.Log("Log file started at: " + path);
        gazeTimer += Time.deltaTime;
    }

    void StopLogging()
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
        Debug.Log("Logging ended");
    }

    public void CalibrateGaze()
    {
        VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }
}
