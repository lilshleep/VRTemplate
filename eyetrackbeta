using UnityEngine;
using Varjo;

public class VarjoEyeTrackingAttention : MonoBehaviour
{
    private VarjoEyeTracker eyeTracker;

    // Threshold to define overt attention
    public float overtAttentionThreshold = 0.1f;
    public Transform targetObject; // Target object for overt attention

    void Start()
    {
        if (VarjoEyeTracker.IsAvailable())
        {
            eyeTracker = VarjoEyeTracker.Instance;
        }
        else
        {
            Debug.LogError("Varjo Eye Tracker is not available.");
        }
    }

    void Update()
    {
        if (eyeTracker != null && eyeTracker.HasEyeTrackingData)
        {
            VarjoEyeTrackingData gazeData = eyeTracker.GetGaze();
            Vector3 gazeDirection = gazeData.GazeForward;

            // Check if the gaze is on the target object (overt) or elsewhere (covert)
            bool isOvertAttention = CheckOvertAttention(gazeDirection);

            // Log this data point as overt or covert
            LogAttentionData(isOvertAttention);
        }
    }

    private bool CheckOvertAttention(Vector3 gazeDirection)
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, gazeDirection, out hit))
        {
            float distanceToObject = Vector3.Distance(hit.point, targetObject.position);
            return distanceToObject < overtAttentionThreshold;
        }
        return false;
    }

    private void LogAttentionData(bool isOvertAttention)
    {
        // Implement your logic to log the attention data
        // Example: Debug.Log(isOvertAttention ? "Overt" : "Covert");
    }
}

