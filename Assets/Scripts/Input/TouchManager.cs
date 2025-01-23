using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;

public class TouchManager : MonoBehaviour
{
    public GameManager gameManager;
    private PlayerInput playerInput;

    private InputAction touchPositionAction;
    private InputAction touchPressAction;
    private InputAction touchHoldAction;
    private Client netClient;
    private bool isDragging = false;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        gameManager = GameObject.Find("/GameManager").GetComponent<GameManager>();
        netClient = GameObject.Find("/NetworkManager").GetComponent<Client>();
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
                hit.transform.GetComponent<LightTower>().CollectTowerRewards();
            } else
            {
                Vector3 hitPosition = hit.point;
                gameManager.CollectLights(hitPosition);
            }
        }
    }

    public IEnumerator TouchHoldCallback()
    {
        // Si se está arrastrando, mover la cámara según el toque
        Vector2 initialTouchPosition = touchPositionAction.ReadValue<Vector2>();
        Vector3 initialCameraPosition = Camera.main.transform.position;
        float speedFactor = 0.1f; // Factor de velocidad (ajústalo según lo necesario)

        // Obtener la rotación de la cámara para moverla en el espacio del mundo correctamente
        Quaternion cameraRotation = Camera.main.transform.rotation;
        Vector3 cameraRight = cameraRotation * Vector3.right;  // Dirección derecha de la cámara en el espacio mundial
        Vector3 cameraForward = cameraRotation * Vector3.forward; // Dirección hacia adelante de la cámara
        
        while (isDragging)
        {
            Vector2 currentTouchPosition = touchPositionAction.ReadValue<Vector2>();
            Vector2 touchDelta = currentTouchPosition - initialTouchPosition;

            // Invertir la dirección del movimiento
            Vector3 horizontalMovement = -cameraRight * touchDelta.x * speedFactor; // Movimiento horizontal invertido
            Vector3 verticalMovement = -cameraForward * touchDelta.y * speedFactor;  // Movimiento vertical invertido

            // La posición final de la cámara debe respetar su altura actual (Y) para que no cambie
            Vector3 newPosition = initialCameraPosition + horizontalMovement + verticalMovement;
            newPosition.y = initialCameraPosition.y;  // Mantener la misma altura

            // Actualizar la posición de la cámara
            Camera.main.transform.position = newPosition;

            yield return new WaitForEndOfFrame();
        }
    }




}

