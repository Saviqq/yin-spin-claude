```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private IntegerValue Health;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float colorRatio = 0.5f;
    [SerializeField] private float collectDelta = 0.1f;

    private Rigidbody2D rb;
    private Material splitMaterial;
    private float leftBound;
    private float rightBound;
    private int score = 0;

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

    void FixedUpdate()
    {
        MoveHorizontal();
        HandleRotation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
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
            if (Health.Current > 0)
            {
                Health.Set(Health.Current - 1);
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

    // --- Public API (used by Step 5 systems) ---

    public void UpdateColorRatio(float newRatio)
    {
        colorRatio = Mathf.Clamp01(newRatio);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }

    public float GetColorRatio() => colorRatio;
    public int GetScore() => score;
}
```

```csharp
using UnityEngine;

public class OrbSpawner : MonoBehaviour
{
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnMargin = 0.3f; // how far outside screen to spawn

    private float timer;
    private float halfWidth;
    private float halfHeight;

    void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnOrb();
        }
    }

    void SpawnOrb()
    {
        int edge = Random.Range(0, 4);
        Vector2 spawnPos;
        float angleMin, angleMax;

        // Pick edge, position along it, and the inward angle range
        switch (edge)
        {
            case 0: // top — orb must point downward
                spawnPos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    halfHeight + spawnMargin
                );
                angleMin = 180f; angleMax = 360f;
                break;

            case 1: // right — orb must point leftward
                spawnPos = new Vector2(
                    halfWidth + spawnMargin,
                    Random.Range(-halfHeight, halfHeight)
                );
                angleMin = 90f; angleMax = 270f;
                break;

            case 2: // bottom — orb must point upward
                spawnPos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    -halfHeight - spawnMargin
                );
                angleMin = 0f; angleMax = 180f;
                break;

            default: // left — orb must point rightward
                spawnPos = new Vector2(
                    -halfWidth - spawnMargin,
                    Random.Range(-halfHeight, halfHeight)
                );
                angleMin = -90f; angleMax = 90f;
                break;
        }

        // Convert random angle to direction vector
        float angleDeg = Random.Range(angleMin, angleMax);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        bool isWhite = Random.value > 0.5f;
        Orb orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        orb.GetComponent<Orb>().Initialize(isWhite, direction);
    }
}
```

```csharp
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float minLifetime = 4f;
    [SerializeField] private float maxLifetime = 9f;

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

    // Called by OrbSpawner immediately after Instantiate
    public void Initialize(bool isWhite, Vector2 direction)
    {
        IsWhite = isWhite;
        GetComponent<SpriteRenderer>().color = isWhite ? Color.white : Color.black;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, Random.Range(minLifetime, maxLifetime));
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
}
```

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IntegerValue", menuName = "Scriptable Objects/IntegerValue")]
public class IntegerValue : ScriptableObject
{
    [SerializeField] private int Value;

    public int Current { get; private set; }

    public event Action<int> OnChange;

    void OnEnable()
    {
        Current = Value;
    }

    public void Set(int value)
    {
        Current = value;
        OnChange?.Invoke(Current);
    }
}
```

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private IntegerValue Health;

    private Label[] heartLabels;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        heartLabels = new Label[]
        {
            root.Q<Label>("heart-0"),
            root.Q<Label>("heart-1"),
            root.Q<Label>("heart-2"),
        };

        Health.OnChange += OnHealthChange;
        OnHealthChange(Health.Current);
    }

    void OnDisable()
    {
        Health.OnChange -= OnHealthChange;
    }

    private void OnHealthChange(int currentHealth)
    {
        for (int i = 0; i < heartLabels.Length; i++)
        {
            bool filled = i < currentHealth;
            heartLabels[i].EnableInClassList("heart--lost", !filled);
        }
    }
}
```