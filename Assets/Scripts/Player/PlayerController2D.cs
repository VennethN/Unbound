using Unbound.Utility;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbound.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.5f;
        [SerializeField] private bool smoothMovement = true;
        [SerializeField, Min(0f)] private float acceleration = 14f;
        [SerializeField, Min(0f)] private float deceleration = 18f;
        [SerializeField, Range(0f, 0.5f)] private float inputDeadzone = 0.08f;

        [Header("Presentation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private bool faceMovementDirection = true;

        private Rigidbody2D _rigidbody;
        private Vector2 _inputVector;
        [Header("Debug")]
        [ReadOnly] [SerializeField] private float moveXDebug;
        [ReadOnly] [SerializeField] private float moveYDebug;
        [ReadOnly] [SerializeField] private float speedDebug;

        private Vector2 _currentVelocity;
        private float _currentSpeedMultiplier = 1f;
        private Vector2 _animationDirection;
        private Vector3 _initialLocalScale;
        private bool _movementEnabled = true;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _interactAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
#endif

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _initialLocalScale = transform.localScale;

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerController2D)} expects a PlayerInput component to be present to read actions.",
                    this);
                return;
            }

            _moveAction = _playerInput.actions?.FindAction("Move", false);
            if (_moveAction == null)
            {
                Debug.LogWarning("Move action not found on the assigned PlayerInput actions map.", this);
            }

            _sprintAction = _playerInput.actions?.FindAction("Sprint", false);
            _interactAction = _playerInput.actions?.FindAction("Interact", false);
            _jumpAction = _playerInput.actions?.FindAction("Jump", false);
            _crouchAction = _playerInput.actions?.FindAction("Crouch", false);
#endif
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _moveAction?.Enable();
            _sprintAction?.Enable();
            _interactAction?.Enable();
            _jumpAction?.Enable();
            _crouchAction?.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _moveAction?.Disable();
            _sprintAction?.Disable();
            _interactAction?.Disable();
            _jumpAction?.Disable();
            _crouchAction?.Disable();
#endif
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (_moveAction != null && _movementEnabled)
            {
                _inputVector = _moveAction.ReadValue<Vector2>();
                if (_inputVector.sqrMagnitude < inputDeadzone * inputDeadzone)
                {
                    _inputVector = Vector2.zero;
                }
            }
            else if (!_movementEnabled)
            {
                _inputVector = Vector2.zero;
            }

            _currentSpeedMultiplier = _sprintAction != null && _sprintAction.IsPressed()
                ? sprintMultiplier
                : 1f;
#endif

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Vector2 normalisedInput = _inputVector.sqrMagnitude > 0f
                ? _inputVector.normalized
                : Vector2.zero;

            Vector2 targetVelocity = normalisedInput * walkSpeed * _currentSpeedMultiplier;
            Vector2 newVelocity;

            if (smoothMovement)
            {
                float rate = normalisedInput.sqrMagnitude > 0f ? acceleration : deceleration;
                newVelocity = Vector2.MoveTowards(
                    _rigidbody.linearVelocity,
                    targetVelocity,
                    rate * Time.fixedDeltaTime);
            }
            else
            {
                newVelocity = targetVelocity;
            }

            _rigidbody.linearVelocity = newVelocity;
            _currentVelocity = newVelocity;

            if (faceMovementDirection && newVelocity.sqrMagnitude > 0.0001f)
            {
                Vector3 localScale = transform.localScale;
                float baseMagnitude = _initialLocalScale.x;

                localScale.x = newVelocity.x >= 0f ? baseMagnitude : -baseMagnitude;
                transform.localScale = localScale;
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null)
            {
                return;
            }

            if (_inputVector.sqrMagnitude > 0f)
            {
                Vector2 direction = _inputVector.normalized;
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    _animationDirection = new Vector2(Mathf.Sign(direction.x), 0f);
                }
                else
                {
                    _animationDirection = new Vector2(0f, Mathf.Sign(direction.y));
                }
            }
            else
            {
                _animationDirection = Vector2.zero;
            }

            animator.SetFloat(moveXParameter, _animationDirection.x);
            animator.SetFloat(moveYParameter, _animationDirection.y);
            animator.SetFloat(speedParameter, _currentVelocity.sqrMagnitude);

            moveXDebug = _animationDirection.x;
            moveYDebug = _animationDirection.y;
            speedDebug = _currentVelocity.sqrMagnitude;
        }

        /// <summary>
        /// Enables or disables player movement
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                // Stop movement immediately when disabled
                _inputVector = Vector2.zero;
                _rigidbody.linearVelocity = Vector2.zero;
                _currentVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Returns whether player movement is currently enabled
        /// </summary>
        public bool IsMovementEnabled()
        {
            return _movementEnabled;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0f, walkSpeed);
            sprintMultiplier = Mathf.Max(1f, sprintMultiplier);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            inputDeadzone = Mathf.Clamp(inputDeadzone, 0f, 0.5f);
        }
#endif
    }
}

