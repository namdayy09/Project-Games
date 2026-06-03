using UnityEngine;

public class Player : MonoBehaviour
{
    public Sprite[] sprites;
    public float strength = 5f;
    public float gravity = -9.81f;
    public float tilt = 5f;

    private SpriteRenderer spriteRenderer;
    private Vector3 direction;
    private int spriteIndex;
    private bool isPlaying = false; // trạng thái chơi

    // Âm thanh
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wing;   // tiếng khi tap
    [SerializeField] private AudioClip point;  // tiếng khi qua ống
    [SerializeField] private AudioClip die;    // tiếng khi chết

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 1f, 0.5f);
    }

    private void OnEnable()
    {
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
        isPlaying = false; // reset trạng thái
    }

    private void Update()
    {
        // Nếu chưa bắt đầu chơi, chỉ chờ input
        if (!isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                isPlaying = true;
                direction = Vector3.up * strength;

                PlayWing();
            }
            return; // chưa chơi thì không rơi
        }

        // Khi đã bắt đầu chơi
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            direction = Vector3.up * strength;
            PlayWing();
        }

        // Apply gravity and update the position
        direction.y += gravity * Time.deltaTime;
        transform.position += direction * Time.deltaTime;

        // Tilt the bird based on the direction
        Vector3 rotation = transform.eulerAngles;
        rotation.z = direction.y * tilt;
        transform.eulerAngles = rotation;
    }

    private void AnimateSprite()
    {
        spriteIndex++;

        if (spriteIndex >= sprites.Length)
        {
            spriteIndex = 0;
        }

        if (spriteIndex < sprites.Length && spriteIndex >= 0)
        {
            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            PlayDie(); // phát âm thanh chết
            GameManager.Instance.GameOver();
        }
        else if (other.gameObject.CompareTag("Scoring"))
        {
            PlayPoint(); // phát âm thanh ghi điểm
            GameManager.Instance.IncreaseScore();
        }
    }

    // Hàm phát âm thanh tap
    private void PlayWing()
    {
        if (audioSource != null && wing != null)
        {
            audioSource.PlayOneShot(wing);
        }
    }

    // Hàm phát âm thanh ghi điểm
    private void PlayPoint()
    {
        if (audioSource != null && point != null)
        {
            audioSource.PlayOneShot(point);
        }
    }

    // Hàm phát âm thanh chết
    private void PlayDie()
    {
        if (audioSource != null && die != null)
        {
            audioSource.PlayOneShot(die);
        }
    }
}
