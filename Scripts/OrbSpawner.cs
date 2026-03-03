using UnityEngine;

public class OrbSpawner : MonoBehaviour
{
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnMargin = 0.3f; // how far outside screen to spawn

    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private float timer;
    private float halfWidth;
    private float halfHeight;

    private bool isSpawning = true;

    void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;
    }

    void OnEnable()
    {
        gameOverEvent.OnRaised += OnGameOver;
        gameStartEvent.OnRaised += OnGameStart;
    }

    void OnDisable()
    {
        gameOverEvent.OnRaised -= OnGameOver;
        gameStartEvent.OnRaised -= OnGameStart;
    }

    void Update()
    {
        if (!isSpawning) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnOrb();
        }
    }

    private void SpawnOrb()
    {
        int edge = Random.Range(0, 4);
        Vector2 spawnPos;
        float angleMin, angleMax;

        // Pick edge, position along it, and the inward angle range
        switch (edge)
        {
            case 0: // top — orb must point downward
                spawnPos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    halfHeight + spawnMargin
                );
                angleMin = 210f; angleMax = 330f;
                break;

            case 1: // right — orb must point leftward
                spawnPos = new Vector2(
                    halfWidth + spawnMargin,
                    Random.Range(-halfHeight, halfHeight)
                );
                angleMin = 120f; angleMax = 240f;
                break;

            case 2: // bottom — orb must point upward
                spawnPos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    -halfHeight - spawnMargin
                );
                angleMin = 30f; angleMax = 150f;
                break;

            default: // left — orb must point rightward
                spawnPos = new Vector2(
                    -halfWidth - spawnMargin,
                    Random.Range(-halfHeight, halfHeight)
                );
                angleMin = -60f; angleMax = 60f;
                break;
        }

        // Convert random angle to direction vector
        float angleDeg = Random.Range(angleMin, angleMax);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        bool isWhite = Random.value > 0.5f;
        Orb orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        orb.GetComponent<Orb>().Initialize(isWhite, direction);
    }

    private void OnGameOver() => isSpawning = false;

    private void OnGameStart()
    {
        isSpawning = true;
        timer = 0f;
    }
}