using UnityEngine;
using Game.EnemyManager;
using ScriptableObjects;
using Game.GameManager;
using Game.Events;
using Game.GameManager.Player;

public class ArenaManager : MonoBehaviour 
{
    [Header("Dependencies")]
    public SkeletonController theSkeleton;
    public EnemySO trainingEnemyData; 
    public HealthController playerHealth;

    [Header("Spawn Settings")]
    public Vector2 playerSpawnPos = new Vector2(10.68f, 2.65f);
    public Vector2 enemySpawnPos = new Vector2(6.17f, 3.50f);

    [Header("Wave Settings")]
    public int totalEnemiesInWave = 1;
    private int _currentKills = 0;

    private PlayerController _playerController;

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

        if (theSkeleton == null) theSkeleton = FindFirstObjectByType<SkeletonController>();
        
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerController = playerObj.GetComponent<PlayerController>();
            playerHealth = playerObj.GetComponent<HealthController>();
        }

        CleanupMainGameSystems();
        ForcePositions();
        InitializeArena();
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
        if (theSkeleton != null && trainingEnemyData != null)
        {
            theSkeleton.LoadEnemyData(trainingEnemyData, 1);
            theSkeleton.StopAllCoroutines();
            theSkeleton.StartCoroutine("WalkAndWait"); 
        }
    }

    public void RegisterKill()
    {
        _currentKills++;
        Debug.Log($"[Arena] Enemy Down! Kills: {_currentKills}/{totalEnemiesInWave}");

        if (_currentKills >= totalEnemiesInWave)
        {
            Debug.Log("[Arena] Wave Cleared! Resetting...");
            ResetTrainingCycle();
        }
    }

    private void ForcePositions()
    {
        if (_playerController != null)
        {
            _playerController.transform.position = playerSpawnPos;
            // Stop physics momentum
            if (_playerController.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
        }

        if (theSkeleton != null)
        {
            theSkeleton.transform.position = enemySpawnPos;
            if (theSkeleton.TryGetComponent(out Rigidbody2D erb)) erb.linearVelocity = Vector2.zero;
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(playerSpawnPos.x, playerSpawnPos.y, Camera.main.transform.position.z);
        }
    }

    private void OnPlayerDamaged(object sender, PlayerIsDamagedEventArgs e)
    {
        if (e.PlayerHealth <= 0) ResetTrainingCycle();
    }

    public void ResetTrainingCycle()
    {
        _currentKills = 0;
        Time.timeScale = 1.0f;

        // RESET PLAYER
        if (_playerController != null)
        {
            _playerController.ResetHealth(); // Resets PlayerController state
            if (_playerController.TryGetComponent(out HealthController pHealth))
            {
                pHealth.ResetHealth(); // Resets the invincibility lock!
            }
        }

        // RESET SKELETON
        if (theSkeleton != null)
        {
            theSkeleton.gameObject.SetActive(true);
            
            if (theSkeleton.TryGetComponent(out HealthController eHealth))
            {
                eHealth.ResetHealth();
            }

            if (theSkeleton.TryGetComponent(out Animator anim))
            {
                anim.Rebind(); // THIS CLEARS THE DEATH ANIMATION STATE
                anim.Update(0f);
            }
            
            theSkeleton.enabled = true;
        }

        ForcePositions();
    }
}