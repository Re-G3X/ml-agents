using Game.Audio;
using Game.GameManager.Player;
using Game.Quests;
using ScriptableObjects;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using Util;
using Game.Events;

namespace Game.GameManager
{
    public class EnemyController : MonoBehaviour, IQuestElement, ISoundEmitter
    {
        [field: SerializeField] protected int IndexOnEnemyList { get; set; }
        [field: SerializeField] protected GameObject PlayerObj { get; set; }
        [field: SerializeField] protected ColorPaletteSo enemyColorPalette;
        [SerializeField] private ParticleSystem bloodParticle;
        [field: SerializeField] protected ParticleSystem CureParticle { get; set; }
        public Transform target;
        private bool _isAIActive = false;
        protected bool isResetting = false;
        private BehaviorType behavior;
        protected static readonly int DieTrigger = Animator.StringToHash("Die");
        private Animator _animator;
        private Color _originalColor;
        public EnemySO EnemyData { get; set; }
        public int QuestId { get; set; }
        private Vector2 _directionMask;
        private float _lastX, _lastY;
        private HealthController _healthController;
        private Rigidbody2D _enemyRigidBody;
        private Collider2D[] _childrenCollider;
        private Collider2D _enemyCollider;
        private bool _isRandomMovement;
        public static event EventHandler PlayerHitEventHandler;
        public static event KillEnemyEvent KillEnemyEventHandler;
        private bool _hasGotComponents;
        public EventHandler<EnemySO> EnemyKilledHandler;
        private Coroutine _walkRoutine;
        
        // end of variables //

        protected virtual void Start()
        {
            // We no longer start the routine here. 
            // Start() is now only for internal component safety.
            if (!_hasGotComponents) GetAllComponents();
        }

        private void FixedUpdate()
        {
            // 1. REACTIVE GATE: Check if we have data/target yet
            if (!_isAIActive)
            {
                CheckReadiness();
            }

            // 2. PHYSICS CONSISTENCY: Ensure we stop moving if resetting
            if (isResetting && _enemyRigidBody != null)
            {
                _enemyRigidBody.linearVelocity = Vector2.zero;
            }
        }

        protected virtual void CheckReadiness()
        {
            // Access the Singleton state
            bool isArena = GameManagerSingleton.Instance != null && GameManagerSingleton.Instance.arenaMode;

            if (_isAIActive) return; 

            if (EnemyData != null && PlayerObj != null)
            {
                StartAI();
            }
            else if (isArena) 
            {
                // In Arena, we allow it to start 'empty' so it doesn't just stand there,
                // but our LoadEnemyData fix will now properly REBOOT this later.
                IsRandomMovement(); 
                StartAI();
            }
        }

        protected virtual void StartAI()
        {
            // FINAL CHECK: If ArenaManager just injected data, make sure we use it!
            if (EnemyData == null && GameManagerSingleton.Instance.arenaMode) 
            {
                // Try to wait one more frame or check if data is coming
                // For now, let's just log it.
            }

            _isAIActive = true;
            Debug.Log($"[AI] {gameObject.name} Logic Activated.");
            
            // Safety: Stop any existing routine before starting a new one
            if (_walkRoutine != null) StopCoroutine(_walkRoutine);
            _walkRoutine = StartCoroutine(WalkAndWait());
        }

        private void OnEnable()
        {
            PlayerController.PlayerDeathEventHandler += PlayerHasDied;
        }

        private void OnDisable()
        {
            PlayerController.PlayerDeathEventHandler -= PlayerHasDied;
        }

        // \/ check later if this method is really necessary \/
        private void PlayerHasDied(object sender, EventArgs eventArgs)
        {
            StartDeath();
        }



        private void GetAllComponents()
        {
            _enemyCollider = GetComponent<Collider2D>();
            _childrenCollider = GetComponentsInChildren<Collider2D>();
            _animator = GetComponent<Animator>();
            _healthController = gameObject.GetComponent<HealthController>();
            _enemyRigidBody = gameObject.GetComponent<Rigidbody2D>();
            PlayerObj = Player.DungeonPlayer.Instance.gameObject;
            _hasGotComponents = true;
        }

