using Game.Events;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.GameManager.Player
{

    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        protected int maxHealth;
        [SerializeField]
        protected ParticleSystem bloodParticle;
        private Collider2D playerCollider;
        private SpriteRenderer spriteRenderer;
        private HealthController healthController;

        public static InitializePlayerHealthEvent InitializePlayerHealthEventHandler;
        public static event EventHandler PlayerDeathEventHandler;
        public static event EventHandler ResetHealthEventHandler;
        public static event EventHandler SceneLoaded;

        public void Awake()
        {
            healthController = gameObject.GetComponent<HealthController>();
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            playerCollider = gameObject.GetComponent<Collider2D>();
        }

        // Use this for initialization
        private void Start()
        {
            healthController.SetHealth(maxHealth);
            InitializePlayerHealthEventHandler?.Invoke( this, new InitializePlayerHealthEventArgs(maxHealth));
            var originalColor = spriteRenderer.color;
            healthController.SetOriginalColor(originalColor);
        }

        private void OnEnable()
        {
            HealthController.PlayerIsDamagedEventHandler += CheckDeath;
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void OnDisable()
        {
            HealthController.PlayerIsDamagedEventHandler -= CheckDeath;
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        }

        private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Overworld" || scene.name == "LevelWithEnemies" || scene.name == "ML-Agents-Env")
            {
                playerCollider.enabled = true;
            }
        }

        private void CheckDeath(object sender, PlayerIsDamagedEventArgs eventArgs)
        {
            // Always play the blood particles so we can see the hit
            var mainParticle = bloodParticle.main;
            mainParticle.startSpeed = 0;
            var forceOverLifetime = bloodParticle.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.x = eventArgs.ImpactDirection.x * 20;
            forceOverLifetime.y = eventArgs.ImpactDirection.y * 20;
            forceOverLifetime.z = eventArgs.ImpactDirection.z * 20;
            bloodParticle.Play();

            // If health is still > 0, do nothing
            if (eventArgs.PlayerHealth > 0) return;

            // Check our Singleton to see if we should actually "die"
            if (GameManagerSingleton.Instance != null && GameManagerSingleton.Instance.arenaMode)
            {
                Debug.Log("[Arena] Death prevented. Resetting health via Arena Mode bypass.");
                ResetHealth(); // This heals the player back to max
                return;        // EXIT EARLY: This prevents the 'PlayerDeathEventHandler' below from firing!
            }
            // ----------------------------

            // Original death logic (Scene will only freeze/skeleton only disappears if we reach here)
            SceneLoaded?.Invoke(null, EventArgs.Empty);
            playerCollider.enabled = false;
            PlayerDeathEventHandler?.Invoke(null, EventArgs.Empty);
        }

        public void ResetHealth()
        {
            healthController.SetHealth(maxHealth);
            ResetHealthEventHandler?.Invoke(null, EventArgs.Empty);
        }

        public int GetHealth()
        {
            return healthController.GetHealth();
        }

        public int GetMaxHealth()
        {
            return maxHealth;
        }

        public bool IsInvincible()
        {
            return healthController.IsInvincible();
        }
    }
}