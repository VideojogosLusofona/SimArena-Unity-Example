# SimArena Unity Example    

This repository provides a **Unity implementation example** of [SimArena](https://github.com/VideojogosLusofona/SimArena), a flexible simulation framework for AI-driven agent-based simulations.  

It demonstrates how to:  
- Render a **grid-based map** in Unity using Tilemap.  
- Instantiate and control **AI agents** powered by the SimArena engine.  
- Run and visualize **team-based objectives** (like Team Deathmatch).  

<br>
<p align="center">
  <img width="560" height="400" src="docs/images/simarena_unity_preview.png">
  <p align="center"><i>Map and agents screenshot</i>
</p>

---

## Features  

- **Map Rendering**  
  - Uses Unity’s **Tilemap** to display the simulation map.  
  - Walkable tiles are drawn with a `floorTile`, blocked areas with a `wallTile`.  
  - Camera automatically adjusts to fit the map.  

- **Simulation Controller**  
  - `GameController` interprets SimArena’s `Simulation` class in Unity.  
  - Loads JSON configuration files (or uses a default setup).  
  - Spawns agents with assigned **Brains** and team memberships.  
  - Subscribes to simulation events (agent killed, team victory).  

- **Agents & Brains**  
  - Supports multiple brain types (`RandomBrain`, `ChaserBrain`, etc.).  
  - Agents are spawned at random or fixed positions.  
  - Each agent is represented visually in Unity with an `AgentView` prefab.  

- **UI Integration**  
  - Displays a victory panel when a team wins.  
  - Restart button resets the match and spawns agents again.  

---

## Getting Started  

### Requirements  
For full integration, you will need to have System.Text.Json and System.Text.Json.Serialization available in your project. If you are using .NET 4.x, you 
will need to install the System.Text.Json NuGet package. If you are using .NET 5 or later, you can use the built-in System.Text.Json namespace.

Unity does not support System.Text.Json natively, so you will need to import the System.Text.Json.dll file from the NuGet package into your Unity project’s Assets folder.

A recommended way to do this is using [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). Simply follow the instructions on the GitHub page to install the package, and then use the NuGet package manager to install System.Text.Json.

### Installation  

1. Clone the repository:  
   ```bash
   git clone https://github.com/VideojogosLusofona/SimArena-Unity-Example.git
   ```
2. Open the project in Unity.
3. Make sure you also have the [SimArena](https://github.com/VideojogosLusofona/SimArena) repository available.
   
  3.1. The recommended way is to import the project as a submodule on your Assets folder:
   ```bash
   git submodule add https://github.com/VideojogosLusofona/SimArena.git External/SimArena
   ```

## Usage

1. Open the Unity scene (`Scenes/SimulationDemo.unity`).
2. Press Play to start a simulation.
3. Watch as AI agents move, fight, and compete toward objectives.
4. When a team wins, the victory panel will appear.
5. Click Restart to reset the simulation.

<br>
<p align="center">
  <img width="560" height="400" src="docs/images/simarena_unity_demo.gif">
  <p align="center"><i>Example gameplay GIF</i>
</p>

## Configuration

Simulations can be customized through JSON configuration files (loaded via `GameController`).

Example configuration snippet:

```
{
  "Name": "Team Deathmatch",
  "Objective": {
    "Type": "DeathmatchObjective",
    "ObjectiveType": "TeamDeathmatch",
    "Teams": 2,
    "PlayersPerTeam": 3
  },
  "Agents": [
    {
      "Name": "Hunter A",
      "Brain": { "Type": "ChaserBrainConfiguration", "Team": 0 },
      "RandomStart": true
    },
    {
      "Name": "Random B",
      "Brain": { "Type": "RandomBrainConfiguration", "Team": 1 },
      "RandomStart": true
    }
  ]
}
```

If no file is provided, a default configuration is generated with two teams (hunters + random agents).

---

## Project Structure

- `MapView.cs`
  - Renders the map to a Tilemap.
- `GameController.cs`
  - Core bridge between Unity and the SimArena engine.
  - Handles simulation loop, agent instantiation, and UI events.
- `AgentView.cs`
  - Simple MonoBehaviour for visualizing agents.
- UI
  - Victory screen with restart button.
