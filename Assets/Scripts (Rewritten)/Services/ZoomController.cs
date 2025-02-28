using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [SerializeField] public Camera mainCamera;  // Reference to the camera

    [Header("Zoom Settings")]
    [SerializeField]public Slider zoomSlider;
    public float minZoom = 5f;
    public float maxZoom = 80f;

    [Header("Rotation Settings")]
    [SerializeField]public Slider rotationSlider;
    public float minRotation = -45f;
    public float maxRotation = 45f;

    private float initialRotationY; // Store initial camera Y rotation

    void Start()
    {
        if (zoomSlider != null)
        {
            zoomSlider.minValue = minZoom;
            zoomSlider.maxValue = maxZoom;
            zoomSlider.value = mainCamera.orthographic ? mainCamera.orthographicSize : mainCamera.fieldOfView;
            zoomSlider.onValueChanged.AddListener(UpdateZoom);
        }

        if (rotationSlider != null)
        {
            initialRotationY = mainCamera.transform.eulerAngles.y;
            rotationSlider.minValue = minRotation;
            rotationSlider.maxValue = maxRotation;
            rotationSlider.value = 0f;  // Start at default rotation
            rotationSlider.onValueChanged.AddListener(UpdateRotation);
        }
    }

    void UpdateZoom(float value)
    {
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = value;
        }
        else
        {
            mainCamera.fieldOfView = value;
        }
    }

    void UpdateRotation(float value)
    {
        mainCamera.transform.rotation = Quaternion.Euler(
            mainCamera.transform.eulerAngles.x,
            initialRotationY + value,
            mainCamera.transform.eulerAngles.z
        );
    }
}