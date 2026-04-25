using System.Collections.Generic;
using UnityEngine;
using HornOkPlease.Data;
using HornOkPlease.Traffic.Vehicle;
using HornOkPlease.Traffic.Waypoints;

namespace HornOkPlease.Traffic.Core
{
    public class TrafficManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnAheadDistance = 250f;
        [SerializeField] private float despawnBehindDistance = 150f;
        [SerializeField] private float minSpawnGap = 8f;
        [SerializeField] private int maxVehicles = 60;
        [SerializeField] private float spawnInterval = 0.1f;

        [Header("Archetypes")]
        [SerializeField] private VehicleArchetype[] archetypes;

        private Transform player;
        private List<NPCVehicle> activeVehicles = new List<NPCVehicle>();
        private float spawnTimer;
        private int totalSpawnWeight;
        private bool spawnForwardNext = true; 

        private void Start()
        {
            var bike = FindFirstObjectByType<HornOkPlease.Bike.BikeController>();
            if (bike != null)
                player = bike.transform;
            else
                Debug.LogError("[TrafficManager] No BikeController found!");

            totalSpawnWeight = 0;
            foreach (var a in archetypes)
            {
                if (a != null) totalSpawnWeight += a.spawnWeight;
            }

            // Heavy Initial burst
            if (player != null)
            {
                for (int i = 0; i < maxVehicles; i++)
                {
                    float z = player.position.z + Random.Range(-despawnBehindDistance, spawnAheadDistance * 1.5f);
                    bool oncoming = i % 2 == 0;
                    SpawnVehicle(z, oncoming);
                }
            }
        }

        private void Update()
        {
            if (player == null) return;

            // Despawn
            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                if (activeVehicles[i] == null)
                {
                    activeVehicles.RemoveAt(i);
                    continue;
                }

                NPCVehicle npc = activeVehicles[i];
                float dist = npc.transform.position.z - player.position.z;

                if (npc.Direction > 0 && dist < -despawnBehindDistance)
                {
                    RemoveVehicle(i);
                }
                else if (npc.Direction < 0 && dist < -despawnBehindDistance)
                {
                    RemoveVehicle(i);
                }
                else if (npc.Direction < 0 && dist > spawnAheadDistance * 2f)
                {
                    RemoveVehicle(i);
                }
            }

            // Spawn
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && activeVehicles.Count < maxVehicles)
            {
                spawnTimer = spawnInterval;
                // Try spawning up to 5 vehicles per tick rapidly if we are severely under target
                int burst = Mathf.Min(5, maxVehicles - activeVehicles.Count);
                for (int i = 0; i < burst; i++)
                {
                    float z = player.position.z + Random.Range(spawnAheadDistance * 0.8f, spawnAheadDistance * 1.4f);
                    bool success = SpawnVehicle(z, !spawnForwardNext);
                    if (success) spawnForwardNext = !spawnForwardNext;
                }
            }
        }

        private void RemoveVehicle(int index)
        {
            activeVehicles[index].Deactivate();
            Destroy(activeVehicles[index].gameObject);
            activeVehicles.RemoveAt(index);
        }

        private bool SpawnVehicle(float zPosition, bool oncoming)
        {
            if (archetypes == null || archetypes.Length == 0) return false;

            VehicleArchetype archetype = PickRandomArchetype();
            if (archetype == null) return false;

            int lane;
            if (oncoming)
            {
                int[] lanes = LaneConfig.OncomingLanes;
                lane = lanes[Random.Range(0, lanes.Length)];
            }
            else
            {
                int[] lanes = LaneConfig.ForwardLanes;
                lane = lanes[Random.Range(0, lanes.Length)];
            }

            float laneX = LaneConfig.GetLaneX(lane);
            Vector3 checkPos = new Vector3(laneX, 1f, zPosition);
            if (Physics.CheckBox(checkPos, new Vector3(1.5f, 1f, minSpawnGap / 2f),
                Quaternion.identity, (1 << 7)))
            {
                return false; // occupied
            }

            GameObject go = CreateVehicleVisual(archetype);
            go.name = $"NPC_{archetype.vehicleName}";
            go.layer = LayerMask.NameToLayer("NPCVehicle");

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = archetype.size;
            col.center = Vector3.zero;

            Rigidbody rbComp = go.AddComponent<Rigidbody>();
            rbComp.isKinematic = true;

            NPCVehicle npc = go.AddComponent<NPCVehicle>();
            npc.Initialize(archetype, lane, zPosition);

            var blinkers = go.GetComponent<HornOkPlease.Traffic.Behaviors.VehicleBlinkers>();
            if (blinkers != null)
            {
                Transform lb = go.transform.Find("LeftBlinker");
                Transform rightB = go.transform.Find("RightBlinker");
                blinkers.Initialize(npc, lb?.gameObject, rightB?.gameObject);
            }

            activeVehicles.Add(npc);
            return true;
        }

        private VehicleArchetype PickRandomArchetype()
        {
            int roll = Random.Range(0, totalSpawnWeight);
            int cumulative = 0;
            foreach (var a in archetypes)
            {
                if (a == null) continue;
                cumulative += a.spawnWeight;
                if (roll < cumulative) return a;
            }
            return archetypes[0];
        }

        private GameObject CreateVehicleVisual(VehicleArchetype archetype)
        {
            GameObject root = new GameObject();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.parent = root.transform;
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = archetype.size;

            Object.Destroy(body.GetComponent<Collider>());

            Renderer rend = body.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = archetype.GetColor();
            rend.material = mat;

            // Generate temporary blinker visual cubes (front of vehicle)
            Material blinkerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            blinkerMat.color = new Color(1f, 0.5f, 0f); // Amber / orange
            
            GameObject leftBlinker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBlinker.name = "LeftBlinker";
            leftBlinker.transform.parent = root.transform;
            leftBlinker.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            leftBlinker.transform.localPosition = new Vector3(-archetype.size.x / 2f + 0.1f, archetype.size.y / 2f, archetype.size.z / 2f - 0.1f);
            Object.Destroy(leftBlinker.GetComponent<Collider>());
            leftBlinker.GetComponent<Renderer>().material = blinkerMat;

            GameObject rightBlinker = Object.Instantiate(leftBlinker, root.transform);
            rightBlinker.name = "RightBlinker";
            rightBlinker.transform.localPosition = new Vector3(archetype.size.x / 2f - 0.1f, archetype.size.y / 2f, archetype.size.z / 2f - 0.1f);

            // Add blinker behaviour component
            root.AddComponent<HornOkPlease.Traffic.Behaviors.VehicleBlinkers>();

            return root;
        }
    }
}
