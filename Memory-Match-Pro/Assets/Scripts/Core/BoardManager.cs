using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryMatchPro
{
    /// <summary>
    /// BoardManager – quản lý bảng thẻ.
    /// Thêm ForceMatchAll() cho debug mode.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject    cardPrefab;
        [SerializeField] private GridLayoutGroup cardGrid;
        [SerializeField] private RectTransform gridContainer;

        [Header("Layout")]
        [SerializeField] private float cardSpacing  = 10f;
        [SerializeField] private Vector2 gridPadding = new Vector2(20f, 20f);

        // ==================== State ====================
        private LevelData   _levelData;
        private List<Card>  _allCards     = new List<Card>();
        private Card        _firstCard;
        private Card        _secondCard;
        private bool        _isChecking;
        private int         _matchedPairs;
        private int         _totalPairs;

        public System.Action OnAllMatched;

        // ==================== Init ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(LevelData levelData)
        {
            if (levelData == null) { Debug.LogError("[BoardManager] LevelData null!"); return; }
            if (!levelData.IsValid(out string err)) { Debug.LogError(err); return; }

            _levelData    = levelData;
            _matchedPairs = 0;
            _totalPairs   = levelData.TotalPairs;
            _firstCard    = null;
            _secondCard   = null;
            _isChecking   = false;

            ClearBoard();
            SetupGrid(levelData.rows, levelData.columns);
            SpawnCards(levelData);
        }

        private void ClearBoard()
        {
            foreach (var c in _allCards) if (c != null) Destroy(c.gameObject);
            _allCards.Clear();
            if (cardGrid != null)
                foreach (Transform t in cardGrid.transform) Destroy(t.gameObject);
        }

        private void SetupGrid(int rows, int cols)
        {
            if (cardGrid == null) { Debug.LogError("[BoardManager] cardGrid chưa gán!"); return; }

            // Lấy kích thước container
            float containerW = gridContainer != null ? gridContainer.rect.width  : Screen.width  * 0.9f;
            float containerH = gridContainer != null ? gridContainer.rect.height : Screen.height * 0.55f;
            if (containerW < 10f) containerW = Screen.width  * 0.88f;
            if (containerH < 10f) containerH = Screen.height * 0.55f;

            float sp     = cardSpacing;
            float cellW  = (containerW  - sp * (cols - 1) - gridPadding.x * 2f) / cols;
            float cellH  = (containerH  - sp * (rows - 1) - gridPadding.y * 2f) / rows;
            float cell   = Mathf.Max(40f, Mathf.Min(cellW, cellH));

            cardGrid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            cardGrid.constraintCount = cols;
            cardGrid.cellSize        = new Vector2(cell, cell);
            cardGrid.spacing         = new Vector2(sp, sp);
            cardGrid.padding         = new RectOffset(
                (int)gridPadding.x, (int)gridPadding.x,
                (int)gridPadding.y, (int)gridPadding.y);
            cardGrid.childAlignment  = TextAnchor.MiddleCenter;
        }

        private void SpawnCards(LevelData data)
        {
            if (cardPrefab == null) { Debug.LogError("[BoardManager] cardPrefab chưa gán!"); return; }

            // Tạo danh sách card IDs (mỗi cặp xuất hiện 2 lần)
            var ids = new List<int>();
            for (int i = 0; i < data.TotalPairs; i++) { ids.Add(i); ids.Add(i); }
            Shuffle(ids);

            for (int i = 0; i < data.TotalCards; i++)
            {
                var go   = Instantiate(cardPrefab, cardGrid.transform);
                var card = go.GetComponent<Card>();
                if (card == null) { Debug.LogError("[BoardManager] CardPrefab thiếu Card!"); continue; }

                Sprite sp = (data.cardSprites != null && ids[i] < data.cardSprites.Count)
                    ? data.cardSprites[ids[i]] : null;
                card.Setup(ids[i], sp, null);
                _allCards.Add(card);
            }
        }

        // ==================== Card Selection ====================

        public void OnCardSelected(Card card)
        {
            if (_isChecking || card == null || card.IsFlipped || card.IsMatched) return;
            if (card == _firstCard) return;

            AudioManager.Instance?.PlayFlip();

            if (_firstCard == null)
            {
                _firstCard = card;
                card.Flip();
            }
            else
            {
                _secondCard = card;
                card.Flip(() => StartCoroutine(CheckMatchCoroutine()));
            }
        }

        private IEnumerator CheckMatchCoroutine()
        {
            _isChecking = true;
            yield return new WaitUntil(() => !_firstCard.IsAnimating && !_secondCard.IsAnimating);

            GameManager.Instance?.IncrementMoves();

            if (_firstCard.CardId == _secondCard.CardId)
            {
                _firstCard.SetMatched();
                _secondCard.SetMatched();
                _matchedPairs++;
                GameManager.Instance?.OnCorrectMatch();

                var f = _firstCard; var s = _secondCard;
                _firstCard = null; _secondCard = null;
                _isChecking = false;

                if (_matchedPairs >= _totalPairs)
                {
                    yield return new WaitForSeconds(0.3f);
                    GameManager.Instance?.OnGameWin();
                    OnAllMatched?.Invoke();
                }
            }
            else
            {
                _firstCard.PlayWrongEffect();
                _secondCard.PlayWrongEffect();
                GameManager.Instance?.OnWrongMatch();
                yield return new WaitForSeconds(0.6f);

                if (_firstCard  != null && !_firstCard.IsMatched)  _firstCard.FlipBack();
                if (_secondCard != null && !_secondCard.IsMatched) _secondCard.FlipBack();
                yield return new WaitForSeconds(0.35f);

                _firstCard = null; _secondCard = null;
                _isChecking = false;
            }
        }

        // ==================== Hint ====================

        public bool ShowHint()
        {
            var unmatched = new List<Card>();
            foreach (var c in _allCards)
                if (!c.IsMatched && !c.IsFlipped) unmatched.Add(c);
            if (unmatched.Count < 2) return false;

            var byId = new Dictionary<int, List<Card>>();
            foreach (var c in unmatched)
            {
                if (!byId.ContainsKey(c.CardId)) byId[c.CardId] = new List<Card>();
                byId[c.CardId].Add(c);
            }
            foreach (var kv in byId)
            {
                if (kv.Value.Count >= 2)
                {
                    kv.Value[0].ShowHintFlip();
                    kv.Value[1].ShowHintFlip();
                    return true;
                }
            }
            return false;
        }

        // ==================== Debug ====================

        /// <summary>[DEBUG] Match tất cả cards còn lại ngay lập tức</summary>
        public void ForceMatchAll()
        {
            StopAllCoroutines();
            _isChecking = false;

            // Úp lại các thẻ đang lật dở
            if (_firstCard  != null && !_firstCard.IsMatched)  { _firstCard.gameObject.SetActive(false); }
            if (_secondCard != null && !_secondCard.IsMatched) { _secondCard.gameObject.SetActive(false); }
            _firstCard = null; _secondCard = null;

            // Match tất cả
            foreach (var c in _allCards)
                if (!c.IsMatched) { c.Flip(); c.SetMatched(); }

            _matchedPairs = _totalPairs;

            StartCoroutine(DelayedWin());
        }

        private IEnumerator DelayedWin()
        {
            yield return new WaitForSeconds(0.5f);
            GameManager.Instance?.OnGameWin();
        }

        // ==================== Control ====================

        public void SetAllCardsInteractable(bool v)
        {
            foreach (var c in _allCards) if (c != null && !c.IsMatched) c.SetInteractable(v);
        }

        public int MatchedPairs => _matchedPairs;
        public int TotalPairs   => _totalPairs;

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
