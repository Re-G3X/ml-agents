using UnityEngine;

public class TrainingSpeedBoost : MonoBehaviour
{
    [Tooltip("Makes unity process time faster to accelerate ml-agents training times.")]
    [SerializeField] private float timeScale = 5f;

    void Start()
    {
        Time.timeScale = timeScale;
        Application.targetFrameRate = 500; // to remove the default frame cap.
    }
}