using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoguesharpBased
{
    public class TilemapView : MonoBehaviour
    {
        public Tilemap tilemap;
        public Tile floorTile;
        public RuleTile wallTile;
        public FitCameraToTilemap fitCameraToTilemap;
        
        public GameEngine Engine { get; private set; }

        public void Init(GameEngine engine)
        {
            Engine = engine;
            RenderMap();
        }

        void RenderMap()
        {
            for (int x = 0; x < Engine.Map.Width; x++)
            {
                for (int y = 0; y < Engine.Map.Height; y++)
                {
                    if (Engine.Map.IsWalkable(x, y))
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                    }
                    else
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                }
            }
            
            fitCameraToTilemap.Fit();
        }
    }
}