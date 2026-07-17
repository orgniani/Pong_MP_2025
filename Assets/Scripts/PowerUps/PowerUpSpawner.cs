using Config;
using Fusion;
using Helpers;
using UnityEngine;

namespace PowerUps
{
    public class PowerUpSpawner : NetworkBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private NetworkObject powerUpPrefab;

        [Header("References")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Config")]
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private PowerUpDefinition[] powerUpDefinitions;

        [Networked] private float _timer { get; set; }
        [Networked] private NetworkRNG _rng { get; set; }
        [Networked] private NetworkBehaviourId _activePowerUp { get; set; }
        [Networked] private int _lastSpawnIndex { get; set; }

        private bool _hasAnyValidDefinition;

        private void Awake()
        {
            if (!ReferenceValidator.Validate(powerUpPrefab, nameof(powerUpPrefab), this))
                return;

            if (spawnPoints != null)
            {
                for (int i = 0; i < spawnPoints.Length; i++)
                    ReferenceValidator.ValidateOptional(spawnPoints[i], $"spawnPoints[{i}]", this);
            }

            if (powerUpDefinitions != null)
            {
                for (int i = 0; i < powerUpDefinitions.Length; i++)
                {
                    ReferenceValidator.ValidateOptional(powerUpDefinitions[i], $"powerUpDefinitions[{i}]", this);
                    if (powerUpDefinitions[i] != null)
                        _hasAnyValidDefinition = true;
                }
            }

            if (powerUpDefinitions == null || powerUpDefinitions.Length == 0)
                Debug.LogError("[PowerUpSpawner] No power-up definitions are assigned. Spawning and replicated visual resolution will fail.", this);
            else if (!_hasAnyValidDefinition)
                Debug.LogError("[PowerUpSpawner] Power-up definitions list contains no valid entries. Spawning and replicated visual resolution will fail.", this);

            PowerUpDefinitionRegistry.RegisterProvider(new PowerUpDefinitionProvider(powerUpDefinitions, this), this);
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

            var rng = _rng;
            if (!TryGetRandomDefinition(ref rng, out var definition))
            {
                _rng = rng;
                return;
            }

            _timer = 0f;
            int index = GetSpawnPointIndex(ref rng);
            _rng = rng;
            _lastSpawnIndex = index;

            Vector3 position = spawnPoints[index].position;

            var spawned = Runner.Spawn(powerUpPrefab, position);
            if (spawned.TryGetBehaviour(out PowerUp powerUp))
            {
                powerUp.SetSpawner(this);
                powerUp.Initialize(definition);
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

        private bool TryGetRandomDefinition(ref NetworkRNG rng, out PowerUpDefinition definition)
        {
            definition = null;

            if (powerUpDefinitions == null || powerUpDefinitions.Length == 0)
                return false;

            int validCount = 0;
            for (int i = 0; i < powerUpDefinitions.Length; i++)
            {
                if (powerUpDefinitions[i] != null)
                    validCount += 1;
            }

            if (validCount == 0)
                return false;

            int selectedIndex = rng.RangeInclusive(0, validCount - 1);
            for (int i = 0; i < powerUpDefinitions.Length; i++)
            {
                if (powerUpDefinitions[i] == null)
                    continue;

                if (selectedIndex == 0)
                {
                    definition = powerUpDefinitions[i];
                    return true;
                }

                selectedIndex -= 1;
            }

            return false;
        }
    }
}
