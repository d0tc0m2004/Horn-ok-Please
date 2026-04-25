using UnityEngine;
using UnityEditor;
using HornOkPlease.Bike;
using HornOkPlease.Data;
using HornOkPlease.Road;
using HornOkPlease.Traffic.Core;
using HornOkPlease.Traffic.Pedestrian;
using HornOkPlease.Traffic.Waypoints;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace HornOkPlease.Editor
{
    public static class BikeSceneSetup
    {
        private const string BIKE_LAYER = "Bike";
        private const string ROAD_LAYER = "Road";
        private const string NPC_VEHICLE_LAYER = "NPCVehicle";
        private const string PEDESTRIAN_LAYER = "Pedestrian";
        private const string ROAD_HAZARD_LAYER = "RoadHazard";
        private const string SIDEWALK_LAYER = "Sidewalk";

        // Road dimensions matching LaneConfig (4-lane divided road)
        private const float LaneWidth = 3.5f;
        private const float RoadWidth = LaneWidth * LaneConfig.LaneCount; // 14m
        private const float ShoulderWidth = 0.75f;
        private const float SidewalkWidth = 2f;

        [MenuItem("Horn OK Please/Setup Full Test Scene", false, 0)]
        public static void SetupFullTestScene()
        {
            SetupLayers();
            SetupPhysics();
            BikeConfig bikeConfig = CreateBikeConfigAsset();
            VehicleArchetype[] archetypes = CreateVehicleArchetypes();
            InputActionAsset inputAsset = FindInputActionsAsset();
            SetupProceduralRoad();
            GameObject bike = CreateBike(bikeConfig, inputAsset);
            SetupCamera(bike);
            SetupTraffic(archetypes);
            SetupPedestrians();
            SetupCollisionMatrix();

            Debug.Log("Horn OK Please: Full scene setup complete! 4-lane divided road with traffic + pedestrians. Hit Play!");
        }

        [MenuItem("Horn OK Please/Setup Layers & Physics Only", false, 1)]
        public static void SetupLayersAndPhysicsOnly()
        {
            SetupLayers();
            SetupPhysics();
            SetupCollisionMatrix();
            Debug.Log("Horn OK Please: Layers and physics configured.");
        }

        // ===================== LAYERS & PHYSICS =====================

        private static void SetupLayers()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            SerializedProperty layers = tagManager.FindProperty("layers");

            SetLayer(layers, 6, BIKE_LAYER);
            SetLayer(layers, 7, NPC_VEHICLE_LAYER);
            SetLayer(layers, 8, PEDESTRIAN_LAYER);
            SetLayer(layers, 9, ROAD_LAYER);
            SetLayer(layers, 10, ROAD_HAZARD_LAYER);
            SetLayer(layers, 11, SIDEWALK_LAYER);

            tagManager.ApplyModifiedProperties();
        }

        private static void SetLayer(SerializedProperty layers, int index, string name)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(layer.stringValue))
                layer.stringValue = name;
            else if (layer.stringValue != name)
                Debug.LogWarning($"Layer {index} is already '{layer.stringValue}', skipping '{name}'.");
        }

        private static void SetupPhysics()
        {
            SerializedObject physicsManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/DynamicsManager.asset"));

            var solverIter = physicsManager.FindProperty("m_DefaultSolverIterations");
            if (solverIter != null) solverIter.intValue = 8;
            var velIter = physicsManager.FindProperty("m_DefaultSolverVelocityIterations");
            if (velIter != null) velIter.intValue = 3;

            physicsManager.ApplyModifiedProperties();
        }

        private static void SetupCollisionMatrix()
        {
            int npcLayer = LayerMask.NameToLayer(NPC_VEHICLE_LAYER);
            int pedLayer = LayerMask.NameToLayer(PEDESTRIAN_LAYER);
            int hazardLayer = LayerMask.NameToLayer(ROAD_HAZARD_LAYER);

            if (npcLayer < 0) return;

            Physics.IgnoreLayerCollision(npcLayer, npcLayer, true);
            Physics.IgnoreLayerCollision(npcLayer, pedLayer, true);
            Physics.IgnoreLayerCollision(npcLayer, hazardLayer, true);
            Physics.IgnoreLayerCollision(pedLayer, hazardLayer, true);
            Physics.IgnoreLayerCollision(pedLayer, pedLayer, true);
        }

        // ===================== ROAD =====================

        private static void SetupProceduralRoad()
        {
            // Clean up old static road
            var oldGround = GameObject.Find("Ground");
            if (oldGround != null) Object.DestroyImmediate(oldGround);
            var oldObstacles = GameObject.Find("--- OBSTACLES ---");
            if (oldObstacles != null) Object.DestroyImmediate(oldObstacles);
            var oldRoad = GameObject.Find("--- ROAD ---");
            if (oldRoad != null) Object.DestroyImmediate(oldRoad);

            // Remove any existing ProceduralRoad
            var existingRoad = Object.FindFirstObjectByType<ProceduralRoad>();
            if (existingRoad != null)
            {
                Object.DestroyImmediate(existingRoad.gameObject);
            }

            GameObject go = new GameObject("ProceduralRoad");
            go.AddComponent<ProceduralRoad>();

            Debug.Log("ProceduralRoad created - infinite road generates at runtime");
        }

        // ===================== VEHICLE ARCHETYPES =====================

        private static VehicleArchetype[] CreateVehicleArchetypes()
        {
            string folder = "Assets/Scripts/Data/Archetypes";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scripts/Data"))
                    AssetDatabase.CreateFolder("Assets/Scripts", "Data");
                AssetDatabase.CreateFolder("Assets/Scripts/Data", "Archetypes");
            }

            VehicleArchetype[] archetypes = new VehicleArchetype[]
            {
                CreateArchetype(folder, "AutoRickshaw", "Auto Rickshaw",
                    new Vector3(1.3f, 1.7f, 2.6f), 20f, 35f, 8f, 1.5f, 10f,
                    new Color(0.9f, 0.8f, 0.1f), // yellow
                    new Color[] { new Color(0.9f, 0.8f, 0.1f), new Color(0.2f, 0.7f, 0.2f) },
                    new int[] { 1, 2 }, 8),

                CreateArchetype(folder, "Car", "Car",
                    new Vector3(1.7f, 1.5f, 3.8f), 40f, 70f, 6f, 2.5f, 18f,
                    Color.gray,
                    new Color[] { Color.white, Color.gray, new Color(0.2f, 0.2f, 0.6f),
                        new Color(0.6f, 0.15f, 0.1f), new Color(0.1f, 0.1f, 0.1f),
                        new Color(0.7f, 0.7f, 0.7f) },
                    new int[] { 0, 1, 2 }, 10),

                CreateArchetype(folder, "Bus", "Bus",
                    new Vector3(2.5f, 3.2f, 10f), 30f, 50f, 10f, 1.5f, 25f,
                    new Color(0.7f, 0.2f, 0.1f),
                    new Color[] { new Color(0.7f, 0.2f, 0.1f), new Color(0.2f, 0.4f, 0.7f),
                        new Color(0.9f, 0.5f, 0.1f) },
                    new int[] { 1, 2 }, 3),

                CreateArchetype(folder, "Truck", "Truck",
                    new Vector3(2.4f, 3.5f, 8f), 25f, 45f, 12f, 1.2f, 22f,
                    new Color(0.3f, 0.4f, 0.7f),
                    new Color[] { new Color(0.3f, 0.4f, 0.7f), new Color(0.6f, 0.3f, 0.15f),
                        new Color(0.4f, 0.6f, 0.3f), new Color(0.7f, 0.2f, 0.2f) },
                    new int[] { 1, 2 }, 5),

                CreateArchetype(folder, "Motorcycle", "Motorcycle",
                    new Vector3(0.7f, 1.1f, 2f), 40f, 80f, 4f, 3f, 12f,
                    Color.black,
                    new Color[] { Color.black, new Color(0.6f, 0.1f, 0.1f),
                        new Color(0.1f, 0.1f, 0.5f), Color.white },
                    new int[] { 0, 1, 2 }, 6),

                CreateArchetype(folder, "Bicycle", "Bicycle",
                    new Vector3(0.5f, 1.1f, 1.8f), 10f, 20f, 15f, 1f, 8f,
                    new Color(0.3f, 0.3f, 0.3f),
                    new Color[] { new Color(0.3f, 0.3f, 0.3f), new Color(0.1f, 0.4f, 0.1f),
                        Color.black },
                    new int[] { 2 }, 4),
            };

            AssetDatabase.SaveAssets();
            Debug.Log($"Created {archetypes.Length} vehicle archetype assets");
            return archetypes;
        }

        private static VehicleArchetype CreateArchetype(string folder, string fileName, string vehicleName,
            Vector3 size, float minSpeed, float maxSpeed, float laneChangeInterval, float laneChangeSpeed,
            float avoidDist, Color baseColor, Color[] variants, int[] preferredLanes, int spawnWeight)
        {
            string path = $"{folder}/{fileName}.asset";
            VehicleArchetype existing = AssetDatabase.LoadAssetAtPath<VehicleArchetype>(path);
            if (existing != null) return existing;

            VehicleArchetype a = ScriptableObject.CreateInstance<VehicleArchetype>();
            a.vehicleName = vehicleName;
            a.size = size;
            a.minSpeed = minSpeed;
            a.maxSpeed = maxSpeed;
            a.laneChangeInterval = laneChangeInterval;
            a.laneChangeSpeed = laneChangeSpeed;
            a.avoidanceDistance = avoidDist;
            a.baseColor = baseColor;
            a.randomizeColor = true;
            a.colorVariants = variants;
            a.preferredLanes = preferredLanes;
            a.spawnWeight = spawnWeight;

            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        // ===================== TRAFFIC & PEDESTRIANS =====================

        private static void SetupTraffic(VehicleArchetype[] archetypes)
        {
            if (Object.FindFirstObjectByType<TrafficManager>() != null)
            {
                Debug.Log("TrafficManager already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("TrafficManager");
            TrafficManager tm = go.AddComponent<TrafficManager>();

            SerializedObject so = new SerializedObject(tm);
            SerializedProperty arcProp = so.FindProperty("archetypes");
            arcProp.arraySize = archetypes.Length;
            for (int i = 0; i < archetypes.Length; i++)
            {
                arcProp.GetArrayElementAtIndex(i).objectReferenceValue = archetypes[i];
            }
            so.ApplyModifiedProperties();

            Debug.Log("TrafficManager created with all archetypes assigned");
        }

        private static void SetupPedestrians()
        {
            if (Object.FindFirstObjectByType<PedestrianSpawner>() != null)
            {
                Debug.Log("PedestrianSpawner already exists, skipping.");
                return;
            }

            GameObject go = new GameObject("PedestrianSpawner");
            go.AddComponent<PedestrianSpawner>();

            Debug.Log("PedestrianSpawner created");
        }

        // ===================== BIKE =====================

        private static BikeConfig CreateBikeConfigAsset()
        {
            string folder = "Assets/Scripts/Data";
            string path = $"{folder}/DefaultBikeConfig.asset";

            BikeConfig existing = AssetDatabase.LoadAssetAtPath<BikeConfig>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Scripts", "Data");

            BikeConfig config = ScriptableObject.CreateInstance<BikeConfig>();
            config.groundLayers = (1 << 9) | (1 << 10) | (1 << 11); // Road + Hazard + Sidewalk

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static InputActionAsset FindInputActionsAsset()
        {
            string[] guids = AssetDatabase.FindAssets("BikeInputActions t:InputActionAsset");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                if (asset != null) return asset;
            }

            guids = AssetDatabase.FindAssets("t:InputActionAsset");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.Contains("PackageCache"))
                {
                    InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                    if (asset != null)
                    {
                        Debug.LogWarning($"BikeInputActions not found, using fallback: {assetPath}");
                        return asset;
                    }
                }
            }

            Debug.LogError("No InputActionAsset found!");
            return null;
        }

        private static GameObject CreateBike(BikeConfig config, InputActionAsset inputAsset)
        {
            if (GameObject.FindFirstObjectByType<BikeController>() != null)
            {
                Debug.Log("Bike already exists in scene, skipping.");
                return GameObject.FindFirstObjectByType<BikeController>().gameObject;
            }

            // Root - start in lane 2 (player's default forward lane)
            GameObject bike = new GameObject("Bike");
            bike.transform.position = new Vector3(LaneConfig.GetLaneX(2), 1f, 5f); // lane 2: X = +1.75
            bike.layer = LayerMask.NameToLayer(BIKE_LAYER);
            bike.tag = "Player";

            // Rigidbody - direct control
            Rigidbody rb = bike.AddComponent<Rigidbody>();
            rb.mass = 180f;
            rb.angularDamping = 0f;
            rb.linearDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // CapsuleCollider along Z - realistic motorcycle proportions
            CapsuleCollider col = bike.AddComponent<CapsuleCollider>();
            col.direction = 2; // Z-axis
            col.center = new Vector3(0f, 0.55f, 0f);
            col.radius = 0.35f; // ~0.7m wide
            col.height = 2.0f;  // ~2m long

            PhysicsMaterial bikeMat = new PhysicsMaterial("BikeMaterial");
            bikeMat.dynamicFriction = 0.3f;
            bikeMat.staticFriction = 0.3f;
            bikeMat.bounciness = 0.05f;
            bikeMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            col.material = bikeMat;

            // Ground check child
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.parent = bike.transform;
            groundCheck.transform.localPosition = Vector3.zero;

            // Visual placeholder
            GameObject bikeModel = new GameObject("BikeModel");
            bikeModel.transform.parent = bike.transform;
            bikeModel.transform.localPosition = Vector3.zero;

            Material bikeMaterial = CreateMat(new Color(0.8f, 0.15f, 0.1f));
            Material wheelMat = CreateMat(new Color(0.15f, 0.15f, 0.15f));
            Material riderMat = CreateMat(new Color(0.2f, 0.35f, 0.6f));

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.parent = bikeModel.transform;
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.4f, 0.4f, 1.1f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().material = bikeMaterial;

            // Front wheel
            GameObject frontWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            frontWheel.name = "FrontWheel";
            frontWheel.transform.parent = bikeModel.transform;
            frontWheel.transform.localPosition = new Vector3(0f, 0.25f, 0.75f);
            frontWheel.transform.localScale = new Vector3(0.45f, 0.45f, 0.15f);
            Object.DestroyImmediate(frontWheel.GetComponent<Collider>());
            frontWheel.GetComponent<Renderer>().material = wheelMat;

            // Rear wheel
            GameObject rearWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rearWheel.name = "RearWheel";
            rearWheel.transform.parent = bikeModel.transform;
            rearWheel.transform.localPosition = new Vector3(0f, 0.25f, -0.65f);
            rearWheel.transform.localScale = new Vector3(0.45f, 0.45f, 0.15f);
            Object.DestroyImmediate(rearWheel.GetComponent<Collider>());
            rearWheel.GetComponent<Renderer>().material = wheelMat;

            // Handlebar
            GameObject handlebar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handlebar.name = "Handlebar";
            handlebar.transform.parent = bikeModel.transform;
            handlebar.transform.localPosition = new Vector3(0f, 0.95f, 0.55f);
            handlebar.transform.localScale = new Vector3(0.7f, 0.04f, 0.04f);
            Object.DestroyImmediate(handlebar.GetComponent<Collider>());
            handlebar.GetComponent<Renderer>().material = bikeMaterial;

            // Rider
            GameObject rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = "Rider";
            rider.transform.parent = bikeModel.transform;
            rider.transform.localPosition = new Vector3(0f, 1.1f, -0.1f);
            rider.transform.localScale = new Vector3(0.35f, 0.45f, 0.25f);
            Object.DestroyImmediate(rider.GetComponent<Collider>());
            rider.GetComponent<Renderer>().material = riderMat;

            // Scripts
            BikeInputHandler inputHandler = bike.AddComponent<BikeInputHandler>();
            BikeController controller = bike.AddComponent<BikeController>();
            BikeLeanSystem leanSystem = bike.AddComponent<BikeLeanSystem>();
            BikeStabilitySystem stabilitySystem = bike.AddComponent<BikeStabilitySystem>();
            bike.AddComponent<BikeDebugHUD>();

            // Wire serialized fields
            SerializedObject so;

            so = new SerializedObject(controller);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("bikeModel").objectReferenceValue = bikeModel.transform;
            so.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
            so.ApplyModifiedProperties();

            so = new SerializedObject(inputHandler);
            so.FindProperty("inputActions").objectReferenceValue = inputAsset;
            so.ApplyModifiedProperties();

            so = new SerializedObject(leanSystem);
            so.FindProperty("bikeModel").objectReferenceValue = bikeModel.transform;
            so.FindProperty("riderModel").objectReferenceValue = rider.transform;
            so.ApplyModifiedProperties();

            so = new SerializedObject(stabilitySystem);
            so.FindProperty("handlebarTransform").objectReferenceValue = handlebar.transform;
            so.ApplyModifiedProperties();

            Debug.Log("Created Bike with realistic proportions (0.7m wide, 2m long)");
            return bike;
        }

        // ===================== CAMERA =====================

        private static void SetupCamera(GameObject bike)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            if (mainCam.GetComponent<UniversalAdditionalCameraData>() == null)
                mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            BikeCamera existingCam = mainCam.GetComponent<BikeCamera>();
            if (existingCam != null)
                Object.DestroyImmediate(existingCam);

            BikeCamera bikeCam = mainCam.gameObject.AddComponent<BikeCamera>();

            BikeController controller = bike.GetComponent<BikeController>();
            BikeLeanSystem leanSystem = bike.GetComponent<BikeLeanSystem>();

            SerializedObject so = new SerializedObject(bikeCam);
            so.FindProperty("bike").objectReferenceValue = controller;
            so.FindProperty("leanSystem").objectReferenceValue = leanSystem;
            so.FindProperty("fpOffset").vector3Value = new Vector3(0f, 1.5f, 0.3f);
            so.FindProperty("tpOffset").vector3Value = new Vector3(0f, 2.5f, -6f);
            so.FindProperty("yawFollowSpeed").floatValue = 20f;
            so.FindProperty("rollFollowFactor").floatValue = 0.25f;
            so.FindProperty("pitchFollowFactor").floatValue = 0.3f;
            so.FindProperty("baseFOV").floatValue = 65f;
            so.FindProperty("maxFOV").floatValue = 88f;
            so.FindProperty("fovSmoothSpeed").floatValue = 4f;
            so.FindProperty("lookBehindTransitionSpeed").floatValue = 8f;
            so.FindProperty("baseShakeIntensity").floatValue = 0.005f;
            so.FindProperty("speedShakeMultiplier").floatValue = 0.015f;
            so.FindProperty("shakeFrequency").floatValue = 18f;
            so.ApplyModifiedProperties();

            mainCam.transform.position = bike.transform.position + new Vector3(0f, 1.5f, 0.3f);
            mainCam.transform.rotation = bike.transform.rotation;
            mainCam.nearClipPlane = 0.1f;
        }

        // ===================== UTILITY =====================

        private static Material CreateMat(Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            return mat;
        }
    }
}
