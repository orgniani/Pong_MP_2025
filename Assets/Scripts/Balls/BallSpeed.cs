using UnityEngine;

namespace Balls
{
    public class BallSpeed
    {
        private Rigidbody2D _rb;
        private float _currentSpeed;

        public void Initialize(Rigidbody2D rb, float startSpeed)
        {
            _rb = rb;
            _currentSpeed = startSpeed;
        }

        public void Tick()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.0001f)
                _rb.linearVelocity = _rb.linearVelocity.normalized * _currentSpeed;
        }
    }
}
