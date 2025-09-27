using CommerceCore.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CommerceCore.Infrastructure.Services
{
    /// <summary>
    /// Implementação de ICacheService usando MemoryCache do .NET.
    /// Suporta operações básicas e avançadas de cache para e-commerce.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Busca valor no cache por chave
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            _cache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        // Define valor no cache com expiração opcional
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }

        // Remove item do cache por chave
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        // Verifica se chave existe no cache
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cache.TryGetValue(key, out _));
        }

        // Busca no cache ou executa factory se não existir
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            if (_cache.TryGetValue(key, out T? value) && value != null)
                return value;

            value = await factory();
            await SetAsync(key, value, expiration, cancellationToken);
            return value;
        }

        // Remove múltiplas chaves por padrão (regex simples)
        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            // MemoryCache não expõe todas as chaves, então este método é apenas ilustrativo.
            // Para produção, use um cache distribuído (Redis) ou mantenha um índice de chaves.
            return Task.CompletedTask;
        }

        // Limpa todo o cache (MemoryCache não tem método direto, mas pode ser recriado)
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            // MemoryCache não tem método direto para limpar, normalmente se recria a instância.
            return Task.CompletedTask;
        }

        // Invalida cache relacionado a um produto
        public Task InvalidateProductCacheAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _cache.Remove($"product:{productId}");
            return Task.CompletedTask;
        }

        // Invalida cache relacionado a uma categoria
        public Task InvalidateCategoryCacheAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            _cache.Remove($"category:{categoryId}");
            return Task.CompletedTask;
        }

        // Invalida cache relacionado a um usuário
        public Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _cache.Remove($"user:{userId}");
            return Task.CompletedTask;
        }
    }
}