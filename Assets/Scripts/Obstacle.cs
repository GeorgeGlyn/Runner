using UnityEngine;

public class Obstacle : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckHit(collision.gameObject);
    }

    private void CheckHit(GameObject obj)
    {
        PlayerController player = obj.GetComponent<PlayerController>();
        if (player == null) player = obj.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.HandleObstacleHit(gameObject);
        }
        else if (obj.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.GameOver();
        }
    }
}
