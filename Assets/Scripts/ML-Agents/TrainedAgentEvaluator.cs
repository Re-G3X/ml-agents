using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Game.GameManager;

public class TrainedAgentEvaluator : MonoBehaviour
{
    [Header("Evaluation Setup")]
    [Tooltip("Drag the Player object containing the PlayerMLAgent.cs from the arena here.")]
    public PlayerMLAgent agent;
    [Tooltip("Define the amount of episodes will be run to evaluate the trained agent's performance.")]
    public int numberOfEpisodes = 20;

    [Header("Metrics (Calculated During Run)")]
    public int enemiesKilledTotal = 0;
    public int treasuresCollectedTotal = 0;
    public int deathsTotal = 0;
    public int stepsTotal = 0;
    public int successfulExits = 0;

    // Per‑episode counters
    private int episodeEnemyKills, episodeTreasures, episodeSteps;
    private bool episodeEndedInDeath;
    private bool episodeEndedInExit;   
    private ArenaManager arenaManager;

    void Start()
    {
        // Force the agent to use a trained .onnx model (no learning)
        var behaviorParams = agent.GetComponent<BehaviorParameters>();
        behaviorParams.BehaviorType = BehaviorType.InferenceOnly;

        // Finds references
        arenaManager = FindAnyObjectByType<ArenaManager>();

        // Subscribe to events
        if (arenaManager != null) 
        {
            arenaManager.OnEnemyKilled += () => episodeEnemyKills++;
        }
        agent.OnTreasureCollected += () => episodeTreasures++;
        agent.OnAgentDeath += () => episodeEndedInDeath = true;
        agent.OnAgentExit += () => episodeEndedInExit = true;

        // Start the evaluation loop
        StartCoroutine(RunEvaluation());
    }

    void Update()
    {
        // Count steps only while an episode is active
        if (arenaManager != null && arenaManager.isEpisodeActive)
        {
            episodeSteps++;
        }
    }

    private IEnumerator RunEvaluation()
    {
        for (int i = 0; i < numberOfEpisodes; i++)
        {
            Debug.Log($"Episode {i + 1}/{numberOfEpisodes}");

            // Wait until the arena signals that a new episode is active.
            yield return new WaitUntil(() => arenaManager != null && arenaManager.isEpisodeActive);

            // Reset per‑episode counters at the beginning of the episode.
            episodeEnemyKills = 0;
            episodeTreasures = 0;
            episodeSteps = 0;
            episodeEndedInDeath = false;
            episodeEndedInExit = false;

            // wait until the episode ends (flag becomes false)
            yield return new WaitWhile(() => arenaManager != null && arenaManager.isEpisodeActive);

            // Accumulate totals.
            enemiesKilledTotal += episodeEnemyKills;
            treasuresCollectedTotal += episodeTreasures;
            stepsTotal += episodeSteps;
            if (episodeEndedInExit)
                successfulExits++;
            else if (episodeEndedInDeath)
                deathsTotal++;
        }

        Debug.Log("Evaluation complete!");
        LogFinalMetrics();
    }

    private void LogFinalMetrics()
    {
        float n = numberOfEpisodes;
        Debug.Log($"--- Metrics over {n} episodes ---");
        Debug.Log($"Avg. enemies killed: {enemiesKilledTotal / n:F2}");
        Debug.Log($"Avg. treasures collected: {treasuresCollectedTotal / n:F2}");
        Debug.Log($"Avg. deaths: {deathsTotal / n:F2}");
        Debug.Log($"Avg. steps: {stepsTotal / n:F2}");
        Debug.Log($"Successful exits: {successfulExits}/{numberOfEpisodes}");
    }
}