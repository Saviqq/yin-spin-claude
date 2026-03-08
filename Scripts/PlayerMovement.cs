using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    [Header("Scale Effect")]
    [SerializeField] private float scaleDuration = 5f;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent expandPlayerEvent;
    [SerializeField] private GameEvent shrinkPlayerEvent;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float playerRadius;

    private float baseMoveSpeed;
    private float baseRotateSpeed;
    private float baseScale;
    private float baseRadius;

    private Coroutine activeScaleCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        playerRadius = col.bounds.extents.x;

        baseMoveSpeed = moveSpeed;
        baseRotateSpeed = rotateSpeed;
        baseScale = transform.localScale.x;
        baseRadius = playerRadius;
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += OnGameStart;
        expandPlayerEvent.OnRaised += OnExpand;
        shrinkPlayerEvent.OnRaised += OnShrink;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
        expandPlayerEvent.OnRaised -= OnExpand;
        shrinkPlayerEvent.OnRaised -= OnShrink;
    }

    private void OnExpand() => StartScaleEffect(1.25f, 0.75f);
    private void OnShrink() => StartScaleEffect(0.75f, 1.25f);

    private void StartScaleEffect(float scaleMult, float speedMult)
    {
        if (activeScaleCoroutine != null)
            StopCoroutine(activeScaleCoroutine);

        activeScaleCoroutine = StartCoroutine(ScaleEffect(scaleMult, speedMult));
    }

    private IEnumerator ScaleEffect(float scaleMult, float speedMult)
    {
        ApplyScaleValues(baseScale * scaleMult, baseMoveSpeed * speedMult, baseRotateSpeed * speedMult);

        yield return new WaitForSeconds(scaleDuration);

        ResetScaleEffect();
    }

    private void ApplyScaleValues(float scale, float move, float rotate)
    {
        transform.localScale = Vector3.one * scale;
        playerRadius = baseRadius * scale;
        moveSpeed = move;
        rotateSpeed = rotate;
    }

    private void ResetScaleEffect()
    {
        ApplyScaleValues(baseScale, baseMoveSpeed, baseRotateSpeed);
        activeScaleCoroutine = null;
    }

    private void OnGameStart()
    {
        if (activeScaleCoroutine != null)
        {
            StopCoroutine(activeScaleCoroutine);
            activeScaleCoroutine = null;
        }
        ResetScaleEffect();
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input = 1f;
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f;

        rb.MoveRotation(rb.rotation + input * rotateSpeed * Time.fixedDeltaTime);
    }

    private void HandleMovement()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        else if (Input.GetKey(KeyCode.D)) x = 1f;

        if (Input.GetKey(KeyCode.W)) y = 1f;
        else if (Input.GetKey(KeyCode.S)) y = -1f;

        Vector2 moveInput = new Vector2(x, y);

        Vector2 newPos = rb.position + moveInput.normalized * moveSpeed * Time.fixedDeltaTime;
        float xBound = halfWidthPlayArea.Value - playerRadius;
        float yBound = halfHeightPlayArea.Value - playerRadius;
        newPos.x = Mathf.Clamp(newPos.x, -xBound, xBound);
        newPos.y = Mathf.Clamp(newPos.y, -yBound, yBound);

        rb.MovePosition(newPos);
    }

    public void Handle()
    {
        HandleMovement();
        HandleRotation();
    }
}