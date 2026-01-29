using UnityEngine;

[CreateAssetMenu(menuName="FMS/Scenario Definition", fileName="ScenarioDefinition")]
public class ScenarioDefinition : ScriptableObject
{
    [Header("Map Anchor (center tile)")]
    public double centerLatDeg = 30.3;
    public double centerLonDeg = -87.3;

    [Header("ND Tile Set")]
    public int baseZoom = 16; // e.g., 16 for “20NM set” if that’s your convention
}
