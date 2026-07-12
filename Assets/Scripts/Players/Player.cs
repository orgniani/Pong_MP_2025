using System;
using Config;
using Fusion;
using Helpers;
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
        [SerializeField] private PaddleColorPalette paddleColorPalette;
        [SerializeField] private SpriteRenderer viewRenderer;

        [Networked] private int _spawnPointIndex { get; set; }
        [Networked] private int _teamId { get; set; }
        [Networked] private int _laneId { get; set; }
        [Networked] private int _colorId { get; set; } = -1;
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
        public int TeamId => _teamId;
        public int LaneId => _laneId;
        public int ColorId => _colorId;

        public void SetTeamLane(int teamId, int laneId)
        {
            _teamId = teamId;
            _laneId = laneId;
        }

        public void SetColorId(int colorId)
        {
            _colorId = colorId;
        }

        public static event Action OnAnyUsernameChanged;

        private CapsuleCollider2D _capsuleCollider;
        private ChangeDetector _changes;
        private Vector3 _baseLocalScale = Vector3.one;
        private float _displayedSizeMultiplier = 1f;
        private float _sizeAnimationStart = 1f;
        private float _sizeAnimationTarget = 1f;
        private float _sizeAnimationElapsed = float.MaxValue;
        private SpriteRenderer _movementAreaRenderer;
        private bool _isAttachedToSpawnPoint;

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
            ReferenceValidator.ValidateOptional(paddleColorPalette, nameof(paddleColorPalette), this);
            ReferenceValidator.ValidateOptional(viewRenderer, nameof(viewRenderer), this);
        }

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            TryAttachToSpawnPoint();
            RefreshColorVisual();

            if (Object.HasInputAuthority)
                RPC_SetUsername(LocalPlayerSession.Username);
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(_username))
                    OnAnyUsernameChanged?.Invoke();

                if (change == nameof(_colorId))
                    RefreshColorVisual();
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
                float localHalfExtent = GetLocalHalfExtentAlongParentX();
                localPos.x = Mathf.Clamp(localPos.x, GetMinAllowedX(localHalfExtent), GetMaxAllowedX(localHalfExtent));
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
            if (!_isAttachedToSpawnPoint)
                TryAttachToSpawnPoint();

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

        private float GetLocalHalfExtentAlongParentX()
        {
            if (_capsuleCollider == null)
                return 0f;

            Transform referenceSpace = transform.parent != null ? transform.parent : transform;

            Vector3 worldCenter = transform.TransformPoint(_capsuleCollider.offset);
            Vector3 rightAxis = transform.TransformVector(new Vector3(_capsuleCollider.size.x * 0.5f, 0f, 0f));
            Vector3 upAxis = transform.TransformVector(new Vector3(0f, _capsuleCollider.size.y * 0.5f, 0f));

            Vector3 localCenter = referenceSpace.InverseTransformPoint(worldCenter);

            Vector3[] corners =
            {
                referenceSpace.InverseTransformPoint(worldCenter + rightAxis + upAxis),
                referenceSpace.InverseTransformPoint(worldCenter + rightAxis - upAxis),
                referenceSpace.InverseTransformPoint(worldCenter - rightAxis + upAxis),
                referenceSpace.InverseTransformPoint(worldCenter - rightAxis - upAxis)
            };

            float maxExtent = 0f;

            for (int i = 0; i < corners.Length; i++)
                maxExtent = Mathf.Max(maxExtent, Mathf.Abs(corners[i].x - localCenter.x));

            return maxExtent;
        }

        private float GetMinAllowedX(float localHalfExtent)
        {
            if (_movementAreaRenderer != null)
                return (-GetMovementAreaHalfLength()) + localHalfExtent;

            return -arenaBoundX + localHalfExtent;
        }

        private float GetMaxAllowedX(float localHalfExtent)
        {
            if (_movementAreaRenderer != null)
                return GetMovementAreaHalfLength() - localHalfExtent;

            return arenaBoundX - localHalfExtent;
        }

        private float GetMovementAreaHalfLength()
        {
            if (_movementAreaRenderer == null)
                return arenaBoundX;

            return _movementAreaRenderer.size.x * 0.5f;
        }

        private void ResolveMovementAreaRenderer()
        {
            _movementAreaRenderer = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
        }

        private void TryAttachToSpawnPoint()
        {
            if (_isAttachedToSpawnPoint)
                return;

            var manager = NetworkManager.Instance;
            if (manager == null || !manager.TryGetSpawnPoint(_spawnPointIndex, out Transform spawnPoint))
                return;

            transform.SetParent(spawnPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            _isAttachedToSpawnPoint = true;
            _baseLocalScale = transform.localScale;
            ResolveMovementAreaRenderer();
            SnapSize(SizeMultiplier);
            RefreshColorVisual();
        }

        private void RefreshColorVisual()
        {
            if (viewRenderer == null)
                return;

            if (paddleColorPalette == null)
            {
                viewRenderer.color = Color.white;
                return;
            }

            viewRenderer.color = paddleColorPalette.ResolveColor(_colorId);
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
