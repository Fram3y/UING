using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
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

    [Header("External Objects")]
    [SerializeField] private GameObject _mouseTrackerObject;
    private Vector3 _lastMoveDirection;
    private Vector3 _lastLookDirection;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 10f;
    public float _playerStamina = 100f;
    private float _maxStamina = 100f;
    [SerializeField] private float _staminaDrain = 0.15f;
    [SerializeField] private float _staminaRegen = 0.01f;
    private float _attackCost = 15f;
    [HideInInspector] public bool _hasRegenerated = true;
    [SerializeField] private GameObject _groundCheck;
    [SerializeField] private LayerMask _groundLayer = ~0;
    private float _gravity = -9.81f;
    private float _groundCheckRadius = 0.2f;
    private float _movementBuffer = 0.1f;
    private float _movementTimer;
    private Vector3 _lockedLookDirection;
    private Vector3 _verticalVelocity;
    private bool _isGrounded;
    private bool _isRunning;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    private bool _attackMode;
    private float lastAttackTime;
    private bool _canAttack = true;
    private Coroutine attackCooldownRoutine;

    [Header("Exhaustion Settings")]
    [SerializeField] private float _staminaRecoveryThreshold = 35f;
    private bool _isExhausted = false;
    private Coroutine _exhaustionRoutine;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        GetInput();

        GroundCheck();

        Look();

        Move();

        HandleStamina();
    }

    private void HandleStamina()
    {
        if (_playerStamina >= _maxStamina) _playerStamina = _maxStamina;

        if (_playerStamina <= 0f)
        {
            _playerStamina = 0f;

            if (!_isExhausted)
            {
                _exhaustionRoutine = StartCoroutine(ExhaustedState());
            }
        }

        float attackModeRegen = _staminaRegen / 2f;

        // Exhausted players always regen at attack mode rate
        if (_isExhausted)
        {
            _playerStamina += attackModeRegen * Time.deltaTime;
        }
        else
        {
            if (speed == 1.25f) _playerStamina += attackModeRegen * Time.deltaTime;
            if (speed == 2f) _playerStamina += _staminaRegen * Time.deltaTime;
            if (speed == 3.5f) _playerStamina -= _staminaDrain * Time.deltaTime;
        }

        if (CanvasController._instance != null)
        {
            CanvasController._instance.UpdateStaminaUI(_playerStamina, _maxStamina);
        }
    }

    /* GROUND CHECK */
    private void GroundCheck()
    {
        if (_groundCheck != null)
        {
            /* CHECK IF PLAYER IS CLOSE TO GROUND LAYER */
            _isGrounded = Physics.CheckSphere(_groundCheck.transform.position, _groundCheckRadius, _groundLayer);

            Debug.DrawRay(_groundCheck.transform.position, Vector3.down * _groundCheckRadius, Color.red);
        }
        else
        {
            /* USE CHARACTER CONTROLLER'S BUILT IN GROUND CHECK IF WE CANNOT FIND OUR OWN */
            _isGrounded = _characterController.isGrounded;
        }

        /* RESET VERTICAL VELOCITY WHEN GROUNDED */
        if (_isGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = -2f;
        }
    }

    /* TWIN STICK STYLE LOOK FUNCTION */
    private void Look()
    {
        Vector3 direction = _lastLookDirection;

        bool isAttacking = _animator.GetBool("Attacking");

        /* ATTACK STATE CHECK - LOCK DIRECTION DURING ACTIVE ATTACK */
        if (isAttacking)
        {
            direction = _lockedLookDirection;
        }
        /* ATTACK MODE CHECK - ONLY ALLOW AIMING WHEN NOT ATTACKING */
        else if (_attackMode)
        {
            bool isUsingController = _playerInput.currentControlScheme == "Gamepad";

            if (isAttacking)
            {
                direction = _lockedLookDirection;
            }
            /* CONTROLLER AIMING */
            else if (isUsingController)
            {
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
            else if (_mouseTrackerObject != null)
            {
                Vector3 rawDirection = _mouseTrackerObject.transform.position - transform.position;
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

        bool _isAttcking = _animator.GetBool("Attacking");

        /* WHILE PLAYER IS IN ATTACK MODE PLAYER SPEED IS DECREASED */
        if (_isAttcking || _attackMode)
        {
            speed = 1.25f;
        }
        /* SPRINTING INCREASES PLAYER SPEED BUT PLAYER LOSES STAMINA */
        else if (_isRunning)
        {
            speed = 3.5f;
        }
        /* WHILE PLAYER IS WALKING STAMINA REGENERATES */
        else
        {
            speed = 2f;
        }

        _verticalVelocity.y += _gravity * Time.deltaTime;

        Vector3 finalMovement = (moveDirection * speed) + _verticalVelocity;

        _characterController.Move(finalMovement * Time.deltaTime);
    }

    /* SWING BATTA BATTA SWING BATTA */
    private void PerformAttack()
    {
        _lockedLookDirection = _lastLookDirection;

        if (_lockedLookDirection.sqrMagnitude < 0.001f) _lockedLookDirection = transform.forward;

        _animator.SetBool("Attacking", true);

        _playerStamina -= _attackCost;

        if (attackCooldownRoutine != null) StopCoroutine(attackCooldownRoutine);

        attackCooldownRoutine = StartCoroutine(AttackCooldown(1.5f));

        lastAttackTime = Time.time;
    }

    private IEnumerator AttackCooldown(float attackLength)
    {
        _canAttack = false;
        yield return new WaitForSeconds(attackLength);
        _canAttack = true;
        _animator.SetBool("Attacking", false);
    }

    private IEnumerator ExhaustedState()
    {
        _isExhausted = true;
        _isRunning = false;
        _canAttack = false;

        Debug.Log("Player is exhausted!");

        // Optional: cancel attack mode if you want
        _attackMode = false;
        _animator.SetBool("AttackMode", false);
        _animator.SetBool("Running", false);

        // Wait until stamina reaches threshold
        while (_playerStamina < _staminaRecoveryThreshold)
        {
            yield return null;
        }

        _isExhausted = false;
        _canAttack = true;

        Debug.Log("Player has recovered from exhaustion.");
    }

    /* INPUT CHECK FOR EVERYTHING */
    private void GetInput()
    {
        Vector2 input = _playerInput.actions["Move"].ReadValue<Vector2>();
        _input = new Vector3(input.x, 0, input.y);

        _lookInput = _playerInput.actions["Look"].ReadValue<Vector2>();

        bool wantsAttackMode = _playerInput.actions["AttackMode"].IsPressed();
        bool attackPressed = _playerInput.actions["Attack"].WasPressedThisFrame();
        bool wantsToSprint = _playerInput.actions["Sprint"].IsPressed();

        // Block attack mode and sprint while exhausted
        _attackMode = !_isExhausted && wantsAttackMode;
        _isRunning = !_isExhausted && wantsToSprint;

        _animator.SetBool("AttackMode", _attackMode);
        _animator.SetBool("Running", _isRunning);

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

            /* CHECK FOR ATTACK WHEN IN ATTACK MODE */
            if (attackPressed && !_isExhausted && _canAttack && Time.time > lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
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