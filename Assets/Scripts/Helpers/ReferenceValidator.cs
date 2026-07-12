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

        public static bool ValidateOptional(Object reference, string referenceName, MonoBehaviour context)
        {
            if (reference != null) return true;

            Debug.LogWarning($"{context.name}: {referenceName} is not assigned.", context);
            return false;
        }

        public static bool ValidateOptional(Object[] references, string referenceName, MonoBehaviour context)
        {
            if (references != null && references.Length > 0) return true;

            Debug.LogWarning($"{context.name}: {referenceName} is not assigned.", context);
            return false;
        }
    }
}