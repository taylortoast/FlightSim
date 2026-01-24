using UnityEngine;

public class FollowAircraftCamera : MonoBehaviour
{
    [SerializeField] private Transform aircraft;
    [SerializeField] private Vector3 offset = new Vector3(0f, 200f, 0f);

    void LateUpdate()
    {
        // Follow aircraft position
        if (aircraft != null)
        {
            transform.position = aircraft.position + offset;
        }

        // Lock rotation: straight down, north-aligned
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
