using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.Controls;

public class TouchManagerService : ServicesReferences
{
    // Public variables.
    public bool isInMenu;

    // Events.
    public event Action<List<CollectableLight>> CollectLight;
    public event Action<GameObject> InteractWithTower;

    // Private variables.
    private PlayerInput playerInput;
    private InputAction touchPositionAction;
    private bool isDragging = false;
    private Vector3 velocity = Vector3.zero;

    // Camera variables
    [SerializeField] public Transform cameraTransform;
    [SerializeField] public Vector2 minBounds;
    [SerializeField] public Vector2 maxBounds;
    [SerializeField] public float smoothTime = 0.3f;
    [SerializeField] public float zoomSpeed = 5f;
    [SerializeField] public float minZoom = 5f;
    [SerializeField] public float maxZoom = 20f;
    [SerializeField] public float rotationSpeed = 100f;
    [SerializeField] public float minRotationX = 10f;
    [SerializeField] public float maxRotationX = 80f;

    private float currentZoom;
    private Vector3 lastMousePosition;
    private bool isRotating = false;

    void Awake()
    {
        base.GetServices();
        base.Persist<TouchManagerService>();

        playerInput = GetComponent<PlayerInput>();
        touchPositionAction = playerInput.actions.FindAction("TouchPosition");
        currentZoom = 15f;
    }

    void Update()
    {

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Collection_Lvl")
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (isInMenu)
            return;
        switch (context.phase)
        {
            case InputActionPhase.Performed:

                switch (context.interaction)
                {
                    case TapInteraction:
                        TouchPressCallback();
                        break;
                    case HoldInteraction:
                        isDragging = true;
                        StartCoroutine(TouchHoldCallback());
                        break;
                    default:
                        break;
                }

                break;
            case InputActionPhase.Started:
                break;
            case InputActionPhase.Canceled:

                switch (context.interaction)
                {
                    case HoldInteraction:
                        isDragging = false;
                        break;
                }
                break;
            default:
                break;
        }
    }

