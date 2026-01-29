# ND Navigation Display – Status, Decisions, and Next Steps

## Project Context
iPad-based Naval flight training simulation (Unity).  
Focus area: **Navigation Display (ND)** with offline map tiles, deterministic scaling, and avionics-correct behavior.

Core invariants are in effect:
- **1 Unity unit = 1 meter**
- ND map is **north-up**
- ND range is **radius-based** (real avionics behavior)
- Units: meters (world), NM (ND range), feet (altitude), knots (airspeed)

---

## What Is Complete (Locked)

### ND Map & Tiles
- Offline PNG tiles rendered from **OpenMapTiles** via **tileserver-gl (Docker)**.
- Custom dark style: `nd-dark-v1`
  - Land/water
  - Coastlines
  - Major highways + labels
  - City labels
  - Airport labels + aeroways
- Tiles stored in:
  ```
  Assets/StreamingAssets/tiles_nd_dark_v1/
    12/
    13/
    14/
  ```

### ND Range Contract (Authoritative)
- **5 NM**  → ±5 NM (10 NM across) → **z12**
- **10 NM** → ±10 NM (20 NM across) → **z13**
- **20 NM** → ±20 NM (40 NM across) → **z14**
- Tile overscan added for z14 (≈40 NM radius) to avoid edge gaps.

### Camera (ND_Camera)
- Perspective camera, **FOV = 60°**
- Square RenderTexture **800×800** (aspect = 1)
- Camera follows aircraft **X/Z only**
- Camera **Y height is range-driven**, not altitude-driven
- Far clip increased to **≥ 80,000 m**
- Verified math:
  - 5 NM  → 18,520 m across
  - 10 NM → 37,040 m across
  - 20 NM → 74,080 m across

### Architecture
- **NDRangeState** = single source of truth for ND range
- **NDRangeStepper** (+ / − buttons) updates NDRangeState
- **LocalTileGrid** subscribes to NDRangeState (debounced rebuild)
- **FollowAircraftCamera** subscribes to NDRangeState (debounced Y update)
- Dedicated **TileContainer** under GroundRoot prevents scene destruction

### ND Aircraft Symbol
- 3D aircraft mesh **not rendered** on ND
- UI overlay:
  - `ND_AircraftIcon` (Image)
  - Centered on ND
  - Rotates with aircraft heading
- 3D aircraft mesh culled from ND camera via layer mask

---

## Known Good State
- Tiles load correctly at all ranges
- ND fills view at max zoom
- Aircraft icon visible and rotates correctly
- No altitude coupling
- No scene objects destroyed unintentionally

This state has been committed to GitHub as a checkpoint.

---

## Known Non-Goals (Deferred)
- Tile pooling / optimization
- Route/waypoint rendering
- Track-up ND mode
- Heading bug / course line
- Performance profiling on iPad hardware

---

## Next Steps (Proposed Order)

1. **ND Camera Polish**
   - Add small edge padding / overscan margin tuning
   - Verify feel during turns (no edge pop-in)

2. **ND Aircraft Symbology Expansion**
   - Heading bug
   - Course line
   - Track vs heading indication

3. **Route Rendering**
   - Draw active flight plan legs on ND
   - Waypoint symbols and labels

4. **Scenario Switching**
   - Swap ScenarioDefinition (lat/lon)
   - Re-anchor tiles + camera deterministically

5. **Performance Pass**
   - Optional tile pooling
   - Texture reuse
   - Memory audit for iPad

---

## Reference Commands (Summary)

### Docker: Tileserver
```powershell
docker rm -f tileserver 2>$null
docker run --name tileserver --rm -p 8080:8080 `
  -v C:\OSM\data:/data `
  -v C:\OSM\styles:/usr/src/app/node_modules/tileserver-gl-styles/styles `
  maptiler/tileserver-gl:latest `
  --mbtiles /data/us-south.mbtiles --verbose
```

### Tile Export (z12/z13/z14)
See previous session notes for the exact PowerShell export script.

---

## Status
ND subsystem is **architecturally correct, physically accurate, and stable**.  
Ready to build higher-level avionics features without rework.
