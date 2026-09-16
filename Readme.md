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
---

## Table of Contents
* [Preview & Demo](#preview--demo)
* [Project Overview & Summary](#project-overview--summary)
* [Project Logs and Time Tracking](#project-logs-and-time-tracking)
* [Technical Breakdown & Architecture](#technical-breakdown--architecture)
  * [Stage 1: Data Architecture and Influence Map](#stage-1-data-architecture-influence-map)
  * [Stage 2: Agent Movement & Spatial Query API](#stage-2-agent-movement-spatital-query-api)
* [Challenges & Optimization Hurdles](#challenges--optimization-hurdles)
* [Takeaways & Key Learnings](#takeaways--key-learnings)
* [How to Run & Usage](#how-to-run--usage)

## Project Logs and Time Tracking
Breakdown of the time invested during development
| Date | Time Window | Session Duration | Focus Area |
| --- | --- | --- | --- |
| **14th Sept 2026** | 11:25-12:47 | 1 hour 22 mins | Grid coordinate math & Burst Jobs |
| **15th Sept 2026** | 12:36-13:45 & 14:50-15:50 | 2 hour 9 mins | Batched raycast physics for cover & line of sight occlusion |
| **16th Sept 2026** | 19:00-20:30 | TBD | Squad Coordinator & Utility Engine |
| **Future** | TBD | TBD | Squad Manager Integreation & target cell query | 

* **Project Start Date:** September 14th 2026
* **Project Finish Date:** Not finished yet
* **Current Total Time:** 3 hrs 31 mins Hours (Ongoing)
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
---

## How to Run & Usage

### Prerequisites
* **Unity Version:** Unity 6000.5.9f1 or newer
* **Required Packages:** Unity Mathematics, Burst, and the New Input System

### Quick Start Guide
Waiting for more of the project to be made before updating

### License & Usage
This project is open-source and free to use, adapt or build upon without any credit :)