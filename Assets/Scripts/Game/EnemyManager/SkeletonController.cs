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
            // 1. Force the assignment here so the Skeleton definitely has the reference
            this.EnemyData = enemyData; 

            // 2. Run the base logic (movement, health setup, etc.)
            base.LoadEnemyData(enemyData, questId);

            // 3. Safety Check: If the SO or Weapon is missing, stop before crashing
            if (EnemyData == null || EnemyData.weapon == null)
            {
                Debug.LogError($"Skeleton AI: EnemyData or Weapon is null on {gameObject.name}!");
                return;
            }

            // 4. Now the switch is safe
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

        protected override void StartDeath()
        {
            base.StartDeath();
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