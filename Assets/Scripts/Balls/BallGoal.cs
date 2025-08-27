using System;
using UnityEngine;
using UnityEngine.Events;

namespace Balls
{
    public class BallGoal
    {
        private Rigidbody2D _rb;
        private Collider2D _playArea;

        private float _halfWidth;

        private UnityEvent _onLeftGoal;
        private UnityEvent _onRightGoal;

        private Action<bool> _onReset;

        public void Initialize(
            Rigidbody2D rb,
            Collider2D playArea,
            float halfWidth,
            UnityEvent left,
            UnityEvent right,
            Action<bool> onReset)
        {
            _rb = rb;
            _playArea = playArea;
            _halfWidth = halfWidth;
            _onLeftGoal = left;
            _onRightGoal = right;
            _onReset = onReset;
        }

        public void Tick()
        {
            Bounds b = _playArea.bounds;
            Vector3 pos = _rb.position;

            if (pos.x <= b.min.x + _halfWidth)
            {
                _onLeftGoal?.Invoke();
                _onReset(false);
            }

            if (pos.x >= b.max.x - _halfWidth)
            {
                _onRightGoal?.Invoke();
                _onReset(true);
            }
        }

    }
}
