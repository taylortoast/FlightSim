# NAS FMS / Flight Sim — World Scale Reset Plan (v1)
_Date: 2026-01-23_

## Non‑Negotiable Scale Rules (Project Truth)
- **1 Unity unit = 1 meter** (all physics, movement, tiles, cameras, UI overlays).
- **Reference aircraft:** Bombardier Global 7500 length = **33.8 m** ⇒ model length must be **33.8 Unity units**.

## Map Tile System (Web Mercator raster tiles)
- Tile pixel size: **256×256**
- Tile size (meters / Unity units):
  - **Zoom 14:** 2,441 m per tile
  - **Zoom 15:** 1,220 m per tile
  - **Zoom 16:** 610 m per tile

### Unity Quad Mapping
- Unity built‑in **Quad mesh is 2×2 units**, so set **localScale (tileMeters/2, tileMeters/2, 1)**.
- Required quad scales:
  - **Zoom 14:** (1220.5, 1220.5, 1)
  - **Zoom 15:** (610, 610, 1)
  - **Zoom 16:** (305, 305, 1)

## Goal for Today
Re-establish correct scale across the hierarchy so **movement, physics, and instruments** match real‑world meters/seconds, and the **tile ground system** can be added without rework.

---

# Phase 1 — Hierarchy & Transform Sanity (Stop the “mystery scaling”)
## 1.1 Create a “Scale Audit” snapshot (Completed)
- Duplicate current scene (or create `*_ScaleAudit` copy).
- Add a top-level empty: `WorldRoot`.
- Under it, standardize sub-roots:
  - `GroundRoot`
  - `AircraftRoot`
  - `UIRoot`
  - `SystemsRoot`

**Success looks like:** all world objects can be re-parented under these roots without changing their apparent size/position.

## 1.2 Enforce Transform invariants (the big rules) (Completed)
For every Transform used as a **container/parent** in world space:
- `localScale == (1,1,1)`
- `localRotation == (0,0,0)` unless intentionally rotated for heading, etc.

**Do not** “fix” by scaling parents. Fix children/prefabs instead.

### Checklist
- [ ] Any parent with `scale != 1` gets flagged and corrected.
- [ ] Any object with negative scale gets corrected (mirroring breaks normals/physics).
- [ ] Colliders are checked after scaling changes (BoxCollider sizes can become wrong).

---

# Phase 2 — Aircraft Model Calibration (Global 7500 = 33.8 units)
## 2.1 Measure model length  (Completed)
In Unity, use one of:
- MeshRenderer bounds (`bounds.size` along the aircraft forward axis), or
- Two marker empties placed at nose/tail.

## 2.2 Scale the model to 33.8 meters (Completed)
- Set the aircraft mesh prefab scale so its **nose-to-tail length = 33.8 units**.
- Keep the **Rigidbody / colliders** consistent with the scaled mesh.
- Prefer: scale the model prefab once, then keep all instances at `(1,1,1)`.

**Success looks like:** model length reports 33.8 in Scene view measurement.

---

# Phase 3 — Ground Tile Prefabs (Quad-based, offline-ready)
## 3.1 Create `GroundTile_Quad` prefab (Prefab?)
Note: I want to initially create the number of tiles required in the scene and as the plane object moves I'll want to reposition and retexture. Not sure if a prefab would work as i'm attempting not to overload the cpu with destroy/recreates
Prefab components:
- Quad mesh (2×2 base)
- MeshRenderer (material w/ tile texture)

Prefab fields:
- `zoomLevel` (14/15/16)
- `tileMeters` (2441/1220/610)
- `tileId` (x/y/z or your own index)

## 3.2 Apply correct scale per zoom
- Zoom 16 tiles use `localScale = (305,305,1)` (Completed)
- Confirm adjacent tiles align with **no gaps**:
  - Center-to-center spacing must be exactly `tileMeters` in X/Z.

**Success looks like:** a 3×3 grid of tiles aligns perfectly and measures correctly.

---

# Phase 4 — Recalibrate Flight Measurements (because the world changed)
Your scripts must treat Unity units as meters and Unity time as seconds.

## 4.1 Core unit conversions (instrument layer)
- **Speed**
  - m/s (Unity velocity magnitude) → knots: `knots = mps * 1.943844`
- **Altitude**
  - meters → feet: `ft = meters * 3.28084`
- **Vertical Speed**
  - m/s → ft/min: `fpm = mps * 196.8504`

## 4.2 What to check/update in code
**Plane physics layer (PlaneController / Rigidbody):**
- Any value that was “tuned by feel” under the old scale likely needs retuning:
  - thrust, lift coefficients, drag factors
  - torque magnitudes (pitch/roll/yaw)
- Ensure `Rigidbody.mass` is sane for a business jet (order of magnitude check).
- Ensure `Fixed Timestep` (Project Settings) is stable for your dynamics.

**Autopilot layer (NavAutopilot / AutopilotController):**
- Heading/track convergence: depends on speed + turn rate (deg/s).
- Altitude hold: depends on vertical speed and pitch authority.
- Waypoint acceptance radius must be in meters now (e.g., 50–200 m typical for a simplified sim).

**FlightDataBus layer:**
- Centralize conversions here so UI never has to guess units.

**Success looks like:** the HUD/PFD readouts match expected real-world magnitudes when you fly.

---

# Phase 5 — Visual Proportions Check (Camera & Altitude sanity)
Expectation: **At 300 m altitude**, the aircraft should look proportionally correct relative to a **610 m tile** (zoom 16).

## 5.1 Quick proportional tests
- Place aircraft at (0, 300, 0) over the center of a zoom 16 tile.
- Confirm:
  - Tile edge-to-edge ≈ 610 m
  - Aircraft length ≈ 33.8 m (~5.5% of tile width)

## 5.2 Camera tuning notes
- Any follow/ND cameras that depended on old scale need:
  - position offsets in meters
  - FOV checks (avoid fisheye distortion)
  - near/far clipping ranges appropriate to km-scale tiles

---

# Phase 6 — “Scale Regression” Tests (prevent future drift)
Create a tiny test harness scene or mode:
- Spawn a zoom 16 3×3 tile grid
- Spawn the aircraft at known positions/altitudes
- Print key telemetry:
  - m/s, knots, meters, feet, fpm
  - tile size verification (measured distances)

**Success looks like:** the tests pass after future hierarchy edits.

---

# Deliverables for End of Day
- [ ] Scene hierarchy cleaned: no parent scaling hacks, roots standardized.
- [ ] Aircraft prefab scaled to **33.8 units** length.
- [ ] `GroundTile_Quad` prefab created + correct scales for zoom 14/15/16.
- [ ] FlightDataBus conversions confirmed (knots/feet/fpm).
- [ ] Autopilot/physics values reviewed for meter-based tuning.
- [ ] A repeatable scale regression test scene exists.

---

# Notes / Guardrails
- Keep UI scaling separate (Canvas Scaler) — do not “solve” world scale with UI tricks.
- Prefer **editing prefabs** over editing instances.
- If a transform must be scaled (rare), isolate it so it doesn’t parent physics objects.
