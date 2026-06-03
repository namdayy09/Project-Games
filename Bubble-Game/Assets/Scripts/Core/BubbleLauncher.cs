using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BubbleShooterPro.Data;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.Core
{
    public class BubbleLauncher : MonoBehaviour
    {
        private static BubbleLauncher _instance;

        public static BubbleLauncher Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<BubbleLauncher>();

                return _instance;
            }
        }

        [Header("Điểm định vị bóng")]
        public Transform launchPoint;
        public Transform previewPoint;

        [Header("Prefab")]
        public GameObject bubblePrefab;

        [Header("Tốc độ bóng")]
        public float moveSpeed = 9f;

        private Bubble _currentBubble;
        private Bubble _nextBubble;

        private bool _canShoot = true;

        private List<BubbleColor> _activeColors = new List<BubbleColor>
        {
            BubbleColor.Red,
            BubbleColor.Blue,
            BubbleColor.Green,
            BubbleColor.Yellow
        };

        public bool CanShoot => _canShoot;
        public Bubble CurrentBubble => _currentBubble;
        public Bubble NextBubble => _nextBubble;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }

        private void Start()
        {
            EnsurePoints();
            SpawnInitialBubbles();
        }

        private void EnsurePoints()
        {
            if (launchPoint == null)
            {
                GameObject lp = new GameObject("LaunchPoint");
                lp.transform.SetParent(transform, false);
                lp.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                launchPoint = lp.transform;
            }

            if (previewPoint == null)
            {
                GameObject pp = new GameObject("PreviewPoint");
                pp.transform.SetParent(transform, false);
                pp.transform.localPosition = new Vector3(-1.2f, 0f, 0f);
                previewPoint = pp.transform;
            }
        }

        public void SpawnInitialBubbles()
        {
            if (bubblePrefab == null)
            {
                Debug.LogError("BubbleLauncher chưa gán bubblePrefab.");
                return;
            }

            if (_currentBubble != null) Destroy(_currentBubble.gameObject);
            if (_nextBubble != null) Destroy(_nextBubble.gameObject);

            _currentBubble = SpawnBubbleAt(launchPoint.position, GetRandomColor(), transform);
            _nextBubble = SpawnBubbleAt(previewPoint.position, GetRandomColor(), transform);

            _canShoot = true;
        }

        public void SetActiveColors(List<BubbleColor> colors)
        {
            if (colors != null && colors.Count > 0)
                _activeColors = colors;
        }

        public Bubble ShootBubble(Vector2 direction)
        {
            return ShootBubble(direction, null);
        }

        public Bubble ShootBubble(Vector2 direction, ShotPathResult ignoredPath)
        {
            if (!_canShoot) return null;
            if (_currentBubble == null) return null;

            if (LevelManager.Instance != null && !LevelManager.Instance.CanShoot())
                return null;

            _canShoot = false;

            Bubble projectile = _currentBubble;
            _currentBubble = null;

            projectile.transform.SetParent(null);
            projectile.transform.position = launchPoint.position;
            projectile.SetState(BubbleState.Launched);

            if (LevelManager.Instance != null)
                LevelManager.Instance.UseShot();

            if (AudioManager.Instance != null && AudioManager.Instance.shootClip != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.shootClip);

            StartCoroutine(MoveProjectile(projectile, direction.normalized));
            PrepareNextBubble();

            return projectile;
        }

        private IEnumerator MoveProjectile(Bubble projectile, Vector2 direction)
        {
            if (projectile == null) yield break;

            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.up;

            while (projectile != null)
            {
                Vector3 pos = projectile.transform.position;
                Vector3 nextPos = pos + (Vector3)(direction * moveSpeed * Time.deltaTime);

                BubbleGrid grid = BubbleGrid.Instance;

                if (grid == null)
                {
                    Destroy(projectile.gameObject);
                    yield break;
                }

                if (nextPos.x <= grid.leftWallX + grid.bubbleRadius)
                {
                    nextPos.x = grid.leftWallX + grid.bubbleRadius;
                    direction.x = Mathf.Abs(direction.x);
                }
                else if (nextPos.x >= grid.rightWallX - grid.bubbleRadius)
                {
                    nextPos.x = grid.rightWallX - grid.bubbleRadius;
                    direction.x = -Mathf.Abs(direction.x);
                }

                projectile.transform.position = nextPos;

                if (nextPos.y >= grid.ceilingY - grid.bubbleRadius)
                {
                    grid.AttachBubbleAtPoint(projectile, nextPos, true);
                    yield break;
                }

                if (grid.IsProjectileTouchingGrid(nextPos))
                {
                    grid.AttachBubbleAtPoint(projectile, nextPos, true);
                    yield break;
                }

                if (nextPos.y < grid.loseLineY - 2f)
                {
                    Destroy(projectile.gameObject);
                    yield break;
                }

                yield return null;
            }
        }

        private void PrepareNextBubble()
        {
            if (_nextBubble == null)
                _nextBubble = SpawnBubbleAt(previewPoint.position, GetRandomColor(), transform);

            _currentBubble = _nextBubble;
            _currentBubble.transform.SetParent(transform);
            _currentBubble.transform.position = launchPoint.position;
            _currentBubble.SetState(BubbleState.Preview);

            _nextBubble = SpawnBubbleAt(previewPoint.position, GetRandomColor(), transform);

            _canShoot = true;
        }

        public void SwapBubbles()
        {
            if (!_canShoot) return;
            if (_currentBubble == null || _nextBubble == null) return;

            Bubble temp = _currentBubble;
            _currentBubble = _nextBubble;
            _nextBubble = temp;

            _currentBubble.transform.SetParent(transform);
            _nextBubble.transform.SetParent(transform);

            _currentBubble.transform.position = launchPoint.position;
            _nextBubble.transform.position = previewPoint.position;

            _currentBubble.SetState(BubbleState.Preview);
            _nextBubble.SetState(BubbleState.Preview);
        }

        private Bubble SpawnBubbleAt(Vector3 position, BubbleColor color, Transform parent)
        {
            GameObject obj = Instantiate(bubblePrefab, position, Quaternion.identity, parent);
            Bubble bubble = obj.GetComponent<Bubble>();
            bubble.Initialize(color, BubbleState.Preview);
            return bubble;
        }

        private BubbleColor GetRandomColor()
        {
            if (_activeColors == null || _activeColors.Count == 0)
                return BubbleColor.Red;

            return _activeColors[Random.Range(0, _activeColors.Count)];
        }
    }
}