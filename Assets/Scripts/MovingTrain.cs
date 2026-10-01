using UnityEngine;

public class MovingTrain : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsPlaying || GameManager.Instance.isGameOver)) return;
        transform.Translate(Vector3.back * speed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckHit(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
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
