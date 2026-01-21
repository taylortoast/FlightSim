# FMS Instrument Wiring Plan (v1)

## North Star Goal
Deliver a stable **student-facing instrument panel** (PFD + ND/HSI + MCP/FMS overlays) where instruments **visibly and correctly respond** to a single authoritative flight-data source (`FlightDataBus`).

Success looks like:
- **No “mystery data sources”** (everything reads from `FlightDataBus`).
- Instruments move smoothly, predictably, and are easy to debug.
- Layout remains correct at **iPad mini resolution (1488×2266)** and scales reliably.

---

## Current Baseline (Done)
- `FlightDataBus` exists in-scene and outputs live values during Play Mode.
- ND is rendered inside the instrument panel using **RenderTexture → RawImage**.
- Canvas scaling standardized to **Scale With Screen Size** with a 1488×2266 reference.

---

## Constraints and Realities
- Some **UI components may not exist yet** (needles, tapes, bugs, digit drums).  
  This plan supports **placeholder** elements first, then swaps in final art without code rewrites.
- Flight sim realism is secondary; **data stability and instrument correctness** are primary.

---

## Data Contract (What Instruments Consume)
`FlightDataBus` should expose (v1):
- **IAS** (m/s now → knots later)
- **ALT** (m now → feet later)
- **HDG** (deg, normalized 0..360)
- **VSI** (m/s now → ft/min later)
- **BRG** (bearing to active waypoint, deg 0..360)
- **DIST** (meters to active waypoint)

### Unit conversions (when we switch the UI to aviation units)
- knots = m/s × **1.94384**
- feet = meters × **3.28084**
- ft/min = m/s × **196.8504**

---

## Wiring Strategy
### Principle: “One bus, many dumb drivers”
All instruments read from `FlightDataBus`.  
Each instrument has a tiny, reusable “driver” script that does one job:

- **RotateNeedleDriver**
  - Input: float value
  - Output: UI rotation between min/max angles
- **ScrollTapeDriver**
  - Input: float value
  - Output: UI translation (pixels/unit) with optional wrapping
- **AttitudeIndicatorDriver**
  - Inputs: pitch + bank (future)
  - Output: horizon translate + bank rotate
- **HeadingRoseDriver**
  - Input: heading
  - Output: rotate compass rose + optional heading bug

Drivers should be:
- Inspector-configurable (min/max ranges, scale factors)
- Simple to debug (optional on-screen/console readout)
- Safe when references are missing (no spam)

---

## Execution Order (Fastest Visible Payoff)
1. **Heading rose rotation** (high impact, minimal complexity)
2. **VSI needle**
3. **Airspeed indicator** (needle or tape)
4. **Altitude indicator** (needle or tape)
5. **Attitude indicator** (most math; do after basics)
6. **Wayfinding cues** (BRG/DIST readouts, course needle, etc.)

---

## Step-by-Step Implementation Plan

### Step 1 — Inventory UI Elements
For each instrument, identify:
- The **Transform/RectTransform** that moves
- Movement type: **rotate** or **translate**
- Scale mapping: e.g. degrees-per-unit or pixels-per-knot
- Value range and clamp behavior

Deliverable:
- A short mapping table of *Instrument → UI element → behavior*.

### Step 2 — Create Driver Scripts (Reusable)
Implement drivers with:
- `FlightDataBus bus` reference
- Value selection (choose which bus field drives it)
- Range/scale parameters
- Optional smoothing (e.g., `Mathf.Lerp`)

Deliverable:
- Drivers compile with no scene dependencies.

### Step 3 — Wire One Instrument End-to-End (Proof Pattern)
Start with **HeadingRoseDriver**:
- Rotate compass rose based on `bus.hdg`
- Verify no 360/0 jitter (normalize angles)

Deliverable:
- Student sees heading respond live.

### Step 4 — Repeat for VSI, Airspeed, Altitude
Wire each in priority order.
Deliverable:
- PFD core instruments animate correctly.

### Step 5 — Attitude Indicator (Pitch/Bank)
When pitch/bank data exists (or is derived):
- Horizon moves with pitch
- Bank rotates around center pivot

Deliverable:
- ADI behaves plausibly for training.

### Step 6 — ND/HSI Enhancements
- BRG/DIST text readout
- Course/track needles (optional)
- Improve map fidelity later (tile/hi-res)

Deliverable:
- ND supports basic navigation comprehension.

---

## Debug & Verification Checklist (Per Instrument)
- Moves in the correct direction (sign correct)
- Correct clamp limits (no wrap glitches unless intended)
- Smoothness acceptable (no stutter)
- Works at target resolution (1488×2266)
- Missing references fail gracefully (no console spam)

---

## Notes on Future Improvements
- Replace placeholder art with final assets without changing scripts.
- Add a “data freeze / replay” mode for training scenarios.
- Upgrade ND map rendering (hi-res or tiled) once core training loop is stable.
