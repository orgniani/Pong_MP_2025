namespace Config
{
    public interface ISceneCatalogProvider
    {
        bool TryGetCatalog(out SceneIndexCatalog catalog);
    }
}
