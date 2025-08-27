using UnityEngine;

namespace Helpers
{
    public static class PlayAreaHelper
    {
        public static Vector3 ClampToBounds(Vector3 position, Collider2D playArea, float halfWidth, float halfHeight)
        {
            if (!playArea) return position;

            Bounds b = playArea.bounds;

            position.x = Mathf.Clamp(position.x, b.min.x + halfWidth, b.max.x - halfWidth);
            position.y = Mathf.Clamp(position.y, b.min.y + halfHeight, b.max.y - halfHeight);

            return position;
        }
    }
}