using System;
using Fusion;
using Managers;
using UnityEngine;

namespace Players
{
    [RequireComponent(typeof(Collider2D))]
    public class Player : NetworkBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float arenaBoundX = 4f;
        [SerializeField] private float sizeAnimationDuration = 0.2f;
        [SerializeField] private AnimationCurve sizeAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Networked] private int _spawnPointIndex { get; set; }
        [Networked] private NetworkString<_16> _username { get; set; }
        [Networked] private float _sizeMultiplier { get; set; } = 1f;
        [Networked] private float _sizeEffectTimer { get; set; } = 0f;

        public int SpawnPointIndex
        {
            get => _spawnPointIndex;
            set => _spawnPointIndex = value;
        }

        public string Username => _username.ToString();
        public float SizeMultiplier => _sizeMultiplier;

        public static event Action OnAnyUsernameChanged;

        private CapsuleCollider2D _capsuleCollider;
        private ChangeDetector _changes;
        private Vector3 _baseLocalScale = Vector3.one;
        private float _displayedSizeMultiplier = 1f;
        private float _sizeAnimationStart = 1f;
        private float _sizeAnimationTarget = 1f;
        private float _sizeAnimationElapsed = float.MaxValue;

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            Transform spawnPoint = NetworkManager.Instance.GetSpawnPoint(_spawnPointIndex);
            if (spawnPoint != null)
            {
                transform.SetParent(spawnPoint, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            _baseLocalScale = transform.localScale;
            SnapSize(SizeMultiplier);

            if (Object.HasInputAuthority)
                RPC_SetUsername(LocalPlayerSession.Username);
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(_username))
                    OnAnyUsernameChanged?.Invoke();
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetUsername(NetworkString<_16> username)
        {
            _username = username;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (GetInput<PlayerInputData>(out var input))
            {
                Vector3 localPos = transform.localPosition;
                localPos.x -= input.MoveY * speed * Runner.DeltaTime;
                float horizontalHalfExtent = GetHorizontalHalfExtent();
                localPos.x = Mathf.Clamp(localPos.x, -arenaBoundX + horizontalHalfExtent, arenaBoundX - horizontalHalfExtent);
                transform.localPosition = localPos;
            }

            if (_sizeEffectTimer > 0f)
            {
                _sizeEffectTimer -= Runner.DeltaTime;
                if (_sizeEffectTimer <= 0f)
                {
                    _sizeEffectTimer = 0f;
                    _sizeMultiplier = 1f;
                }
            }
        }

        private void Update()
        {
            if (!Mathf.Approximately(_sizeAnimationTarget, SizeMultiplier))
                StartSizeAnimation(SizeMultiplier);

            UpdateSizeAnimation();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                RPC_DebugApplySizeBoost();
            }
#endif
        }

        public void ApplySizeBoost(float multiplier, float duration)
        {
            if (!Object.HasStateAuthority) return;

            _sizeMultiplier = multiplier;
            _sizeEffectTimer = duration;
        }

        private void StartSizeAnimation(float targetMultiplier)
        {
            _sizeAnimationStart = _displayedSizeMultiplier;
            _sizeAnimationTarget = targetMultiplier;
            _sizeAnimationElapsed = 0f;

            if (sizeAnimationDuration <= 0f)
                SnapSize(targetMultiplier);
        }

        private void UpdateSizeAnimation()
        {
            if (_sizeAnimationElapsed >= sizeAnimationDuration)
            {
                ApplySize(_sizeAnimationTarget);
                return;
            }

            _sizeAnimationElapsed += Time.deltaTime;
            float normalizedTime = sizeAnimationDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_sizeAnimationElapsed / sizeAnimationDuration);
            float curveValue = sizeAnimationCurve != null
                ? sizeAnimationCurve.Evaluate(normalizedTime)
                : normalizedTime;

            float currentMultiplier = Mathf.LerpUnclamped(_sizeAnimationStart, _sizeAnimationTarget, curveValue);
            ApplySize(currentMultiplier);
        }

        private void SnapSize(float multiplier)
        {
            _sizeAnimationStart = multiplier;
            _sizeAnimationTarget = multiplier;
            _sizeAnimationElapsed = sizeAnimationDuration;
            ApplySize(multiplier);
        }

        private void ApplySize(float multiplier)
        {
            _displayedSizeMultiplier = multiplier;

            Vector3 scaledLocalScale = _baseLocalScale;
            scaledLocalScale.x *= multiplier;

            transform.localScale = scaledLocalScale;
        }

        private float GetHorizontalHalfExtent()
        {
            if (_capsuleCollider == null)
                return 0f;

            Vector2 worldCenter = transform.TransformPoint(_capsuleCollider.offset);
            Vector2 rightAxis = transform.TransformVector(new Vector3(_capsuleCollider.size.x * 0.5f, 0f, 0f));
            Vector2 upAxis = transform.TransformVector(new Vector3(0f, _capsuleCollider.size.y * 0.5f, 0f));

            float maxExtent = 0f;

            maxExtent = Mathf.Max(maxExtent, Mathf.Abs((worldCenter + rightAxis + upAxis).x - transform.position.x));
            maxExtent = Mathf.Max(maxExtent, Mathf.Abs((worldCenter + rightAxis - upAxis).x - transform.position.x));
            maxExtent = Mathf.Max(maxExtent, Mathf.Abs((worldCenter - rightAxis + upAxis).x - transform.position.x));
            maxExtent = Mathf.Max(maxExtent, Mathf.Abs((worldCenter - rightAxis - upAxis).x - transform.position.x));

            return maxExtent;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_DebugApplySizeBoost()
        {
            ApplySizeBoost(1.5f, 5f);
        }
#endif
    }
}
