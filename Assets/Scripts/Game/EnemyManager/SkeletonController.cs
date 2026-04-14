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
            // 1. Assign the data reference
            this.EnemyData = enemyData; 

            // 2. Resolve the Health Value
            int initialHealth = 1; // Fallback safety

            if (enemyData is TopdownEnemySO topdownData)
            {
                // If it's already a TopdownSO, use the explicit health field
                initialHealth = topdownData.health;
            }
            else
            {
                // Otherwise, use the status1 mapping confirmed by your conversion script
                initialHealth = (int)enemyData.status1;
            }

            // 3. Force the HealthController to capture this as MAX and CURRENT
            if (TryGetComponent(out HealthController h))
            {
                h.SetHealth(initialHealth);
                
                if (enemyColorPalette != null) 
                    h.SetOriginalColor(enemyColorPalette.MainColorD);
            }

            // 4. Run base class logic
            base.LoadEnemyData(enemyData, questId);

            // 5. Equipment Logic (unchanged)
            if (EnemyData != null && EnemyData.weapon != null)
            {
                switch (EnemyData.weapon.name)
                {
                    case "Sword":
                        if (Sword != null) Sword.SetActive(true);
                        break;
                    case "Shield":
                        if (Shield != null) Shield.SetActive(true);
                        break;
                }
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