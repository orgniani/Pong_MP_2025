namespace Config
{
    public interface IPowerUpDefinitionProvider
    {
        bool TryGetDefinition(int definitionId, out PowerUpDefinition definition);
    }
}