    public void TouchPressCallback(Vector2? screenPos = null)
    {
        if (screenPos == null)
            screenPos = touchPositionAction.ReadValue<Vector2>();

        Ray ray = Camera.main.ScreenPointToRay((Vector3)screenPos);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.tag == "LightTower")
            {
                //hit.transform.GetComponent<LightTower>().CollectTowerRewards();
                InteractWithTower?.Invoke(hit.collider.gameObject);
            }
            else
            {
                Vector3 hitPosition = hit.point;
                List<CollectableLight> toCollect = new List<CollectableLight>();
                Collider[] hitColliders = Physics.OverlapSphere(hitPosition, 5f);
                foreach (Collider collider in hitColliders)
                {
                    if (collider.tag == "CollectableLight")
                    {
                        CollectableLight lightObject = entityService.spawnedLights.FirstOrDefault(l =>
                        {
                            return l.lightGameObject == collider.gameObject;
                        });
                        if (lightObject != null)
                            toCollect.Add(lightObject);
                    }
                }

                if (toCollect.Count == 0)
                    return;

                Dictionary<string, object> paramsDict = new Dictionary<string, object>()
                {
                    { "mousePosX", hitPosition.x},
                    { "mousePosY", hitPosition.y},
                    { "mousePosZ", hitPosition.z},
                    { "uuidList", new List<string>() }
                };

                Dictionary<string, object> collectLights = new Dictionary<string, object>() {
                    { "action", "CollectLights" },
                    { "params", paramsDict }
                };

                foreach (CollectableLight collectableLight in toCollect)
                {
                    paramsDict["uuidList"].ConvertTo<List<string>>().Add(collectableLight.UUID.ToString());
                }

                Packet collectionPacket = new Packet((byte)Packet.PacketType.Action, JObject.FromObject(collectLights));
                collectionPacket.Send(networkService.localClient.serverSocket);

                CollectLight?.Invoke(toCollect);
            }
        }
    }

    public IEnumerator TouchHoldCallback()
    {
        Vector3 initialCameraPosition = Camera.main.transform.position;
        float speedFactor = 0.5f; // Reduced for smoother, slower movement
        float smoothTime = 0.1f; // Time for smooth transition
        Quaternion cameraRotation = Camera.main.transform.rotation;
        Vector3 cameraRight = cameraRotation * Vector3.right;
        Vector3 cameraForward = cameraRotation * Vector3.forward;

        while (isDragging)
        {
            Vector2 initialTouchPosition = touchPositionAction.ReadValue<Vector2>(); // Reset each frame
            yield return null; // Wait one frame before processing movement
            Vector2 currentTouchPosition = touchPositionAction.ReadValue<Vector2>();

            Vector2 touchDelta = currentTouchPosition - initialTouchPosition;

            // Calculate movement direction for panning (dragging the camera)
            Vector3 horizontalMovement = -cameraRight * touchDelta.x * speedFactor;
            Vector3 verticalMovement = -cameraForward * touchDelta.y * speedFactor;

            // Target position with controlled movement
            Vector3 targetPosition = initialCameraPosition + horizontalMovement + verticalMovement;
            targetPosition.y = initialCameraPosition.y; // Keep camera height constant

            // SmoothDamp for fluid, controlled movement
            Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, targetPosition, ref velocity, smoothTime);

            // Handle Zooming (if two fingers are on the screen)
            //HandleZoom();

            // Handle Rotation (if two fingers are on the screen)
            //HandleRotation();

            initialCameraPosition = Camera.main.transform.position; // Update for next iteration

            yield return new WaitForEndOfFrame();
        }
    }
    /*
    void HandleZoom()
    {
        // Check if there are exactly two touches
        if (Touchscreen.current != null && Touchscreen.current.touches[0].position.x.ReadValue() != 0 && Touchscreen.current.touches[1].position.x.ReadValue() != 0)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            if (touch0.isInProgress && touch1.isInProgress)
            {
                // Calculate initial distance between two touches (used at start)
                float prevDistance = (touch0.startPosition.ReadValue() - touch1.startPosition.ReadValue()).magnitude;
                // Get current distance between touches
                float currentDistance = (touch0.position.ReadValue() - touch1.position.ReadValue()).magnitude;
                // Determine the change in distance between the two touches
                float zoomDelta = currentDistance - prevDistance;

                // Apply zoom, scale the zoomSpeed factor (multiply by zoomSpeed)
                currentZoom -= zoomDelta * zoomSpeed * Time.deltaTime;
                // Clamp zoom within min and max limits
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

                // Adjust camera's local position based on the zoom
                cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, currentZoom, cameraTransform.localPosition.z);
            }
        }
    }

    void HandleRotation()
    {
        Debug.Log(Touchscreen.current.touches[0].position.x.ReadValue());
        // Check if there are exactly two touches
        if (Touchscreen.current != null && Touchscreen.current.touches[0].position.x.ReadValue() != 0 && Touchscreen.current.touches[1].position.x.ReadValue() != 0)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            if (touch0.isInProgress && touch1.isInProgress)
            {
                // Calculate the delta (change in position) of each touch
                Vector2 touch0Delta = touch0.delta.ReadValue();
                Vector2 touch1Delta = touch1.delta.ReadValue();

                // Average the deltas from both touch points
                Vector2 avgDelta = (touch0Delta + touch1Delta) * 0.5f;

                // Calculate rotation based on the average delta
                float rotationX = Mathf.Clamp(cameraTransform.eulerAngles.x - avgDelta.y * rotationSpeed * Time.deltaTime, minRotationX, maxRotationX);
                float rotationY = cameraTransform.eulerAngles.y + avgDelta.x * rotationSpeed * Time.deltaTime;

                // Apply the new rotation to the camera
                cameraTransform.eulerAngles = new Vector3(rotationX, rotationY, 0);
            }
        }
    }
    */

}