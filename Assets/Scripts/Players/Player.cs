using System;
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
        [Networked] public NetworkString<_16> Username { get; private set; }

        public static event Action OnAnyUsernameChanged;

        private float _halfWidth;
        private ChangeDetector _changes;

        private void Awake()
        {
            var col = GetComponent<CapsuleCollider2D>();
            _halfWidth = col.size.y / 2f;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            Transform spawnPoint = NetworkManager.Instance.GetSpawnPoint(spawnPointIndex);
            if (spawnPoint != null)
            {
                transform.SetParent(spawnPoint, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            if (Object.HasInputAuthority)
                RPC_SetUsername(LocalPlayerSession.Username);
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(Username))
                    OnAnyUsernameChanged?.Invoke();
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetUsername(NetworkString<_16> username)
        {
            Username = username;
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