        protected virtual void Awake()
        {
            _hasGotComponents = false;
        }

        private void OnPlayerHit()
        {
            PlayerHitEventHandler?.Invoke(null, EventArgs.Empty);
        }

        public virtual bool Heal(int health)
        {
            if (!_healthController.ApplyHeal(health)) return false;
            CureParticle.Play();
            return true;
        }

        public void ApplyDamageEffects(Vector3 impactDirection)
        {
            if (_healthController.GetHealth() <= 0) return;
            ((ISoundEmitter)this).OnSoundEmitted(this, new EmitSfxEventArgs(AudioManager.SfxTracks.EnemyHit));
            var mainParticle = bloodParticle.main;
            mainParticle.startSpeed = 0;
            var forceOverLifetime = bloodParticle.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.x = impactDirection.x * 40;
            forceOverLifetime.y = impactDirection.y * 40;
            forceOverLifetime.z = impactDirection.z * 40;
            bloodParticle.Play();
        }

        private IEnumerator WalkAndWait()
        {
            while (true)
            {
                if (EnemyData is TopdownEnemySO ed)
                    yield return new WaitForSeconds(ed.restTime);
                yield return StartCoroutine(Walk());
                _enemyRigidBody.linearVelocity = Vector3.zero; // old wait() function (which contained only this line of code)
            }
        }

        private IEnumerator Walk()
        {
            var timeWalked = 0.0f;
            var directionMask = new Vector2();
            if (_isRandomMovement)
            {
                _enemyRigidBody.linearVelocity = GetMovementVector(ref directionMask, true);
                if (EnemyData is TopdownEnemySO ed)
                    yield return new WaitForSeconds(ed.activeTime);
            }
            else
            {
                _enemyRigidBody.linearVelocity = GetMovementVector(ref directionMask, true);
                if (EnemyData is TopdownEnemySO ed)
                    while (timeWalked < ed.activeTime)
                    {
                        _enemyRigidBody.linearVelocity = GetMovementVector(ref directionMask, false);
                        timeWalked += Time.deltaTime;
                        yield return null;
                    }
            }
        }

        private bool IsRandomMovement()
        {
            // Safety Check: If data is missing (common in training), default to true
            if (EnemyData == null || EnemyData.movement == null) 
            {
                Debug.LogWarning("EnemyData missing on " + gameObject.name + ". Defaulting to Random Movement.");
                return true; 
            }
            return EnemyData.movement.name.Contains("Random");
        }

        private Vector2 GetMovementVector(ref Vector2 directionMask, bool updateMask)
        {
            // 1. Modified Safety: We only NEED EnemyData and PlayerObj. 
            // We don't strictly need .movement if we have a fallback!
            if (EnemyData == null || PlayerObj == null)
                return Vector2.zero;

            var playerPosition = (Vector2)PlayerObj.transform.position;
            var currentPosition = (Vector2)gameObject.transform.position;
            Vector2 targetMoveDir = Vector2.zero;

            // 2. Determine Direction - Check if movement SO exists
            if (EnemyData.movement != null && EnemyData.movement.movementType != null)
            {
                targetMoveDir = EnemyData.movement.movementType(playerPosition, currentPosition, ref directionMask, updateMask);
            }
            else
            {
                // Fallback: Just move toward the player
                targetMoveDir = (playerPosition - currentPosition).normalized;
                // Debug.Log($"[AI] No MovementSO on {EnemyData.name}, using direct follow.");
            }

            // 3. Determine Speed
            float finalSpeed = (EnemyData is TopdownEnemySO ed) ? ed.movementSpeed : EnemyData.status3;

            // 4. Final Safety: Use 3.0f if the data says 0
            if (finalSpeed <= 0) finalSpeed = 3.0f;

            return targetMoveDir * finalSpeed;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            var collisionDirection = Vector3.Normalize(gameObject.transform.position - collision.gameObject.transform.position);
            if (!collision.gameObject.CompareTag("Player")) return;
            OnPlayerHit();
            if (EnemyData is TopdownEnemySO ed)
                collision.gameObject.GetComponent<HealthController>().ApplyDamage(ed.damage, collisionDirection, IndexOnEnemyList);
        }

