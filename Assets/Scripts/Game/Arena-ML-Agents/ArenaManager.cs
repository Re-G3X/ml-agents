        using UnityEngine;
        using System.Collections.Generic;
        using Game.GameManager;
        using Game.Events;
        using Game.GameManager.Player;
        using ScriptableObjects;
        using Game.LevelManager.DungeonManager;

        public class ArenaManager : MonoBehaviour 
        {
            [Header("Episode Control")]
            [Tooltip("If true, reaching the exit door ends the episode and resets the arena.")]
            public bool endEpisodeOnExit = true;
            [Tooltip("If true, defeating all enemies ends the episode and resets the arena.")]
            public bool endEpisodeByEliminatingEnemies = true;
            [Header("Room Reference")]
            public RoomBhv roomBhv;
            [Header("Training Entities")]
            public List<EnemyController> trainingEnemies = new List<EnemyController>();
            public List<TopdownEnemySO> trainingEnemyData = new List<TopdownEnemySO>(); 
            
            [Header("Treasure Settings")]
            [Tooltip("Assets/Prefabs/ML-Agents/Treasure.prefab.")]
            [SerializeField] private GameObject treasurePrefab;   // insert treasure prefab here
            [SerializeField] private int numberOfTreasures = 3;    // how many treasures will be spawn
            [HideInInspector] public List<GameObject> treasures = new List<GameObject>(); // hidden because its populated in runtime
            
            [Header("Spawn Settings")]
            public Vector2 playerSpawnPos = new Vector2(10.68f, 2.65f);
            
            private PlayerController _playerController;
            private HealthController _playerHealth;
            private int _remainingEnemies = 0;
            
            // FIX 1: Use the new class name to avoid conflict with Dialogue Agent
            private PlayerMLAgent _playerAgent; 

            void OnEnable()
            {
                HealthController.PlayerIsDamagedEventHandler += OnPlayerDamaged;
            }

            void OnDisable()
            {
                HealthController.PlayerIsDamagedEventHandler -= OnPlayerDamaged;
            }

            void Start() 
            {
                if (GameManagerSingleton.Instance != null)
                    GameManagerSingleton.Instance.arenaMode = true;

                // 1. find the player object first
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                
                if (playerObj != null)
                {
                    // 2. grab the components from it
                    _playerController = playerObj.GetComponent<PlayerController>();
                    _playerHealth = playerObj.GetComponent<HealthController>();
                    
                    // FIX 2: Grabbing the updated ML component name
                    _playerAgent = playerObj.GetComponent<PlayerMLAgent>();
                }

                CleanupMainGameSystems();
                InitializeArena();

                // Create treasure pool
                foreach (Transform child in transform)
                {
                    if (child.CompareTag("Treasure")) Destroy(child.gameObject);
                }
                treasures.Clear();

                for (int i = 0; i < numberOfTreasures; i++)
                {
                    GameObject t = Instantiate(treasurePrefab, transform);
                    t.name = $"Treasure_{i}";
                    treasures.Add(t);
                }

                // Initial random placement
                ScatterTreasures();
            }

            // New helper method
            private void ScatterTreasures()
            {
                if (roomBhv == null || roomBhv.spawnPoints.Count == 0) return;

                foreach (var treasure in treasures)
                {
                    if (treasure == null) continue;
                    int idx = Random.Range(0, roomBhv.spawnPoints.Count);
                    treasure.transform.position = roomBhv.spawnPoints[idx];
                    treasure.SetActive(true);
                }
            }

            private void CleanupMainGameSystems()
            {
                var questController = FindFirstObjectByType<Game.Quests.QuestController>();
                if (questController != null) questController.enabled = false;

                GameObject canvas = GameObject.Find("DungeonCanvas");
                if (canvas != null) Destroy(canvas);

                foreach (var dc in FindObjectsByType<Game.DataCollection.DungeonDataController>(FindObjectsSortMode.None)) dc.enabled = false;
                foreach (var pd in FindObjectsByType<Game.DataCollection.PlayerDataController>(FindObjectsSortMode.None)) pd.enabled = false;
            }

            void InitializeArena()
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); 

                for (int i = 0; i < trainingEnemies.Count; i++)
                {
                    var enemy = trainingEnemies[i];
                    if (enemy == null) continue;

                    if (playerObj != null) enemy.target = playerObj.transform;

                    if (i < trainingEnemyData.Count)
                    {
                        enemy.LoadEnemyData(trainingEnemyData[i], 1);
                        enemy.enabled = false;
                        enemy.enabled = true;
                    }

                    _remainingEnemies++;
                }
                ForcePositions();
            }

            public void RegisterKill()
            {
                _remainingEnemies--;
                
                // Give a positive reward for the kill (value stored in PlayerMLAgent)
                _playerAgent?.RegisterKill();
                


                if (_remainingEnemies <= 0)
                {
                    if (endEpisodeByEliminatingEnemies)
                    {
                        _playerAgent?.EndEpisode();
                        ResetTrainingCycle();
                    }
                }
            }

            public void RegisterExitReached()
            {
                // Grant the exit reward
                _playerAgent?.RegisterExit();

                if (endEpisodeOnExit)
                {
                    _playerAgent?.EndEpisode();
                    ResetTrainingCycle();
                }
            }

            public void ResetTrainingCycle()
            {
                if (_playerController != null)
                {
                    _playerController.ResetHealth(); 
                    if (_playerHealth != null) _playerHealth.ResetHealth();
                }

                Transform playerTransform = _playerController != null ? _playerController.transform : null;

                for (int i = 0; i < trainingEnemies.Count; i++)
                {
                    var enemy = trainingEnemies[i];
                    if (enemy == null) continue;

                    enemy.gameObject.SetActive(true);

                    // Re-enable colliders that were disabled during death {enemyController's Die()}
                    var cols = enemy.GetComponentsInChildren<Collider2D>();
                    foreach (var c in cols) c.enabled = true;

                    if (playerTransform != null) enemy.target = playerTransform;
                    if (enemy.TryGetComponent(out HealthController eHealth)) eHealth.ResetHealth();
                    
                    if (enemy.TryGetComponent(out Animator anim))
                    {
                        anim.Rebind();
                        anim.Update(0f);
                    }
                    
                    enemy.enabled = true;
                    
                    if (i < trainingEnemyData.Count)
                    {
                        enemy.LoadEnemyData(trainingEnemyData[i], 1);
                    }
                }

                // scatters all treasures
                ScatterTreasures();

                _remainingEnemies = trainingEnemies.Count;
                ForcePositions();
            }

            private void ForcePositions()
            {
                // player spawn
                if (_playerController != null)
                {
                    _playerController.transform.position = playerSpawnPos;
                    if (_playerController.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
                }
                
                // enemy spawn
                foreach (var enemy in trainingEnemies)
                {
                    if (enemy != null)
                    {
                        if (roomBhv != null && roomBhv.spawnPoints.Count > 0)
                        {
                            int idx = Random.Range(0, roomBhv.spawnPoints.Count);
                            enemy.transform.position = roomBhv.spawnPoints[idx];
                        }
                        else
                        {
                            // No spawn points configured; leave the enemy where it is and log a warning.
                            Debug.LogWarning("No spawn points available in RoomBhv. Enemy position unchanged.");
                        }
                        if (enemy.TryGetComponent(out Rigidbody2D erb)) erb.linearVelocity = Vector2.zero;
                    }
                }

                // camera spawn
                if (Camera.main != null)
                {
                    Camera.main.transform.position = new Vector3(playerSpawnPos.x, playerSpawnPos.y, Camera.main.transform.position.z);
                }
            }

            private void OnPlayerDamaged(object sender, PlayerIsDamagedEventArgs e)
            {
                if (e.PlayerHealth <= 0)
                {
                    _playerAgent?.RegisterDeath(); // Give a negative reward for dying before resetting
                    _playerAgent?.EndEpisode();
                    ResetTrainingCycle();
                }
            }
        }