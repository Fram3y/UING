using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Components
    private PlayerInput _playerInput;
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

    [Header("Mouse Tracking")]
    [SerializeField] private GameObject mouseTrackerObject;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        GetInput();

        Look();

        Move();
    }

    /* TWIN STICK STYLE LOOK FUNCTION */
    private void Look()
    {
        Vector3 direction = _lastLookDirection;

        /* ATTACK STATE CHECK TO ALLOW FOR LOOK CODE TO FUNCTION */
        if (_attackMode)
        {
            speed = 1.25f;
            bool isUsingController = _playerInput.currentControlScheme == "Gamepad";

            /* CONTROLLER AIMING */
            if (isUsingController)
            {
                _lookInput = _playerInput.actions["Look"].ReadValue<Vector2>();

                if (_lookInput.sqrMagnitude > 0.01f)
                {
                    Vector2 rotatedInput = new Vector2(_lookInput.y, -_lookInput.x);
                    direction = new Vector3(rotatedInput.x, 0, rotatedInput.y);
                }
                else
                {
                    direction = _lastLookDirection;
                    if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
                }
            }
            /* MOUSE AIMING */
            else if (mouseTrackerObject != null)
            {
                Vector3 rawDirection = mouseTrackerObject.transform.position - transform.position;
                rawDirection.y = 0;

                if (rawDirection.magnitude > 0.5f)
                {
                    float angle = 45f;
                    Quaternion offsetRotation = Quaternion.Euler(0, angle, 0);
                    direction = offsetRotation * rawDirection;
                }
                else
                {
                    direction = transform.forward;
                }
            }
        }
        else
        {
            speed = 2;
            direction = _lastMoveDirection;

            if (direction.sqrMagnitude > 0.001f) direction = ToIsometric(direction);
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

    /* MOVE FUNCTION BASED ON ISOMETRIC MATRIX TRANSLATION */
    private void Move()
    {
        Matrix4x4 isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
        Vector3 moveDirection = isoMatrix.MultiplyPoint3x4(_input);

        _characterController.Move(moveDirection * speed * Time.deltaTime);
    }

    /* INPUT CHECK FOR EVERYTHING */
    private void GetInput()
    {
        Vector2 input = _playerInput.actions["Move"].ReadValue<Vector2>();
        _input = new Vector3(input.x, 0, input.y);

        _lookInput = _playerInput.actions["Look"].ReadValue<Vector2>();

        _attackMode = _playerInput.actions["AttackMode"].IsPressed();

        _animator.SetBool("AttackMode", _attackMode);

        float animatorDampTime = 0.1f;

        /* CONVERT WORLD INPUT TO LOCAL SPACE RELATIVE TO PLAYER'S LOOK DIRECTION FOR ACCURATE MOVEMENTS */
        if (_attackMode)
        {
            Vector3 playerForward = transform.forward;

            Quaternion offsetRotation = Quaternion.Euler(0, -90f, 0);
            playerForward = offsetRotation * playerForward;

            playerForward.y = 0;
            playerForward.Normalize();

            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForward).normalized;

            float localX = Vector3.Dot(_input, playerRight);
            float localY = Vector3.Dot(_input, playerForward);

            _animator.SetFloat("InputX", localX, animatorDampTime, Time.deltaTime);
            _animator.SetFloat("InputY", localY, animatorDampTime, Time.deltaTime);
        }
        else
        {
            _animator.SetFloat("InputX", input.x, animatorDampTime, Time.deltaTime);
            _animator.SetFloat("InputY", input.y, animatorDampTime, Time.deltaTime);
        }

        if (_input.magnitude > 0.1f)
        {
            /* CACHING PLAYER DIRECTION AND APPLYING IDLE BUFFER FOR JITTER */
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