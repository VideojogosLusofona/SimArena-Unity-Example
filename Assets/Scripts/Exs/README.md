# SimArena Unity Visualization

This package provides Unity visualization for the SimArena simulation framework. It allows you to run simulations with visual representation of agents, maps, and events.

## Setup Instructions

### 1. Create a Scene

Create a new Unity scene or use an existing one.

### 2. Set Up Tilemaps

1. Create a Grid GameObject (GameObject > 2D Object > Grid)
2. Add two Tilemap children to the Grid:
   - Floor Tilemap (for walkable areas)
   - Wall Tilemap (for obstacles)
3. Create Rule Tiles for floor and walls in your project

### 3. Create Agent Prefab

1. Create a new prefab for agents with the following components:
   - Sprite Renderer
   - Entity Visualizer script
   - Canvas with:
     - Name Text (TextMeshProUGUI)
     - Health Bar (Slider with Fill Image)

### 4. Create Kill Feed UI

1. Create a Canvas for UI elements
2. Add a vertical layout group for the kill feed
3. Create a kill feed item prefab with:
   - TextMeshProUGUI component
   - KillFeedItem script

### 5. Create End Simulation Panel

1. Add a panel to your UI canvas
2. Add a TextMeshProUGUI for displaying simulation results
3. Add buttons for restarting, pausing, resuming, etc.

### 6. Set Up SimulationLoader

1. Add an empty GameObject to your scene
2. Attach the SimulationLoader script
3. Configure the SimulationLoader in the Inspector:
   - Assign your configuration file (JSON)
   - Set the simulation mode
   - Assign the agent prefab
   - Assign tilemaps and tiles
   - Configure UI elements

### 7. Create Configuration File

1. Create a JSON configuration file for your simulation
2. Place it in the Resources folder or another accessible location
3. Assign it to the SimulationLoader

## Usage

1. Press Play to start the simulation
2. Use the UI buttons to control the simulation (pause, resume, restart)
3. Watch as agents move around, attack each other, and interact with the environment

## Customization

- Modify the configuration file to change simulation parameters
- Create different agent prefabs for different types of entities
- Customize the UI to match your game's style
- Extend the EntityVisualizer to add more visual effects

## Troubleshooting

- If agents don't appear, check that the agent prefab is properly assigned
- If the map doesn't appear, verify that tilemaps and tiles are correctly set up
- If events don't trigger, ensure that event subscriptions are properly set up in SimulationLoader