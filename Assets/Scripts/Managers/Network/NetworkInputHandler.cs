using Fusion;
using Inputs;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkInputHandler
    {
        //private readonly NetworkPlayerSetup _localPlayer;
        //private float _jumpBufferTimer;
        //private readonly float _jumpBufferDuration = 0.1f;

        //public NetworkInputHandler(NetworkPlayerSetup localPlayer)
        //{
        //    _localPlayer = localPlayer;
        //}

        //public void CollectInput(NetworkInput input)
        //{
        //    if (_localPlayer == null)
        //        return;

        //    NetworkInputData data = new NetworkInputData();

        //    float horizontal = Input.GetAxis("Horizontal");
        //    float vertical = Input.GetAxis("Vertical");
        //    bool sprinting = Input.GetKey(KeyCode.LeftShift);
        //    bool jumpPressed = Input.GetKey(KeyCode.Space);

        //    if (Input.GetKeyDown(KeyCode.Space))
        //        _jumpBufferTimer = _jumpBufferDuration;

        //    if (_jumpBufferTimer > 0f)
        //        _jumpBufferTimer -= Time.deltaTime;

        //    data.LookDirection = _localPlayer.GetNormalizedLookDirection();

        //    if (vertical > 0f)
        //        data.AddInput(NetworkInputType.MoveForward);
        //    else if (vertical < 0f)
        //        data.AddInput(NetworkInputType.MoveBackwards);

        //    if (horizontal < 0f)
        //        data.AddInput(NetworkInputType.MoveLeft);

        //    else if (horizontal > 0f)
        //        data.AddInput(NetworkInputType.MoveRight);

        //    if (sprinting)
        //        data.AddInput(NetworkInputType.Sprint);

        //    if (_jumpBufferTimer > 0f || jumpPressed)
        //        data.AddInput(NetworkInputType.Jump);

        //    input.Set(data);
        //}
    }
}