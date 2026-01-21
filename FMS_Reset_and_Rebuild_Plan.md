# FMS Flight Simulation -- Reset & Rebuild Plan

## Purpose

This document defines a clean restart of the Flight Management System
(FMS) flight simulation project.\
Its role is to provide **clear context, constraints, and objectives** so
development can resume without inheriting accidental complexity or
broken assumptions.

This is a **training simulator**, not a physics research project.

------------------------------------------------------------------------

## Core Training Goal

Enable students to observe and understand how **FMS / Autopilot inputs**
affect:

-   Aircraft speed
-   Aircraft altitude
-   Aircraft heading
-   Lateral navigation relative to a flight plan

The simulation must be: - Predictable - Stable - Explainable - Easy to
tune

Visual plausibility matters more than physical accuracy.

------------------------------------------------------------------------

## Non‑Goals (Explicitly Out of Scope for v1)

The following are intentionally excluded from the initial
implementation:

-   Aerodynamic realism (lift curves, stalls, AoA modeling)
-   Ground effect, induced drag, or air density modeling
-   Aircraft-specific performance envelopes
-   Failure modes or edge-case physics
-   "Feels like real flying" tuning

These may be layered in later **only if they support training clarity**.

------------------------------------------------------------------------

## System Philosophy

The simulation will be **state-driven**, not force-driven.

The aircraft does not "fly physics." It **converges toward commanded
targets**.

Real aircraft autopilots operate this way conceptually.

------------------------------------------------------------------------

## Authoritative Model

The following are authoritative targets:

-   Target Airspeed
-   Target Altitude
-   Target Heading

The aircraft controller's responsibility is to **smoothly converge**
toward these targets.

Physics (Rigidbody) exists only to: - Smooth transitions - Prevent
instant changes - Provide visual inertia

------------------------------------------------------------------------

## Minimum Viable Simulation (MVS)

The system is considered successful when:

-   The aircraft spawns in a stable state
-   Target speed, altitude, and heading can be set
-   The aircraft smoothly converges toward those targets
-   HUD values are stable, readable, and sane
-   No oscillation, flipping, or runaway behavior occurs

If this works, the foundation is sound.

------------------------------------------------------------------------

## High‑Level Architecture

### Autopilot / FMS

-   Owns desired targets
-   Updates targets based on flight plan or user input
-   No physics calculations

### PlaneController

-   Reads target values
-   Smoothly adjusts:
    -   Forward speed
    -   Vertical position
    -   Yaw rotation
-   Uses damping and interpolation
-   Avoids force-based aerodynamics

### Rigidbody

-   Optional gravity
-   Used for smoothing, not realism
-   No lift equations in v1

------------------------------------------------------------------------

## Development Plan (Phased)

### Phase 1 -- Baseline Control

-   Implement target speed, altitude, heading
-   Achieve smooth convergence
-   Stable HUD output

### Phase 2 -- Navigation

-   Introduce flight plan tracking
-   Compute cross-track error (XTK)
-   Turn rate limited heading corrections

### Phase 3 -- Polishing

-   Input smoothing
-   Optional visual banking
-   Minor inertia tuning

### Phase 4 -- Optional Realism (Only If Justified)

-   Limited climb/descent rate caps
-   Simplified energy management
-   Instructor-adjustable difficulty

------------------------------------------------------------------------

## Success Criteria

A student or instructor should be able to say:

"I changed the FMS value, and the aircraft reacted exactly as expected."

If behavior surprises the user, the system has failed.

------------------------------------------------------------------------

## Reset Rule

If complexity grows faster than understanding: - Stop - Re-evaluate
objectives - Simplify before adding features

------------------------------------------------------------------------

## Guiding Principle

**Clarity beats realism.\
Stability beats cleverness.\
Training value beats ego.**
