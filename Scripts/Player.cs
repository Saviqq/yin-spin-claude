using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private Material splitMaterial;

    private float colorRatio = Constants.DEFAULT_COLOR_RATIO;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;
    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    void FixedUpdate()
    {
        playerMovement.Handle();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Orb orb = other.GetComponent<Orb>();
        if (orb == null) return;

        bool hitWhiteHalf = IsWhiteHalf(other.transform.position);

        if (orb.IsWhite == hitWhiteHalf)
        {
            colorRatio += orb.IsWhite ? Constants.COLLECT_DELTA : -Constants.COLLECT_DELTA;
            colorRatio = Mathf.Clamp01(colorRatio);
            splitMaterial.SetFloat("_ColorRatio", colorRatio);
            score.Set(score.Value + 1);
        }
        else
        {
            if (health.Value > 0)
                health.Set(health.Value - 1);
        }

        Destroy(other.gameObject);
    }

    private bool IsWhiteHalf(Vector3 orbWorldPos)
    {
        Vector2 worldDir = (Vector2)(orbWorldPos - transform.position);
        float worldAngle = Mathf.Atan2(worldDir.y, worldDir.x);
        float localAngle = worldAngle - rb.rotation * Mathf.Deg2Rad;
        float t = localAngle / (2f * Mathf.PI) + 0.5f;
        t = t - Mathf.Floor(t);
        return t < colorRatio;
    }

    private void OnGameStart()
    {
        transform.position = Vector2.zero;
        colorRatio = Mathf.Clamp01(Constants.DEFAULT_COLOR_RATIO);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }
}