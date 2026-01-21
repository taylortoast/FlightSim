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

    [Header("Mode")]
    public bool navEngaged = false; // when true, NAV drives targetHeading




    void Awake()
    {
        if (!plan) plan = GetComponent<FlightPlan>();
    }

    void Update()
    {
        if (!plan || plan.waypoints == null || plan.waypoints.Length == 0) return;
        if (!targets || !aircraft) return;

        activeIndex = Mathf.Clamp(activeIndex, 0, plan.waypoints.Length - 1);
        Transform wp = plan.waypoints[activeIndex];

        Vector3 P = aircraft.position; P.y = 0f;
        Vector3 B = wp.position; B.y = 0f;

        Vector3 toWp = B - P;
        float dist = toWp.magnitude;

        float bearingToWp = Mathf.Atan2(toWp.x, toWp.z) * Mathf.Rad2Deg;
        bearingToWp = (bearingToWp + 360f) % 360f;

        float desiredHeading = bearingToWp; // DIRECT-TO by default

        if (activeIndex > 0)
        {
            Vector3 A = plan.waypoints[activeIndex - 1].position; A.y = 0f;
            Vector3 AB = B - A;
            if (AB.sqrMagnitude > 1f)
            {
                Vector3 ABn = AB.normalized;
                Vector3 AP = P - A;
                float xtk = Vector3.Cross(ABn, AP).y;

                float courseHdg = Mathf.Atan2(ABn.x, ABn.z) * Mathf.Rad2Deg;
                courseHdg = (courseHdg + 360f) % 360f;

                float intercept = Mathf.Clamp(xtk * xtkToInterceptDeg, -maxInterceptDeg, maxInterceptDeg);
                desiredHeading = (courseHdg - intercept + 360f) % 360f;
            }
        }

        if (navEngaged) targets.targetHeading = desiredHeading;

        // ND-friendly outputs: distance + bearing to active waypoint
        activeDistance = dist;
        activeBearing = bearingToWp;

        // Smart capture (use direction to waypoint, not desiredHeading)
        Vector3 forward = aircraft.forward; forward.y = 0f; forward.Normalize();
        Vector3 dirToWp = (dist > 0.001f) ? (toWp / dist) : forward;
        float angleToWp = Vector3.Angle(forward, dirToWp);

        if (dist <= captureRadius && angleToWp <= forwardConeDeg)
        {
            activeIndex++;
            if (activeIndex >= plan.waypoints.Length)
                activeIndex = loop ? 0 : plan.waypoints.Length - 1;
        }

        Debug.DrawLine(aircraft.position, wp.position, Color.yellow);
    }


    public void SetNavEngaged(bool on)
    {
        navEngaged = on;

        // When disengaging NAV, freeze the target to current heading
        // so we don’t “snap back” to some old value.
        if (!navEngaged && targets && aircraft)
            targets.targetHeading = aircraft.eulerAngles.y;
    }

    public void ToggleNav() => SetNavEngaged(!navEngaged);



}
