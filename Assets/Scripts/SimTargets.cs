using UnityEngine;

public class SimTargets : MonoBehaviour
{
    [Header("Authoritative Targets")]
    public float targetSpeed = 10f;    // m/s (v1 units)
    public float targetHeading = 0f;    // degrees (0 = north/Z+)
    public float targetAltitude = 1000f; // meters (unused for now)
}
