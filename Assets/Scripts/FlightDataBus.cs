using UnityEngine;

public class FlightDataBus : MonoBehaviour
{
    public PlaneController plane;
    public SimTargets targets;
    public NavAutopilot nav;

    [Header("Outputs (read-only)")]
    public float ias;   // knots
    public float alt;   // feet MSL
    public float hdg;   // degrees [0..360)
    public float vsi;   // feet per minute
    public float brg;   // degrees to active wp
    public float dist;  // meters to active wp

    // Conversion constants
    const float KT_PER_MS = 1.94384449f;
    const float FT_PER_M = 3.2808399f;
    const float FTMIN_PER_MS = 196.850394f;

    void Update()
    {
        if (!plane) return;
        var rb = plane.GetComponent<Rigidbody>();
        if (!rb) return;

        // Convert m/s → knots
        ias = rb.linearVelocity.magnitude * KT_PER_MS;

        // Convert meters → feet
        alt = rb.position.y * FT_PER_M;

        // Heading stays in degrees
        hdg = Mathf.Repeat(plane.transform.eulerAngles.y, 360f);

        // Convert m/s vertical speed → ft/min
        vsi = rb.linearVelocity.y * FTMIN_PER_MS;

        if (nav)
        {
            // Bearing stays in degrees
            brg = Mathf.Repeat(nav.activeBearing, 360f);

            // Distance stays in meters
            dist = nav.activeDistance;
        }
    }
}
