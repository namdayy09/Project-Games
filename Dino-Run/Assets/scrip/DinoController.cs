using UnityEngine;

public class DinoController : MonoBehaviour
{
    public static DinoController Instance;

    public float jumpForce = 7f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D dinoCollider;

    private bool isGrounded = true;
    private bool isDead = false;

    public Collider2D DinoCollider
    {
        get { return dinoCollider; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        dinoCollider = GetComponent<Collider2D>();

        rb.gravityScale = 1.2f;
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (isDead) return;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = Vector2.up * jumpForce;
            isGrounded = false;

            PlayTrigger("Jump");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            PlayTrigger("Run");
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    public void HitObstacle()
    {
        Die();
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        Debug.Log("Dino died!");
    }

    void PlayTrigger(string triggerName)
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }
}