using UnityEngine;

public class PowerupManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Powerup powerupPrefab;
    [SerializeField] private PowerupEffect[] effects;
    [SerializeField] private FloatValue powerupSpawnInterval;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    [Header("Tracking")]
    [SerializeField] private PowerupSet powerupSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private float timer;

    void Start() => timer = 0f;

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;

    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= powerupSpawnInterval.Value)
        {
            timer = 0f;
            SpawnPowerup();
        }
    }

    private void SpawnPowerup()
    {
        float hw = halfWidthPlayArea.Value * 0.8f;
        float hh = halfHeightPlayArea.Value * 0.8f;
        Vector2 pos = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));

        Powerup p = Instantiate(powerupPrefab, pos, Quaternion.identity);
        p.SetEffect(effects[Random.Range(0, effects.Length)]);
    }

    private void OnGameStart()
    {
        for (int i = powerupSet.Items.Count - 1; i >= 0; i--)
        {
            if (powerupSet.Items[i] != null)
                Destroy(powerupSet.Items[i].gameObject);
        }
        timer = 0f;
    }
}