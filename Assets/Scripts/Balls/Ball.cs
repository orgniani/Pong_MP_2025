using UnityEngine;
using UnityEngine.Events;

namespace Balls
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Ball : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float startingSpeed = 5f;
        [SerializeField] private float minBounceAngle = 15f;
        [SerializeField] private Collider2D playArea;

        [Header("Events")]
        public UnityEvent onLeftGoal;
        public UnityEvent onRightGoal;

        private BallBounce _ballBounce;
        private BallGoal _ballGoal;
        private BallSpeed _ballSpeed;

        private Rigidbody2D _rb;
        private float _currentSpeed;
        private float _halfHeight;
        private float _halfWidth;

        private void Awake()
        {
            _ballBounce = new BallBounce();
            _ballGoal = new BallGoal();
            _ballSpeed = new BallSpeed();

            _rb = GetComponent<Rigidbody2D>();
            var col = GetComponent<Collider2D>();
            _halfHeight = col.bounds.extents.y;
            _halfWidth = col.bounds.extents.x;

            _ballBounce.Initialize(_rb, playArea, _halfHeight, minBounceAngle);
            _ballGoal.Initialize(_rb, playArea, _halfWidth, onLeftGoal, onRightGoal, ResetBall);
            _ballSpeed.Initialize(_rb, startingSpeed);
        }

        private void Start()
        {
            _currentSpeed = startingSpeed;
            ResetBall(Random.value > 0.5f);
        }

        private void FixedUpdate()
        {
            _ballBounce.Tick();
            _ballGoal.Tick();
            _ballSpeed.Tick();
        }

        private void ResetBall(bool toRight)
        {
            _currentSpeed = startingSpeed;
            _rb.position = Vector2.zero;

            float x = toRight ? 1f : -1f;
            float y = Random.Range(-0.4f, 0.4f);
            Vector2 dir = new Vector2(x, y).normalized;

            _rb.linearVelocity = dir * _currentSpeed;
        }

    }
}
