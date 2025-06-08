# Configuration System

This directory contains JSON configuration files that define the simulation scenarios for the game.

## File Structure

Each JSON file represents a complete simulation configuration, including:
- Game settings
- Teams of agents
- Agent properties and behaviors

## How to Use

1. Add a configuration file to the `GameController` in the Unity Inspector
2. Select a configuration from the dropdown (if implemented)
3. Press the Restart button to apply changes

## Creating New Configurations

### Method 1: Using the Configuration Viewer
1. Open the Configuration Viewer from the Tools menu
2. Select an existing configuration as a template
3. Click "Create New Config Based On This"
4. Save the file in the Resources/Configs folder

### Method 2: Manual Editing
1. Copy an existing configuration file
2. Rename it to a descriptive name (keep the .json extension)
3. Edit the file in any text editor
4. Place it in the Resources/Configs folder

## Configuration File Format

```json
{
  "Name": "Scenario Name",
  "MapPath": "",
  "RealtimeMode": true,
  "Agents": [
    {
      "Name": "Agent Name",
      "Brain": {
        "Type": "ChaserBrain or RandomBrain",
        "BrainTypeName": "ChaserBrain or RandomBrain",
        "Awareness": 10,
        "TickIntervalMs": 500,
        "Team": 0,
        "IsPassive": false (ChaserBrain only)
      },
      "RandomStart": true,
      "StartX": 0 (only used if RandomStart is false),
      "StartY": 0 (only used if RandomStart is false),
      "MaxHealth": 100,
      "AttackPower": 10,
      "Defense": 5,
      "Speed": 1.0
    }
  ]
}
```

## Brain Types

### ChaserBrain
A brain that chases or flees from enemy agents.

- **IsPassive**: When true, the agent will flee; when false, it will chase enemies

### RandomBrain
A brain that moves randomly around the map.

## Teams

Agents are grouped by team (Team property in the Brain section). Agents on the same team work together.