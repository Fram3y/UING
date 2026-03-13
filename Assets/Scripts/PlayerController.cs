using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Components
    private InputSystem_Actions _playerInputActions;
    private CharacterController _characterController;
    private Animator _animator;

    // Input Variables
    private Vector3 _input;
    private Vector2 _lookInput;
    private Vector3 _lastMoveDirection;
    private Vector3 _lastLookDirection;
    private float _movementBuffer = 0.1f;
    private float _movementTimer;
    private bool _attackMode;
    [SerializeField] private float speed = 10f;

    private void Awake()
    {
        _playerInputActions = new InputSystem_Actions();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Disable();
    }

    private void Update()
    {
        GetInput();

        Look();

        Move();
    }


    // Code for twin stick attack mode looking behaviour - bug with mouse version inputs
    private void Look()
    {
        Vector3 direction = _lastLookDirection;

        if (_attackMode)
        {
            // Controller aiming (right stick)
            if (_lookInput.sqrMagnitude > 0.01f)
            {
                direction = new Vector3(_lookInput.x, 0, _lookInput.y);
            }
            // Mouse aiming (only if the mouse moved)
            else if (Mouse.current != null)
            {
                // Get mouse position in screen space
                Vector2 mousePos = Mouse.current.position.ReadValue();

                // Create a plane at the player's height
                Plane playerPlane = new Plane(Vector3.up, transform.position);

                // Ray from camera through mouse position
                Ray ray = Camera.main.ScreenPointToRay(mousePos);

                if (playerPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    direction = hitPoint - transform.position;
                    direction.y = 0; // Flatten to horizontal
                    direction = ToIsometric(direction); // Apply isometric rotation
                }
            }
        }
        else
        {
            direction = _lastMoveDirection;

            if (direction.sqrMagnitude > 0.001f)
            {
                // Convert movement vector to isometric
                direction = ToIsometric(direction);
            }
        }

        if (direction.sqrMagnitude < 0.001f) return;

        _lastLookDirection = direction;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotation,
            20f * Time.deltaTime
        );
    }

    private void Move()
    {
        // Convert input to world-space isometric direction
        Matrix4x4 isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
        Vector3 moveDirection = isoMatrix.MultiplyPoint3x4(_input);

        _characterController.Move(moveDirection * speed * Time.deltaTime);
    }

    private void GetInput()
    {
        Vector2 input = _playerInputActions.Player.Move.ReadValue<Vector2>();
        _input = new Vector3(input.x, 0, input.y);

        _lookInput = _playerInputActions.Player.Look.ReadValue<Vector2>();

        _attackMode = _playerInputActions.Player.AttackMode.IsPressed();

        _animator.SetBool("AttackMode", _attackMode);

        // Animations are set based on player input
        if (_input.magnitude > 0.1f)
        {
            // We cache player's direction and create a buffer before idle state is triggered
            // This fixes the idle frame flicker when moving from left to right quickly
            _lastMoveDirection = _input;
            _movementTimer = _movementBuffer;
        }
        else
        {
            _movementTimer -= Time.deltaTime;
        }

        _animator.SetBool("Walking", _movementTimer > 0);
    }

    private Vector3 ToIsometric(Vector3 input)
    {
        Matrix4x4 isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
        return isoMatrix.MultiplyPoint3x4(input);
    }
}