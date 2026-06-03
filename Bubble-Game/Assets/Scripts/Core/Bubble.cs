using UnityEngine;
using BubbleShooterPro.Data;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.Core
{
    public enum BubbleState
    {
        Preview,
        Launched,
        Snapped,
        Falling
    }

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Bubble : MonoBehaviour
    {
        [Header("Trạng thái và màu sắc")]
        public BubbleColor colorType = BubbleColor.None;
        public BubbleState state = BubbleState.Preview;

        [Header("Vị trí trong lưới")]
        public int gridRow = -1;
        public int gridCol = -1;

        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;
        private CircleCollider2D _collider;

        private void Awake()
        {
            CacheComponents();
            ConfigureAsKinematic();
        }

        private void Update()
        {
            if (state == BubbleState.Falling && transform.position.y < -10f)
            {
                Destroy(gameObject);
            }
        }

        private void CacheComponents()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_collider == null) _collider = GetComponent<CircleCollider2D>();
        }

        private void ConfigureAsKinematic()
        {
            CacheComponents();

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.useFullKinematicContacts = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _collider.isTrigger = false;
        }

        public void Initialize(BubbleColor color, BubbleState initialState)
        {
            colorType = color;
            ApplyVisualColor();
            SetState(initialState);
        }

        public void SetGridPosition(int row, int col)
        {
            gridRow = row;
            gridCol = col;
        }

        public void SetState(BubbleState newState)
        {
            CacheComponents();
            state = newState;

            switch (newState)
            {
                case BubbleState.Preview:
                    ConfigureAsKinematic();
                    _collider.enabled = false;
                    break;

                case BubbleState.Launched:
                    // Cơ chế mới: bóng bay bằng code, không dùng Rigidbody velocity.
                    ConfigureAsKinematic();
                    _collider.enabled = false;
                    break;

                case BubbleState.Snapped:
                    ConfigureAsKinematic();
                    _collider.enabled = true;
                    break;

                case BubbleState.Falling:
                    _collider.enabled = false;
                    _rb.bodyType = RigidbodyType2D.Dynamic;
                    _rb.gravityScale = 1.6f;
                    _rb.angularVelocity = Random.Range(-180f, 180f);
                    _rb.linearVelocity = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.6f, 2.0f));
                    break;
            }
        }

        public void BeginManualMove()
        {
            SetState(BubbleState.Launched);
        }

        // Giữ hàm cũ để script khác gọi không lỗi, nhưng không dùng physics nữa.
        public void Launch(Vector2 direction, float speed)
        {
            BeginManualMove();
        }

        public void Pop()
        {
            CacheComponents();
            _collider.enabled = false;
            ConfigureAsKinematic();

            SpawnPopDebris();
            StartCoroutine(ShrinkAndDestroy());
        }

        private System.Collections.IEnumerator ShrinkAndDestroy()
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            const float duration = 0.15f;

            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void SpawnPopDebris()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            Color debrisColor = _spriteRenderer.color;
            Sprite circleSprite = _spriteRenderer.sprite;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                GameObject debris = new GameObject("BubbleDebris");
                debris.transform.position = transform.position;
                debris.transform.localScale = Vector3.one * 0.12f;

                SpriteRenderer sr = debris.AddComponent<SpriteRenderer>();
                sr.sprite = circleSprite;
                sr.color = debrisColor;
                sr.sortingOrder = 10;

                Rigidbody2D debrisRb = debris.AddComponent<Rigidbody2D>();
                debrisRb.bodyType = RigidbodyType2D.Dynamic;
                debrisRb.gravityScale = 1f;
                debrisRb.linearVelocity = dir * Random.Range(2.5f, 4.5f);

                StartCoroutine(FadeOutDebris(sr, 0.6f));
                Destroy(debris, 0.7f);
            }
        }

        private System.Collections.IEnumerator FadeOutDebris(SpriteRenderer sr, float duration)
        {
            float elapsed = 0f;
            Color startColor = sr.color;

            while (elapsed < duration)
            {
                if (sr == null) yield break;

                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = c;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void ApplyVisualColor()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            switch (colorType)
            {
                case BubbleColor.Red: _spriteRenderer.color = Color.red; break;
                case BubbleColor.Blue: _spriteRenderer.color = new Color(0.1f, 0.45f, 1f); break;
                case BubbleColor.Green: _spriteRenderer.color = new Color(0.1f, 0.85f, 0.2f); break;
                case BubbleColor.Yellow: _spriteRenderer.color = new Color(1f, 0.9f, 0.1f); break;
                case BubbleColor.Purple: _spriteRenderer.color = new Color(0.65f, 0.15f, 0.95f); break;
                case BubbleColor.Orange: _spriteRenderer.color = new Color(1f, 0.5f, 0f); break;
                default: _spriteRenderer.color = Color.white; break;
            }
        }
    }
}
