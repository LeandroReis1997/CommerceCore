using CommerceCore.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CommerceCore.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SqlConnection _connection;
        private SqlTransaction? _transaction;

        // Campos privados dos repositórios
        private readonly ProductRepository _products;
        private readonly BrandRepository _brands;
        private readonly CategoryRepository _categories;
        private readonly UserRepository _users;
        private readonly OrderRepository _orders;
        private readonly CartRepository _carts;

        // Propriedades da interface
        public IProductRepository Products => _products;
        public IBrandRepository Brands => _brands;
        public ICategoryRepository Categories => _categories;
        public IUserRepository Users => _users;
        public IOrderRepository Orders => _orders;
        public ICartRepository Carts => _carts;

        private bool _disposed = false;

        public UnitOfWork(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = new SqlConnection(connectionString);
            _connection.Open();

            // Instancia os repositórios
            _products = new ProductRepository(connectionString, _connection, _transaction);
            _brands = new BrandRepository(connectionString, _connection, _transaction);
            _categories = new CategoryRepository(connectionString, _connection, _transaction);
            _users = new UserRepository(connectionString, _connection, _transaction);
            _orders = new OrderRepository(connectionString, _connection, _transaction);
            _carts = new CartRepository(connectionString, _connection, _transaction);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                throw new InvalidOperationException("Já existe uma transação ativa.");

            _transaction = _connection.BeginTransaction();
            UpdateRepositoriesTransaction();
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("Nenhuma transação ativa para commit.");

            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
            UpdateRepositoriesTransaction();
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("Nenhuma transação ativa para rollback.");

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
            UpdateRepositoriesTransaction();
            return Task.CompletedTask;
        }

        public Task<bool> HasActiveTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_transaction != null);
        }

        public void DetachAllEntities()
        {
            // Dapper não faz tracking de entidades
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                await CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Atualiza a transação compartilhada em todos os repositórios.
        /// </summary>
        private void UpdateRepositoriesTransaction()
        {
            _products.SetTransaction(_transaction);
            _brands.SetTransaction(_transaction);
            _categories.SetTransaction(_transaction);
            _users.SetTransaction(_transaction);
            _orders.SetTransaction(_transaction);
            _carts.SetTransaction(_transaction);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                if (_connection.State == System.Data.ConnectionState.Open)
                    _connection.Close();
                _connection.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}