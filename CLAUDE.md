# Horn OK Please - Project Guide

## What is this?
A 3D motorcycle driving game set on chaotic Indian roads, built in Unity 6. The player weaves through unpredictable traffic on a bike.

## Tech Stack
- **Unity 6** with URP 17.2.0
- **New Input System** 1.16.0 (custom BikeInputActions asset at `Assets/Scripts/Input/`)
- **AI Navigation** 2.0.9 (for pedestrian NavMesh pathfinding)
- **Physics module** (Rigidbody-based bike, NOT WheelCollider)

## Architecture Decisions
- **Bike physics:** Raw Rigidbody + manual force application. CapsuleCollider Z-oriented. No WheelCollider (can't model motorcycle lean).
- **NPC vehicles:** Kinematic Rigidbodies on waypoint splines. NPC-NPC collision disabled (handled by AI raycasts). Only player bike has real physics.
- **Pedestrians:** NavMeshAgent on baked NavMesh covering road + sidewalks.
- **Data:** ScriptableObjects for all tuning (BikeConfig, VehicleConfig, RoadSurfaceConfig, TrafficDensityConfig).
- **Camera:** Custom BikeCamera script (no Cinemachine dependency).

## Script Structure
```
Assets/Scripts/
  Bike/        - BikeController, BikeInputHandler, BikeLeanSystem, BikeStabilitySystem, BikeCrashHandler, BikeCamera, BikeDebugHUD
  Road/        - RoadSurface, RoadSurfaceDetector, RoadSurfaceEffect, Pothole, SpeedBump
  Traffic/     - Core/ (TrafficManager, spawn/despawn), Vehicle/ (NPCVehicle, motor, steering, avoidance), Behaviors/ (per-archetype), Pedestrian/, Waypoints/
  Data/        - ScriptableObjects (BikeConfig, VehicleConfig, etc.)
  Input/       - BikeInputActions.inputactions
  Game/        - GameManager, ScoreSystem, UIManager
  Utility/     - ObjectPool, MathUtils
  Editor/      - BikeSceneSetup (auto scene setup via menu "Horn OK Please > Setup Full Test Scene")
```

## Controls
W=throttle, S=brake, A/D=steer, Q/E=lean (body lean for threading gaps), Space=hard brake, H=horn, C=look behind

## Layers
Bike(6), NPCVehicle(7), Pedestrian(8), Road(9), RoadHazard(10), Sidewalk(11)

## Key Physics Notes
- Ground detection uses Raycast from center of mass downward (NOT SphereCast - overlaps at rest and fails)
- Bike auto-upright torque weakens at high speed (creates instability/wobble)
- Lean (Q/E) shifts center of mass laterally + applies lateral force - instant response, no smoothing on physics
- All namespaces under `HornOkPlease.*`

## Build Phases
1. Bike on flat plane (DONE - in progress tuning)
2. Road surfaces + crash system
3. Waypoints + basic NPC traffic
4. NPC archetypes (auto, truck, bus, car, biker, cycle, wrong-way)
5. Pedestrians
6. Game systems + UI

## How to set up a test scene
Menu: **Horn OK Please > Setup Full Test Scene** - creates ground, obstacles, bike with all components, camera, layers, physics settings automatically.

## Common Issues
- If bike doesn't move: check BikeDebugHUD - Grounded must be true, Throttle must show input
- Ground detection needs `groundLayers` set on BikeConfig (should include Road layer 9)
- URP camera needs UniversalAdditionalCameraData component (setup script adds it)
