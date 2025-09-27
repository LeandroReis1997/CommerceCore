using CommerceCore.Domain.Entities;

namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface IProductRepository
    { // Método para GetProductsQueryHandler
        Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca por nome, descrição ou SKU
            Guid? categoryId = null, // Filtro por categoria
            Guid? brandId = null, // Filtro por marca
            bool? isActive = null, // Filtro por status ativo/inativo
            bool? inStock = null, // Filtro por disponibilidade em estoque
            decimal? minPrice = null, // Preço mínimo
            decimal? maxPrice = null, // Preço máximo
            bool includeBrand = false, // Incluir dados da marca
            bool includeCategory = false, // Incluir dados da categoria
            bool includeImages = false, // Incluir imagens do produto
            int maxImagesPerProduct = 5, // Máximo de imagens por produto
            string sortBy = "Name", // Campo para ordenação
            string sortDirection = "ASC", // Direção da ordenação
            CancellationToken cancellationToken = default
        );

        // Método para GetProductByIdQueryHandler
        Task<Product?> GetByIdAsync(
            Guid id, // ID do produto
            bool includeBrand = false, // Incluir dados da marca
            bool includeCategory = false, // Incluir dados da categoria
            bool includeImages = false, // Incluir imagens do produto
            bool includeReviews = false, // Incluir avaliações do produto
            CancellationToken cancellationToken = default
        );

        // Método para GetProductsByCategoryQueryHandler
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByCategoryPagedAsync(
            Guid categoryId, // ID da categoria
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca adicional dentro da categoria
            Guid? brandId = null, // Filtro por marca
            decimal? minPrice = null, // Preço mínimo
            decimal? maxPrice = null, // Preço máximo
            bool? isActive = null, // Filtro por status
            bool? inStock = null, // Filtro por estoque
            bool includeBrand = false, // Incluir dados da marca
            bool includeCategory = false, // Incluir dados da categoria
            bool includeImages = false, // Incluir imagens
            bool includeSubcategories = false, // Incluir produtos de subcategorias
            string sortBy = "Name", // Campo para ordenação
            string sortDirection = "ASC", // Direção da ordenação
            CancellationToken cancellationToken = default
        );

        // Métodos básicos de CRUD (para Commands)
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
        Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        // Métodos utilitários
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsBySkuAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);
    }
}
