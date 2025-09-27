using CommerceCore.Domain.Entities;

namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface ICartRepository
    {
        // Busca carrinho ativo de um usuário específico
        Task<Cart?> GetByUserIdAsync(
            Guid userId, // ID do usuário
            bool includeItems = true, // Incluir itens do carrinho
            bool includeItemDetails = false, // Incluir detalhes completos dos produtos dos itens
            bool includeProductImages = false, // Incluir imagens dos produtos
            bool includeBrandInfo = false, // Incluir informações das marcas dos produtos
            bool includeAvailability = false, // Verificar disponibilidade/estoque dos produtos
            bool calculateTotals = true, // Calcular subtotal, desconto e total do carrinho
            CancellationToken cancellationToken = default
        );

        // Busca carrinhos ativos no sistema (para admin/relatórios)
        Task<(IEnumerable<Cart> Carts, int TotalCount)> GetActiveCartsPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            DateTime? updatedAfter = null, // Carrinhos atualizados após esta data
            DateTime? updatedBefore = null, // Carrinhos atualizados antes desta data
            bool hasItems = true, // Filtrar apenas carrinhos com itens
            decimal? minCartValue = null, // Valor mínimo do carrinho
            bool includeUser = false, // Incluir dados do usuário dono do carrinho
            bool includeItemCount = true, // Incluir quantidade total de itens no carrinho
            bool calculateTotals = false, // Calcular totais de cada carrinho
            CancellationToken cancellationToken = default
        );

        // Busca carrinho por ID específico
        Task<Cart?> GetByIdAsync(
            Guid id, // ID do carrinho
            bool includeItems = true, // Incluir itens do carrinho
            bool includeUser = false, // Incluir dados do usuário
            CancellationToken cancellationToken = default
        );

        // Busca carrinhos abandonados (para remarketing)
        Task<IEnumerable<Cart>> GetAbandonedCartsAsync(
            int daysAgo = 7, // Carrinhos não atualizados há X dias
            decimal? minValue = null, // Valor mínimo para considerar relevante
            bool includeUser = true, // Incluir dados do usuário para contato
            bool includeItems = true, // Incluir itens para análise
            int? limit = null, // Limite de resultados
            CancellationToken cancellationToken = default
        );

        // Métodos básicos de CRUD para Commands
        Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); // Busca simples por ID
        Task<Cart> AddAsync(Cart cart, CancellationToken cancellationToken = default); // Adiciona novo carrinho
        Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default); // Atualiza carrinho existente
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default); // Remove carrinho por ID

        // Métodos de gestão de itens do carrinho
        Task<CartItem?> GetCartItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default); // Busca item específico no carrinho
        Task<CartItem> AddItemAsync(CartItem cartItem, CancellationToken cancellationToken = default); // Adiciona item ao carrinho
        Task UpdateItemAsync(CartItem cartItem, CancellationToken cancellationToken = default); // Atualiza quantidade/preço do item
        Task RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default); // Remove item do carrinho
        Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default); // Remove todos os itens do carrinho

        // Métodos utilitários
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default); // Verifica se carrinho existe
        Task<bool> UserHasActiveCartAsync(Guid userId, CancellationToken cancellationToken = default); // Verifica se usuário já tem carrinho ativo
        Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken cancellationToken = default); // Busca ou cria carrinho para usuário
        Task<bool> BelongsToUserAsync(Guid cartId, Guid userId, CancellationToken cancellationToken = default); // Verifica se carrinho pertence ao usuário (autorização)

        // Métodos de cálculos e validações
        Task<decimal> CalculateCartTotalAsync(Guid cartId, CancellationToken cancellationToken = default); // Calcula total do carrinho
        Task<int> GetItemCountAsync(Guid cartId, CancellationToken cancellationToken = default); // Conta itens no carrinho
        Task<bool> IsCartValidAsync(Guid cartId, CancellationToken cancellationToken = default); // Valida se todos os itens estão disponíveis
        Task UpdateItemPricesAsync(Guid cartId, CancellationToken cancellationToken = default); // Atualiza preços dos itens com valores atuais

        // Métodos de limpeza e manutenção
        Task DeleteExpiredCartsAsync(int daysOld = 30, CancellationToken cancellationToken = default); // Remove carrinhos muito antigos
        Task DeleteEmptyCartsAsync(int daysOld = 7, CancellationToken cancellationToken = default); // Remove carrinhos vazios antigos

        // Métodos de conversão
        Task<Order?> ConvertToOrderAsync(Guid cartId, CancellationToken cancellationToken = default); // Converte carrinho em pedido (checkout)
        Task MergeCartsAsync(Guid sourceCartId, Guid targetCartId, CancellationToken cancellationToken = default); // Mescla dois carrinhos (usuário logou)
    }
}