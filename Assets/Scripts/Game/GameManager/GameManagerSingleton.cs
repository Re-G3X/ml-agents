using Game.Audio;
using Game.SaveLoadSystem;
using MyBox;
using ScriptableObjects;
using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using Game.Events;
using Util;

namespace Game.GameManager
{
    public class GameManagerSingleton : MonoBehaviour, ISoundEmitter
    {
        public bool IsTraining => SceneManager.GetActiveScene().name == "ML-Agents-Env"; // Update this to match the training scene name for consistency
        
        public bool IsInPortuguese = false;
        public Enums.GameType GameType;
        public static GameManagerSingleton Instance { get; private set; }
        [field: SerializeField] public ProjectileTypeSO playerProjectile { get; set; }

        public bool IsLastQuestLine { get; set; }
        [field: SerializeField] private SceneReference experimentSelectorScreen;

        public static event EventHandler GameStartEventHandler;
        public static event Action LoadStateHandler;
        public static event FormAnsweredEvent PreTestFormQuestionAnsweredEventHandler;
        private bool _hasLoaded;

        [Header("Arena Mode (for ml-agents training)")]
        [Tooltip("When enabled, activates the simplified arena environment for ML-Agents training. " +
        "This bypasses the main game's quests, dialogue, data collection, and room transitions, " +
        "and enables the arena-specific reward and reset logic.")]
        public bool arenaMode;

        private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            // Add the training scene here if music has to be played while training
            // (leave it out if silence is preferred for better performance.
            if (scene.name == "Main" || scene.name == "ContentGenerator" || scene.name == "PlatformMain" || scene.name == "ML-Agents-Env")
            {
                ((ISoundEmitter)this).OnSoundEmitted(this, new PlayBgmEventArgs(AudioManager.BgmTracks.MainMenuTheme));
            }

            if (scene.name == "ExperimentLevelSelector")
            {
                if (!_hasLoaded)
                {
                    _hasLoaded = true;
                }
            }
        }

        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GameStartEventHandler?.Invoke(null, EventArgs.Empty);
        }

        private void OnApplicationQuit()
        {
            AnalyticsEvent.GameOver();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }
        
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        }

        public void MainMenu()
        {
            // Safety Check: Reset arenaMode if manually returning to Main
            // arenaMode = false; 
            
            GameStartEventHandler?.Invoke(null, EventArgs.Empty);
            SceneManager.LoadScene("Main");
        }
    }
}