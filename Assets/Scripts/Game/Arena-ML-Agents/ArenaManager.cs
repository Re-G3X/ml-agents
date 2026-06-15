        using UnityEngine;
        using System.Collections.Generic;
        using Game.GameManager;
        using Game.Events;
        using Game.GameManager.Player;
        using ScriptableObjects;
        using Game.LevelManager.DungeonManager;
        using System.Collections;

        public class ArenaManager : MonoBehaviour 
        {
            [Header("Episode Control")]
            [Tooltip("If true, reaching the exit door ends the episode and resets the arena.")]
            public bool endEpisodeOnExit = true;
            [Tooltip("If true, defeating all enemies ends the episode and resets the arena.")]
            public bool endEpisodeByEliminatingEnemies = true;
            [Header("Room Reference")]
            [Tooltip("Insert the room, with the RoomBhv script, where the training will happen.")]
            public RoomBhv roomBhv;
            [Header("Training Entities")]
            [SerializeField] private List<EnemySpawnConfig> enemyTypes = new List<EnemySpawnConfig>();
            [SerializeField] private int minEnemies = 1;
            [SerializeField] private int maxEnemies = 5;

            // Populated at runtime, do not assign manually
            [HideInInspector] public List<EnemyController> trainingEnemies = new List<EnemyController>();
            [HideInInspector] public List<TopdownEnemySO> trainingEnemyData = new List<TopdownEnemySO>();
            
            [Header("Treasure Settings")]
            [Tooltip("Assets/Prefabs/ML-Agents/Treasure.prefab")]
            [SerializeField] private GameObject treasurePrefab;   // insert treasure prefab here
            [SerializeField] private int minTreasures = 1;
            [SerializeField] private int maxTreasures = 5;
            [HideInInspector] public List<GameObject> treasures = new List<GameObject>(); // hidden because its populated in runtime
            
            [Header("Spawn Settings")]
            public Vector2 playerSpawnPos = new Vector2(10.68f, 2.65f);
            [Tooltip("Fixed position for the main camera during arena episodes.")]
            public Vector2 cameraPosition = new Vector2(10.89f, 3.0f); 

            [System.Serializable]
            public struct EnemySpawnConfig
            {
                public GameObject prefab;
                public TopdownEnemySO data;
            }

            private PlayerController _playerController;
            private HealthController _playerHealth;
            private int _remainingEnemies = 0;
            private PlayerMLAgent _playerAgent; 
            private bool _isResetting = false;
            [HideInInspector] public bool isEpisodeActive = false; // for PersonaEvaluator.cs to calculate episodes
            public event System.Action OnEnemyKilled; // to warn personaevaluator.cs about enemies killed

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
                    
                    // 3. grab the updated ML component name
                    _playerAgent = playerObj.GetComponent<PlayerMLAgent>();
                }

                CleanupMainGameSystems();
                SpawnEnemies();

                // Random treasure spawn [old ScatterTreasures()]
                SpawnTreasures();

                isEpisodeActive = true; // episode is now active for PersonaEvaluator.cs
            }

            // randomized treasure population (destroys old treasures and creates new ones)
            private void SpawnTreasures()
            {
                // 1. Destroy old treasure instances
                foreach (var treasure in treasures)
                {
                    if (treasure != null) Destroy(treasure);
                }
                treasures.Clear();

                // 2. Random count
                int count = Random.Range(minTreasures, maxTreasures + 1);

                for (int i = 0; i < count; i++)
                {
                    // 3. Instantiate new treasure from prefab
                    GameObject t = Instantiate(treasurePrefab, transform);
                    t.name = $"Treasure_{i}";
                    treasures.Add(t);

                    // 4. Place at a random spawn point
                    if (roomBhv != null && roomBhv.spawnPoints.Count > 0)
                    {
                        int idx = Random.Range(0, roomBhv.spawnPoints.Count);
                        t.transform.position = roomBhv.spawnPoints[idx];
                    }
                    t.SetActive(true);
                }
            }

            // randomized enemy population
            private void SpawnEnemies()
            {
                // 1. Destroy old enemy instances
                foreach (var enemy in trainingEnemies)
                {
                    if (enemy != null) Destroy(enemy.gameObject);
                }
                trainingEnemies.Clear();
                trainingEnemyData.Clear();

                // 2. Find player reference
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

                // 3. Random count
                int count = Random.Range(minEnemies, maxEnemies + 1);

                for (int i = 0; i < count; i++)
                {
                    // Pick random enemy type
                    var config = enemyTypes[Random.Range(0, enemyTypes.Count)];

                    // Instantiate
                    GameObject instance = Instantiate(config.prefab, transform);
                    instance.name = config.data.name + "_" + i;

                    // Get controller
                    var controller = instance.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        // Set player reference
                        if (playerObj != null)
                        {
                            controller.target = playerObj.transform;
                            controller.SetPlayerObject(playerObj);
                        }

                        // Load data (this also initializes movement delegate)
                        controller.LoadEnemyData(config.data, 1);
                        controller.enabled = true;
                    }

                    trainingEnemies.Add(controller);
                    trainingEnemyData.Add(config.data);
                }

                _remainingEnemies = count;

                // 4. Randomize positions using existing spawn points
                ForcePositions();
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

            public void RegisterKill()
            {
                // ignore kills that happen during a reset cycle
                if (_isResetting) return;

                _remainingEnemies--;
                _playerAgent?.RegisterKill(); // give a positive reward for the kill (value stored in PlayerMLAgent)
                OnEnemyKilled?.Invoke(); // calls the OnEnemyKilled event to calculate metrics in personaevaluator.cs

                if (_remainingEnemies <= 0)
                {
                    if (endEpisodeByEliminatingEnemies)
                    {
                        StartCoroutine(EndEpisodeAndReset());
                    }
                }
            }

            public void RegisterExitReached()
            {
                if (endEpisodeOnExit)
                {
                    _playerAgent?.RegisterExit();   // reward is given ONLY when exit ends the episode
                    EndEpisodeAndReset();
                }
            }


            private IEnumerator EndEpisodeAndReset()
            {
                isEpisodeActive = false;
                _playerAgent?.EndEpisode();
                yield return null;
                ResetTrainingCycle();
            }

            public void ResetTrainingCycle()
            {
                isEpisodeActive = true; // for personaevaluator.cs
                _isResetting = true;   // <-- locks the reseting cycle process

                if (_playerController != null)
                {
                    _playerController.ResetHealth(); 
                    if (_playerHealth != null) _playerHealth.ResetHealth();
                }

                SpawnEnemies();
                SpawnTreasures();
                ForcePositions();

                _isResetting = false;  // <-- unlocks the reseting cycle process
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
                    Camera.main.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, Camera.main.transform.position.z);
                }
            }

            private void OnPlayerDamaged(object sender, PlayerIsDamagedEventArgs e)
            {
                if (e.PlayerHealth <= 0)
                {
                    _playerAgent?.RegisterDeath(); // Give a negative reward for dying before resetting
                    StartCoroutine(EndEpisodeAndReset());
                }
            }
        }