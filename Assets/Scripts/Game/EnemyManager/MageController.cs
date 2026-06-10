using Game.GameManager;
using ScriptableObjects;
using System.Collections;
using UnityEngine;

namespace Game.EnemyManager
{
    public class MageController : EnemyController
    {
        [field: SerializeField] private ProjectileTypeSO ProjectileType { get; set; }
        [field: SerializeField] private float ProjectileSpeed { get; set; }
        [field: SerializeField] private GameObject ProjectilePrefab { get; set; }
        [field: SerializeField] protected float CooldownTime { get; set; }
        [field: SerializeField] private GameObject ProjectileSpawn { get; set; }
        [field: SerializeField] private GameObject HeadObject { get; set; }
        [field: SerializeField] private GameObject Eyes { get; set; }
        [field: SerializeField] private GameObject Hands { get; set; }
        [field: SerializeField] private float WaitForStartTimer { get; set; }

        private Coroutine _attackRoutine;

        protected override void Awake()
        {
            base.Awake();
            WaitForStartTimer = 0.5f;
        }
        protected override void Start()
        {
            base.Start();
            HeadObject.GetComponent<SpriteRenderer>().color = GetColorBasedOnMovement();
            _attackRoutine = StartCoroutine(BeginAttackRoutine());
        }

        protected override void StartDeath()
        {
            base.StartDeath();
            Hands.SetActive(false);
            StopCoroutine(_attackRoutine);
        }

        private IEnumerator BeginAttackRoutine()
        {
            yield return new WaitForSeconds(WaitForStartTimer);
            // Safety: wait until PlayerObj is assigned
            while (PlayerObj == null)
                yield return null;
            yield return StartCoroutine(UseSkill());
        }

        protected virtual IEnumerator UseSkill()
        {
            while (true)
            {
                var playerPosition = PlayerObj.transform.position;
                var thisPosition = transform.position;
                var target = new Vector2(playerPosition.x - thisPosition.x, playerPosition.y - thisPosition.y);
                target.Normalize();
                target *= ProjectileSpeed;

                var bullet = Instantiate(ProjectilePrefab, ProjectileSpawn.transform.position, ProjectileSpawn.transform.rotation);
                if (ProjectilePrefab.name == "EnemyBomb")
                {
                    var bombController = bullet.GetComponent<BombController>();
                    bombController.ShootDirection = target;
                    if (EnemyData is TopdownEnemySO ed)
                        bombController.Damage = ed.damage;
                    bombController.EnemyThatShot = IndexOnEnemyList;
                }
                else
                {
                    var projectileController = bullet.GetComponent<ProjectileController>();
                    projectileController.SetEnemyThatShot(IndexOnEnemyList);
                    projectileController.ProjectileSo = ProjectileType;
                    projectileController.Shoot(target);
                }
                yield return new WaitForSeconds(CooldownTime);
            }
        }

        public override void LoadEnemyData(EnemySO enemyData, int questId)
        {
            base.LoadEnemyData(enemyData, questId);

            // Apply color only if HeadObject exists
            if (HeadObject != null)
            {
                HeadObject.GetComponent<SpriteRenderer>().color = GetColorBasedOnMovement();
            }

            // Assign projectile data ~first~
            ProjectilePrefab = enemyData.weapon.Projectile.projectilePrefab;
            ProjectileType = enemyData.weapon.Projectile;

            if (ProjectilePrefab != null)
            {
                if (ProjectilePrefab.name == "EnemyBomb")
                {
                    // Calculate bomb cooldown without modifying the asset 
                    //  (which was being slowed down for whatever reasons)
                    float bombAttackSpeed = (EnemyData is TopdownEnemySO edd) ? edd.attackSpeed : 1.0f;
                    // Bombs fire at half the rate (original design intent)
                    float adjustedSpeed = bombAttackSpeed / 2.0f;
                    CooldownTime = (adjustedSpeed > 0) ? 1.0f / adjustedSpeed : 0.5f;

                    SetColors(enemyColorPalette.MainColorA, enemyColorPalette.DetailColorA);
                }
                else
                {
                    // Non‑bomb projectiles use the attack speed directly
                    if (EnemyData is TopdownEnemySO ed)
                    {
                        CooldownTime = (ed.attackSpeed > 0) ? 1.0f / ed.attackSpeed : 0.5f;
                        ProjectileSpeed = ed.projectileSpeed * 4;
                    }
                    SetColors(enemyColorPalette.MainColorB, enemyColorPalette.DetailColorB);
                }
            }
            else
            {
                SetColors(enemyColorPalette.MainColorC, enemyColorPalette.DetailColorC);
            }

            // Stop and restart attack routine ~secondly~ after all data is set
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
            }
            _attackRoutine = StartCoroutine(BeginAttackRoutine());
        }

        private void SetColors(Color mainColor, Color detailColor)
        {
            OriginalColor = mainColor;
            GetComponent<SpriteRenderer>().color = OriginalColor;
            Eyes.GetComponent<SpriteRenderer>().color = detailColor;
        }
    }
}