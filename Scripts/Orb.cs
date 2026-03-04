using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private OrbSet orbSet;

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

    void OnEnable() => orbSet.Add(this);

    void OnDisable() => orbSet.Remove(this);

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool bounced = false;

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

    public void Initialize(bool isWhite, Vector2 direction, float speed)
    {
        IsWhite = isWhite;
        GetComponent<SpriteRenderer>().color = isWhite ? Color.white : Color.black;
        rb.linearVelocity = direction.normalized * speed;
    }
}