using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    public sealed class PowerUpDefinitionProvider : IPowerUpDefinitionProvider
    {
        private readonly Dictionary<int, PowerUpDefinition> _definitionsById;

        public PowerUpDefinitionProvider(PowerUpDefinition[] definitions, Object source)
        {
            _definitionsById = new Dictionary<int, PowerUpDefinition>();

            var sourceName = source != null ? source.name : "Unknown";

            if (definitions == null)
            {
                Debug.LogError($"[PowerUpDefinitionProvider] '{sourceName}' has no power-up definitions assigned.");
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    Debug.LogError($"[PowerUpDefinitionProvider] '{sourceName}' has a null power-up definition at index {i}.");
                    continue;
                }

                if (_definitionsById.TryGetValue(definition.DefinitionId, out var existingDefinition))
                {
                    Debug.LogError($"[PowerUpDefinitionProvider] '{sourceName}' contains duplicate definitionId {definition.DefinitionId} on '{definition.name}' and '{existingDefinition.name}'. Keeping the later entry.");
                }

                _definitionsById[definition.DefinitionId] = definition;
            }

            if (_definitionsById.Count == 0)
            {
                Debug.LogError($"[PowerUpDefinitionProvider] '{sourceName}' did not provide any valid power-up definitions.");
            }
        }

        public bool TryGetDefinition(int definitionId, out PowerUpDefinition definition)
        {
            if (definitionId < 0)
            {
                definition = null;
                return false;
            }

            return _definitionsById.TryGetValue(definitionId, out definition);
        }
    }
}
