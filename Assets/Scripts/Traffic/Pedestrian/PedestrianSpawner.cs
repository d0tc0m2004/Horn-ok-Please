using System.Collections.Generic;
using UnityEngine;
using HornOkPlease.Traffic.Waypoints;

namespace HornOkPlease.Traffic.Pedestrian
{
    public class PedestrianSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxPedestrians = 15;
        [SerializeField] private float spawnAheadDistance = 100f;
        [SerializeField] private float despawnBehindDistance = 50f;
        [SerializeField] private float spawnInterval = 1.5f;

        [Header("Appearance")]
        [SerializeField] private Color[] shirtColors = new Color[]
        {
            new Color(0.8f, 0.8f, 0.8f),  // white
            new Color(0.2f, 0.35f, 0.6f), // blue
            new Color(0.6f, 0.15f, 0.1f), // red
            new Color(0.3f, 0.5f, 0.2f),  // green
            new Color(0.5f, 0.3f, 0.15f), // brown
            new Color(0.9f, 0.7f, 0.1f),  // yellow
        };

        private Transform player;
        private List<PedestrianController> activePedestrians = new List<PedestrianController>();
        private float spawnTimer;

        private const float PedHeight = 1.7f;
        private const float PedWidth = 0.4f;

        private void Start()
        {
            var bike = FindFirstObjectByType<HornOkPlease.Bike.BikeController>();
            if (bike != null)
                player = bike.transform;

            // Initial spawn
            if (player != null)
            {
                for (int i = 0; i < maxPedestrians / 2; i++)
                {
                    float z = player.position.z + Random.Range(10f, spawnAheadDistance);
                    SpawnPedestrian(z);
                }
            }
        }

        private void Update()
        {
            if (player == null) return;

            // Despawn
            for (int i = activePedestrians.Count - 1; i >= 0; i--)
            {
                if (activePedestrians[i] == null)
                {
                    activePedestrians.RemoveAt(i);
                    continue;
                }

                float dist = activePedestrians[i].transform.position.z - player.position.z;
                if (dist < -despawnBehindDistance)
                {
                    activePedestrians[i].Deactivate();
                    Destroy(activePedestrians[i].gameObject);
                    activePedestrians.RemoveAt(i);
                }
            }

            // Spawn
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && activePedestrians.Count < maxPedestrians)
            {
                float z = player.position.z + spawnAheadDistance + Random.Range(-10f, 10f);
                SpawnPedestrian(z);
                spawnTimer = spawnInterval;
            }
        }

        private void SpawnPedestrian(float zPosition)
        {
            // Pick a sidewalk side
            bool leftSide = Random.value < 0.5f;
            float x = leftSide ? LaneConfig.SidewalkLeftCenter : LaneConfig.SidewalkRightCenter;
            // Slight random offset within sidewalk
            x += Random.Range(-LaneConfig.SidewalkWidth * 0.3f, LaneConfig.SidewalkWidth * 0.3f);

            Vector3 pos = new Vector3(x, PedHeight / 2f, zPosition);

            // Create visual
            GameObject go = CreatePedestrianVisual();
            go.name = "Pedestrian";
            go.layer = LayerMask.NameToLayer("Pedestrian");

            // Capsule collider
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.height = PedHeight;
            col.radius = PedWidth / 2f;
            col.center = Vector3.zero;

            // Kinematic rigidbody
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Initialize
            PedestrianController ped = go.AddComponent<PedestrianController>();
            float direction = Random.value < 0.5f ? 1f : -1f;
            ped.Initialize(pos, direction);

            activePedestrians.Add(ped);
        }

        private GameObject CreatePedestrianVisual()
        {
            GameObject root = new GameObject();

            // Body (torso)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.parent = root.transform;
            torso.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            torso.transform.localScale = new Vector3(PedWidth, PedHeight * 0.35f, PedWidth * 0.7f);
            Object.Destroy(torso.GetComponent<Collider>());

            Renderer torsoRend = torso.GetComponent<Renderer>();
            Material torsoMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            torsoMat.color = shirtColors[Random.Range(0, shirtColors.Length)];
            torsoRend.material = torsoMat;

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.parent = root.transform;
            head.transform.localPosition = new Vector3(0f, PedHeight * 0.3f, 0f);
            head.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.Destroy(head.GetComponent<Collider>());

            Renderer headRend = head.GetComponent<Renderer>();
            Material skinMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            skinMat.color = new Color(0.72f, 0.55f, 0.4f); // skin tone
            headRend.material = skinMat;

            // Legs (simple cube)
            GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legs.name = "Legs";
            legs.transform.parent = root.transform;
            legs.transform.localPosition = new Vector3(0f, -PedHeight * 0.25f, 0f);
            legs.transform.localScale = new Vector3(PedWidth * 0.8f, PedHeight * 0.35f, PedWidth * 0.6f);
            Object.Destroy(legs.GetComponent<Collider>());

            Renderer legsRend = legs.GetComponent<Renderer>();
            Material legsMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            legsMat.color = new Color(0.2f, 0.2f, 0.25f); // dark pants
            legsRend.material = legsMat;

            return root;
        }
    }
}
