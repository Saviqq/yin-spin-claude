using UnityEngine;

public class PowerupManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Powerup powerupPrefab;
    [SerializeField] private PowerupEffect[] effects;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    [Header("Spawn Validation")]
    [SerializeField] private Transform player;
    [SerializeField] private float minDistance = 1f;

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
        if (timer >= Constants.POWERUP_INTERVAL)
        {
            timer = 0f;
            SpawnPowerup();
        }
    }

    private void SpawnPowerup()
    {
        if (!TryGetValidPosition(out Vector2 pos)) return;

        Powerup p = Instantiate(powerupPrefab, pos, Quaternion.identity);
        p.SetEffect(effects[Random.Range(0, effects.Length)]);
    }

    private bool TryGetValidPosition(out Vector2 pos)
    {
        float hw = halfWidthPlayArea.Value * 0.9f;
        float hh = halfHeightPlayArea.Value * 0.9f;

        const int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));

            if (Vector2.Distance(candidate, player.position) < minDistance)
                continue;

            bool tooClose = false;
            foreach (Powerup existing in powerupSet.Items)
            {
                if (Vector2.Distance(candidate, existing.transform.position) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                pos = candidate;
                return true;
            }
        }

        pos = Vector2.zero;
        return false;
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