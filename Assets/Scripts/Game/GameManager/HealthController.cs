using Game.Events;
using Game.Quests;
using UnityEngine;
using System;

namespace Game.GameManager
{
    public class HealthController : MonoBehaviour
    {
        [SerializeField] private int health;
        private int _maxHealth;
        private bool _isInvincible;
        private float _invincibilityTime;
        private float _invincibilityCount;
        private Color _originalColor;
        private SpriteRenderer _spriteRenderer;
        private EnemyController _enemyController;
        public event Action<float> OnDamageTaken; // Passes the damage amount to Brain/ML-Agents
        public static event PlayerIsDamagedEvent PlayerIsDamagedEventHandler;

        private void Start()
        {
            _enemyController = gameObject.GetComponent<EnemyController>();
            _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

            // FIX 1: Capture the color at the very start so we don't reset to "Invisible"
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void Awake()
        {
            _maxHealth = -1;
            _isInvincible = false;
            _invincibilityCount = 0f;
            _invincibilityTime = 0.2f;
        }

        //TODO change invincibility timer to coroutine
        private void Update()
        {
            if (!_isInvincible) return;
            if (_invincibilityTime < _invincibilityCount)
            {
                _isInvincible = false;
                _spriteRenderer.color = _originalColor;
            }
            else
            {
                _invincibilityCount += Time.deltaTime;
            }
        }

        public void ApplyDamage(int damage, Vector3 impactDirection, int enemyIndex = -1)
        {
            if (_isInvincible) return;
            if (_spriteRenderer != null) _spriteRenderer.color = Color.red;
            
            health -= damage;
            _isInvincible = true;
            _invincibilityCount = 0f;

            OnDamageTaken?.Invoke((float)damage); // NEW: Notify the Agent instantly

            if (gameObject.CompareTag("Player"))
            {
                PlayerIsDamagedEventHandler?.Invoke(this, new PlayerIsDamagedEventArgs(enemyIndex, damage, health, impactDirection));
            }
            else if (gameObject.CompareTag("Enemy"))
            {
                // SAFETY GATE: Check if all quest-related data exists before calling the event
                if (_enemyController != null && 
                    _enemyController.EnemyData != null && 
                    _enemyController.EnemyData.weapon != null)
                {
                    ((IQuestElement)this._enemyController).OnQuestTaskResolved(this, 
                        new QuestDamageEnemyEventArgs(_enemyController.EnemyData.weapon, damage, _enemyController.QuestId));
                }

                // Check for death so the enemy can actually be destroyed
                if (_enemyController != null)
                {
                    _enemyController.CheckDeath();
                }
            }
        }

        public bool ApplyHeal(int healing)
        {
            if (GetMaxHealth() <= GetHealth()) return false;
            var newHealth = health + healing;
            health = _maxHealth >= newHealth ? newHealth : _maxHealth;
            return true;
        }

        public void ResetHealth()
        {
            // 1. Dynamic Health Restore (No more hardcoded 10)
            if (_maxHealth > 0) 
            {
                health = _maxHealth;
            }
            else 
            {
                // Fallback only if _maxHealth was never initialized
                health = gameObject.CompareTag("Player") ? 10 : 3;
                _maxHealth = health;
            }

            // 2. Immediate Shield
            _isInvincible = true;
            _invincibilityCount = 0f;

            // 3. Stealth Physics Reset
            var rbType = System.Type.GetType("UnityEngine.Rigidbody2D, UnityEngine.Physics2DModule");
            if (rbType != null)
            {
                var physBody = GetComponent(rbType) as UnityEngine.Component;
                if (physBody != null)
                {
                    physBody.GetType().GetProperty("simulated")?.SetValue(physBody, true);
                    physBody.GetType().GetProperty("velocity")?.SetValue(physBody, Vector2.zero);
                }
            }

            var colType = System.Type.GetType("UnityEngine.Collider2D, UnityEngine.Physics2DModule");
            if (colType != null)
            {
                var physShape = GetComponent(colType) as UnityEngine.Component;
                if (physShape != null)
                {
                    physShape.GetType().GetProperty("enabled")?.SetValue(physShape, true);
                    physShape.GetType().GetProperty("isTrigger")?.SetValue(physShape, false);
                }
            }

            // 4. Visuals
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;

            // 5. UI REFRESH (The "Double-Tap" Fix)
            if (gameObject.CompareTag("Player"))
            {
                // We fire it twice: once immediately, and once via a delayed call 
                // to force the UI out of its "Grey/Dead" state.
                RefreshUI();
                Invoke(nameof(RefreshUI), 0.1f); 
            }

            Debug.Log($"[Health] {gameObject.name} Reset to {health}/{_maxHealth}");
        }

        private void RefreshUI()
        {
            // Sending damage: 0, current health: full
            PlayerIsDamagedEventHandler?.Invoke(this, new PlayerIsDamagedEventArgs(-1, 0, health, Vector3.zero));
        }

        public void SetHealth(int newHealth)
        {
            // Force maxHealth to be at least the initial health given
            _maxHealth = newHealth; 
            health = newHealth;
        }

        public int GetHealth()
        {
            return health;
        }

        public int GetMaxHealth()
        {
            return _maxHealth;
        }

        public bool IsInvincible()
        {
            return _isInvincible;
        }

        public void SetOriginalColor(Color color)
        {
            _originalColor = color;
        }
    }
}
