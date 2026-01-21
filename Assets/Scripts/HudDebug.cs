using UnityEngine;
using TMPro;

public class HudDebug : MonoBehaviour
{
    public PlaneController plane;
    public SimTargets targets;
    public TMP_Text text;

    public NavAutopilot nav;


    void Update()
    {
        if (!plane || !targets || !text) return;

        Rigidbody rb = plane.GetComponent<Rigidbody>();
        float spd = rb.linearVelocity.magnitude;
        float alt = rb.position.y;
        float hdg = plane.transform.eulerAngles.y;

        text.text =
            $"SPD {spd:0.0} (T {targets.targetSpeed:0.0})\n" +
            $"ALT {alt:0.0} (T {targets.targetAltitude:0.0})\n" +
            $"HDG {hdg:0} (T {targets.targetHeading:0})";

        if (nav)
        {
            text.text += $"\nWP {nav.activeIndex}  {nav.activeDistance:0}m  NAVHDG {nav.activeBearing:0}";
        }

    }
}
