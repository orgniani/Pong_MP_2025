using Balls;
using Config;
using Fusion;
using Helpers;
using Players;
using UnityEngine;

namespace PowerUps
{
    public class PowerUp : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Networked] private int _definitionId { get; set; } = -1;
        [Networked] private int _effectKindId { get; set; } = -1;
        [Networked] private float _effectMagnitude { get; set; }
        [Networked] private float _effectDuration { get; set; }

        private PowerUpSpawner _spawner;
        private ChangeDetector _changes;
        private Sprite _defaultSprite;
        private bool _visualResolved;

        private void Awake()
        {
            ReferenceValidator.ValidateOptional(spriteRenderer, nameof(spriteRenderer), this);
            if (spriteRenderer != null)
                _defaultSprite = spriteRenderer.sprite;
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            RefreshVisual();
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(_definitionId))
                {
                    _visualResolved = false;
                    RefreshVisual();
                }
            }

            if (!_visualResolved && _definitionId >= 0)
                RefreshVisual();
        }

        public void SetSpawner(PowerUpSpawner spawner)
        {
            _spawner = spawner;
        }

        public void Initialize(PowerUpDefinition definition)
        {
            if (!HasStateAuthority || definition == null)
                return;

            _definitionId = definition.DefinitionId;
            _effectKindId = (int)definition.EffectKind;
            _effectMagnitude = definition.Magnitude;
            _effectDuration = definition.Duration;
            ApplyDefinitionVisual(definition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasStateAuthority) return;

            var ball = other.GetComponent<Ball>();
            if (ball == null) return;

            Player lastHitter = FindLastHitter(ball.LastHitBy);
            if (lastHitter == null) return;

            if (!ApplyEffect(lastHitter))
                return;

            _spawner?.ClearActivePowerUp();
            Runner.Despawn(Object);
        }

        private bool ApplyEffect(Player lastHitter)
        {
            switch ((PowerUpEffectKind)_effectKindId)
            {
                case PowerUpEffectKind.PaddleSizeBoostSelf:
                    lastHitter.ApplySizeBoost(_effectMagnitude, _effectDuration);
                    return true;

                case PowerUpEffectKind.PaddleSizeShrinkOpponent:
                    return ApplySizeShrinkToOpponents(lastHitter);

                case PowerUpEffectKind.PaddleSpeedBoostSelf:
                    lastHitter.ApplySpeedBoost(_effectMagnitude, _effectDuration);
                    return true;
            }

            return false;
        }

        private bool ApplySizeShrinkToOpponents(Player lastHitter)
        {
            bool applied = false;

            foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None))
            {
                if (player == null || player == lastHitter)
                    continue;

                if (player.TeamId == lastHitter.TeamId)
                    continue;

                player.ApplySizeShrink(_effectMagnitude, _effectDuration);
                applied = true;
            }

            return applied;
        }

        private void RefreshVisual()
        {
            if (_definitionId < 0)
            {
                ApplyDefinitionVisual(null);
                _visualResolved = false;
                return;
            }

            if (!PowerUpDefinitionRegistry.TryGetDefinition(_definitionId, out var definition))
                return;

            ApplyDefinitionVisual(definition);
            _visualResolved = true;
        }

        private void ApplyDefinitionVisual(PowerUpDefinition definition)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.sprite = definition != null && definition.Sprite != null
                ? definition.Sprite
                : _defaultSprite;
        }

        private static Player FindLastHitter(PlayerRef lastHitBy)
        {
            if (!lastHitBy.IsRealPlayer) return null;

            foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None))
            {
                if (player.Object.InputAuthority == lastHitBy)
                    return player;
            }

            return null;
        }
    }
}
