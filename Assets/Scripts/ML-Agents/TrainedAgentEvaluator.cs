using System.Collections;
using System.Collections.Generic;
using System.IO;          // <-- Added for file writing
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Game.GameManager;

public class TrainedAgentEvaluator : MonoBehaviour
{
    [Header("Evaluation Setup")]
    public PlayerMLAgent agent;
    public int numberOfEpisodes = 100;

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

    // Per‑episode storage
    private List<int> perEpisodeKills = new List<int>();
    private List<int> perEpisodeTreasures = new List<int>();
    private List<int> perEpisodeDeaths = new List<int>();
    private List<int> perEpisodeSteps = new List<int>();

    private ArenaManager arenaManager;
    private string csvFilePath;          // <-- Path for the output CSV file

    void Start()
    {
        var behaviorParams = agent.GetComponent<BehaviorParameters>();
        behaviorParams.BehaviorType = BehaviorType.InferenceOnly;

        arenaManager = FindAnyObjectByType<ArenaManager>();

        if (arenaManager != null)
            arenaManager.OnEnemyKilled += () => episodeEnemyKills++;
        agent.OnTreasureCollected += () => episodeTreasures++;
        agent.OnAgentDeath += () => episodeEndedInDeath = true;
        agent.OnAgentExit += () => episodeEndedInExit = true;

        // Prepare a clean CSV file with only the header
        csvFilePath = Path.Combine(Application.persistentDataPath, "evaluation_metrics.csv");
        try
        {
            File.WriteAllText(csvFilePath, "Episode;Kills;Treasures;Deaths;Steps\n");
            Debug.Log($"The CSV file containing metrics will be saved to: {csvFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Could not create CSV file at {csvFilePath}: {e.Message}");
        }

        StartCoroutine(RunEvaluation());
    }

    private IEnumerator RunEvaluation()
    {
        for (int i = 0; i < numberOfEpisodes; i++)
        {
            Debug.Log($"Episode {i + 1}/{numberOfEpisodes}");   // clean progress log

            yield return new WaitUntil(() => arenaManager != null && arenaManager.isEpisodeActive);

            // Reset counters
            episodeEnemyKills = 0;
            episodeTreasures = 0;
            episodeSteps = 0;
            episodeEndedInDeath = false;
            episodeEndedInExit = false;

            yield return new WaitWhile(() => arenaManager != null && arenaManager.isEpisodeActive);

            episodeSteps = arenaManager.lastEpisodeSteps;

            // Accumulate totals
            enemiesKilledTotal += episodeEnemyKills;
            treasuresCollectedTotal += episodeTreasures;
            stepsTotal += episodeSteps;
            if (episodeEndedInExit)
                successfulExits++;
            else if (episodeEndedInDeath)
                deathsTotal++;

            // Store per‑episode values
            perEpisodeKills.Add(episodeEnemyKills);
            perEpisodeTreasures.Add(episodeTreasures);
            perEpisodeDeaths.Add(episodeEndedInDeath ? 1 : 0);
            perEpisodeSteps.Add(episodeSteps);
        }

        Debug.Log("Evaluation complete!");
        LogFinalMetrics();        // Console summary (a few lines, no stack trace clutter)
        LogPerEpisodeCSV();       // Writes the clean CSV data to disk
        LogStatistics();          // Appends statistics to the same file and logs file path
    }

    private void LogFinalMetrics()
    {
        float n = numberOfEpisodes;
        Debug.Log($"--- Metrics over {n} episodes ---");
        Debug.Log($"Total enemies killed: {enemiesKilledTotal}");
        Debug.Log($"Total treasures collected: {treasuresCollectedTotal}");
        Debug.Log($"Total deaths: {deathsTotal}");
        Debug.Log($"Total steps: {stepsTotal}");
        Debug.Log($"Successful exits: {successfulExits}/{numberOfEpisodes}");
        Debug.Log($"--------------------------------------------------");
        Debug.Log($"Averages (for quick reference):");
        Debug.Log($"Avg. enemies killed: {enemiesKilledTotal / n:F2}");
        Debug.Log($"Avg. treasures collected: {treasuresCollectedTotal / n:F2}");
        Debug.Log($"Avg. deaths: {deathsTotal / n:F2}");
        Debug.Log($"Avg. steps: {stepsTotal / n:F2}");
    }

    private void LogPerEpisodeCSV()
    {
        try
        {
            using (var writer = new StreamWriter(csvFilePath, append: true))
            {
                for (int i = 0; i < numberOfEpisodes; i++)
                {
                    writer.WriteLine($"{i + 1};{perEpisodeKills[i]};{perEpisodeTreasures[i]};{perEpisodeDeaths[i]};{perEpisodeSteps[i]}");
                }
            }
            Debug.Log($"Per‑episode data written to {csvFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write per‑episode CSV: {e.Message}");
        }
    }

    private void LogStatistics()
    {
        float n = numberOfEpisodes;

        // Helper function to compute sample standard deviation
        float StdDev(List<int> values, float mean)
        {
            float sumSquaredDeviations = 0f;
            foreach (int v in values)
            {
                float diff = v - mean;
                sumSquaredDeviations += diff * diff;
            }
            return Mathf.Sqrt(sumSquaredDeviations / (n - 1));   // sample SD
        }

        float meanKills     = (float)perEpisodeKills.Average();
        float meanTreasures = (float)perEpisodeTreasures.Average();
        float meanDeaths    = (float)perEpisodeDeaths.Average();
        float meanSteps     = (float)perEpisodeSteps.Average();

        float sdKills     = StdDev(perEpisodeKills,     meanKills);
        float sdTreasures = StdDev(perEpisodeTreasures, meanTreasures);
        float sdDeaths    = StdDev(perEpisodeDeaths,    meanDeaths);
        float sdSteps     = StdDev(perEpisodeSteps,     meanSteps);

        int exitCount = perEpisodeDeaths.Count(d => d == 0);
        float exitRate = exitCount / n * 100f;

        // Append statistics to the same CSV file
        try
        {
            using (var writer = new StreamWriter(csvFilePath, append: true))
            {
                writer.WriteLine();   // empty line before the statistics block
                writer.WriteLine("--- Per‑episode persona statistics (mean ± SD) ---");
                writer.WriteLine($"Enemies killed;{meanKills:F2} ± {sdKills:F2}");
                writer.WriteLine($"Treasures collected;{meanTreasures:F2} ± {sdTreasures:F2}");
                writer.WriteLine($"Deaths;{meanDeaths:F2} ± {sdDeaths:F2}");
                writer.WriteLine($"Steps;{meanSteps:F2} ± {sdSteps:F2}");
                writer.WriteLine($"Exit rate;{exitRate:F0} %");
            }
            Debug.Log($"Statistics appended to {csvFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write statistics: {e.Message}");
        }
    }
}