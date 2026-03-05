using UnityEngine;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    private Rigidbody2D rb;
    private float playerRadius;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input = 1f; // counter-clockwise
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f; // clockwise

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
