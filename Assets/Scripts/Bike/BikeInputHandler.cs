using UnityEngine;
using UnityEngine.InputSystem;

namespace HornOkPlease.Bike
{
    public class BikeInputHandler : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public float Lean { get; private set; }
        public bool HardBrake { get; private set; }
        public bool Horn { get; private set; }
        public bool LookBehind { get; private set; }

        private InputAction throttleAction;
        private InputAction brakeAction;
        private InputAction steerAction;
        private InputAction leanAction;
        private InputAction hardBrakeAction;
        private InputAction hornAction;
        private InputAction lookBehindAction;

        private bool inputReady;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("[BikeInput] InputActionAsset is not assigned! Drag BikeInputActions into the slot.");
                return;
            }

            Debug.Log($"[BikeInput] InputActionAsset: {inputActions.name}, maps: {inputActions.actionMaps.Count}");
            foreach (var map in inputActions.actionMaps)
            {
                Debug.Log($"[BikeInput]   Map: '{map.name}' with {map.actions.Count} actions");
            }

            var bikeMap = inputActions.FindActionMap("Bike", false);
            if (bikeMap == null)
            {
                Debug.LogError($"[BikeInput] No 'Bike' action map found! Available maps listed above. " +
                    $"If using the default InputSystem_Actions, you need BikeInputActions.inputactions instead.");
                return;
            }

            throttleAction = bikeMap.FindAction("Throttle", false);
            brakeAction = bikeMap.FindAction("Brake", false);
            steerAction = bikeMap.FindAction("Steer", false);
            leanAction = bikeMap.FindAction("Lean", false);
            hardBrakeAction = bikeMap.FindAction("HardBrake", false);
            hornAction = bikeMap.FindAction("Horn", false);
            lookBehindAction = bikeMap.FindAction("LookBehind", false);

            if (throttleAction == null) Debug.LogError("[BikeInput] Throttle action not found!");
            if (brakeAction == null) Debug.LogError("[BikeInput] Brake action not found!");
            if (steerAction == null) Debug.LogError("[BikeInput] Steer action not found!");
            if (leanAction == null) Debug.LogError("[BikeInput] Lean action not found!");

            bikeMap.Enable();
            inputReady = throttleAction != null;
            Debug.Log($"[BikeInput] Input ready: {inputReady}");
        }

        private void OnDisable()
        {
            var bikeMap = inputActions?.FindActionMap("Bike");
            bikeMap?.Disable();
            inputReady = false;
        }

        private void Update()
        {
            if (!inputReady) return;

            Throttle = throttleAction.ReadValue<float>();
            Brake = brakeAction?.ReadValue<float>() ?? 0f;
            Steer = steerAction?.ReadValue<float>() ?? 0f;
            Lean = leanAction?.ReadValue<float>() ?? 0f;
            HardBrake = hardBrakeAction?.IsPressed() ?? false;
            Horn = hornAction?.IsPressed() ?? false;
            LookBehind = lookBehindAction?.IsPressed() ?? false;
        }
    }
}
