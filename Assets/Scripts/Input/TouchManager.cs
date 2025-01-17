using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TouchManager : MonoBehaviour
{
    public GameManager gameManager;
    private PlayerInput playerInput;

    private InputAction touchPositionAction;
    private InputAction touchPressAction;
    private Client netClient;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        gameManager = GameObject.Find("/GameManager").GetComponent<GameManager>();
        netClient = GameObject.Find("/NetworkManager").GetComponent<Client>();
        touchPositionAction = playerInput.actions.FindAction("TouchPosition");
        touchPressAction = playerInput.actions.FindAction("TouchPress");
    }

    private void OnEnable()
    {
        touchPressAction.performed += TouchPressCallback;
    }

    private void OnDisable()
    {
        touchPressAction.performed -= TouchPressCallback;
    }

    private void TouchPressCallback(InputAction.CallbackContext context)
    {
        // Get the screen position of the touch input
        Vector2 screenPos = touchPositionAction.ReadValue<Vector2>();

        // Convert screen position to a ray in world space
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // Perform a raycast from the camera to the world position
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Get the world position where the ray hits (e.g., on the ground or at a fixed depth)
            Vector3 hitPosition = hit.point;

            // Call the function to collect lights in the radius around the hit position
            gameManager.CollectLights(hitPosition);
        }
    }
    
}
