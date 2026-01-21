using UnityEngine;

public class NdPresenter : MonoBehaviour
{
    [Header("Refs")]
    public Transform aircraft;
    public FlightPlan flightPlan;
    public Camera ndCamera;

    [Header("ND Objects (in ND layer)")]
    public Transform aircraftIcon;         // ND_AircraftIcon
    public LineRenderer routeLine;         // ND_RouteLine
    public Transform waypointMarkerPrefab; // small quad/sprite
    public Transform waypointParent;       // ND_Waypoints

    Transform[] wpMarkers;

    void Start()
    {

        BuildWaypointMarkers();
        BuildRouteLine();
    }

    void LateUpdate()
    {
        if (!aircraft || !ndCamera) return;

        Vector3 p = aircraft.position;

        // Follow from above (position only; rotation stays Inspector-owned)
        ndCamera.transform.position = new Vector3(p.x, p.y + 30f, p.z);

        if (aircraftIcon)
        {
            aircraftIcon.position = new Vector3(p.x, p.y + 10f, p.z);
            aircraftIcon.rotation = Quaternion.Euler(00f, aircraft.eulerAngles.y, 0f);
        }

    }


    void BuildWaypointMarkers()
    {
        if (!flightPlan || flightPlan.waypoints == null || waypointMarkerPrefab == null || waypointParent == null) return;

        // Clear old markers
        for (int i = waypointParent.childCount - 1; i >= 0; i--)
            Destroy(waypointParent.GetChild(i).gameObject);

        wpMarkers = new Transform[flightPlan.waypoints.Length];

        for (int i = 0; i < flightPlan.waypoints.Length; i++)
        {
            Transform wp = flightPlan.waypoints[i];
            Transform m = Instantiate(waypointMarkerPrefab, waypointParent);
            m.name = $"WP_Marker_{i}";
            Vector3 pos = wp.position;
            m.position = new Vector3(pos.x, pos.y + 1f, pos.z);
            m.rotation = Quaternion.Euler(90f, 0f, 0f);
            wpMarkers[i] = m;
        }
    }

    void BuildRouteLine()
    {
        if (!flightPlan || flightPlan.waypoints == null || routeLine == null) return;

        routeLine.positionCount = flightPlan.waypoints.Length;

        for (int i = 0; i < flightPlan.waypoints.Length; i++)
        {
            Vector3 pos = flightPlan.waypoints[i].position;
            routeLine.SetPosition(i, new Vector3(pos.x, pos.y + 1f, pos.z));
        }
    }
}
