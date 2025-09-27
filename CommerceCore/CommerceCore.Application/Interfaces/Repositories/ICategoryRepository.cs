using CommerceCore.Domain.Entities;

namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface ICategoryRepository
    {
        // Busca categorias paginadas com filtros e includes opcionais
        Task<(IEnumerable<Category> Categories, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca por nome da categoria
            bool? isActive = null, // Filtro por status ativo/inativo
            Guid? parentCategoryId = null, // Filtro por categoria pai (null = categorias raiz)
            bool includeSubcategories = false, // Incluir subcategorias filhas
            bool includeProductCount = false, // Incluir contagem de produtos na categoria
            bool includeProducts = false, // Incluir produtos da categoria
            bool includeHierarchy = false, // Incluir caminho hierárquico completo
            CancellationToken cancellationToken = default
        );

        // Busca categoria por ID com includes condicionais
        Task<Category?> GetByIdAsync(
            Guid id, // ID da categoria
            bool includeParent = false, // Incluir categoria pai
            bool includeSubcategories = false, // Incluir subcategorias filhas
            bool includeProducts = false, // Incluir produtos da categoria
            bool includeProductDetails = false, // Incluir detalhes completos dos produtos
            bool includeHierarchy = false, // Incluir caminho hierárquico completo
            CancellationToken cancellationToken = default
        );

        // Busca hierarquia completa de categorias em formato de árvore
        Task<IEnumerable<Category>> GetHierarchyAsync(
            Guid? rootCategoryId = null, // Categoria raiz (null = desde o topo)
            bool? isActive = true, // Filtro por status ativo/inativo
            bool includeProductCount = true, // Incluir contagem de produtos em cada categoria
            int? maxDepth = null, // Máximo de níveis de profundidade (null = sem limite)
            CancellationToken cancellationToken = default
        );

        // Busca todas as subcategorias de uma categoria (recursivo)
        Task<IEnumerable<Category>> GetSubcategoriesAsync(
            Guid parentId, // ID da categoria pai
            bool includeInactive = false, // Incluir subcategorias inativas
            bool recursive = true, // Buscar recursivamente todos os níveis
            CancellationToken cancellationToken = default
        );

        // Busca o caminho hierárquico completo de uma categoria (breadcrumb)
        Task<IEnumerable<Category>> GetCategoryPathAsync(
            Guid categoryId, // ID da categoria
            CancellationToken cancellationToken = default
        );

        // Métodos básicos de CRUD para Commands
        Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); // Busca simples por ID
        Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default); // Adiciona nova categoria
        Task UpdateAsync(Category category, CancellationToken cancellationToken = default); // Atualiza categoria existente
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default); // Remove categoria por ID

        // Métodos utilitários
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default); // Verifica se categoria existe
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default); // Verifica nome único
        Task<int> GetProductCountAsync(Guid categoryId, bool includeSubcategories = false, CancellationToken cancellationToken = default); // Conta produtos na categoria
        Task<bool> HasSubcategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default); // Verifica se tem subcategorias
        Task<bool> IsValidParentAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default); // Valida hierarquia (evita loops)
        Task<int> GetDepthAsync(Guid categoryId, CancellationToken cancellationToken = default); // Calcula profundidade na hierarquia
    }
}
