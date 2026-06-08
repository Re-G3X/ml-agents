using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Game.GameManager.Player;
using Game.GameManager;

// We rename this to PlayerMLAgent to avoid conflict with Fog.Dialogue.Agent
public class PlayerMLAgent : Unity.MLAgents.Agent 
{
    [Header("Reward Shaping (Experimental Variables)")]
    [Tooltip("Penalty applied every frame to encourage speed.")]
    [SerializeField] private float existencePenalty = -0.0005f;

    [Tooltip("Penalty applied when the agent dies.")]
    [SerializeField] private float deathPenalty = -1.0f;

    [Tooltip("Penalty applied when the agent takes damage.")]
    [SerializeField] private float damagePenalty = -0.1f;

    [Tooltip("Reward applied when an enemy is defeated.")]
    [SerializeField] private float killReward = 1.0f;

    [Tooltip("Reward applied when an enemy is hit. (Call RegisterHit() from projectile)")]
    [SerializeField] private float hitReward = 0.5f;

    [Tooltip("Reward for reaching the exit door.")]
    [SerializeField] private float exitReward = 1.0f;

    [Tooltip("Reward applied when collecting a treasure.")]
    [SerializeField] private float treasureReward = 1f;

    [Tooltip("Reward applied for staying close to the enemy to prevent cowardice.")]
    [SerializeField] private float proximityBonus = 0.001f;

    private PlayerMovement _movement;
    private PlayerShot _shot;
    private HealthController _health;
    private ArenaManager _arena;
    private bool _shootPressedLastFrame = false; // for the new shooting system

    public override void Initialize()
    {
        _movement = GetComponent<PlayerMovement>();
        _shot = GetComponent<PlayerShot>();
        _health = GetComponent<HealthController>();
        _arena = Object.FindFirstObjectByType<ArenaManager>();

        if (_health != null)
        {
            _health.OnDamageTaken += ScoldAgent;
        }
    }

    private void ScoldAgent(float damage)
    {
        // Now uses the inspector variable
        AddReward(damagePenalty);
    }

    public void RegisterKill()
    {
        AddReward(killReward);
    }

    public void RegisterDeath() { AddReward(deathPenalty); }

    public void RegisterHit() { AddReward(hitReward); }

    public override void OnEpisodeBegin()
    {
        // ArenaManager handles the physical reset
    }

    public void RegisterExit() { AddReward(exitReward); }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_health == null || _arena == null) 
        {
            for (int i = 0; i < 6; i++) { sensor.AddObservation(0f); }
            return;
        }

        // 1. Health Ratio (1 float)
        sensor.AddObservation((float)_health.GetHealth() / _health.GetMaxHealth());

        // 2. Position (Vector2 = 2 floats)
        sensor.AddObservation((Vector2)transform.localPosition); 

        // 3. Enemy Data (3 floats total)
        if (_arena.trainingEnemies != null && _arena.trainingEnemies.Count > 0 && _arena.trainingEnemies[0] != null)
        {
            Transform enemy = _arena.trainingEnemies[0].transform;
            Vector2 toEnemy = (enemy.position - transform.position).normalized;
            
            sensor.AddObservation(toEnemy); // 2 floats
            sensor.AddObservation(Vector2.Distance(transform.position, enemy.position)); // 1 float
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Movement
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        _movement.ApplyMovement(new Vector2(moveX, moveY));

        // Shooting
        int shootAction = actions.DiscreteActions[0];
        if (shootAction > 0)
        {
            Vector2 shootDir = Vector2.zero;
            switch (shootAction)
            {
                case 1: shootDir = Vector2.up; break;
                case 2: shootDir = Vector2.down; break;
                case 3: shootDir = Vector2.left; break;
                case 4: shootDir = Vector2.right; break;
            }
            _shot.ApplyShoot(true, shootDir);
        }
        else
        {
            _shot.ApplyShoot(false, Vector2.zero);   // stops firing
        }

        // Existence Penalty
        AddReward(existencePenalty);

        // Proximity Bonus
        if (_arena != null && _arena.trainingEnemies.Count > 0 && _arena.trainingEnemies[0] != null)
        {
            float dist = Vector2.Distance(transform.position, _arena.trainingEnemies[0].transform.position);
            if (dist < 5f) 
            {
                AddReward(proximityBonus);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // --- Movement ---
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");

        // --- Shooting (hold to auto‑fire, release to stop) ---
        var discreteActions = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.UpArrow)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.DownArrow)) discreteActions[0] = 2;
        else if (Input.GetKey(KeyCode.LeftArrow)) discreteActions[0] = 3;
        else if (Input.GetKey(KeyCode.RightArrow)) discreteActions[0] = 4;
        else discreteActions[0] = 0;
    }

    public void RegisterTreasureCollect()
    {
        AddReward(treasureReward);
    }
}