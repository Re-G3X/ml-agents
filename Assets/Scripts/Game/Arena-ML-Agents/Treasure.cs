using UnityEngine;

public class Treasure : MonoBehaviour
{
    [Tooltip("Sound played when the treasure is collected.")]
    [SerializeField] private AudioClip collectSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMLAgent agent = other.GetComponent<PlayerMLAgent>();
        if (agent != null)
        {
            agent.RegisterTreasureCollect();

            // Collection sound at the coin's position
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            gameObject.SetActive(false);   // deactivate instead of destroy(gameobject)
        }
    }
}