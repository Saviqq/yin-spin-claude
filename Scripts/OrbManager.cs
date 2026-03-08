using System.Collections;
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
    [SerializeField] private IntegerEvent spawnOrbEvent;
    [SerializeField] private GameEvent freezeOrbsEvent;

    [Header("Freeze")]
    [SerializeField] private float freezeDuration = 5f;

    private OrbSpawner orbSpawner;
    private float timer;
    private bool isFrozen;
    private Coroutine activeFreezeCoroutine;

    void Start()
    {
        orbSpawner = new OrbSpawner(orbPrefab, halfHeightPlayArea, halfWidthPlayArea);
        timer = spawnInterval.Value;
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += OnGameStart;
        spawnOrbEvent.OnRaised += OnSpawnBurst;
        freezeOrbsEvent.OnRaised += OnFreezeOrbs;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
        spawnOrbEvent.OnRaised -= OnSpawnBurst;
        freezeOrbsEvent.OnRaised -= OnFreezeOrbs;
    }

    void Update()
    {
        if (isFrozen) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval.Value)
        {
            timer = 0f;
            orbSpawner.Spawn(orbSpeed.Value);
        }
    }

    private void OnSpawnBurst(int count)
    {
        for (int i = 0; i < count; i++)
            orbSpawner.Spawn(orbSpeed.Value);
    }

    private void OnFreezeOrbs()
    {
        if (activeFreezeCoroutine != null)
            StopCoroutine(activeFreezeCoroutine);

        activeFreezeCoroutine = StartCoroutine(FreezeCoroutine());
    }

    private IEnumerator FreezeCoroutine()
    {
        isFrozen = true;
        for (int i = 0; i < orbSet.Items.Count; i++)
            orbSet.Items[i].Stop();

        yield return new WaitForSeconds(freezeDuration);

        for (int i = 0; i < orbSet.Items.Count; i++)
            orbSet.Items[i].Resume();

        isFrozen = false;
        activeFreezeCoroutine = null;
    }

    private void OnGameStart()
    {
        if (activeFreezeCoroutine != null)
        {
            StopCoroutine(activeFreezeCoroutine);
            activeFreezeCoroutine = null;
        }

        isFrozen = false;

        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
            Destroy(orbSet.Items[i].gameObject);

        timer = spawnInterval.Value;
    }
}