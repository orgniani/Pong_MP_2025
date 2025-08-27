using UnityEngine;
using UnityEngine.Events;
using Helpers;

namespace Characters
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Ball : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float startingSpeed = 5f;
        [SerializeField] private Collider2D playArea;

        [Header("Events")]
        public UnityEvent onLeftGoal;
        public UnityEvent onRightGoal;

        private Rigidbody2D _rb;
        private float _currentSpeed;
        private float _halfHeight;
        private float _halfWidth;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            var col = GetComponent<Collider2D>();
            _halfHeight = col.bounds.extents.y;
            _halfWidth = col.bounds.extents.x;
        }

        private void Start()
        {
            _currentSpeed = startingSpeed;
            Launch();
        }

        private void FixedUpdate()
        {
            HandleVerticalBounce();
            HandleGoals();
            MaintainSpeed();

            transform.position = PlayAreaHelper.ClampToBounds(transform.position, playArea, _halfWidth, _halfHeight);
        }

        private void HandleVerticalBounce()
        {
            Bounds b = playArea.bounds;
            Vector3 pos = transform.position;

            if (pos.y >= b.max.y - _halfHeight && _rb.linearVelocity.y > 0)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_rb.linearVelocity.y);

            if (pos.y <= b.min.y + _halfHeight && _rb.linearVelocity.y < 0)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_rb.linearVelocity.y);
        }

        private void HandleGoals()
        {
            Bounds b = playArea.bounds;
            Vector3 pos = transform.position;

            if (pos.x <= b.min.x + _halfWidth)
            {
                onLeftGoal?.Invoke();
                Launch();
            }

            if (pos.x >= b.max.x - _halfWidth)
            {
                onRightGoal?.Invoke();
                Launch();
            }
        }

        private void MaintainSpeed()
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * _currentSpeed;
        }

        private void Launch()
        {
            bool isRight = Random.value > 0.5f;
            float xVelocity = isRight ? 1f : -1f;
            float yVelocity = Random.Range(-0.5f, 0.5f);

            Vector2 dir = new Vector2(xVelocity, yVelocity).normalized;
            transform.position = Vector3.zero;
            _rb.linearVelocity = dir * _currentSpeed;
        }
    }
}
