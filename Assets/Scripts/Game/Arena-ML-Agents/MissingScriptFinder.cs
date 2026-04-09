using UnityEngine;

namespace Game.DebugTools
{
    public class MissingScriptFinder : MonoBehaviour
    {
        void Start()
        {
            // Find every single GameObject in the scene (including inactive ones)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int foundCount = 0;

            foreach (GameObject g in allObjects)
            {
                // We only care about objects that are actually in a scene (not project assets)
                if (g.hideFlags != HideFlags.None) continue;

                Component[] components = g.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    // If the component is null, it's a missing script!
                    if (components[i] == null)
                    {
                        Debug.LogError($"[Sleuth] Missing script found on: <b>{GetFullPath(g)}</b>", g);
                        foundCount++;
                        break; // Move to next object
                    }
                }
            }

            if (foundCount == 0) Debug.Log("[Sleuth] No missing scripts found in active scene objects!");
        }

        private string GetFullPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }
    }
}