using UnityEngine;

namespace Balls
{
    public enum GoalResult { None, LeftGoal, RightGoal }

    public class BallGoal
    {
        private Rigidbody2D _rb;
        private float _leftBoundX;
        private float _rightBoundX;

        public void Initialize(Rigidbody2D rb, float leftBoundX, float rightBoundX)
        {
            _rb = rb;
            _leftBoundX = leftBoundX;
            _rightBoundX = rightBoundX;
        }

        public GoalResult Tick()
        {
            float x = _rb.position.x;
            if (x < _leftBoundX)
                return GoalResult.LeftGoal;
            if (x > _rightBoundX)
                return GoalResult.RightGoal;
            return GoalResult.None;
        }
    }
}
