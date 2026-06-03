using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryMatchPro
{
    /// <summary>
    /// Component gắn lên mỗi thẻ bài (CardPrefab).
    /// Xử lý flip animation, trạng thái, và click input.
    /// </summary>
    public class Card : MonoBehaviour
    {
        // ==================== Inspector References ====================
        [Header("UI Components")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Image backImage;   // mặt sau (úp)
        [SerializeField] private Image frontImage;  // mặt trước (lật lên)
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float flipDuration = 0.15f;   // thời gian mỗi nửa flip
        [SerializeField] private float matchScaleDuration = 0.1f;

        // ==================== State ====================
        public int CardId { get; private set; }
        public bool IsFlipped { get; private set; }
        public bool IsMatched { get; private set; }

        private bool _isAnimating = false;
        private Color _originalFrontColor;
        private RectTransform _rect;

        // ==================== Colors cho placeholder (không có sprite) ====================
        private static readonly Color[] CardColors = new Color[]
        {
            new Color(0.95f, 0.30f, 0.30f), // đỏ
            new Color(0.30f, 0.75f, 0.40f), // xanh lá
            new Color(0.30f, 0.55f, 0.95f), // xanh dương
            new Color(0.95f, 0.75f, 0.20f), // vàng
            new Color(0.80f, 0.35f, 0.90f), // tím
            new Color(0.25f, 0.85f, 0.90f), // cyan
            new Color(0.95f, 0.55f, 0.20f), // cam
            new Color(0.90f, 0.35f, 0.65f), // hồng
            new Color(0.45f, 0.90f, 0.60f), // mint
            new Color(0.60f, 0.45f, 0.30f), // nâu
            new Color(0.70f, 0.90f, 0.25f), // xanh vàng
            new Color(0.25f, 0.60f, 0.80f), // xanh nước biển
            new Color(0.95f, 0.45f, 0.45f), // hồng đỏ
            new Color(0.55f, 0.85f, 0.45f), // xanh nhạt
            new Color(0.80f, 0.60f, 0.95f), // lavender
            new Color(0.95f, 0.80f, 0.35f), // vàng đậm
            new Color(0.35f, 0.80f, 0.75f), // teal
            new Color(0.90f, 0.50f, 0.30f), // cam đỏ
            new Color(0.50f, 0.35f, 0.80f), // indigo
            new Color(0.80f, 0.75f, 0.40f), // olive
            new Color(0.40f, 0.85f, 0.85f), // aqua
            new Color(0.85f, 0.40f, 0.80f), // magenta
            new Color(0.60f, 0.80f, 0.40f), // lime
            new Color(0.40f, 0.40f, 0.80f), // navy
            new Color(0.80f, 0.50f, 0.50f), // rose
            new Color(0.50f, 0.80f, 0.65f), // seafoam
            new Color(0.65f, 0.50f, 0.80f), // periwinkle
            new Color(0.80f, 0.70f, 0.50f), // sand
            new Color(0.50f, 0.70f, 0.80f), // sky
            new Color(0.80f, 0.45f, 0.60f), // mauve
            new Color(0.45f, 0.75f, 0.55f), // sage
            new Color(0.75f, 0.55f, 0.45f), // terracotta
        };

        // ==================== Setup ====================

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (cardButton != null)
                cardButton.onClick.AddListener(OnCardClicked);
        }

        /// <summary>
        /// Khởi tạo thẻ với ID và sprite tương ứng
        /// </summary>
        public void Setup(int cardId, Sprite frontSprite = null, Sprite backSprite = null)
        {
            CardId = cardId;
            IsFlipped = false;
            IsMatched = false;
            _isAnimating = false;

            // Reset transform
            if (_rect != null)
                _rect.localScale = Vector3.one;

            // Gán sprite mặt trước
            if (frontImage != null)
            {
                if (frontSprite != null)
                {
                    frontImage.sprite = frontSprite;
                    frontImage.color = Color.white;
                }
                else
                {
                    // Placeholder: dùng màu theo cardId
                    frontImage.sprite = null;
                    frontImage.color = GetCardColor(cardId);
                }
                _originalFrontColor = frontImage.color;
                frontImage.gameObject.SetActive(false); // ẩn mặt trước ban đầu
            }

            // Gán sprite mặt sau
            if (backImage != null)
            {
                if (backSprite != null)
                {
                    backImage.sprite = backSprite;
                    backImage.color = Color.white;
                }
                // backImage giữ màu mặc định (đã set trong prefab)
                backImage.gameObject.SetActive(true);
            }

            // Enable button
            SetInteractable(true);
        }

        private Color GetCardColor(int id)
        {
            return CardColors[id % CardColors.Length];
        }

        // ==================== Interaction ====================

        private void OnCardClicked()
        {
            if (_isAnimating || IsFlipped || IsMatched) return;

            // Thông báo lên BoardManager
            BoardManager board = FindAnyObjectByType<BoardManager>();
            if (board != null)
                board.OnCardSelected(this);
        }

        public void SetInteractable(bool interactable)
        {
            if (cardButton != null)
                cardButton.interactable = interactable;
        }

        // ==================== Flip Animations ====================

        /// <summary>
        /// Lật mở thẻ (mặt sau → mặt trước)
        /// </summary>
        public void Flip(System.Action onComplete = null)
        {
            if (_isAnimating || IsFlipped) return;
            StartCoroutine(FlipCoroutine(true, onComplete));
        }

        /// <summary>
        /// Úp thẻ lại (mặt trước → mặt sau)
        /// </summary>
        public void FlipBack(System.Action onComplete = null)
        {
            if (_isAnimating || !IsFlipped || IsMatched) return;
            StartCoroutine(FlipCoroutine(false, onComplete));
        }

        /// <summary>
        /// Lật nhanh để hiện hint (không ảnh hưởng state IsFlipped)
        /// </summary>
        public void ShowHintFlip()
        {
            if (IsMatched) return;
            StartCoroutine(HintFlipCoroutine());
        }

        private IEnumerator FlipCoroutine(bool flipToFront, System.Action onComplete)
        {
            _isAnimating = true;

            float elapsed = 0f;
            Vector3 startScale = _rect.localScale;

            // Nửa 1: thu nhỏ về X=0
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipDuration);
                float scaleX = Mathf.Lerp(1f, 0f, t);
                _rect.localScale = new Vector3(scaleX, 1f, 1f);
                yield return null;
            }
            _rect.localScale = new Vector3(0f, 1f, 1f);

            // Đổi sprite/image giữa chừng
            if (flipToFront)
            {
                backImage?.gameObject.SetActive(false);
                if (frontImage != null)
                {
                    frontImage.color = _originalFrontColor;
                    frontImage.gameObject.SetActive(true);
                }
                IsFlipped = true;
            }
            else
            {
                frontImage?.gameObject.SetActive(false);
                backImage?.gameObject.SetActive(true);
                IsFlipped = false;
            }

            // Nửa 2: phóng to về X=1
            elapsed = 0f;
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipDuration);
                float scaleX = Mathf.Lerp(0f, 1f, t);
                _rect.localScale = new Vector3(scaleX, 1f, 1f);
                yield return null;
            }
            _rect.localScale = Vector3.one;

            _isAnimating = false;
            onComplete?.Invoke();
        }

        private IEnumerator HintFlipCoroutine()
        {
            // Hiện mặt trước nhanh
            backImage?.gameObject.SetActive(false);
            frontImage?.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.0f);

            // Úp lại nếu vẫn chưa match
            if (!IsMatched && !IsFlipped)
            {
                frontImage?.gameObject.SetActive(false);
                backImage?.gameObject.SetActive(true);
            }
        }

        // ==================== Match / Wrong Effects ====================

        /// <summary>
        /// Thẻ ghép đúng: scale bounce nhẹ rồi disable button
        /// </summary>
        public void SetMatched()
        {
            IsMatched = true;
            IsFlipped = true;
            SetInteractable(false);
            StartCoroutine(MatchEffectCoroutine());
        }

        private IEnumerator MatchEffectCoroutine()
        {
            float duration = matchScaleDuration;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(1f, 1.15f, t);
                _rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            elapsed = 0f;
            // Scale back
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(1.15f, 1f, t);
                _rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _rect.localScale = Vector3.one;

            // Giảm opacity nhẹ để biết đã matched
            if (frontImage != null)
            {
                Color c = frontImage.color;
                c.a = 0.85f;
                frontImage.color = c;
            }
        }

        /// <summary>
        /// Thẻ ghép sai: flash đỏ nhẹ
        /// </summary>
        public void PlayWrongEffect()
        {
            StartCoroutine(WrongEffectCoroutine());
        }

        private IEnumerator WrongEffectCoroutine()
        {
            if (frontImage == null) yield break;

            Color wrongColor = new Color(1f, 0.4f, 0.4f);
            float duration = 0.15f;

            // Flash đỏ
            frontImage.color = wrongColor;
            yield return new WaitForSeconds(duration);
            frontImage.color = _originalFrontColor;
            yield return new WaitForSeconds(duration);
            frontImage.color = wrongColor;
            yield return new WaitForSeconds(duration);
            frontImage.color = _originalFrontColor;
        }

        // ==================== Utility ====================

        public bool IsAnimating => _isAnimating;

        private void OnDestroy()
        {
            if (cardButton != null)
                cardButton.onClick.RemoveListener(OnCardClicked);
        }
    }
}
