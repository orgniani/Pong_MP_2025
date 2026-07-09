using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "PaddleColorPalette", menuName = "Config/Paddle Color Palette")]
    public sealed class PaddleColorPalette : ScriptableObject
    {
        [SerializeField] private Color[] colors =
        {
            new(0.9607843f, 0.34509805f, 0.34509805f, 1f),
            new(0.2509804f, 0.63529414f, 0.98039216f, 1f),
            new(0.2901961f, 0.8000001f, 0.49019608f, 1f),
            new(0.9607843f, 0.7764706f, 0.27058825f, 1f)
        };

        public int Count => colors != null ? colors.Length : 0;

        public bool IsValidColorId(int colorId)
        {
            return colorId >= 0 && colorId < Count;
        }

        public Color ResolveColor(int colorId)
        {
            return IsValidColorId(colorId) ? colors[colorId] : Color.white;
        }

        private void OnValidate()
        {
            if (colors == null || colors.Length == 0)
                colors = new[] { Color.white };
        }
    }
}
