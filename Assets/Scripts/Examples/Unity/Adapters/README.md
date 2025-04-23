# SimToolAI Unity Integration

This directory contains adapter classes that properly decouple the SimToolAI simulation from Unity visualization.

## Architecture Overview

The architecture follows the Adapter pattern to separate the simulation logic from the visualization:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│  Simulation     │◄────┤  Adapter        │◄────┤  Unity          │
│  Core           │     │  Layer          │     │  Visualization  │
│                 │     │                 │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Key Components

### SimulationAdapter

The `SimulationAdapter` class bridges between the core simulation and Unity visualization. It:

- Wraps a `Simulation` instance
- Provides methods to initialize, start, pause, resume, and stop the simulation
- Forwards events from the simulation to Unity components
- Handles player input

### UnitySimulationManager

The `UnitySimulationManager` is a MonoBehaviour that uses the `SimulationAdapter` to:

- Load simulation configurations
- Initialize the simulation with Unity components
- Update entity visualizations
- Process player input

## Usage

### Option 1: Using UnitySceneHook (Recommended)

The `UnitySceneHook` class has been updated to use the `SimulationAdapter`. This approach maintains compatibility with existing code.

```csharp
// Create the simulation adapter
_simulationAdapter = new SimulationAdapter();

// Initialize with map and scene
_simulationAdapter.Initialize(config, mapManager.Map, unityScene);

// Update in the Update method
_simulationAdapter.Update(Time.deltaTime);

// Process player input
_simulationAdapter.ProcessPlayerInput(playerId, direction, attack);
```

### Option 2: Using UnitySimulationManager

For new projects, you can use the `UnitySimulationManager` component:

1. Add the `UnitySimulationManager` component to a GameObject
2. Configure the references to the required components (Grid, Tilemap, etc.)
3. Set the configuration file
4. Call the appropriate methods to control the simulation

## Migration from UnitySimulation

The `UnitySimulation` class is now deprecated. To migrate:

1. Replace `UnitySimulation` with `SimulationAdapter`
2. Initialize the adapter with your map and scene
3. Use the adapter's methods instead of directly accessing the simulation

## Example

```csharp
// Create the adapter
var adapter = new SimulationAdapter();

// Initialize with existing map and scene
adapter.Initialize(config, map, scene);

// Start the simulation
adapter.Start();

// Update the simulation
adapter.Update(deltaTime);

// Process player input
adapter.ProcessPlayerInput(playerId, direction, attack);

// Clean up
adapter.Cleanup();
```