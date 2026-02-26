using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ScenarioBboxExporter
{
    [MenuItem("FMS/Export/Print Scenario BBox (Selected ScenarioDefinition)")]
    public static void PrintSelectedScenarioBbox()
    {
        var s = Selection.activeObject as ScenarioDefinition;
        if (!s)
        {
            Debug.LogError("Select a ScenarioDefinition asset in Project window first.");
            return;
        }

        if (s.waypoints == null || s.waypoints.Count == 0)
        {
            Debug.LogError($"Scenario '{s.name}' has no waypoints.");
            return;
        }

        double minLat = double.PositiveInfinity, maxLat = double.NegativeInfinity;
        double minLon = double.PositiveInfinity, maxLon = double.NegativeInfinity;

        foreach (var w in s.waypoints.Where(w => w != null && !string.IsNullOrWhiteSpace(w.ident)))
        {
            minLat = System.Math.Min(minLat, w.latDeg);
            maxLat = System.Math.Max(maxLat, w.latDeg);
            minLon = System.Math.Min(minLon, w.lonDeg);
            maxLon = System.Math.Max(maxLon, w.lonDeg);
        }

        // padding (~10 NM): 1 deg lat ≈ 60 NM
        const double padDeg = 10.0 / 60.0; // 0.1666667

        double pMinLat = minLat - padDeg;
        double pMaxLat = maxLat + padDeg;
        double pMinLon = minLon - padDeg;
        double pMaxLon = maxLon + padDeg;

        Debug.Log(
            $"[ScenarioBBox] {s.name}\n" +
            $"Raw:    minLat={minLat:F5}, minLon={minLon:F5}, maxLat={maxLat:F5}, maxLon={maxLon:F5}\n" +
            $"Padded: minLat={pMinLat:F5}, minLon={pMinLon:F5}, maxLat={pMaxLat:F5}, maxLon={pMaxLon:F5}\n" +
            $"Python:\nminLat, minLon = {pMinLat:F5}, {pMinLon:F5}\nmaxLat, maxLon = {pMaxLat:F5}, {pMaxLon:F5}"
        );
    }
}