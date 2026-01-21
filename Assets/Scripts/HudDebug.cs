using UnityEngine;
using TMPro;

public class HudDebug : MonoBehaviour
{
    public PlaneController plane;
    public SimTargets targets;
    public TMP_Text text;
    public NavAutopilot nav;

    static int HdgInt(float deg) => (int)Mathf.Repeat(deg + 0.5f, 360f);

    void Update()
    {
        if (!plane || !targets || !text) return;

        var rb = plane.GetComponent<Rigidbody>();
        float spd = rb.linearVelocity.magnitude;
        float alt = rb.position.y;

        int hdgI = HdgInt(plane.transform.eulerAngles.y);
        int tgtI = HdgInt(targets.targetHeading);

        text.text =
            $"SPD {spd:0.0} (T {targets.targetSpeed:0.0})\n" +
            $"ALT {alt:0.0} (T {targets.targetAltitude:0.0})\n" +
            $"HDG {hdgI} (T {tgtI})";

        if (nav)
            text.text += $"\nWP {nav.activeIndex}  {nav.activeDistance:0}m  BRG {nav.activeBearing:0}";
    }
}
