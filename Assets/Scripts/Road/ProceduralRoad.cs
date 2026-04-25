using System.Collections.Generic;
using UnityEngine;
using HornOkPlease.Traffic.Waypoints;

namespace HornOkPlease.Road
{
    /// <summary>
    /// Spawns and recycles road chunks around the player for an infinite road.
    /// Each chunk is a 4-lane divided road with shoulders, sidewalks, and lane markings.
    /// Lanes 0-1 oncoming, lanes 2-3 forward, yellow center divider at X=0.
    /// </summary>
    public class ProceduralRoad : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField] private float chunkLength = 100f;
        [SerializeField] private int chunksAhead = 4;
        [SerializeField] private int chunksBehind = 2;

        [Header("Materials")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material shoulderMaterial;
        [SerializeField] private Material sidewalkMaterial;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material centerLineMaterial;

        private Transform player;
        private Dictionary<int, GameObject> activeChunks = new Dictionary<int, GameObject>();
        private int lastPlayerChunk = int.MinValue;

        // Road dimensions from LaneConfig
        private const float LaneWidth = LaneConfig.LaneWidth;
        private const float RoadWidth = LaneWidth * LaneConfig.LaneCount;
        private const float ShoulderWidth = LaneConfig.ShoulderWidth;
        private const float SidewalkWidth = LaneConfig.SidewalkWidth;

        private void Start()
        {
            var bike = FindFirstObjectByType<HornOkPlease.Bike.BikeController>();
            if (bike != null)
                player = bike.transform;

            if (roadMaterial == null) roadMaterial = CreateMat(new Color(0.22f, 0.22f, 0.22f));
            if (shoulderMaterial == null) shoulderMaterial = CreateMat(new Color(0.3f, 0.3f, 0.28f));
            if (sidewalkMaterial == null) sidewalkMaterial = CreateMat(new Color(0.55f, 0.5f, 0.45f));
            if (lineMaterial == null) lineMaterial = CreateMat(new Color(0.9f, 0.9f, 0.9f));
            if (centerLineMaterial == null) centerLineMaterial = CreateMat(new Color(0.95f, 0.75f, 0.05f));

            UpdateChunks();
        }

        private void Update()
        {
            if (player == null) return;

            int currentChunk = Mathf.FloorToInt(player.position.z / chunkLength);
            if (currentChunk != lastPlayerChunk)
            {
                lastPlayerChunk = currentChunk;
                UpdateChunks();
            }
        }

        private void UpdateChunks()
        {
            int currentChunk = lastPlayerChunk;
            int minChunk = currentChunk - chunksBehind;
            int maxChunk = currentChunk + chunksAhead;

            // Remove chunks outside range
            List<int> toRemove = new List<int>();
            foreach (var kv in activeChunks)
            {
                if (kv.Key < minChunk || kv.Key > maxChunk)
                    toRemove.Add(kv.Key);
            }
            foreach (int key in toRemove)
            {
                Destroy(activeChunks[key]);
                activeChunks.Remove(key);
            }

            // Spawn missing chunks
            for (int i = minChunk; i <= maxChunk; i++)
            {
                if (!activeChunks.ContainsKey(i))
                {
                    activeChunks[i] = CreateChunk(i);
                }
            }
        }

        private GameObject CreateChunk(int chunkIndex)
        {
            float zStart = chunkIndex * chunkLength;
            float zCenter = zStart + chunkLength / 2f;

            GameObject chunk = new GameObject($"RoadChunk_{chunkIndex}");
            chunk.transform.parent = transform;

            int roadLayer = LayerMask.NameToLayer("Road");
            int sidewalkLayer = LayerMask.NameToLayer("Sidewalk");

            // Road surface
            CreateBox(chunk, "Road", roadMaterial,
                new Vector3(0f, -0.5f, zCenter),
                new Vector3(RoadWidth, 1f, chunkLength), roadLayer);

            // Shoulders
            float shoulderLeftX = -(RoadWidth / 2f + ShoulderWidth / 2f);
            float shoulderRightX = RoadWidth / 2f + ShoulderWidth / 2f;
            CreateBox(chunk, "ShoulderL", shoulderMaterial,
                new Vector3(shoulderLeftX, -0.5f, zCenter),
                new Vector3(ShoulderWidth, 1f, chunkLength), roadLayer);
            CreateBox(chunk, "ShoulderR", shoulderMaterial,
                new Vector3(shoulderRightX, -0.5f, zCenter),
                new Vector3(ShoulderWidth, 1f, chunkLength), roadLayer);

            // Sidewalks (raised 0.15m)
            float swLeftX = -(RoadWidth / 2f + ShoulderWidth + SidewalkWidth / 2f);
            float swRightX = RoadWidth / 2f + ShoulderWidth + SidewalkWidth / 2f;
            CreateBox(chunk, "SidewalkL", sidewalkMaterial,
                new Vector3(swLeftX, -0.35f, zCenter),
                new Vector3(SidewalkWidth, 0.7f, chunkLength), sidewalkLayer);
            CreateBox(chunk, "SidewalkR", sidewalkMaterial,
                new Vector3(swRightX, -0.35f, zCenter),
                new Vector3(SidewalkWidth, 0.7f, chunkLength), sidewalkLayer);

            // Center divider - solid double yellow line at X=0 (between oncoming and forward)
            CreateBox(chunk, "CenterLineL", centerLineMaterial,
                new Vector3(-0.1f, 0.005f, zCenter),
                new Vector3(0.12f, 0.01f, chunkLength), roadLayer);
            CreateBox(chunk, "CenterLineR", centerLineMaterial,
                new Vector3(0.1f, 0.005f, zCenter),
                new Vector3(0.12f, 0.01f, chunkLength), roadLayer);

            // Lane divider dashes - between lanes within each direction
            CreateDashes(chunk, -LaneWidth * 2f, zStart);
            CreateDashes(chunk, -LaneWidth, zStart);
            CreateDashes(chunk, LaneWidth, zStart);
            CreateDashes(chunk, LaneWidth * 2f, zStart);

            // Edge lines (solid white)
            CreateBox(chunk, "EdgeL", lineMaterial,
                new Vector3(-RoadWidth / 2f, 0.005f, zCenter),
                new Vector3(0.15f, 0.01f, chunkLength), roadLayer);
            CreateBox(chunk, "EdgeR", lineMaterial,
                new Vector3(RoadWidth / 2f, 0.005f, zCenter),
                new Vector3(0.15f, 0.01f, chunkLength), roadLayer);

            return chunk;
        }

        private void CreateDashes(GameObject parent, float x, float zStart)
        {
            float dashLength = 3f;
            float gapLength = 5f;
            int roadLayer = LayerMask.NameToLayer("Road");

            for (float z = zStart; z < zStart + chunkLength; z += dashLength + gapLength)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "Dash";
                dash.transform.parent = parent.transform;
                dash.transform.position = new Vector3(x, 0.005f, z + dashLength / 2f);
                dash.transform.localScale = new Vector3(0.15f, 0.01f, dashLength);
                dash.layer = roadLayer;
                dash.isStatic = false;
                dash.GetComponent<Renderer>().material = lineMaterial;
                // Remove collider from tiny dashes - not worth the physics cost
                Object.Destroy(dash.GetComponent<Collider>());
            }
        }

        private GameObject CreateBox(GameObject parent, string name, Material mat,
            Vector3 pos, Vector3 scale, int layer)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.parent = parent.transform;
            box.transform.position = pos;
            box.transform.localScale = scale;
            box.layer = layer;
            box.GetComponent<Renderer>().material = mat;
            return box;
        }

        private Material CreateMat(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            return mat;
        }
    }
}
