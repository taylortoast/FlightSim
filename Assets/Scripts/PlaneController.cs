using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    public SimTargets targets;

    [Header("Tuning")]
    public float speedResponse = 1.5f;
    public float headingResponse = 2.0f;

    public float altitudeResponse = 1.5f;
    public float maxClimbRate = 15f; // m/s


    public float maxTurnRateDegPerSec = 10f; // yaw rate cap


    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!targets) targets = GetComponent<SimTargets>();
    }

    void FixedUpdate()
    {
        UpdateSpeed();
        UpdateHeading();
        UpdateAltitude();

    }

    void UpdateSpeed()
    {
        Vector3 desiredVelocity = transform.forward * targets.targetSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, speedResponse * Time.fixedDeltaTime);

        Debug.DrawRay(transform.position, rb.linearVelocity, Color.green);
        Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);

    }

    void UpdateHeading()
    {
        float currentYaw = transform.eulerAngles.y;
        float desiredYaw = targets.targetHeading;

        float error = Mathf.DeltaAngle(currentYaw, desiredYaw);

        float maxStep = maxTurnRateDegPerSec * Time.fixedDeltaTime;
        float step = Mathf.Clamp(error, -maxStep, maxStep);

        float newYaw = currentYaw + step;
        rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
    }


    void UpdateAltitude()
    {
        float altitudeError = targets.targetAltitude - rb.position.y;

        // Desired vertical speed proportional to error, capped
        float desiredVy = Mathf.Clamp(altitudeError * altitudeResponse, -maxClimbRate, maxClimbRate);

        Vector3 v = rb.linearVelocity;
        v.y = Mathf.Lerp(v.y, desiredVy, altitudeResponse * Time.fixedDeltaTime);
        rb.linearVelocity = v;
    }

}
