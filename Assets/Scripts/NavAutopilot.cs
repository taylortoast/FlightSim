using UnityEngine;

public class NavAutopilot : MonoBehaviour
{

    [HideInInspector] public float activeDistance;
    [HideInInspector] public float activeBearing;

    public FlightPlan plan;
    public SimTargets targets;
    public Transform aircraft;

    [Header("Nav")]
    public int activeIndex = 0;
    public float captureRadius = 150f; // meters
    public bool loop = false;

    public float forwardConeDeg = 60f; // must be within +/- this angle to count as "ahead"

    public float maxInterceptDeg = 30f;
    public float xtkToInterceptDeg = 0.02f; // deg per meter (tune)



    void Awake()
    {
        if (!plan) plan = GetComponent<FlightPlan>();
    }

    void Update()
    {
        if (!plan || plan.waypoints == null || plan.waypoints.Length == 0) return;
        if (!targets || !aircraft) return;

        // Active waypoint
        Transform wp = plan.waypoints[Mathf.Clamp(activeIndex, 0, plan.waypoints.Length - 1)];

        // --- LEG DEFINITION ---
        int prevIndex = Mathf.Max(activeIndex - 1, 0);

        Vector3 A = plan.waypoints[prevIndex].position;
        Vector3 B = wp.position;
        Vector3 P = aircraft.position;

        A.y = B.y = P.y = 0f;

        Vector3 AB = B - A;
        Vector3 AP = P - A;

        float dist = Vector3.Distance(P, B);

        Vector3 ABn = AB.normalized;

        // --- CROSS-TRACK ERROR (meters) ---
        float xtk = Vector3.Cross(ABn, AP).y;

        // --- BASE COURSE HEADING ---
        float courseHdg = Mathf.Atan2(ABn.x, ABn.z) * Mathf.Rad2Deg;
        courseHdg = (courseHdg + 360f) % 360f;

        // --- INTERCEPT HEADING ---
        float intercept =
            Mathf.Clamp(xtk * xtkToInterceptDeg, -maxInterceptDeg, maxInterceptDeg);

        float desiredHeading = (courseHdg - intercept + 360f) % 360f;

        targets.targetHeading = desiredHeading;

        // --- SMART WAYPOINT CAPTURE ---
        Vector3 forward = aircraft.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 dirToWp = (B - P).normalized;
        float angleToWp = Vector3.Angle(forward, dirToWp);

        if (dist <= captureRadius && angleToWp <= forwardConeDeg)
        {
            activeIndex++;
            if (activeIndex >= plan.waypoints.Length)
                activeIndex = loop ? 0 : plan.waypoints.Length - 1;
        }

        activeDistance = dist;
        activeBearing = desiredHeading;

        Debug.DrawLine(aircraft.position, wp.position, Color.yellow);
    }


}
