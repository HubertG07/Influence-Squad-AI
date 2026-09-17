# Influence-Driven AI Squad Manager (Unity)
> A high-performance, real-time tactical influence map and squad coordinator build for Unity. The framework processes real-time heatmaps for threat evaluation, cover analysis and squad cluster prevention with no runtime allocation.
---

## Preview & Demo


*Demonstation of the the map processing and AI.
---

## Project Overview & Summary
The **AI Squad Manager** provides tactical spatial awareness for AI agents. Rather than evaluating individual raycasts, the system categorizes the environment into a grid, running parallelized jobs to evaluate the position risk, line of sight, environmental cover and ally density in real time

### Key Features
* **Zero Runtime Heap Allocation:** Update loops use persistent native memory, avoiding GC pauses.
* **Parallel Physics Integration:** Evaluates environmental cover and sight occulusions using `RaycastCommand` queried off the main thread.
* **Modular Layer System:** Heatmaps (Theat, Cover, Ally Density) are all computed independantly and blended into a final actionable score.
* **Normalized Utility Engine:** Evaluate squad metrics through mathematical curves to compute threat urgency.
* **Dynamaic Role Assignment:** Distribute roles among squad based on the team composition contraints while applying cooldown timers.
---

## Table of Contents
* [Preview & Demo](#preview--demo)
* [Project Overview & Summary](#project-overview--summary)
* [Project Logs and Time Tracking](#project-logs-and-time-tracking)
* [Technical Breakdown & Architecture](#technical-breakdown--architecture)
  * [Stage 1: Data Architecture and Influence Map](#stage-1-data-architecture-influence-map)
  * [Stage 2: Agent Movement & Spatial Query API](#stage-2-agent-movement-spatital-query-api)
  * [Stage 3: Squad Coordinator & Utility Engine](#stage-3-squad-coordinator-utility-engine)
  * [Stage 4: Agent State Machine](#stage-4-agent-state-machine)
  * [Stage 5: Unity Editor Tools](#stage-5-unity-editor-tools)
* [Challenges & Optimization Hurdles](#challenges--optimization-hurdles)
* [Takeaways & Key Learnings](#takeaways--key-learnings)
* [How to Run & Usage](#how-to-run--usage)

## Project Logs and Time Tracking
Breakdown of the time invested during development
| Date | Time Window | Session Duration | Focus Area |
| --- | --- | --- | --- |
| **14th Sept 2026** | 11:25-12:47 | 1 hour 22 mins | Grid coordinate math & Burst Jobs |
| **15th Sept 2026** | 12:36-13:45 & 14:50-15:50 | 2 hour 9 mins | Batched raycast physics for cover & line of sight occlusion |
| **16th Sept 2026** | 19:00-20:30 | 1 hour 30 mins | Squad Coordinator & Utility Engine |
| **17th Sept 2026** | 12:40-14:46 | 2 hrs 6 mins | State Machines, Combat Behaviours & Unity Editor Tools Menu |
| **Future** | TBD | TBD | Make the editor tools work & update the AI to improve it | 

* **Project Start Date:** September 14th 2026
* **Project Finish Date:** Not finished yet
* **Current Total Time:** 7 hrs 07 mins Hours (Ongoing)
---

## Technical Breakdown & Architecture
Currently the pipeline operates on a modular, data-oriented workflow

### Stage 1: Data Architecture and Influence Map:
* **Objective:** Establish the low-level grid structures, native memory management and parallel processing job for the heatmaps.
* **Technical Overview:**
    * **Grid Math:** Translate between the 3D world space $(X, Y, Z)$ and 1D flat memory slots using:
    $$\text{Index} = X + (Z \times \text{Width})$$
    * **Line of Sight Threat Cones:** Evaluate the threat levels per grid cell based on the scalar distance falloff and directional dot-product alignment relative to the enemie's forward vector:
    $$\text{Angle} = \arccos(\mathbf{F}_{\text{threat}} \cdot \mathbf{D}_{\text{cell}})$$
    * **Ally Clustering Prevention:** Map active squad positions into `allyDensityMap` to discourage agents for selecting identical cells.
    * **Layer Combination:** Blends the layer buffers into `combindMap` using weighted mathematical mulitplication
    $$\text{Score} = (\text{Cover} \times w_c) \times (1 - \text{Threat} \times w_t) \times (1 - \text{AllyDensity} \times w_a)$$

### Stage 2: Agent Movement & Spatial Query API
* **Objective:** Bridge the spatial heatmaps to active 3D `NavMeshAgent` components without allocation overhead on hte main thread.
* **Technical Overview:**
  * **Allocation Free NavMesh Validation:** Coordinates are validated against the static geometry using `NavMesh.SamplePosition()` to ensure high-scoring cells are walkable.
  * **Query Staggering:** Agent's evaluation timers are initialized with random offsets to prevent multiple AI agents from evaluating the spaition queries on the same frame.
  * **Hysteresis:** Preventing agent oscillation by enforcing a minimum improvement (`scoreThresholdData`). Position is held at a safe location and only repathed if a new cell offers a higher score.
  $$\text{ShouldMove} = \text{Score}_{\text{candidate}} > (\text{Score}_{\text{current}} + \Delta_{\text{threshold}})$$

### Stage 3: Squad Coordinator & Utility Engine
* **Objective:** Dynamically assign roles to squad members using macro-level tactical assessment and value-type scoring
* **Technical Overview:**
  * **Normialized Response Curves:** Use `UtilityEngine` to map raw tactical metrics into normalized (0.0-1.0) scores using mathmatical curves:
    * **Linear:** Predicatable scaling across min/max bounds
    * **Exponential:** Accelerate the urgency response as a metric approaches the key thresholds:
      $$\text{Utility} = \left(\frac{v - v_{\text{min}}}{v_{\text{max}} - v_{\text{min}}}\right)^e$$
    * **Logistic:** Smooth transition around an inflection midpoint:
      $$\text{Utility} = \frac{1}{1 + e^{-k(v - m)}}$$
    * **Decoupled Role Driving:** Roles adjust the movement parameters inside `TacticalAgentMovement` (e.g expanding `searchRadius` for Flankers or tightening `scoreThresholdDelta` for Suppressors)

### Stage 4: Agent State Machine:
* **Objective:** Execute physical behaviours, state transitions and body rotations based on the assigned roles and real time spaital conditions.
* **Technical Overview:**
  * **Pre-allocated FSM:** All states (`IdleState`, `SeekingCoverState`, `SuppressingState`, `FlankingState`, `RetreatingState`) are instantiated once in `Awake()` and stored in a lookup dictionary. Transitions are swap pointers rather than new objects.
  * **Role Driven Behaviours:**
    * **Suppressor:** Locks body rotation directly towards the threat vector.
    * **Flanker:** Aligns body rotation along the active movement velocity vector.
    * **Seeking Cover:** Smoothly rotates toward the threat position.

### Stage 5: Unity Editor Tools
* **Objective:** Provide a runtime debugging visual, heatmap inspector tools, and Unity Editor menu extensions for real time debugging.
* **Technical Overview:**
  * **Real Time Heatmap Visualizer:** `OnDrawGizmos()` to render the active heatmaps (`Combined`, `Threat`, `Cover`, `AllyDensity`) in the scene view.
---

## Challenges & Optimization Hurdles
### 1. Obstacle Threat Penetration
* **Problem:** The raw distance/angle calculations caused threat values to bleed through walls and cover, marking the safe cover positions as dangerous
* **Solution:** Integrated a line of sight raycast sweep originating from the threat source towards the grid cell center and setting the threat values to 0 for anything obscured.
### 2. NavMesh Pacing Race Condition
* **Problem:** Setting `navAgent.SetDestination()` doesn't update the `remainingDistance` immediately on the first frame, causing `remainingDistance <= arrivalDistance` to be triggered prematurely and freeze the agents.
* **Solution:** Updated the evaluation guard to explicitly verify `!navAgent.pathPending && navAgent.hasPath` before evaluating the arrival distance threshold

## Takeaways & Learnings
1. **Flattened Memory Arrays:** Traversing multi-dimensional arrays caused object overhead. Using a 1D flat array ($$\text{Index} = X + Z \times \text{Width}$$) to optimise the memory layout.
2. **Decoupling Data Processing:** Seperating heatmap generation from agent path consumption using a query to keep the systems modular.
3. **Pre-Allocated FSM Design:** Instantiate the state interfaces once into a lookup table eliminiating allocation during transitions.
---

## How to Run & Usage

### Prerequisites
* **Unity Version:** Unity 6000.5.9f1 or newer
* **Required Packages:** Unity Mathematics, Burst, and the New Input System

### Quick Start Guide
Waiting for more of the project to be made before updating

### License & Usage
This project is open-source and free to use, adapt or build upon without any credit :)