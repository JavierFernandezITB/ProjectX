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

    // Events.
    public event Action<List<CollectableLight>> CollectLight;
    public event Action<GameObject> CollectTower;

    // Private variables.
    private PlayerInput playerInput;
    private InputAction touchPositionAction;
    private InputAction touchPressAction;
    private InputAction touchHoldAction;
    private bool isDragging = false;

    void Awake()
    {
        base.GetServices();
        base.Persist<TouchManagerService>();

        playerInput = GetComponent<PlayerInput>();
        touchPositionAction = playerInput.actions.FindAction("TouchPosition");
        touchPressAction = playerInput.actions.FindAction("TouchPress");
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                Debug.Log(context.interaction + " - Performed");

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
                Debug.Log(context.interaction + " - Started");
                break;
            case InputActionPhase.Canceled:
                Debug.Log(context.interaction + " - Canceled");

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

    public void TouchPressCallback()
    {

        Vector2 screenPos = touchPositionAction.ReadValue<Vector2>();

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.tag == "LightTower")
            {
                //hit.transform.GetComponent<LightTower>().CollectTowerRewards();
                CollectTower?.Invoke(hit.collider.gameObject);
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

            Vector3 newPosition = initialCameraPosition + horizontalMovement + verticalMovement;
            newPosition.y = initialCameraPosition.y;

            Camera.main.transform.position = newPosition;

            yield return new WaitForEndOfFrame();
        }
    }
}
