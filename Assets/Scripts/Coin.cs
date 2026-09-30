using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotationSpeed = 120f;
    public AudioClip coinSound;
    private bool isCollected = false;

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<PlayerController>() != null ||
                        other.GetComponentInParent<PlayerController>() != null;

        if (isPlayer)
        {
            isCollected = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (coinSound != null)
            {
                AudioSource.PlayClipAtPoint(coinSound, transform.position);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin();
            }
            Destroy(gameObject);
        }
    }
}
