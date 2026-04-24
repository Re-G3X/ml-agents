using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Game.GameManager.Player;
using Game.GameManager;

// We rename this to PlayerMLAgent to avoid conflict with Fog.Dialogue.Agent
public class PlayerMLAgent : Unity.MLAgents.Agent 
{
    private PlayerMovement _movement;
    private PlayerShot _shot;
    private HealthController _health;
    private ArenaManager _arena;

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
        AddReward(-0.1f);
    }

    public override void OnEpisodeBegin()
    {
        // ArenaManager handles the physical reset
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // If essential components are missing, we MUST still add 6 observations 
        // to prevent the "Observation Size Mismatch" crash.
        if (_health == null || _arena == null) 
        {
            for (int i = 0; i < 6; i++)
            {
                sensor.AddObservation(0f);
            }
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
            // Must add exactly 3 floats to keep the total at 6
            sensor.AddObservation(Vector2.zero); // Adds 2 floats (0,0)
            sensor.AddObservation(0f);           // Adds 1 float
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        _movement.ApplyMovement(new Vector2(moveX, moveY));

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

        AddReward(-0.0005f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");

        var discreteActions = actionsOut.DiscreteActions;
        // Basic check for shooting in heuristic
        if (Input.GetKey(KeyCode.UpArrow)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.DownArrow)) discreteActions[0] = 2;
        else if (Input.GetKey(KeyCode.LeftArrow)) discreteActions[0] = 3;
        else if (Input.GetKey(KeyCode.RightArrow)) discreteActions[0] = 4;
        else discreteActions[0] = 0;
    }
}