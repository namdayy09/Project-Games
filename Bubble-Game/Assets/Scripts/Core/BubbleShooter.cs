using UnityEngine;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.Core
{
    [RequireComponent(typeof(BubbleLauncher))]
    public class BubbleShooter : MonoBehaviour
    {
        public float maxAngle = 75f;
        public Transform pivotVisual;
        public TrajectoryPreview trajectoryPreview;

        private BubbleLauncher _launcher;
        private bool _isAiming;
        private Vector2 _shootDirection = Vector2.up;

        private void Awake()
        {
            _launcher = GetComponent<BubbleLauncher>();
        }

        private void Start()
        {
            if (trajectoryPreview == null) trajectoryPreview = GetComponentInChildren<TrajectoryPreview>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                if (trajectoryPreview != null) trajectoryPreview.ClearTrajectory();
                return;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            if (_launcher == null) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
            {
                _launcher.SwapBubbles();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (Camera.main == null) return;
                Vector2 clickWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (_launcher.previewPoint != null && Vector2.Distance(clickWorld, _launcher.previewPoint.position) < 0.65f)
                {
                    _launcher.SwapBubbles();
                    return;
                }

                _isAiming = true;
                UpdateAimDirection();
            }

            if (Input.GetMouseButton(0) && _isAiming)
            {
                UpdateAimDirection();
            }

            if (Input.GetMouseButtonUp(0) && _isAiming)
            {
                _isAiming = false;
                if (trajectoryPreview != null) trajectoryPreview.ClearTrajectory();

                if (_launcher.CanShoot)
                {
                    _launcher.ShootBubble(_shootDirection);
                    StartCoroutine(PlayRecoil());
                }
            }
        }

        private void UpdateAimDirection()
        {
            if (Camera.main == null) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector2 origin = GetShootOrigin();
            Vector2 rawDirection = ((Vector2)mouseWorld - origin).normalized;
            if (rawDirection.y <= 0.05f) return;

            float angle = Vector2.SignedAngle(Vector2.up, rawDirection);
            angle = Mathf.Clamp(angle, -maxAngle, maxAngle);
            _shootDirection = Quaternion.Euler(0f, 0f, angle) * Vector2.up;

            if (pivotVisual != null) pivotVisual.rotation = Quaternion.Euler(0f, 0f, angle);
            if (trajectoryPreview != null && _launcher.CanShoot) trajectoryPreview.DrawTrajectory(origin, _shootDirection);
        }

        private Vector2 GetShootOrigin()
        {
            if (_launcher != null && _launcher.launchPoint != null) return _launcher.launchPoint.position;
            return transform.position;
        }

        private System.Collections.IEnumerator PlayRecoil()
        {
            if (pivotVisual == null) yield break;

            Vector3 original = pivotVisual.localPosition;
            Vector3 recoil = original - Vector3.up * 0.12f;
            float elapsed = 0f;

            while (elapsed < 0.04f)
            {
                pivotVisual.localPosition = Vector3.Lerp(original, recoil, elapsed / 0.04f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.10f)
            {
                pivotVisual.localPosition = Vector3.Lerp(recoil, original, elapsed / 0.10f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            pivotVisual.localPosition = original;
        }
    }
}
