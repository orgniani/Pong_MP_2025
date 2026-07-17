using System;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "PowerUpDefinition", menuName = "Config/Power Up Definition")]
    public sealed class PowerUpDefinition : ScriptableObject
    {
        [SerializeField] private int definitionId;
        [SerializeField] private PowerUpEffectKind effectKind = PowerUpEffectKind.PaddleSizeBoostSelf;
        [SerializeField] private Sprite sprite;
        [SerializeField] private float magnitude = 1f;
        [SerializeField] private float duration = 5f;

        public int DefinitionId => Mathf.Max(0, definitionId);
        public PowerUpEffectKind EffectKind => effectKind;
        public Sprite Sprite => sprite;
        public float Magnitude => Mathf.Max(0.01f, magnitude);
        public float Duration => Mathf.Max(0.01f, duration);

        private void OnValidate()
        {
            definitionId = Mathf.Max(0, definitionId);
            magnitude = Mathf.Max(0.01f, magnitude);
            duration = Mathf.Max(0.01f, duration);
        }
    }
}
