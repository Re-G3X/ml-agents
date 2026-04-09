using UnityEngine;
using Game.EnemyManager; // This allows the script to see SkeletonController
using ScriptableObjects;   // This allows the script to see EnemySO
// If EnemyController is in a different place:
using PlatformGame.Enemy; 

public class ArenaManager : MonoBehaviour 
{
    public SkeletonController theSkeleton;
    public EnemySO trainerStats; 

    void Start() 
    {
        // If you are spawning the skeleton, make sure this runs AFTER spawn
        if (theSkeleton == null) {
            theSkeleton = FindObjectOfType<SkeletonController>();
        }

        if (theSkeleton != null && trainerStats != null)
        {
            // LOG FOR CERTAINTY:
            Debug.Log($"Arena: Sending {trainerStats.name} to {theSkeleton.name}");
            
            theSkeleton.LoadEnemyData(trainerStats, 999); 
        }
        else
        {
            Debug.LogError("ArenaManager: I have no Skeleton or no SO assigned in the Inspector!");
        }
    }
}