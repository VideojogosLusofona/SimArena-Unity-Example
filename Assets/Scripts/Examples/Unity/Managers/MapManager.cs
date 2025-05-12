using System;
using SimArena.Core.SimulationElements.Map;
using SimArena.Core.SimulationElements.Map.Parser;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Examples.Unity.Managers
{
    /// <summary>
    /// Manages map initialization and rendering
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TileBase wallTile;
        [SerializeField] private TileBase floorTile;
        [SerializeField] private Grid grid;

        /// <summary>
        /// The map instance
        /// </summary>
        public IMap Map { get; private set; }

        /// <summary>
        /// The grid component
        /// </summary>
        public Grid Grid => grid;

        /// <summary>
        /// Initializes the map
        /// </summary>
        public void Initialize()
        {
            ParseMap();
        }
        
        /// <summary>
        /// Initializes the map
        /// </summary>
        public void Initialize(string mapPath)
        {
            ParseMap(mapPath);
        }

        private void ParseMap(string mapPath)
        {
            GridMapParser<GridMap> map = new GridMapParser<GridMap>();

            Map = map.LoadMapFromFile(mapPath);
            Map.Initialize(Map.Width, Map.Height);
            Render(map.GetMapGrid(), Map.Height, Map.Width);
        }

        /// <summary>
        /// Parses the map from the text asset or file
        /// </summary>
        private void ParseMap()
        {
            GridMapParser<GridMap> map = new GridMapParser<GridMap>();

            Map = map.LoadMapFromFile();
            Map.Initialize(Map.Width, Map.Height);
            Render(map.GetMapGrid(), Map.Height, Map.Width);
        }
        
        public void Render(char[,] mapGrid, int height, int width)
        {
            if (mapGrid == null)
                return;
            
            Debug.Log($"Map.Width: {width}, Map.Height: {height}");
            Debug.Log($"Grid Width (X): {mapGrid.GetLength(0)}, Grid Height (Y): {mapGrid.GetLength(1)}");

            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Set the appropriate color based on the map cell
                        switch (mapGrid[x, y])
                        {
                            case '#': // Wall
                                tilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                                break;
                            case '.': // Floor
                                tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                                break;
                            case '&': // Door
                                break;
                            case 'O': // Window
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore exceptions related to console buffer size changes
                if (!(ex is ArgumentOutOfRangeException || ex is System.IO.IOException))
                    throw;
            }
        }
    }
}