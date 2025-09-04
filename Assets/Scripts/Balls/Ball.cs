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
        [SerializeField] private float speedIncreasePerSecond = 0.2f;

        [Header("Events")]
        public UnityEvent onLeftGoal;
        public UnityEvent onRightGoal;

        private BallBounce _ballBounce;
        private BallGoal _ballGoal;
        private BallSpeed _ballSpeed;

        private Rigidbody2D _rb;
        private float _currentSpeed;

        private void Awake()
        {
            _ballBounce = new BallBounce();
            _ballGoal = new BallGoal();
            _ballSpeed = new BallSpeed();

            _rb = GetComponent<Rigidbody2D>();
            var col = GetComponent<Collider2D>();

            _ballBounce.Initialize(_rb, minBounceAngle);
            _ballGoal.Initialize(_rb, onLeftGoal, onRightGoal, ResetBall);
            _ballSpeed.Initialize(_rb, startingSpeed, speedIncreasePerSecond);
        }

        private void Start()
        {
            _currentSpeed = startingSpeed;
            ResetBall(Random.value > 0.5f);
        }

        private void FixedUpdate()
        {
            _ballGoal.Tick();
            _ballSpeed.Tick();
        }

        private void ResetBall(bool toRight)
        {
            _ballSpeed.ResetSpeed();

            _currentSpeed = startingSpeed;
            _rb.position = Vector2.zero;

            float x = toRight ? 1f : -1f;
            float y = Random.Range(-0.4f, 0.4f);
            Vector2 dir = new Vector2(x, y).normalized;

            _rb.linearVelocity = dir * _currentSpeed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            _ballBounce.ReflectY();
        }
    }
}
