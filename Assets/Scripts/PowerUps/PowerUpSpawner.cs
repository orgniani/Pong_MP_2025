using Fusion;
using Helpers;
using UnityEngine;

namespace PowerUps
{
    public class PowerUpSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject powerUpPrefab;
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private Transform[] spawnPoints;

        [Networked] private float _timer { get; set; }
        [Networked] private NetworkRNG _rng { get; set; }
        [Networked] private NetworkBehaviourId _activePowerUp { get; set; }
        [Networked] private int _lastSpawnIndex { get; set; }

        private void Awake()
        {
            if (!ReferenceValidator.Validate(powerUpPrefab, nameof(powerUpPrefab), this))
                return;

            if (spawnPoints != null)
            {
                for (int i = 0; i < spawnPoints.Length; i++)
                    ReferenceValidator.ValidateOptional(spawnPoints[i], $"spawnPoints[{i}]", this);
            }
        }

        public override void Spawned()
        {
            if (!HasStateAuthority) return;
            _rng = new NetworkRNG(Runner.Tick.Raw);
            _lastSpawnIndex = -1;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (_activePowerUp.IsValid) return;
            if (powerUpPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

            _timer += Runner.DeltaTime;
            if (_timer < spawnInterval) return;

            _timer = 0f;

            var rng = _rng;
            int index = GetSpawnPointIndex(ref rng);
            _rng = rng;
            _lastSpawnIndex = index;

            Vector3 position = spawnPoints[index].position;

            var spawned = Runner.Spawn(powerUpPrefab, position);
            if (spawned.TryGetBehaviour(out PowerUp powerUp))
            {
                powerUp.SetSpawner(this);
                _activePowerUp = powerUp.Id;
            }
        }

        public void ClearActivePowerUp()
        {
            if (!HasStateAuthority) return;
            _activePowerUp = default;
        }

        private int GetSpawnPointIndex(ref NetworkRNG rng)
        {
            if (spawnPoints.Length == 1)
                return 0;

            if (_lastSpawnIndex < 0 || _lastSpawnIndex >= spawnPoints.Length)
                return rng.RangeInclusive(0, spawnPoints.Length - 1);

            int index = rng.RangeInclusive(0, spawnPoints.Length - 2);
            if (index >= _lastSpawnIndex)
                index += 1;

            return index;
        }
    }
}
