using Fusion;
using Managers;
using UnityEngine;

namespace Players
{
    [RequireComponent(typeof(Collider2D))]
    public class Player : NetworkBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float arenaBoundX = 4f;

        [Networked] public int spawnPointIndex { get; set; }

        private float _halfWidth;

        private void Awake()
        {
            var col = GetComponent<CapsuleCollider2D>();
            _halfWidth = col.size.y / 2f;
        }

        public override void Spawned()
        {
            Transform spawnPoint = NetworkManager.Instance.GetSpawnPoint(spawnPointIndex);
            if (spawnPoint != null)
            {
                transform.SetParent(spawnPoint, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            Transform parent = transform.parent;
            if (parent != null)
                Debug.Log($"[Player] Spawned — parent='{parent.name}' parentWorldPos={parent.position} parentWorldRot={parent.rotation.eulerAngles} parentLossyScale={parent.lossyScale}");
            else
                Debug.LogWarning("[Player] Spawned — NO PARENT");

            Debug.Log($"[Player] Spawned — localPos={transform.localPosition} localRot={transform.localRotation.eulerAngles} localScale={transform.localScale} worldPos={transform.position} worldRot={transform.rotation.eulerAngles}");
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (GetInput<PlayerInputData>(out var input))
            {
                Vector3 localPos = transform.localPosition;
                localPos.x -= input.MoveY * speed * Runner.DeltaTime;
                localPos.x = Mathf.Clamp(localPos.x, -arenaBoundX + _halfWidth, arenaBoundX - _halfWidth);
                transform.localPosition = localPos;
            }
        }
    }
}
