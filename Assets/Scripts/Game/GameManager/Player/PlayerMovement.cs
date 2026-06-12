using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GameManager.Player
{
    public class PlayerMovement : PlayerInputHandler
    {

        private bool _canMove;
        private Rigidbody2D _rigidbody2D;
        [SerializeField] private float speed;
        private Vector2 _lastFacingVector;
        private Vector2 _lastSpeed;
        private static readonly int LastDirX = Animator.StringToHash("LastDirX");
        private static readonly int LastDirY = Animator.StringToHash("LastDirY");
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int DirX = Animator.StringToHash("DirX");
        private static readonly int DirY = Animator.StringToHash("DirY");
        private static readonly int IsShooting = Animator.StringToHash("IsShooting");

        // --- NEW INPUT HANDLER ---
        public void OnMoveInput(InputAction.CallbackContext context)
        {
            //Debug.Log($"[Movement Check] Input Received! Can move: {_canMove}");
            if (!_canMove) return;
            if (context.canceled)
            {
                ApplyMovement(Vector2.zero);
                return;
            }

            if (context.performed)
            {
                var input = context.ReadValue<Vector2>();
                ApplyMovement(input);
            }
        }

        // --- THE MOTOR (This is what will be called by ml-agents) ---
        public void ApplyMovement(Vector2 movement)
        {
            if (!_canMove) 
            {
                //Debug.LogWarning("APPLY MOVEMENT CALLED BUT _CANMOVE IS FALSE!");
                return;
            }
            // If the magnitude is very small, treat it as zero to avoid "drifting"
            if (movement.sqrMagnitude < 0.01f)
            {
                _lastSpeed = Vector2.zero;
                UpdateMoveAnimation(Vector2.zero);
                return;
            }

            movement.Normalize();
            _lastSpeed = movement * speed;
            UpdateMoveAnimation(movement);
        }

        private void FixedUpdate()
        {
            _rigidbody2D.linearVelocity = _lastSpeed;
        }

        private void Awake()
        {
            _canMove = true;
            //Debug.Log($"[Movement Check] Awake fired. _canMove is {_canMove}");
        }

        protected override void Start()
        {
            base.Start();
            _rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
        }

        protected override void StartInput(object sender, EventArgs eventArgs)
        {
            _canMove = true;
        }

        protected override void StopInput(object sender, EventArgs eventArgs)
        {
            _canMove = false;
            _rigidbody2D.linearVelocity = Vector3.zero;
        }


        private void UpdateMoveAnimation(Vector2 movement)
        {
            //If not shooting nor moving, maintain the idle direction
            if (movement.x == 0f && movement.y == 0f)
            {
                if (!PlayerAnimator.GetBool(IsShooting))
                {
                    PlayerAnimator.SetFloat(LastDirX, _lastFacingVector.x);
                    PlayerAnimator.SetFloat(LastDirY, _lastFacingVector.y);
                }
                PlayerAnimator.SetBool(IsMoving, false);
            }
            //Else, update the idle direction
            else
            {
                _lastFacingVector.x = movement.x;
                _lastFacingVector.y = movement.y;
                PlayerAnimator.SetFloat(DirX, movement.x);
                PlayerAnimator.SetFloat(DirY, movement.y);
                PlayerAnimator.SetBool(IsMoving, true);
            }
        }
    }
}