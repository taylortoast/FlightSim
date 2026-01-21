using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class McpStubUI : MonoBehaviour
{
    public SimTargets targets;

    public Slider speed;
    public Slider altitude;
    public Slider heading;

    public TMP_Text speedLabel;
    public TMP_Text altitudeLabel;
    public TMP_Text headingLabel;

    void Start()
    {
        if (!targets) return;

        speed.value = targets.targetSpeed;
        altitude.value = targets.targetAltitude;
        heading.value = targets.targetHeading;

        speed.onValueChanged.AddListener(v => { targets.targetSpeed = v; Refresh(); });
        altitude.onValueChanged.AddListener(v => { targets.targetAltitude = v; Refresh(); });
        heading.onValueChanged.AddListener(v =>
        {
            targets.targetHeading = Mathf.Repeat(v, 360f);
            Refresh();
        });


        Refresh();
    }

    void Refresh()
    {
        if (!targets) return;
        if (speedLabel) speedLabel.text = $"SPD {targets.targetSpeed:0.0}";
        if (altitudeLabel) altitudeLabel.text = $"ALT {targets.targetAltitude:0}";
        if (headingLabel) headingLabel.text = $"HDG {targets.targetHeading:0}";
    }
}
