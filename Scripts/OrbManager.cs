using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [Header("Spawner config")]
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private float spawnInterval = 2f;   // replaced by DifficultyManager later
    [SerializeField] private float orbSpeed = 3f;        // replaced by DifficultyManager later

    [Header("Orbs")]
    [SerializeField] private OrbSet orbSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private OrbSpawner orbSpawner;
    private float timer;
    private bool isSpawning;

    void Start()
    {
        orbSpawner = new OrbSpawner(orbPrefab, halfHeightPlayArea, halfWidthPlayArea);
        isSpawning = true;
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
            orbSpawner.Spawn(orbSpeed);
        }
    }

    private void OnGameOver() => isSpawning = false;

    private void OnGameStart()
    {
        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
        {
            Destroy(orbSet.Items[i].gameObject);
        }
        isSpawning = true;
        timer = 0f;
    }
}
