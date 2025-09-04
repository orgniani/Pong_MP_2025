using UnityEngine;

namespace Balls
{
    public class BallBounce
    {
        private Rigidbody2D _rb;
        private float _minBounceAngleDeg;

        public void Initialize(Rigidbody2D rb, float minBounceAngleDeg)
        {
            _rb = rb;
            _minBounceAngleDeg = minBounceAngleDeg;
        }

        public void ReflectY()
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