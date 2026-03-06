using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [Header("Spawner config")]
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue spawnInterval;
    [SerializeField] private FloatValue orbSpeed;

    [Header("Orbs")]
    [SerializeField] private OrbSet orbSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private OrbSpawner orbSpawner;
    private float timer;

    void Start()
    {
        orbSpawner = new OrbSpawner(orbPrefab, halfHeightPlayArea, halfWidthPlayArea);
        timer = spawnInterval.Value;
    }

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;
    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval.Value)
        {
            timer = 0f;
            orbSpawner.Spawn(orbSpeed.Value);
        }
    }

    private void OnGameStart()
    {
        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
            Destroy(orbSet.Items[i].gameObject);

        timer = spawnInterval.Value;
    }
}