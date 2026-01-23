using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    public SimTargets targets;

    [Header("Tuning")]
    public float speedResponse = 1.5f;

    // Now used in turn‑rate calculation
    public float headingResponse = 2.0f;

    public float altitudeResponse = 1.5f;
    public float maxClimbRate = 15f; // m/s

    public float maxTurnRateDegPerSec = 10f; // yaw rate cap

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!targets) targets = GetComponent<SimTargets>();

        rb.useGravity = false;
        rb.linearDamping = 0f;      // if your Unity uses drag, set rb.drag = 0f instead
        rb.angularDamping = 0f;     // or rb.angularDrag = 0f
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.None; // deterministic
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    void FixedUpdate()
    {
        UpdateSpeed();
        UpdateHeading();
        UpdateAltitude();
    }

    void UpdateSpeed()
    {
        Vector3 desiredVelocity = transform.forward * targets.TargetSpeedMS;

        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            desiredVelocity,
            speedResponse * Time.fixedDeltaTime
        );

        Debug.DrawRay(transform.position, rb.linearVelocity, Color.green);
        Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);
    }

    void UpdateHeading()
    {
        float currentYaw = transform.eulerAngles.y;
        float desiredYaw = targets.targetHdgDeg;

        float error = Mathf.DeltaAngle(currentYaw, desiredYaw);

        // Now includes headingResponse
        float maxStep = maxTurnRateDegPerSec * headingResponse * Time.fixedDeltaTime;
        float step = Mathf.Clamp(error, -maxStep, maxStep);

        float newYaw = currentYaw + step;

        rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
    }

    void UpdateAltitude()
    {
        Vector3 pos = rb.position;
        pos.y = targets.TargetAltM;     // meters, absolute
        rb.MovePosition(pos);

        // prevent any leftover vertical motion from accumulating
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
    }


}
