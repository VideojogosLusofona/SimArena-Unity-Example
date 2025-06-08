using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnitySide.Utilities
{
    [RequireComponent(typeof(Camera))]
    public class FitCameraToTilemap : MonoBehaviour
    {
        public Tilemap tilemap;
        public float padding = 1f;

        private Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
        
            if (tilemap == null)
            {
                Debug.LogError("Tilemap not assigned.");
                return;
            }

            Fit();
        }

        public void Fit()
        {
            // Get the bounds in cell coordinates, then convert to world
            BoundsInt bounds = tilemap.cellBounds;

            Vector3 min = tilemap.CellToWorld(bounds.min);
            Vector3 max = tilemap.CellToWorld(bounds.max);

            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;

            // Move camera to center

            if (tilemap != null)
            {
                Debug.Log(tilemap.name);
            }
            else
            {
                Debug.Log("No tilemap");
            }

            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }
            else
            {
                Debug.Log(cam.name);
            }

            if (cam.transform == null)
            {
                Debug.LogError("Transform component in camera still missing.");
            }
            else
            {
                Debug.Log(cam.transform.name);
            }
        
            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

            if (cam.orthographic)
            {
                float aspect = Screen.width / (float)Screen.height;
                float halfHeight = size.y / 2f + padding;
                float halfWidth = size.x / 2f + padding;

                cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
            }
            else
            {
                Debug.LogWarning("This script is designed for orthographic cameras.");
            }
        }
    }
}