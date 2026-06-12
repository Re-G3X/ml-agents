using Game.Events;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.GameManager;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Game.DataCollection
{
    public class FormBhv : MonoBehaviour
    {
        [Header("Form Settings")]
        [SerializeField] private bool _hasCheckbox = false;
        public FormQuestionsData enQuestionsData;
        public FormQuestionsData ptQuestionsData;
        public GameObject questionPrefab;
        public GameObject checkboxPrefab;
        public RectTransform questionsPanel;
        public RectTransform submitButton;
        public float extraQuestionsPanelHeight = 100;
        
        [Header("Form ID (0=Pre, 1=Post)")]
        public int formID; 

        private List<FormQuestionBhv> questions = new List<FormQuestionBhv>();
        private FormCheckboxBhv checkboxForm;
        private List<int> answers;

        private FormQuestionsData questionsData => GameManagerSingleton.Instance.IsInPortuguese ? ptQuestionsData : enQuestionsData;

        public static event FormAnsweredEvent PreTestFormQuestionAnsweredEventHandler;
        public static event FormAnsweredEvent PostTestFormQuestionAnsweredEventHandler;

        void Start()
        {
            // 1. Make the UI invisible immediately
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) 
            {
                cg.alpha = 0; 
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            else 
            {
                transform.position = new Vector3(-10000, -10000, 0);
            }

            // 2. Start the automated sequence
            StartCoroutine(GhostFlow());
        }

        IEnumerator GhostFlow()
        {
            yield return new WaitForSeconds(0.2f);
            
            // 1. Setup the dummy data
            InstantiateForms();
            foreach (var q in questions)
            {
                if (q.questionData != null) q.questionData.answer = 1; 
            }

            // 2. Surgical Shutdown for Arena Mode
            if (GameManagerSingleton.Instance != null && GameManagerSingleton.Instance.arenaMode)
            {
                //Debug.Log("Arena Mode: Surgical shutdown of generators.");
                MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
                foreach (var script in allScripts)
                {
                    if (script == null) continue;

                    string typeName = script.GetType().Name;
                    
                    // TARGETS: These are heavy procedural scripts that causes lag
                    bool isGenerator = typeName.Contains("GeneratorManager") || 
                                       typeName.Contains("PcgController") || 
                                       typeName.Contains("QuestGenerator");

                    // PROTECTED: Do NOT disable these or the game breaks/silences
                    bool isEssential = typeName.Contains("ExperimentController") || 
                                       typeName.Contains("Audio") || 
                                       typeName.Contains("GameManager") ||
                                       typeName.Contains("Singleton") ||
                                       typeName.Contains("DungeonLoader");
                    
                    if (isGenerator && !isEssential)
                    {
                        script.StopAllCoroutines();
                        script.enabled = false; 
                        //Debug.Log($"Surgical Kill (Script Only): {typeName}");

                        // Only deactivate the object if it's a dedicated procedural holder
                        // Avoid deactivating general "Manager" objects that might contain combat logic
                        if (typeName == "GeneratorManager" || typeName == "PcgController") 
                        {
                            // Optional: Only do this if it has no other essential scripts
                            script.gameObject.SetActive(false);
                            //Debug.Log($"Object Shutdown: {script.gameObject.name}");
                        }
                    }
                }
            }

            // 3. Submit and Transition
            Submit(); 
            
            yield return new WaitForSeconds(0.1f);

            if (GameManagerSingleton.Instance != null && GameManagerSingleton.Instance.arenaMode)
            {
                SceneManager.LoadScene("ML-Agents-Env"); 
            }
            else
            {
                SceneManager.LoadScene("ContentGenerator");
            }
        }
                
        public void Submit()
        {
#if UNITY_EDITOR
            AssetDatabase.SaveAssetIfDirty(questionsData);
#endif
            answers = GetIntListFromFormQuestionBhvList(questions);
            GetToggleAnswersFromFormCheckboxBhv(checkboxForm);
            SendFormToRightEventHandler(formID);
        }

        private void InstantiateForms()
        {
            if (questionsData == null) return;

            foreach (FormQuestionData q in questionsData.questions)
            {
                GameObject g = Instantiate(questionPrefab);
                var qBhv = g.GetComponent<FormQuestionBhv>();
                qBhv.LoadData(q);
                g.transform.SetParent(questionsPanel);
                questions.Add(qBhv);
            }
        }

        private void ResizeFormsHeight()
        {
            float panelHeight = questionsData.questions.Count
                        * questionPrefab.GetComponent<RectTransform>().rect.height;
            panelHeight += extraQuestionsPanelHeight;

            if (_hasCheckbox)
            {
                GameObject g = Instantiate(checkboxPrefab);
                g.transform.SetParent(questionsPanel);
                checkboxForm = g.GetComponent<FormCheckboxBhv>();
                panelHeight += checkboxPrefab.GetComponent<RectTransform>().rect.height;
            }

            questionsPanel.sizeDelta = new Vector2(0.0f, panelHeight);
            submitButton.SetAsLastSibling();
        }

        private List<int> GetIntListFromFormQuestionBhvList(List<FormQuestionBhv> questions)
        {
            List<int> answersList = new List<int>();
            foreach (FormQuestionBhv q in questions)
            {
                answersList.Add(q.questionData.answer);
                q.ResetToggles();
            }
            return answersList;
        }

        private void GetToggleAnswersFromFormCheckboxBhv(FormCheckboxBhv checkboxForm)
        {
            if (_hasCheckbox && checkboxForm != null)
            {
                foreach (Toggle t in checkboxForm.toggles)
                {
                    answers.Add(t.isOn ? -1 : -2);
                }
            }
        }

        private void SendFormToRightEventHandler(int formID)
        {
            if (formID == 1)
            {
                PostTestFormQuestionAnsweredEventHandler?.Invoke(null, new FormAnsweredEventArgs(formID, answers));
            }
            else
            {
                PreTestFormQuestionAnsweredEventHandler?.Invoke(this, new FormAnsweredEventArgs(formID, answers));
            }
        }
    }
}