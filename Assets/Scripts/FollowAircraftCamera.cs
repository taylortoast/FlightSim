using UnityEngine;
using System.Collections;

public class FollowAircraftCamera : MonoBehaviour
{
    [SerializeField] private Transform aircraft;
    [SerializeField] private Vector3 offset = new Vector3(0f, 200f, 0f);

    public NDRangeState rangeState;
    public float rebuildDelay = 0.1f;
    public float fovDeg = 60f;
    public float farClipMin = 80000f;

    Coroutine pending;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fovDeg;
        cam.farClipPlane = farClipMin;
    }

    void OnEnable() { if (rangeState != null) rangeState.OnRangeChanged += HandleRangeChanged; }
    void OnDisable() { if (rangeState != null) rangeState.OnRangeChanged -= HandleRangeChanged; }

    void LateUpdate()
    {
        if (aircraft != null)
            transform.position = new Vector3(aircraft.position.x, 0f, aircraft.position.z) + offset;

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void HandleRangeChanged(int nm)
    {
        if (pending != null) StopCoroutine(pending);
        pending = StartCoroutine(ApplyAfterDelay(nm));
    }

    IEnumerator ApplyAfterDelay(int nm)
    {
        yield return new WaitForSeconds(rebuildDelay);

        float W = 2f * nm * 1852f; // meters across (radius-based ND range)
        float H = W / (2f * Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView)); // aspect=1
        float Wcheck = 2f * H * Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView);
        Debug.Log($"[ND] range={nm}NM  H={H:F0}m  width={Wcheck:F0}m (expected {2f * nm * 1852f:F0}m)");


        offset = new Vector3(offset.x, H, offset.z);
        cam.farClipPlane = Mathf.Max(farClipMin, H + 1000f);

        pending = null;
    }
}
