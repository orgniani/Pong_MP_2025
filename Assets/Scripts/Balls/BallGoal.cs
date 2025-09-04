using System;
using UnityEngine;
using UnityEngine.Events;

namespace Balls
{
    public class BallGoal
    {
        private Rigidbody2D _rb;

        private UnityEvent _onLeftGoal;
        private UnityEvent _onRightGoal;
        private Action<bool> _onReset;

        public void Initialize(
            Rigidbody2D rb,
            UnityEvent left,
            UnityEvent right,
            Action<bool> onReset)
        {
            _rb = rb;
            _onLeftGoal = left;
            _onRightGoal = right;
            _onReset = onReset;
        }

        public void Tick()
        {
            if (!Camera.main) return;

            Vector3 viewportPos = Camera.main.WorldToViewportPoint(_rb.position);

            if (viewportPos.x < 0f - 0.05f)
            {
                _onLeftGoal?.Invoke();
                _onReset(false);
            }
            else if (viewportPos.x > 1f + 0.05f)
            {
                _onRightGoal?.Invoke();
                _onReset(true);
            }
        }
    }
}