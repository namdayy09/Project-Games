using System.Collections.Generic;
using UnityEngine;

namespace BubbleShooterPro.Core
{
    public class ShotPathResult
    {
        public List<Vector2> points = new List<Vector2>();
        public GameObject finalHitObject;
        public Vector2 finalHitPoint;
        public Vector2 finalDirection;
        public bool hitBubbleOrCeiling;
    }

    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        public int maxReflections = 5;
        public float maxDistance = 20f;

        [Header("Tương thích SetupProject cũ")]
        public LayerMask collisionMask;

        public float stepDistance = 0.06f;
        public float lineWidth = 0.055f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            if (_lineRenderer == null) return;

            _lineRenderer.positionCount = 0;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.sortingOrder = 20;

            if (_lineRenderer.sharedMaterial == null)
            {
                _lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            _lineRenderer.startColor = new Color(1f, 1f, 1f, 0.75f);
            _lineRenderer.endColor = new Color(1f, 1f, 1f, 0.3f);
        }

        public void DrawTrajectory(Vector2 origin, Vector2 direction)
        {
            ShotPathResult path = CalculatePath(origin, direction);
            DrawPath(path);
        }

        public ShotPathResult CalculatePath(Vector2 origin, Vector2 direction)
        {
            ShotPathResult result = new ShotPathResult();
            result.points.Add(origin);

            BubbleGrid grid = BubbleGrid.Instance;
            if (grid == null)
            {
                result.points.Add(origin + direction.normalized * maxDistance);
                return result;
            }

            Vector2 pos = origin;
            Vector2 dir = direction.normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;

            int reflections = 0;
            float traveled = 0f;
            float radius = grid.bubbleRadius;

            while (traveled < maxDistance && reflections <= maxReflections)
            {
                Vector2 next = pos + dir * stepDistance;
                traveled += stepDistance;

                if (next.x <= grid.leftWallX + radius)
                {
                    next.x = grid.leftWallX + radius;
                    dir.x = Mathf.Abs(dir.x);
                    result.points.Add(next);
                    reflections++;
                }
                else if (next.x >= grid.rightWallX - radius)
                {
                    next.x = grid.rightWallX - radius;
                    dir.x = -Mathf.Abs(dir.x);
                    result.points.Add(next);
                    reflections++;
                }

                if (next.y >= grid.ceilingY - radius)
                {
                    next.y = grid.ceilingY - radius;
                    result.points.Add(next);
                    result.finalHitPoint = next;
                    result.finalDirection = dir;
                    result.hitBubbleOrCeiling = true;
                    return result;
                }

                Vector2 contact;
                if (grid.IsProjectileTouchingGrid(next, out contact))
                {
                    result.points.Add(contact);
                    result.finalHitPoint = contact;
                    result.finalDirection = dir;
                    result.hitBubbleOrCeiling = true;
                    return result;
                }

                pos = next;
            }

            result.points.Add(pos);
            result.finalHitPoint = pos;
            result.finalDirection = dir;
            result.hitBubbleOrCeiling = false;
            return result;
        }

        public void DrawPath(ShotPathResult path)
        {
            if (_lineRenderer == null) return;
            if (path == null || path.points == null || path.points.Count == 0)
            {
                ClearTrajectory();
                return;
            }

            _lineRenderer.positionCount = path.points.Count;
            for (int i = 0; i < path.points.Count; i++)
            {
                Vector2 p = path.points[i];
                _lineRenderer.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }
        }

        public void ClearTrajectory()
        {
            if (_lineRenderer != null) _lineRenderer.positionCount = 0;
        }
    }
}
