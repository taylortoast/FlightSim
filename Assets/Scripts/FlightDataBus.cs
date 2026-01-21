using UnityEngine;

public class FlightDataBus : MonoBehaviour
{
    public PlaneController plane;
    public SimTargets targets;
    public NavAutopilot nav;

    [Header("Outputs (read-only)")]
    public float ias;   // m/s for now
    public float alt;   // meters
    public float hdg;   // degrees [0..360)
    public float vsi;   // m/s
    public float brg;   // degrees to active wp
    public float dist;  // meters to active wp

    void Update()
    {
        if (!plane) return;
        var rb = plane.GetComponent<Rigidbody>();
        if (!rb) return;

        ias = rb.linearVelocity.magnitude;
        alt = rb.position.y;
        hdg = Mathf.Repeat(plane.transform.eulerAngles.y, 360f);
        vsi = rb.linearVelocity.y;

        if (nav)
        {
            brg = Mathf.Repeat(nav.activeBearing, 360f);
            dist = nav.activeDistance;
        }
    }
}
