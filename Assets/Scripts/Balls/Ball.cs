using Config;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace Balls
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Ball : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] private MatchRulesConfig matchRulesConfig;
        [SerializeField] private float startingSpeed = 5f;
        [SerializeField] private float minBounceAngle = 15f;
        [SerializeField] private float speedIncreasePerSecond = 0.2f;
        [SerializeField] private float leftBoundX = -9f;
        [SerializeField] private float rightBoundX = 9f;

        [Header("Events")]
        public UnityEvent onLeftGoal;
        public UnityEvent onRightGoal;

        [Networked] public float SpeedMultiplier { get; set; } = 1f;
        [Networked] private float _powerUpTimer { get; set; } = 0f;
        [Networked] private NetworkRNG _rng { get; set; }
        [Networked] private TickTimer _launchTimer { get; set; }

        private BallBounce _ballBounce;
        private BallGoal _ballGoal;
        private BallSpeed _ballSpeed;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _ballBounce = new BallBounce();
            _ballGoal = new BallGoal();
            _ballSpeed = new BallSpeed();
            _rb = GetComponent<Rigidbody2D>();

            _ballBounce.Initialize(_rb, minBounceAngle);
            _ballGoal.Initialize(_rb, leftBoundX, rightBoundX);
            _ballSpeed.Initialize(_rb, startingSpeed, speedIncreasePerSecond);

            if (!matchRulesConfig)
            {
                Debug.LogError("[Ball] MatchRulesConfig is missing.", this);
                enabled = false;
            }
        }

        public override void Spawned()
        {
            _rb.bodyType = HasStateAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

            if (!HasStateAuthority) return;

            _rng = new NetworkRNG(42);
            _rb.position = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _launchTimer = TickTimer.CreateFromSeconds(Runner, matchRulesConfig.CountdownSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (_launchTimer.IsRunning)
            {
                if (_launchTimer.Expired(Runner))
                {
                    _launchTimer = TickTimer.None;
                    LaunchBall();
                }
                return;
            }

            _ballSpeed.Tick(Runner.DeltaTime);

            if (SpeedMultiplier != 1f)
            {
                Vector2 vel = _rb.linearVelocity;
                _rb.linearVelocity = vel.normalized * (vel.magnitude * SpeedMultiplier);

                _powerUpTimer -= Runner.DeltaTime;
                if (_powerUpTimer <= 0f)
                {
                    _powerUpTimer = 0f;
                    SpeedMultiplier = 1f;
                }
            }

            var result = _ballGoal.Tick();
            if (result == GoalResult.LeftGoal)
            {
                onLeftGoal?.Invoke();
                ResetBall();
            }
            else if (result == GoalResult.RightGoal)
            {
                onRightGoal?.Invoke();
                ResetBall();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasStateAuthority) return;
            _ballBounce.ReflectY();
        }

        private void LaunchBall()
        {
            _ballSpeed.ResetSpeed();
            _rb.position = Vector2.zero;

            var rng = _rng;
            bool toRight = rng.NextSingle() > 0.5f;
            float y = rng.RangeInclusive(-0.4f, 0.4f);
            _rng = rng;

            float x = toRight ? 1f : -1f;
            _rb.linearVelocity = new Vector2(x, y).normalized * startingSpeed;
        }

        private void ResetBall()
        {
            LaunchBall();
        }
    }
}
