using Fusion;
using UnityEngine;

namespace Players
{
    [RequireComponent(typeof(Collider2D))]
    public class Player : NetworkBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private SpriteRenderer parentRenderer;

        private float _halfHeight;
        private float _halfWidth;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            _halfHeight = col.bounds.extents.y;
            _halfWidth = col.bounds.extents.x;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput<PlayerInputData>(out var input))
            {
                Vector3 move = Vector3.up * input.MoveY * speed * Runner.DeltaTime;
                transform.position += move;
                ClampToParentBounds();
            }
        }

        private void ClampToParentBounds()
        {
            if (!parentRenderer) return;

            Bounds bounds = parentRenderer.bounds;
            Vector3 pos = transform.position;

            float minX = bounds.min.x + _halfWidth;
            float maxX = bounds.max.x - _halfWidth;
            float minY = bounds.min.y + _halfHeight;
            float maxY = bounds.max.y - _halfHeight;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }
    }
}
