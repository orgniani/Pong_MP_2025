using UnityEngine;

namespace Helpers
{
    public class ReferenceValidator
    {
        public static bool Validate(Object reference, string referenceName, MonoBehaviour context)
        {
            if (reference != null) return true;

            Debug.LogError($"{context.name}: {referenceName} is null!\nDisabling component to avoid errors.");
            context.enabled = false;
            return false;
        }
    }
}