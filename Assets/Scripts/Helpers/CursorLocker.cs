using UnityEngine;

namespace Helpers
{
    public static class CursorLocker
    {
        private static bool _isLocked = true;

        public static void SetLockState(bool isLocked)
        {
            _isLocked = isLocked;

            if (_isLocked)
                Lock();
            else
                Unlock();
        }

        public static void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void Toggle()
        {
            SetLockState(!_isLocked);
        }

        public static bool IsLocked => _isLocked;
    }
}