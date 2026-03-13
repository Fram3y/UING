using UnityEngine;
using UnityEngine.InputSystem;

public class MousePositionFollower : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private float maxDistance = 100f;    
    private Camera mainCamera;
    private Mouse mouse;
    
    private void Start()
    {
        mainCamera = Camera.main;
        mouse = Mouse.current;
    }
    
    private void Update()
    {
        if (mainCamera == null || mouse == null) return;
        
        // Get mouse position using Input System
        Vector2 mousePos = mouse.position.ReadValue();
        
        // Create a ray from the camera through the mouse position
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        
        RaycastHit hit;
        
        // Try to hit actual geometry first
        if (Physics.Raycast(ray, out hit, maxDistance, groundLayer))
        {
            transform.position = hit.point;
        }
        else
        {
            // Fallback: intersect with ground plane at y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                transform.position = hitPoint;
            }
        }
    }
}