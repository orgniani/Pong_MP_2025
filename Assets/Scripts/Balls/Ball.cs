using Config;
using Fusion;
using Helpers;
using Players;
using PowerUps;
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

        [Networked] private PlayerRef _lastHitBy { get; set; }
        [Networked] private NetworkRNG _rng { get; set; }
        [Networked] private TickTimer _launchTimer { get; set; }
        [Networked] private bool _isStopped { get; set; }

        public PlayerRef LastHitBy => _lastHitBy;

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

            if (!ReferenceValidator.Validate(matchRulesConfig, nameof(matchRulesConfig), this)) return;
        }

        public override void Spawned()
        {
            _rb.bodyType = HasStateAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

            if (!HasStateAuthority) return;

            _rng = new NetworkRNG(Runner.Tick.Raw);
            _isStopped = false;
            _rb.position = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _launchTimer = TickTimer.CreateFromSeconds(Runner, matchRulesConfig.CountdownSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (_isStopped)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

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

        public void StopImmediately()
        {
            if (!HasStateAuthority)
                return;

            _isStopped = true;
            _launchTimer = TickTimer.None;
            _rb.linearVelocity = Vector2.zero;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasStateAuthority) return;
            if (other.GetComponent<PowerUp>() != null) return;

            _ballBounce.ReflectY();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!HasStateAuthority) return;

            var player = collision.collider.GetComponent<Player>();
            if (player != null)
                _lastHitBy = player.Object.InputAuthority;
        }

        private void LaunchBall()
        {
            if (_isStopped)
                return;

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
            if (_isStopped)
                return;

            LaunchBall();
        }
    }
}
