using UnityEngine;

public class ObstacleDemolishEffect : MonoBehaviour
{
    private Vector3 flyVelocity;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private float timer = 0f;
    private float duration = 0.65f;
    private Vector3 initialScale;

    public static void Demolish(GameObject obstacle, Vector3 impactPoint)
    {
        if (obstacle == null) return;

        // 1. Immediately disable all colliders so player never collides again
        Collider[] colliders = obstacle.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        // 2. Stop moving train translation if present
        MovingTrain mt = obstacle.GetComponent<MovingTrain>();
        if (mt != null) mt.enabled = false;

        // 3. Attach demolition animation to obstacle root
        ObstacleDemolishEffect effect = obstacle.AddComponent<ObstacleDemolishEffect>();
        effect.impactPoint = impactPoint;
    }

    [HideInInspector] public Vector3 impactPoint;

    void Start()
    {
        initialScale = transform.localScale;

        // Subway Surfers smash launch: Obstacle is blasted upward, outward and away from tracks!
        float sideDir = (transform.position.x >= 0f) ? 1f : -1f;
        flyVelocity = new Vector3(sideDir * Random.Range(8f, 13f), Random.Range(11f, 16f), Random.Range(5f, 9f));
        rotationAxis = Random.onUnitSphere;
        rotationSpeed = Random.Range(400f, 750f);

        // Spawn explosion blast VFX at impact point
        CreateBlastVFX(impactPoint != Vector3.zero ? impactPoint : transform.position);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        // Blast through air with gravity
        flyVelocity += Physics.gravity * 1.6f * Time.deltaTime;
        transform.position += flyVelocity * Time.deltaTime;
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);

        // Shrink and vanish in a cartoon pop!
        transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void CreateBlastVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("CrashBlastVFX");
        fx.transform.position = pos;

        // Particle system for blast sparks and shockwave
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0f), new Color(0f, 0.95f, 1f));

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        ps.Play();
        Destroy(fx, 1.2f);
    }
}
