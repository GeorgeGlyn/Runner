using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Spin & Floating Animation")]
    public float rotationSpeed = 150f;
    public AudioClip coinSound;
    private bool isCollected = false;

    private Vector3 initialPos;
    private float bobSeed;
    private float currentYaw;

    void Start()
    {
        initialPos = transform.position;
        bobSeed = Random.Range(0f, Mathf.PI * 2f);
        currentYaw = Random.Range(0f, 360f); // Stagger initial rotation angles

        // Immediately orient vertically upright (standing on rim, face forward)
        transform.rotation = Quaternion.Euler(90f, currentYaw, 0f);
    }

    void Update()
    {
        // Stand vertically upright like a coin on its edge (90° pitch) and spin 360° around vertical Y axis!
        currentYaw = (currentYaw + rotationSpeed * Time.deltaTime) % 360f;
        transform.rotation = Quaternion.Euler(90f, currentYaw, 0f);

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
