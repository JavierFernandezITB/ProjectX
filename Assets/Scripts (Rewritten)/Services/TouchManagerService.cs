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
    private InputAction touchPressAction;
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
        touchPressAction = playerInput.actions.FindAction("TouchPress");
        currentZoom = cameraTransform.localPosition.y;
    }

    void Update()
    {
        if (!PlayerInput.all[0].enabled)
        {
            Debug.LogWarning("PlayerInput got disabled, re-enabling...");
            PlayerInput.all[0].enabled = true;
        }
        HandleZoom();
        HandleRotation();
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (isInMenu) return;

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
                }
                break;
            case InputActionPhase.Canceled:
                if (context.interaction is HoldInteraction)
                {
                    isDragging = false;
                }
                break;
        }
    }

    public void TouchPressCallback()
    {
        Vector2 screenPos = touchPositionAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.tag == "LightTower")
            {
                InteractWithTower?.Invoke(hit.collider.gameObject);
            }
        }
    }

    public IEnumerator TouchHoldCallback()
    {
        Vector2 initialTouchPosition = touchPositionAction.ReadValue<Vector2>();
        Vector3 initialCameraPosition = Camera.main.transform.position;
        float speedFactor = 0.1f;
        Quaternion cameraRotation = Camera.main.transform.rotation;
        Vector3 cameraRight = cameraRotation * Vector3.right;
        Vector3 cameraForward = cameraRotation * Vector3.forward;

        while (isDragging)
        {
            Vector2 currentTouchPosition = touchPositionAction.ReadValue<Vector2>();
            Vector2 touchDelta = currentTouchPosition - initialTouchPosition;

            Vector3 horizontalMovement = -cameraRight * touchDelta.x * speedFactor;
            Vector3 verticalMovement = -cameraForward * touchDelta.y * speedFactor;
            Vector3 targetPosition = initialCameraPosition + horizontalMovement + verticalMovement;
            targetPosition.y = initialCameraPosition.y;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);

            Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, targetPosition, ref velocity, smoothTime);
            yield return new WaitForEndOfFrame();
        }
    }

    void HandleZoom()
    {
        if (Mouse.current.scroll.ReadValue().y != 0)
        {
            currentZoom -= Mouse.current.scroll.ReadValue().y * zoomSpeed * Time.deltaTime;
        }
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count == 2)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            if (touch0.isInProgress && touch1.isInProgress)
            {
                float prevDistance = (touch0.startPosition.ReadValue() - touch1.startPosition.ReadValue()).magnitude;
                float currentDistance = (touch0.position.ReadValue() - touch1.position.ReadValue()).magnitude;
                float zoomDelta = (currentDistance - prevDistance) * 0.01f;
                currentZoom -= zoomDelta * zoomSpeed * Time.deltaTime;
            }
        }

        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, currentZoom, cameraTransform.localPosition.z);
    }

    void HandleRotation()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            if (!isRotating)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
                isRotating = true;
            }
            else
            {
                Vector2 mouseDelta = Mouse.current.position.ReadValue() - (Vector2)lastMousePosition;
                float rotationX = Mathf.Clamp(cameraTransform.eulerAngles.x - mouseDelta.y * rotationSpeed * Time.deltaTime, minRotationX, maxRotationX);
                cameraTransform.eulerAngles = new Vector3(rotationX, cameraTransform.eulerAngles.y + mouseDelta.x * rotationSpeed * Time.deltaTime, 0);
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count == 2)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            if (touch0.isInProgress && touch1.isInProgress)
            {
                Vector2 avgDelta = (touch0.delta.ReadValue() + touch1.delta.ReadValue()) * 0.5f;
                float rotationX = Mathf.Clamp(cameraTransform.eulerAngles.x - avgDelta.y * rotationSpeed * Time.deltaTime, minRotationX, maxRotationX);
                cameraTransform.eulerAngles = new Vector3(rotationX, cameraTransform.eulerAngles.y + avgDelta.x * rotationSpeed * Time.deltaTime, 0);
            }
        }
        else
        {
            isRotating = false;
        }
    }
}