        public void CheckDeath()
        {
            if (_healthController.GetHealth() > 0f) return;
            StartDeath();
            InvokeEnemyKilledEvents();
        }

        protected virtual void StartDeath()
        {
            // Notify sound system
            ((ISoundEmitter)this).OnSoundEmitted(this, new EmitSfxEventArgs(AudioManager.SfxTracks.EnemyDeath));
            
            if (_walkRoutine != null) StopCoroutine(_walkRoutine);
            _animator.SetTrigger(DieTrigger);
            
            // Physics Shutdown
            _enemyCollider.enabled = false;
            if (_enemyRigidBody != null) _enemyRigidBody.linearVelocity = Vector2.zero;
            foreach (var childCollider in _childrenCollider) childCollider.enabled = false;

            // --- AUTOMATIC ARENA REGISTRATION ---
            if (GameManagerSingleton.Instance != null && GameManagerSingleton.Instance.arenaMode)
            {
                // registers +1 kill in the arenamanager
                UnityEngine.Object.FindAnyObjectByType<ArenaManager>()?.RegisterKill();
            }
        }

        private void InvokeEnemyKilledEvents()
        {
            EnemyKilledHandler?.Invoke(this, EnemyData);
            
            // Check if this is a "real" game enemy with quest data
            if (EnemyData != null && EnemyData.movement != null && EnemyData.weapon != null)
            {
                ((IQuestElement) this).OnQuestTaskResolved(this, new QuestKillEnemyEventArgs(EnemyData.weapon, QuestId));
                KillEnemyEventHandler?.Invoke(this, new KillEnemyEventArgs(EnemyData.movement.enemyMovementIndex, EnemyData.weapon.Type));
            }
        }

        public void Die()
        {
            Destroy(gameObject);
        }

        public virtual void LoadEnemyData(EnemySO enemyData, int questId)
        {
            _isAIActive = false; // Reset this so FixedUpdate/CheckReadiness can trigger StartAI again
            if (_walkRoutine != null) StopCoroutine(_walkRoutine);
            // Ensure we have the player reference if GetAllComponents failed earlier
            if (PlayerObj == null && Player.DungeonPlayer.Instance != null)
            PlayerObj = Player.DungeonPlayer.Instance.gameObject;
            EnemyData = enemyData;
            QuestId = questId;
            
            // Ensure HealthController is synced with the new data
            if (_healthController != null) 
            {
                int resolvedHealth = (EnemyData is TopdownEnemySO ed) ? ed.health : (int)EnemyData.status1;
                _healthController.SetHealth(resolvedHealth);
            }
        }

        // Child classes (like Skeleton) will override this to color their swords/armor

        // ~ enemy visuals ~ //

        protected virtual void InitializeEnemyVisuals()
        {
            if (enemyColorPalette != null)
            {
                OriginalColor = enemyColorPalette.MainColorD;
                if (TryGetComponent<SpriteRenderer>(out var sr)) sr.color = OriginalColor;
            }
        }

        protected Color GetColorBasedOnMovement()
        {
            switch (EnemyData.movement.enemyMovementIndex)
            {
                case Enums.MovementEnum.Random:
                case Enums.MovementEnum.Random1D:
                    return enemyColorPalette.OutfitColorA;
                case Enums.MovementEnum.Flee1D:
                case Enums.MovementEnum.Flee:
                    return enemyColorPalette.OutfitColorB;
                case Enums.MovementEnum.Follow1D:
                case Enums.MovementEnum.Follow:
                    return enemyColorPalette.OutfitColorC;
                case Enums.MovementEnum.None:
                    return enemyColorPalette.OutfitColorD;
                default:
                    throw new InvalidEnumArgumentException("Movement Enum does not exist");
            }
        }

                protected Color OriginalColor
        {
            get => _originalColor;
            set
            {
                _originalColor = value;
                if (_healthController != null)
                {
                    _healthController.SetOriginalColor(_originalColor);
                }
            }
        }

    }
}