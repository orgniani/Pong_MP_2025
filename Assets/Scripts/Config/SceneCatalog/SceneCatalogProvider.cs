namespace Config
{
    public sealed class SceneCatalogProvider : ISceneCatalogProvider
    {
        private readonly SceneIndexCatalog _catalog;

        public SceneCatalogProvider(SceneIndexCatalog catalog)
        {
            _catalog = catalog;
        }

        public bool TryGetCatalog(out SceneIndexCatalog catalog)
        {
            catalog = _catalog;
            return catalog != null;
        }
    }
}
