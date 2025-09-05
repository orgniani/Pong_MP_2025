using UnityEngine;
using Fusion;

namespace Inputs
{
    public struct NetworkInputData : INetworkInput
    {
        private byte _buttonsPressed;

        public Vector3 LookDirection { get; set; }


        public void AddInput (NetworkInputType inputType)
        {
            byte flag = (byte)(1 << (int)inputType);
            _buttonsPressed |= flag;
        }

        public readonly bool IsInputDown (NetworkInputType inputType)
        {
            byte flag = (byte)(1 << (int)inputType);
            return (_buttonsPressed & flag) != 0;
        }
    }
}