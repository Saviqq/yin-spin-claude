using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private Rigidbody2D rb;
    private Material splitMaterial;
    private float playerRadius;

    private float colorRatio = Constants.DEFAULT_COLOR_RATIO;
    private bool isActive = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
        playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
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

    void FixedUpdate()
    {
        if (!isActive) return;

        MoveHorizontal();
        HandleRotation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        Orb orb = other.GetComponent<Orb>();
        if (orb == null) return;

        bool hitWhiteHalf = IsWhiteHalf(other.transform.position);

        if (orb.IsWhite == hitWhiteHalf)
        {
            // Correct color — collect
            colorRatio += orb.IsWhite ? Constants.COLLECT_DELTA : -Constants.COLLECT_DELTA;
            colorRatio = Mathf.Clamp01(colorRatio);
            splitMaterial.SetFloat("_ColorRatio", colorRatio);
            score.Set(score.Value + 1);
        }
        else
        {
            if (health.Value > 0)
            {
                health.Set(health.Value - 1);
            }
        }

        Destroy(other.gameObject);
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input = 1f; // counter-clockwise
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f; // clockwise

        rb.MoveRotation(rb.rotation + input * rotateSpeed * Time.fixedDeltaTime);
    }

    private void MoveHorizontal()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = -1f;
        else if (Input.GetKey(KeyCode.D)) input = 1f;

        float bound = halfWidthPlayArea.Value - playerRadius;
        Vector2 newPos = rb.position + Vector2.right * (input * moveSpeed * Time.fixedDeltaTime);
        newPos.x = Mathf.Clamp(newPos.x, -bound, bound);
        rb.MovePosition(newPos);
    }

    private bool IsWhiteHalf(Vector3 orbWorldPos)
    {
        // Direction from player center to orb in world space
        Vector2 worldDir = (Vector2)(orbWorldPos - transform.position);

        // World-space angle of that direction (radians)
        float worldAngle = Mathf.Atan2(worldDir.y, worldDir.x);

        // Subtract player's rotation to convert to local space
        // rb.rotation is in degrees, shader works in local space
        float localAngle = worldAngle - rb.rotation * Mathf.Deg2Rad;

        // Same formula as the shader: t = angle / 2π + 0.5
        float t = localAngle / (2f * Mathf.PI) + 0.5f;

        // frac() — strip integer part to keep t in [0, 1] across wrap-around
        t = t - Mathf.Floor(t);

        // White sector is t < colorRatio — matches shader's step(t, _ColorRatio)
        return t < colorRatio;
    }

    private void OnGameOver() => isActive = false;

    private void OnGameStart()
    {
        isActive = true;
        transform.position = Vector2.zero;
        colorRatio = Mathf.Clamp01(Constants.DEFAULT_COLOR_RATIO);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }
}