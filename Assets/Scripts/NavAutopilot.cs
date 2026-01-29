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
    public float captureRadius = 60f; // meters
    public bool loop = false;

    public float forwardConeDeg = 60f; // must be within +/- this angle to count as "ahead"

    public float maxInterceptDeg = 30f;
    public float xtkToInterceptDeg = 0.02f; // deg per meter (tune)

    [Header("Mode")]
    public bool navEngaged = false; // when true, NAV drives targetHeading

    [Header("Capture Robustness (v1)")]
    public float nearRadiusMultiplier = 1.25f; // 150 * 1.25 = 187.5m "near"
    public int minNearFrames = 3;              // require a few frames near before capturing

    float prevDist = float.PositiveInfinity;
    bool wasNear = false;
    int nearFrames = 0;

    static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);






    void Awake()
    {
        if (!plan) plan = GetComponent<FlightPlan>();
    }

    void FixedUpdate()
    {


        if (!plan || plan.waypoints == null || plan.waypoints.Length == 0) return;
        if (!targets || !aircraft) return;

        activeIndex = Mathf.Clamp(activeIndex, 0, plan.waypoints.Length - 1);
        Transform wp = plan.waypoints[activeIndex];

        Vector3 P = Flat(aircraft.position);
        Vector3 B = Flat(wp.position);


        Vector3 toWp = B - P;
        float dist = toWp.magnitude;


        float nearRadius = captureRadius * nearRadiusMultiplier;

        if (dist <= nearRadius)
        {
            wasNear = true;
            nearFrames++;
        }




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

        if (navEngaged) targets.targetHdgDeg = desiredHeading;

        // ND-friendly outputs: distance + bearing to active waypoint
        activeDistance = dist;
        activeBearing = bearingToWp;

        // Smart capture (use direction to waypoint, not desiredHeading)
        Vector3 forward = Flat(aircraft.forward).normalized;

        Vector3 dirToWp = (dist > 0.001f) ? (toWp / dist) : forward;


        bool movingAwayAfterNear = wasNear && nearFrames >= minNearFrames && dist > prevDist;
        bool inRadius = dist <= captureRadius;

        if (inRadius || movingAwayAfterNear)
        {
            activeIndex++;
            if (activeIndex >= plan.waypoints.Length)
                activeIndex = loop ? 0 : plan.waypoints.Length - 1;

            wasNear = false; nearFrames = 0; prevDist = float.PositiveInfinity;
        }
        else
        {
            prevDist = dist;
        }


        Debug.DrawLine(aircraft.position, wp.position, Color.black);
    }



    public void SetNavEngaged(bool on)
    {
        navEngaged = on;

        // When disengaging NAV, freeze the target to current heading
        // so we don’t “snap back” to some old value.
        if (!navEngaged && targets && aircraft)
            targets.targetHdgDeg = aircraft.eulerAngles.y;
    }

    public void ToggleNav() => SetNavEngaged(!navEngaged);



}
