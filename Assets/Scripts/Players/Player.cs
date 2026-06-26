using Fusion;
using UnityEngine;

namespace Players
{
    [RequireComponent(typeof(Collider2D))]
    public class Player : NetworkBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float arenaBoundY = 4f;

        private float _halfHeight;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            _halfHeight = col.bounds.extents.y;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (GetInput<PlayerInputData>(out var input))
            {
                Vector3 pos = transform.position;
                pos.y += input.MoveY * speed * Runner.DeltaTime;
                pos.y = Mathf.Clamp(pos.y, -arenaBoundY + _halfHeight, arenaBoundY - _halfHeight);
                transform.position = pos;
            }
        }
    }
}
