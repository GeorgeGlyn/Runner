using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotationSpeed = 140f;
    public AudioClip coinSound;
    private bool isCollected = false;

    private Vector3 initialPos;
    private float bobSeed;

    void Start()
    {
        initialPos = transform.position;
        bobSeed = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        // Continuous 3D spin
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);

        // Subtle vertical floating bob
        float bob = Mathf.Sin((Time.time * 4.5f) + bobSeed) * 0.06f;
        transform.position = new Vector3(initialPos.x, initialPos.y + bob, initialPos.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // If a train intersects this coin, immediately destroy it so no coins ever exist below/inside a train!
        if (other.GetComponent<MovingTrain>() != null || other.name.Contains("Train"))
        {
            Destroy(gameObject);
            return;
        }

        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<PlayerController>() != null ||
                        other.GetComponentInParent<PlayerController>() != null;

        if (isPlayer)
        {
            isCollected = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoinSound();
            }
            else if (coinSound != null)
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
