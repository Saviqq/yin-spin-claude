using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float minLifetime = 4f;
    [SerializeField] private float maxLifetime = 9f;

    [SerializeField] private GameEvent gameStartEvent;

    public bool IsWhite { get; private set; }

    private Rigidbody2D rb;
    private float orbRadius;
    private float halfWidth;
    private float halfHeight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        orbRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += OnGameStart;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool bounced = false;

        // Left / right walls
        if (pos.x - orbRadius < -halfWidth)
        {
            vel.x = Mathf.Abs(vel.x);
            pos.x = -halfWidth + orbRadius;
            bounced = true;
        }
        else if (pos.x + orbRadius > halfWidth)
        {
            vel.x = -Mathf.Abs(vel.x);
            pos.x = halfWidth - orbRadius;
            bounced = true;
        }

        // Top / bottom walls
        if (pos.y - orbRadius < -halfHeight)
        {
            vel.y = Mathf.Abs(vel.y);
            pos.y = -halfHeight + orbRadius;
            bounced = true;
        }
        else if (pos.y + orbRadius > halfHeight)
        {
            vel.y = -Mathf.Abs(vel.y);
            pos.y = halfHeight - orbRadius;
            bounced = true;
        }

        if (bounced)
        {
            rb.position = pos;
            rb.linearVelocity = vel;
        }
    }

    // --- Private ---

    private void OnGameStart()
    {
        Destroy(gameObject);
    }

    // --- Public API ---

    // Called by OrbSpawner immediately after Instantiate
    public void Initialize(bool isWhite, Vector2 direction)
    {
        IsWhite = isWhite;
        GetComponent<SpriteRenderer>().color = isWhite ? Color.white : Color.black;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, Random.Range(minLifetime, maxLifetime));
    }

}