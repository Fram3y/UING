using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController _instance;
    [SerializeField] private CinemachineCamera _cineCam;
    [SerializeField] private InputActionReference _zoomAction;
    [SerializeField] private float _changeAmount = 0.25f;

    private float _minCamView = 2.5f;
    private float _maxCamView = 6.5f;

    private float _lastScrollValue = 0f;
    private const float ScrollThreshold = 0.01f; // Ignore tiny values

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, this becomes the persistent instance
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (_zoomAction != null)
            _zoomAction.action.Enable();
    }

    void OnDisable()
    {
        if (_zoomAction != null)
            _zoomAction.action.Disable();
    }

    void Update()
    {
        HandleZoom();
    }

    void HandleZoom()
    {
        if (_zoomAction == null || _cineCam == null)
        return;

        Vector2 scrollInput = _zoomAction.action.ReadValue<Vector2>();
        float scrollDelta = scrollInput.y;

        // Only process when there's a meaningful change
        if (Mathf.Abs(scrollDelta) < ScrollThreshold)
            return;

        // Determine direction: positive = scroll up (zoom in)
        float direction = Mathf.Sign(scrollDelta);
        
        // Apply discrete step
        float newSize = _cineCam.Lens.OrthographicSize - (direction * _changeAmount);
        newSize = Mathf.Clamp(newSize, _minCamView, _maxCamView);
        
        _cineCam.Lens.OrthographicSize = newSize;
    }
}
