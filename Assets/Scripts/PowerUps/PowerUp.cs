using Balls;
using Fusion;
using Players;
using UnityEngine;

namespace PowerUps
{
    public class PowerUp : NetworkBehaviour
    {
        [SerializeField] private float sizeMultiplier = 1.5f;
        [SerializeField] private float effectDuration = 5f;

        private PowerUpSpawner _spawner;

        public void SetSpawner(PowerUpSpawner spawner)
        {
            _spawner = spawner;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasStateAuthority) return;

            var ball = other.GetComponent<Ball>();
            if (ball == null) return;

            Player lastHitter = FindLastHitter(ball.LastHitBy);
            if (lastHitter == null) return;

            lastHitter.ApplySizeBoost(sizeMultiplier, effectDuration);

            _spawner?.ClearActivePowerUp();
            Runner.Despawn(Object);
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
