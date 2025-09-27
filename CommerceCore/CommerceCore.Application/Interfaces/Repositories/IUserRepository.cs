using CommerceCore.Domain.Entities;

namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        // Busca usuários paginados com filtros e includes opcionais
        Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca por nome, email ou CPF
            bool? isActive = null, // Filtro por status ativo/inativo
            DateTime? createdAfter = null, // Usuários criados após esta data
            DateTime? createdBefore = null, // Usuários criados antes desta data
            bool includeProfile = false, // Incluir dados completos do perfil
            bool includeOrderHistory = false, // Incluir histórico de pedidos
            bool includeOrderCount = false, // Incluir contagem de pedidos
            bool includeLastLogin = false, // Incluir informações de último login
            CancellationToken cancellationToken = default
        );

        // Busca usuário por ID com includes condicionais
        Task<User?> GetByIdAsync(
            Guid id, // ID do usuário
            bool includeProfile = false, // Incluir dados completos do perfil (endereços, telefones)
            bool includeOrders = false, // Incluir pedidos do usuário
            bool includeOrderDetails = false, // Incluir detalhes completos dos pedidos
            bool includeCart = false, // Incluir carrinho ativo do usuário
            bool includeAddresses = false, // Incluir endereços cadastrados
            CancellationToken cancellationToken = default
        );

        // Busca usuário por email (método essencial para login e validações)
        Task<User?> GetByEmailAsync(
            string email, // Email do usuário
            bool includeProfile = false, // Incluir dados do perfil
            bool includeOrderCount = false, // Incluir contagem de pedidos
            CancellationToken cancellationToken = default
        );

        // Busca usuário por CPF (para validações de duplicidade)
        Task<User?> GetByCpfAsync(
            string cpf, // CPF do usuário
            bool includeProfile = false, // Incluir dados do perfil
            CancellationToken cancellationToken = default
        );

        // Busca usuários por lista de IDs (útil para relatórios e operações em lote)
        Task<IEnumerable<User>> GetByIdsAsync(
            IEnumerable<Guid> ids, // Lista de IDs de usuários
            bool includeProfile = false, // Incluir dados do perfil
            CancellationToken cancellationToken = default
        );

        // Métodos básicos de CRUD para Commands
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); // Busca simples por ID
        Task<User> AddAsync(User user, CancellationToken cancellationToken = default); // Adiciona novo usuário
        Task UpdateAsync(User user, CancellationToken cancellationToken = default); // Atualiza usuário existente
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default); // Remove usuário por ID (soft delete)

        // Métodos utilitários e validações
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default); // Verifica se usuário existe
        Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default); // Verifica email único
        Task<bool> ExistsByCpfAsync(string cpf, Guid? excludeId = null, CancellationToken cancellationToken = default); // Verifica CPF único
        Task<int> GetOrderCountAsync(Guid userId, CancellationToken cancellationToken = default); // Conta pedidos do usuário
        Task<DateTime?> GetLastLoginAsync(Guid userId, CancellationToken cancellationToken = default); // Obtém último login
        Task UpdateLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default); // Atualiza último login

        // Métodos de segurança e auditoria
        Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default); // Verifica se conta está bloqueada
        Task LockAccountAsync(Guid userId, DateTime? lockUntil = null, CancellationToken cancellationToken = default); // Bloqueia conta
        Task UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default); // Desbloqueia conta
        Task<int> GetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default); // Conta tentativas de login falhadas
        Task IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default); // Incrementa tentativas falhadas
        Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default); // Reseta contador de tentativas
    }
}
