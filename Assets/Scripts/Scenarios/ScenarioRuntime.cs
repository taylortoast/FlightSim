using System;
using UnityEngine;

public static class ScenarioRuntime
{
    public static event Action<ScenarioDefinition> OnChanged;
    public static ScenarioDefinition Current { get; private set; }

    public static void Set(ScenarioDefinition scenario)
    {
        Current = scenario;
        OnChanged?.Invoke(Current);
        Debug.Log($"[ScenarioRuntime] Selected: {(scenario ? scenario.name : "null")}");
    }
}
