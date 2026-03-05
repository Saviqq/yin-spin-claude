using UnityEngine;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;

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
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = -1f;
        else if (Input.GetKey(KeyCode.D)) input = 1f;

        float bound = halfWidthPlayArea.Value - playerRadius;
        Vector2 newPos = rb.position + Vector2.right * (input * moveSpeed * Time.fixedDeltaTime);
        newPos.x = Mathf.Clamp(newPos.x, -bound, bound);
        rb.MovePosition(newPos);
    }

    public void Handle()
    {
        HandleMovement();
        HandleRotation();
    }
}
