using UnityEngine;

namespace BubbleShooterPro.Data
{
    /// <summary>
    /// Các màu sắc cơ bản của bóng trong game.
    /// </summary>
    public enum BubbleColor
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6
    }

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Bubble Shooter Pro/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Thông tin Level")]
        public int levelIndex = 1;
        public int maxShots = 30; // Giới hạn số lượt bắn bóng

        [Header("Cấu hình Lưới")]
        public int rows = 8;
        public int cols = 8; // Số cột tối đa cho các hàng chẵn

        [System.Serializable]
        public struct RowData
        {
            public BubbleColor[] rowColors;
        }

        [Tooltip("Bản đồ lưới chứa các màu bóng. Hàng 0 ở trên cùng. Các hàng chẵn/lẻ sẽ tự động offset.")]
        public RowData[] initialGrid;
    }
}
