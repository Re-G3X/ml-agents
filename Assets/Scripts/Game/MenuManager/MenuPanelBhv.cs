using UnityEngine;
using System.Collections;
using Game.GameManager;

namespace Game.MenuManager
{
    public class MenuPanelBhv : MonoBehaviour, IMenuPanel
    {
        [SerializeField] protected GameObject previousPanel, nextPanel;

        protected virtual void OnEnable()
        {
            // 1. If we are in Arena/Training, hide visuals INSTANTLY 
            // even if we aren't sure about the GameManager yet.
            if (Application.isPlaying)
            {
                // Try to find the GameManager. If it's already there and in Arena mode, kill now.
                if (GameManagerSingleton.Instance != null && 
                   (GameManagerSingleton.Instance.IsTraining || GameManagerSingleton.Instance.arenaMode))
                {
                    HideAndBypass();
                }
                else
                {
                    // 2. If GameManager isn't ready, hide just in case and start a "waiter"
                    StartCoroutine(WaitAndBypass());
                }
            }
        }

        private IEnumerator WaitAndBypass()
        {
            // While the GameManager is null, we stay hidden but wait
            while (GameManagerSingleton.Instance == null)
            {
                yield return null; 
            }

            // Now that GameManager exists, check if we should kill this panel
            if (GameManagerSingleton.Instance.IsTraining || GameManagerSingleton.Instance.arenaMode)
            {
                HideAndBypass();
            }
        }

        private void HideAndBypass()
        {
            // Move it away and hide it so it doesn't flicker or block raycasts
            transform.localPosition = new Vector3(-10000, 0, 0);
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0;

            Debug.Log($"[Arena-Final-Kill] Bypassing UI: {gameObject.name}");
            
            if (nextPanel != null) nextPanel.SetActive(true);
            gameObject.SetActive(false);
        }

        public void GoToNext()
        {
            if (nextPanel != null) nextPanel.SetActive(true);
            gameObject.SetActive(false);
        }

        public void GoToPrevious()
        {
            if (previousPanel != null) previousPanel.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}