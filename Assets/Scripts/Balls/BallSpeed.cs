using UnityEngine;

namespace Balls
{
    public class BallSpeed
    {
        private Rigidbody2D _rb;
        private float _baseSpeed;
        private float _speedIncreasePerSecond = 0.2f;

        private float _currentSpeed;

        public void Initialize(Rigidbody2D rb, float baseSpeed, float speedIncreasePerSecond)
        {
            _rb = rb;

            _baseSpeed = baseSpeed;
            _currentSpeed = baseSpeed;
            _speedIncreasePerSecond = speedIncreasePerSecond;
        }

        public void Tick()
        {
            if (_rb.linearVelocity == Vector2.zero) return;

            _currentSpeed += _speedIncreasePerSecond * Time.fixedDeltaTime;

            Vector2 dir = _rb.linearVelocity.normalized;
            _rb.linearVelocity = dir * _currentSpeed;
        }

        public void ResetSpeed()
        {
            _currentSpeed = _baseSpeed;
        }
    }
}
