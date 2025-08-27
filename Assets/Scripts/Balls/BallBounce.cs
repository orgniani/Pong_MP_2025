using UnityEngine;

namespace Balls
{
    public class BallBounce
    {
        private Rigidbody2D _rb;
        private Collider2D _playArea;

        private float _halfHeight;
        private float _minBounceAngleDeg;

        public void Initialize(Rigidbody2D rb, Collider2D playArea, float halfHeight, float minBounceAngleDeg)
        {
            _rb = rb;
            _playArea = playArea;
            _halfHeight = halfHeight;
            _minBounceAngleDeg = minBounceAngleDeg;
        }

        public void Tick()
        {
            Bounds b = _playArea.bounds;
            Vector3 pos = _rb.position;

            if (pos.y >= b.max.y - _halfHeight && _rb.linearVelocity.y > 0)
                ReflectY();

            if (pos.y <= b.min.y + _halfHeight && _rb.linearVelocity.y < 0)
                ReflectY();
        }

        private void ReflectY()
        {
            Vector2 v = _rb.linearVelocity;
            v = new Vector2(v.x, -v.y);

            float angle = Mathf.Atan2(v.y, v.x);
            float minRad = _minBounceAngleDeg * Mathf.Deg2Rad;

            if (Mathf.Abs(Mathf.Sin(angle)) > Mathf.Cos(minRad))
            {
                float signX = Mathf.Sign(v.x);
                float signY = Mathf.Sign(v.y);

                float newX = Mathf.Cos(minRad) * signX;
                float newY = Mathf.Sin(minRad) * signY;
                v = new Vector2(newX, newY) * v.magnitude;
            }

            _rb.linearVelocity = v;
        }
    }
}