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
using TMPro;

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
    private InputAction touchHoldAction;
    private bool isDragging = false;
    private Vector3 velocity = Vector3.zero;

    // Camera boundaries
    public Vector2 minBounds;
    public Vector2 maxBounds;
    public float smoothTime = 0.3f;

    void Awake()
    {
        base.GetServices();
        base.Persist<TouchManagerService>();

        playerInput = GetComponent<PlayerInput>();
        touchPositionAction = playerInput.actions.FindAction("TouchPosition");
        touchPressAction = playerInput.actions.FindAction("TouchPress");
    }

    void Update()
    {
        if (!PlayerInput.all[0].enabled)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Collection_Lvl");
            Debug.LogWarning("PlayerInput got disabled, re-enabling...");
            PlayerInput.all[0].enabled = true;
        }

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
            else
            {
                Vector3 hitPosition = hit.point;
                List<CollectableLight> toCollect = new List<CollectableLight>();
                Collider[] hitColliders = Physics.OverlapSphere(hitPosition, 5f);
                foreach (Collider collider in hitColliders)
                {
                    if (collider.tag == "CollectableLight")
                    {
                        CollectableLight lightObject = entityService.spawnedLights.FirstOrDefault(l => l.lightGameObject == collider.gameObject);
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

                foreach (CollectableLight collectableLight in toCollect)
                {
                    paramsDict["uuidList"].ConvertTo<List<string>>().Add(collectableLight.UUID.ToString());
                }

                Packet collectionPacket = new Packet((byte)Packet.PacketType.Action, JObject.FromObject(new { action = "CollectLights", paramsDict }));
                collectionPacket.Send(networkService.localClient.serverSocket);

                CollectLight?.Invoke(toCollect);
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

            // Clamp position to stay within bounds
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);

            Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, targetPosition, ref velocity, smoothTime);

            yield return new WaitForEndOfFrame();
        }
    }
}