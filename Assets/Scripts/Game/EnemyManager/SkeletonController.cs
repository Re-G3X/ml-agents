using Game.GameManager;
using ScriptableObjects;
using UnityEngine;

namespace Game.EnemyManager
{
    public class SkeletonController : EnemyController
    {
        [field: SerializeField] protected GameObject Armor { get; set; }
        [field: SerializeField] protected GameObject Sword { get; set; }
        [field: SerializeField] protected GameObject Handle { get; set; }
        [field: SerializeField] protected GameObject Shield { get; set; }

        protected override void Start()
        {
            base.Start();

            // Check if we have the palette and movement data before coloring
            if (enemyColorPalette != null)
            {
                OriginalColor = enemyColorPalette.MainColorD;
                GetComponent<SpriteRenderer>().color = OriginalColor;

                // Only try to color equipment if movement data exists
                if (EnemyData != null && EnemyData.movement != null)
                {
                    var movementColor = GetColorBasedOnMovement();
                    if (Armor != null) Armor.GetComponent<SpriteRenderer>().color = movementColor;
                    
                    if (Sword != null && Sword.activeSelf)
                    {
                        Sword.GetComponent<SpriteRenderer>().color = movementColor;
                        Handle.GetComponent<SpriteRenderer>().color = movementColor;
                    }
                    else if (Shield != null && Shield.activeSelf)
                    {
                        Shield.GetComponent<SpriteRenderer>().color = movementColor;
                    }
                }
            }
        }

        public override void LoadEnemyData(EnemySO enemyData, int questId)
        {
            // 1. Run base class logic FIRST to ensure AI Reboot happens
            base.LoadEnemyData(enemyData, questId);

            // 2. Resolve the Health Value
            int initialHealth = (enemyData is TopdownEnemySO topdownData) ? topdownData.health : (int)enemyData.status1;

            if (TryGetComponent(out HealthController h))
            {
                h.SetHealth(initialHealth);
                if (enemyColorPalette != null) h.SetOriginalColor(enemyColorPalette.MainColorD);
            }

            // 3. Equipment Logic with strict Null Checks
            if (enemyData != null && enemyData.weapon != null)
            {
                string weaponName = enemyData.weapon.name;
                if (weaponName == "Sword" && Sword != null) Sword.SetActive(true);
                if (weaponName == "Shield" && Shield != null) Shield.SetActive(true);
            }
        }

        protected override void StartDeath()
        {
            base.StartDeath();
            
            if (GameManagerSingleton.Instance != null && GameManagerSingleton.Instance.arenaMode)
            {
                // Send the "amountOfKills += 1" message
                Object.FindAnyObjectByType<ArenaManager>()?.RegisterKill();
            }

            if (Sword.activeSelf)
            {
                Sword.GetComponent<Animator>().SetTrigger(DieTrigger);
            }

            if (Shield.activeSelf)
            {
                Shield.GetComponent<Animator>().SetTrigger(DieTrigger);
            }
        }
    }
}