namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        // Repositories - acesso centralizado a todos os repositories
        IProductRepository Products { get; } // Repository de produtos
        IBrandRepository Brands { get; } // Repository de marcas
        ICategoryRepository Categories { get; } // Repository de categorias
        IUserRepository Users { get; } // Repository de usuários
        IOrderRepository Orders { get; } // Repository de pedidos
        ICartRepository Carts { get; } // Repository de carrinhos

        // Métodos de transação
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); // Salva todas as mudanças pendentes no contexto
        Task BeginTransactionAsync(CancellationToken cancellationToken = default); // Inicia uma nova transação
        Task CommitTransactionAsync(CancellationToken cancellationToken = default); // Confirma a transação atual
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default); // Desfaz a transação atual

        // Métodos utilitários
        Task<bool> HasActiveTransactionAsync(CancellationToken cancellationToken = default); // Verifica se existe transação ativa
        void DetachAllEntities(); // Remove todas as entidades do contexto (útil para testes)
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default); // Executa operação dentro de transação
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default); // Executa operação sem retorno dentro de transação
    }
}