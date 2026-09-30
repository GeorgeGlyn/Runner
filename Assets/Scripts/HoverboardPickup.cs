using UnityEngine;

public class HoverboardPickup : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float bobSpeed = 3f;
    public float bobHeight = 0.15f;
    private Vector3 startPos;
    private bool isCollected = false;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            isCollected = true;

            // Immediately disable collider to guarantee no double-triggering
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            player.CollectHoverboardItem();
            Destroy(gameObject);
        }
    }
}
