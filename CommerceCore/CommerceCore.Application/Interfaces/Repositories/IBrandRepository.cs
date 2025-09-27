using CommerceCore.Domain.Entities;

namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface IBrandRepository
    {
        // Método para GetBrandsQueryHandler
        Task<(IEnumerable<Brand> Brands, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca por nome da marca
            bool? isActive = null, // Filtro por status ativo/inativo
            bool includeProductCount = false, // Incluir contagem de produtos da marca
            bool includeStatistics = false, // Incluir estatísticas da marca
            bool includeProducts = false, // Incluir produtos da marca
            CancellationToken cancellationToken = default
        );

        // Método para GetBrandByIdQueryHandler
        Task<Brand?> GetByIdAsync(
            Guid id, // ID da marca
            bool includeProducts = false, // Incluir produtos da marca
            bool includeStatistics = false, // Incluir estatísticas (total vendas, etc.)
            bool includeProductDetails = false, // Incluir detalhes completos dos produtos
            CancellationToken cancellationToken = default
        );

        // Métodos básicos de CRUD (para Commands)
        Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Brand> AddAsync(Brand brand, CancellationToken cancellationToken = default);
        Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        // Métodos utilitários
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> GetProductCountAsync(Guid brandId, CancellationToken cancellationToken = default);
    }
}
