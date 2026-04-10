using UnityEngine;
using Game.EnemyManager;
using ScriptableObjects;
using Game.GameManager; // Added to see HealthController/Player
using Game.Events;      // Added to see the Damage Events

public class ArenaManager : MonoBehaviour 
{
    public SkeletonController theSkeleton;
    public EnemySO trainerStats; 
    public HealthController playerHealth; // Assign the Player's HealthController here
    [Header("Training Data")]
    public ScriptableObjects.EnemySO trainingEnemyData; // Drag 'StandardSword' here
    
    void OnEnable()
    {
        // Subscribe to the damage event to monitor player life
        HealthController.PlayerIsDamagedEventHandler += OnPlayerDamaged;
    }

    void OnDisable()
    {
        HealthController.PlayerIsDamagedEventHandler -= OnPlayerDamaged;
    }

    void Start() 
    {
        if (GameManagerSingleton.Instance != null)
        {
            GameManagerSingleton.Instance.arenaMode = true;
        }
        // SURGICAL KILL: Disable Quest tracking to prevent the NullReference crash on enemy death
        var questController = FindObjectOfType<Game.Quests.QuestController>();
        if (questController != null)
        {
            questController.enabled = false;
            Debug.Log("[Arena] QuestController disabled to prevent Kill-Quest crash.");
        }

        // 2. DESTROY THE CANVAS (Keep this, it's working!)
        GameObject canvas = GameObject.Find("DungeonCanvas");
        if (canvas != null)
        {
            Destroy(canvas);
        }

        // 3. SURGICAL KILL: Disable the other controllers
        foreach (var dc in FindObjectsOfType<Game.DataCollection.DungeonDataController>()) dc.enabled = false;
        foreach (var pd in FindObjectsOfType<Game.DataCollection.PlayerDataController>()) pd.enabled = false;

        // Standard initialization
        if (theSkeleton == null) theSkeleton = FindObjectOfType<SkeletonController>();
        if (playerHealth == null) playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthController>();

        InitializeArena();
    }

    void InitializeArena()
    {
        if (theSkeleton != null && trainingEnemyData != null)
        {
            // 1. Manually trigger the data load that the Generator usually does
            theSkeleton.LoadEnemyData(trainingEnemyData, 1);
            
            // 2. FORCE THE BRAIN TO RESTART
            // Since we bypassed the normal start-up, the AI might be "asleep"
            theSkeleton.StopAllCoroutines();
            
            // We use the exact Coroutine name from your previous logs
            theSkeleton.StartCoroutine("WalkAndWait"); 
            
            Debug.Log($"[Arena] Injected {trainingEnemyData.name} into Skeleton and jump-started AI.");
        }
    }

    private void OnPlayerDamaged(object sender, PlayerIsDamagedEventArgs e)
    {
        // Check if player just reached 0 or less HP
        if (e.PlayerHealth <= 0)
        {
            Debug.Log("[Arena] Player has died! Resetting for training...");
            ResetTrainingCycle();
        }
    }

    private void ResetTrainingCycle()
    {
        // 1. USE THE PLAYER'S OWN RESET LOGIC
        // This calls the ResetHealth() in the script you just showed me!
        var playerController = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Game.GameManager.Player.PlayerController>();
        if (playerController != null)
        {
            playerController.ResetHealth();
            
            // 2. RE-ENABLE THE COLLIDER
            // CheckDeath turns this off; we MUST turn it back on.
            var collider = playerController.GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;
        }

        // 3. UNFREEZE TIME
        Time.timeScale = 1.0f;

        // 4. BRING BACK THE SKELETON
        if (theSkeleton != null)
        {
            theSkeleton.gameObject.SetActive(true);
            theSkeleton.enabled = true;
            
            // Reset position so he doesn't just stand on top of the player
            theSkeleton.transform.position = new Vector3(2, 0, 0); 
        }

        Debug.Log("[Arena] Reset complete using PlayerController.ResetHealth().");
    }
}