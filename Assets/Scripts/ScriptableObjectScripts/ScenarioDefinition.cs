using UnityEngine;

[CreateAssetMenu(menuName = "FMS/Scenario Definition", fileName = "ScenarioDefinition")]
public class ScenarioDefinition : ScriptableObject
{
    [Header("Scenario Info")]
    public string scenarioTitle;
    [TextArea(3, 6)]
    public string scenarioDescription;

    [Header("Map Anchor (center tile)")]
    public double centerLatDeg = 30.3;
    public double centerLonDeg = -87.3;

    [Header("ND Tile Set")]
    public int baseZoom = 16;
}
