namespace CommerceCore.Application.Interfaces.Services
{
    public interface ICacheService
    {
        // Métodos básicos de cache
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class; // Busca valor no cache por chave
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class; // Define valor no cache com expiração opcional
        Task RemoveAsync(string key, CancellationToken cancellationToken = default); // Remove item do cache por chave
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default); // Verifica se chave existe no cache

        // Métodos avançados
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class; // Busca no cache ou executa factory se não existir
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default); // Remove múltiplas chaves por padrão
        Task ClearAsync(CancellationToken cancellationToken = default); // Limpa todo o cache

        // Métodos específicos para e-commerce
        Task InvalidateProductCacheAsync(Guid productId, CancellationToken cancellationToken = default); // Invalida cache relacionado a um produto
        Task InvalidateCategoryCacheAsync(Guid categoryId, CancellationToken cancellationToken = default); // Invalida cache relacionado a uma categoria
        Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default); // Invalida cache relacionado a um usuário
    }
}