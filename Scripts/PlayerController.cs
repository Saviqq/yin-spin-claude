using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private IntegerValue health;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float colorRatio = 0.5f;
    [SerializeField] private float collectDelta = 0.1f;

    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private Rigidbody2D rb;
    private Material splitMaterial;
    private float leftBound;
    private float rightBound;
    private int score = 0;

    private bool isActive = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);

        float playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
        float camHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        leftBound = -camHalfWidth + playerRadius;
        rightBound = camHalfWidth - playerRadius;
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
            colorRatio += orb.IsWhite ? collectDelta : -collectDelta;
            colorRatio = Mathf.Clamp01(colorRatio);
            splitMaterial.SetFloat("_ColorRatio", colorRatio);
            score++;
            Debug.Log($"Collected! Score: {score} | ColorRatio: {colorRatio:F2}");
        }
        else
        {
            if (health.Current > 0)
            {
                health.Set(health.Current - 1);
            }
        }

        Destroy(other.gameObject);
    }

    // --- Private ---

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

        Vector2 newPos = rb.position + Vector2.right * (input * moveSpeed * Time.fixedDeltaTime);
        newPos.x = Mathf.Clamp(newPos.x, leftBound, rightBound);
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
        UpdateColorRatio(0.5f);
    }

    // --- Public API (used by Step 5 systems) ---

    public void UpdateColorRatio(float newRatio)
    {
        colorRatio = Mathf.Clamp01(newRatio);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }

    public float GetColorRatio() => colorRatio;
    public int GetScore() => score;
}