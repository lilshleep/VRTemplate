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

may want to call VarjoEyeTracking.IsGaze(Allowed|Available|Calibrated)() before tracking and issue warning to console else
we can get the XR rig with "this"

NOTE: this script does *not* track eye measurements (interpupillary distance, pupil dilation, etc.)
 - we can add this functionality if desired!

*/

public class ET_Explained : MonoBehaviour
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

    

    // unused variables
    /*
    // lets you see where the participant is looking. Use for debugging eye tracking
    // needs a gazeTarget object, look at the varjo eye tracking examples in samples\
    public GameObject gazeTarget;
    [Header("Toggle gaze target visibility")]
    public KeyCode toggleGazeTarget = KeyCode.Return;
    [Header("Gaze point distance if not hit anything")]
    public float floatingGazeTargetDistance = 5f;
    [Header("Toggle fixation point indicator visibility")]
    public bool showFixationPoint = true;
    [Header("Visualization Transforms")]
    public Transform fixationPointTransform;
    public Transform leftEyeTransform;
    public Transform rightEyeTransform;

    // [Header("Log file name (defaults to current date/time)")]
    // public bool useCustomLogFileName = false;
    // public string customLogFileName = "";

    [Header("Gaze data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    [Header("Debug Gaze")]
    public KeyCode checkGazeAllowed = KeyCode.PageUp;
    public KeyCode checkGazeCalibrated = KeyCode.PageDown;

    [Header("Gaze ray radius")]
    public float gazeRadius = 0.01f;


    [Header("Gaze target offset towards viewer")]
    public float targetOffset = 0.2f;

    [Header("Amout of force give to freerotating objects at point where user is looking")]
    public float hitForce = 5f;

    [Header("Default path is Logs under application data path.")]
    public bool useCustomLogPath = false;
    public string customLogPath = "";

    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private VarjoEyeTracking.GazeData gazeData;
    private List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    private List<VarjoEyeTracking.EyeMeasurements> eyeMeasurementsSinceLastUpdate;
    private Vector3 leftEyePosition;
    private Vector3 rightEyePosition;
    private Quaternion leftEyeRotation;
    private Quaternion rightEyeRotation;
    private Vector3 fixationPoint;
    private Vector3 direction;
    private Vector3 rayOrigin;
    private RaycastHit hit;
    private float distance;
    private StreamWriter writer = null;
    private bool logging = false;
    */

    // Start is called before the first frame update
    private void Start()
    {
        VarjoEyeTracking.SetGazeOutputFrequency(frequency);
        VarjoEyeTracking.SetGazeOutputFilterType(gazeFilterType);
        CalibrateGaze();
        // set up for gaze target, see above
        /*
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            gazeTarget.SetActive(true);
        }
        else
        {
            gazeTarget.SetActive(false);
        }

        if (showFixationPoint)
        {
            fixationPointTransform.gameObject.SetActive(true);
        }
        else
        {
            fixationPointTransform.gameObject.SetActive(false);
        }
        */
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

        // unused stuff
        /*
        // stuff for gaze target, see above
        // Toggle gaze target visibility
        if (Input.GetKeyDown(toggleGazeTarget))
        {
            gazeTarget.GetComponentInChildren<MeshRenderer>().enabled = !gazeTarget.GetComponentInChildren<MeshRenderer>().enabled;
        }
        // Check if gaze is allowed
        // kinda makes more sense to see if it's not allowed
        if (Input.GetKeyDown(checkGazeAllowed))
        {
            Debug.Log("Gaze allowed: " + VarjoEyeTracking.IsGazeAllowed());
        }
        // Set output filter type
        if (Input.GetKeyDown(setOutputFilterTypeKey))
        {
            VarjoEyeTracking.SetGazeOutputFilterType(gazeOutputFilterType);
            Debug.Log("Gaze output filter type is now: " + VarjoEyeTracking.GetGazeOutputFilterType());
        }
        // gaze vector visualization examples:
                // Get gaze data if gaze is allowed and calibrated
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            //Get device if not valid
            if (!device.isValid)
            {
                GetDevice();
            }

            // Show gaze target
            gazeTarget.SetActive(true);

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                // Get data for eye positions, rotations and the fixation point
                if (device.TryGetFeatureValue(CommonUsages.eyesData, out eyes))
                {
                    if (eyes.TryGetLeftEyePosition(out leftEyePosition))
                    {
                        leftEyeTransform.localPosition = leftEyePosition;
                    }

                    if (eyes.TryGetLeftEyeRotation(out leftEyeRotation))
                    {
                        leftEyeTransform.localRotation = leftEyeRotation;
                    }

                    if (eyes.TryGetRightEyePosition(out rightEyePosition))
                    {
                        rightEyeTransform.localPosition = rightEyePosition;
                    }

                    if (eyes.TryGetRightEyeRotation(out rightEyeRotation))
                    {
                        rightEyeTransform.localRotation = rightEyeRotation;
                    }

                    if (eyes.TryGetFixationPoint(out fixationPoint))
                    {
                        fixationPointTransform.localPosition = fixationPoint;
                    }
                }

                // Set raycast origin point to VR camera position
                rayOrigin = xrCamera.transform.position;

                // Direction from VR camera towards fixation point
                direction = (fixationPointTransform.position - xrCamera.transform.position).normalized;

            } else
            {
                gazeData = VarjoEyeTracking.GetGaze();

                if (gazeData.status != VarjoEyeTracking.GazeStatus.Invalid)
                {
                    // GazeRay vectors are relative to the HMD pose so they need to be transformed to world space
                    if (gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                    {
                        leftEyeTransform.position = xrCamera.transform.TransformPoint(gazeData.left.origin);
                        leftEyeTransform.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.left.forward));
                    }

                    if (gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
                    {
                        rightEyeTransform.position = xrCamera.transform.TransformPoint(gazeData.right.origin);
                        rightEyeTransform.rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.right.forward));
                    }

                    // Set gaze origin as raycast origin
                    rayOrigin = xrCamera.transform.TransformPoint(gazeData.gaze.origin);

                    // Set gaze direction as raycast direction
                    direction = xrCamera.transform.TransformDirection(gazeData.gaze.forward);

                    // Fixation point can be calculated using ray origin, direction and focus distance
                    fixationPointTransform.position = rayOrigin + direction * gazeData.focusDistance;
                }
            }
        }

        // Raycast to world from VR Camera position towards fixation point
        if (Physics.SphereCast(rayOrigin, gazeRadius, direction, out hit))
        {
            // Put target on gaze raycast position with offset towards user
            gazeTarget.transform.position = hit.point - direction * targetOffset;

            // Make gaze target point towards user
            gazeTarget.transform.LookAt(rayOrigin, Vector3.up);

            // Scale gazetarget with distance so it apperas to be always same size
            distance = hit.distance;
            gazeTarget.transform.localScale = Vector3.one * distance;

            // Prefer layers or tags to identify looked objects in your application
            // This is done here using GetComponent for the sake of clarity as an example
            RotateWithGaze rotateWithGaze = hit.collider.gameObject.GetComponent<RotateWithGaze>();
            if (rotateWithGaze != null)
            {
                rotateWithGaze.RayHit();
            }

            // Alternative way to check if you hit object with tag
            if (hit.transform.CompareTag("FreeRotating"))
            {
                AddForceAtHitPosition();
            }
        }
        else
        {
            // If gaze ray didn't hit anything, the gaze target is shown at fixed distance
            gazeTarget.transform.position = rayOrigin + direction * floatingGazeTargetDistance;
            gazeTarget.transform.LookAt(rayOrigin, Vector3.up);
            gazeTarget.transform.localScale = Vector3.one * floatingGazeTargetDistance;
        }
        */

    }

    /*
    // used for gaze interactions in a unity project
    void AddForceAtHitPosition()
    {
        //Get Rigidbody form hit object and add force on hit position
        Rigidbody rb = hit.rigidbody;
        if (rb != null)
        {
            rb.AddForceAtPosition(direction * hitForce, hit.point, ForceMode.Force);
        }
    }
    */

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

        // unused info
        /*
        // Gaze data frame number
        logData[0] = data.frameNumber.ToString();

        // Gaze data capture time (nanoseconds)
        logData[1] = data.captureTime.ToString();

        // Log time (milliseconds)
        logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();
        
        // headset position + rotation
        logData[1] = xrCamera.transform.localPosition.ToString(calcPrecision);
        logData[2] = xrCamera.transform.localRotation.ToString(calcPrecision);
        // HMD
        logData[3] = xrCamera.transform.localPosition.ToString("F3");
        logData[4] = xrCamera.transform.localRotation.ToString("F3");

        // Combined gaze
        bool invalid = data.status == VarjoEyeTracking.GazeStatus.Invalid;
        logData[5] = invalid ? InvalidString : ValidString;
        logData[6] = invalid ? "" : data.gaze.forward.ToString("F3");
        logData[7] = invalid ? "" : data.gaze.origin.ToString("F3");

        // IPD
        logData[8] = invalid ? "" : eyeMeasurements.interPupillaryDistanceInMM.ToString("F3");

        // Left eye
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[9] = leftInvalid ? InvalidString : ValidString;
        logData[10] = leftInvalid ? "" : data.left.forward.ToString("F3");
        logData[11] = leftInvalid ? "" : data.left.origin.ToString("F3");
        logData[12] = leftInvalid ? "" : eyeMeasurements.leftPupilIrisDiameterRatio.ToString("F3");
        logData[13] = leftInvalid ? "" : eyeMeasurements.leftPupilDiameterInMM.ToString("F3");
        logData[14] = leftInvalid ? "" : eyeMeasurements.leftIrisDiameterInMM.ToString("F3");

        // Right eye
        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[15] = rightInvalid ? InvalidString : ValidString;
        logData[16] = rightInvalid ? "" : data.right.forward.ToString("F3");
        logData[17] = rightInvalid ? "" : data.right.origin.ToString("F3");
        logData[18] = rightInvalid ? "" : eyeMeasurements.rightPupilIrisDiameterRatio.ToString("F3");
        logData[19] = rightInvalid ? "" : eyeMeasurements.rightPupilDiameterInMM.ToString("F3");
        logData[20] = rightInvalid ? "" : eyeMeasurements.rightIrisDiameterInMM.ToString("F3");

        // Focus
        logData[21] = invalid ? "" : data.focusDistance.ToString();
        logData[22] = invalid ? "" : data.focusStability.ToString();
        // focus distance
        logData[4] = data.focusDistance.ToString();
        // focus stability
        logData[5] = data.focusStability.ToString();
         */

        Log(logData);
    }

    // doesn't work and seems unnecessary
    /*
    void GetDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
    }

    void OnEnable()
    {
        if (!device.isValid)
        {
            GetDevice();
        }
    }
    */

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
