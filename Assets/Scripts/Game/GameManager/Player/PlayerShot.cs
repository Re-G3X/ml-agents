using ScriptableObjects;
using System;
using System.Collections;
using System.Reflection; // Added for reflection
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GameManager.Player
{
    public class PlayerShot : PlayerInputHandler
    {
        private struct BulletForceAndRotation
        {
            public Vector2 Force;
            public int Rotation;
        }

        [SerializeField] protected float shootSpeed, atkSpeed;
        private bool _canShoot;
        private bool _isHoldingShoot;
        private Vector2 _currentShotDir;

        [SerializeField] private GameObject bulletSpawn;
        [SerializeField] private GameObject bulletPrefab;
        [field: SerializeField] public ProjectileTypeSO ProjectileType { get; set; }
        [SerializeField] private ProjectileTypeRuntimeSetSO projectilesAvailable;
        
        private Component _physicsBody; 
        
        private static readonly int LastDirX = Animator.StringToHash("LastDirX");
        private static readonly int LastDirY = Animator.StringToHash("LastDirY");
        private static readonly int IsShooting = Animator.StringToHash("IsShooting");

        private const float _minShootMagnitude = 0.01f;
        private const float _maxShootDir = 0.125f;

        private void Awake()
        {
            _canShoot = true;
        }

        protected override void Start()
        {
            base.Start();
            SetProjectileSo();
            _physicsBody = GetComponent("Rigidbody2D");
        }

        // --- ML-AGENTS COMPATIBLE FUNCTION ---
        public void ApplyShoot(bool isFiring, Vector2 direction)
        {
            if (direction.sqrMagnitude > _minShootMagnitude)
            {
                _currentShotDir = direction.normalized;
            }

            if (isFiring && !_isHoldingShoot)
            {
                _isHoldingShoot = true;
                PlayerAnimator.SetBool(IsShooting, true);
                StopCoroutine(nameof(ShootBulletLoop));
                StartCoroutine(nameof(ShootBulletLoop));
            }
            else if (!isFiring && _isHoldingShoot)
            {
                _isHoldingShoot = false;
                PlayerAnimator.SetBool(IsShooting, false);
            }
        }

        // --- PLAYER INPUT SYSTEM ---
        public void Shoot(InputAction.CallbackContext context)
        {
            Vector2 inputVal = context.ReadValue<Vector2>();
            
            if (context.performed)
                ApplyShoot(true, inputVal);
            else if (context.canceled)
                ApplyShoot(false, inputVal);
        }

        private IEnumerator ShootBulletLoop()
        {
            while (_isHoldingShoot)
            {
                if (!_canShoot)
                {
                    yield return null;
                    continue;
                }

                var bfr = GetBulletForceAndRotation(_currentShotDir);
                UpdateShotAnimation(_currentShotDir);
                
                bulletSpawn.transform.rotation = Quaternion.Euler(0, 0, bfr.Rotation);

                var bullet = Instantiate(bulletPrefab, bulletSpawn.transform.position, bulletSpawn.transform.rotation);
                var bulletController = bullet.GetComponent<ProjectileController>();
                bulletController.ProjectileSo = ProjectileType;

                Vector2 currentVel = Vector2.zero;
                if (_physicsBody != null)
                {
                    // Stealth access to 'velocity' or 'linearVelocity' via reflection
                    PropertyInfo prop = _physicsBody.GetType().GetProperty("linearVelocity") ?? _physicsBody.GetType().GetProperty("velocity");
                    if (prop != null) currentVel = (Vector2)prop.GetValue(_physicsBody, null);
                }

                bulletController.Shoot(bfr.Force + currentVel.normalized);
                
                yield return StartCoroutine(CountCooldown(1.0f / atkSpeed));
            }
        }

        private BulletForceAndRotation GetBulletForceAndRotation(Vector2 shotDirection)
        {
            BulletForceAndRotation bfr;
            if (Mathf.Abs(shotDirection.x) > Mathf.Abs(shotDirection.y))
            {
                bfr.Rotation = shotDirection.x > 0 ? 0 : 180;
                bfr.Force = new Vector2(shotDirection.x > 0 ? shootSpeed : -shootSpeed, 0f);
            }
            else
            {
                bfr.Rotation = shotDirection.y > 0 ? 90 : 270;
                bfr.Force = new Vector2(0f, shotDirection.y > 0 ? shootSpeed : -shootSpeed);
            }
            return bfr;
        }

        private IEnumerator CountCooldown(float bulletCooldown)
        {
            _canShoot = false;
            yield return new WaitForSeconds(bulletCooldown);
            _canShoot = true;
        }

        private void UpdateShotAnimation(Vector2 shotDirection)
        {
            PlayerAnimator.SetFloat(LastDirX, shotDirection.x);
            PlayerAnimator.SetFloat(LastDirY, shotDirection.y);
        }

        // --- REQUIRED OVERRIDES FROM PlayerInputHandler ---

        protected override void StartInput(object sender, EventArgs eventArgs)
        {
            _canShoot = true;
        }

        protected override void StopInput(object sender, EventArgs eventArgs)
        {
            _canShoot = false;
            _isHoldingShoot = false;
            if (PlayerAnimator != null) PlayerAnimator.SetBool(IsShooting, false);
        }

        // --- RE-IMPLEMENTED UTILITIES ---

        private void SetProjectileSo()
        {
            bulletPrefab = ProjectileType.projectilePrefab;
            atkSpeed = ProjectileType.atkSpeed;
        }

        public void ChangeWeapon(InputAction.CallbackContext context)
        {
            var currentIndex = projectilesAvailable.Items.IndexOf(ProjectileType);
            var nextIndex = (currentIndex + 1) % projectilesAvailable.Items.Count;
            ProjectileType.Copy(projectilesAvailable.Items[nextIndex]);
            SetProjectileSo();
        }
    }
}