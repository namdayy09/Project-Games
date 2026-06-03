using UnityEngine;
using BubbleShooterPro.Core;
using BubbleShooterPro.Data;

namespace BubbleShooterPro.Utils
{
    public class GridTester : MonoBehaviour
    {
        [Header("Dữ liệu Level Test")]
        public LevelData testLevelData;

        /// <summary>
        /// Tải lưới từ LevelData lên màn hình (Có thể nhấn nút chuột phải vào Component này -> chọn 'Load Test Grid' để test trong Play Mode).
        /// </summary>
        [ContextMenu("Load Test Grid")]
        public void LoadGrid()
        {
            if (BubbleGrid.Instance != null)
            {
                if (testLevelData != null)
                {
                    BubbleGrid.Instance.LoadLevelGrid(testLevelData);
                    Debug.Log("Lưới bóng đã được tạo thành công dựa trên LevelData test!");
                }
                else
                {
                    Debug.LogWarning("Chưa gán Test Level Data vào GridTester!");
                }
            }
            else
            {
                Debug.LogError("Không tìm thấy BubbleGrid.Instance trong Scene!");
            }
        }

        private void Start()
        {
            // Tự động load lưới khi chạy để test
            LoadGrid();
        }
    }
}
