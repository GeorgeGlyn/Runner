using UnityEngine;

public class RunnerCharacterAnimator : MonoBehaviour
{
    public Transform hoverboard;
    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (hoverboard == null) hoverboard = transform.Find("Hoverboard");
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            if (animator != null) animator.speed = 0f;
            return;
        }

        // Dynamic banking / tilting into lane changes
        if (controller != null)
        {
            float horizontalVel = controller.velocity.x;
            float targetTilt = -horizontalVel * 2.0f;
            targetTilt = Mathf.Clamp(targetTilt, -18f, 18f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0f, 0f, targetTilt), 14f * Time.deltaTime);
        }
    }
}
