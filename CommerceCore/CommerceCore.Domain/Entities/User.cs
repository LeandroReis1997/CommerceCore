using CommerceCore.Domain.Common;
using CommerceCore.Domain.Enums;
using System.Text.RegularExpressions;

namespace CommerceCore.Domain.Entities
{
    public class User : BaseEntity
    {
        #region Constants

        private const int MIN_NAME_LENGTH = 2;
        private const int MAX_NAME_LENGTH = 100;
        private const int MAX_EMAIL_LENGTH = 256;
        private const string EMAIL_PATTERN = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        #endregion

        #region Properties

        // Email do usuário (único no sistema)
        public string Email { get; private set; } = string.Empty;

        // Primeiro nome do usuário
        public string FirstName { get; private set; } = string.Empty;

        // Sobrenome do usuário
        public string LastName { get; private set; } = string.Empty;

        // Hash da senha (nunca armazenar senha em texto puro)
        public string PasswordHash { get; private set; } = string.Empty;

        // Nível de acesso do usuário (Customer, Admin, SuperAdmin)
        public UserRole Role { get; private set; } = UserRole.Customer;

        // Define se usuário está ativo no sistema
        public bool IsActive { get; private set; } = true;

        #endregion

        #region Navigation Properties

        // Endereços do usuário
        public List<Address> Addresses { get; set; } = [];

        // Pedidos realizados pelo usuário
        public List<Order> Orders { get; set; } = [];

        // Carrinho de compras do usuário
        public Cart? Cart { get; set; }

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private User() { }

        // Cria um novo usuário no sistema
        public User(string email, string firstName, string lastName, string passwordHash, UserRole role = UserRole.Customer)
        {
            ValidateUserCreation(email, firstName, lastName, passwordHash); // Valida todos os dados
            SetUserProperties(email, firstName, lastName, passwordHash, role); // Define as propriedades
            Activate(); // Usuário começa ativo
        }

        #endregion

        #region Public Methods

        // Atualiza o email do usuário
        public void UpdateEmail(string email)
        {
            ValidateEmail(email);                    // Valida novo email
            SetEmail(email);                        // Define novo email
            MarkAsUpdated();                        // Marca como alterado
        }

        // Atualiza nome e sobrenome do usuário
        public void UpdateName(string firstName, string lastName)
        {
            ValidateNames(firstName, lastName);      // Valida nomes
            SetNames(firstName, lastName);          // Define novos nomes
            MarkAsUpdated();                        // Marca como alterado
        }

        // Atualiza hash da senha do usuário
        public void UpdatePasswordHash(string passwordHash)
        {
            ValidatePasswordHash(passwordHash);      // Valida hash
            SetPasswordHash(passwordHash);          // Define novo hash
            MarkAsUpdated();                        // Marca como alterado
        }

        // Altera o nível de acesso do usuário
        public void ChangeRole(UserRole role)
        {
            SetRole(role);                          // Define novo role
            MarkAsUpdated();                        // Marca como alterado
        }

        // Ativa usuário no sistema
        public void Activate()
        {
            IsActive = true;
            MarkAsUpdated();
        }

        // Desativa usuário no sistema
        public void Deactivate()
        {
            IsActive = false;
            MarkAsUpdated();
        }

        // Query Methods - Métodos para consultar informações do usuário

        // Retorna nome completo (nome + sobrenome)
        public string GetFullName() => $"{FirstName} {LastName}";

        // Verifica se usuário é administrador (Admin ou SuperAdmin)
        public bool IsAdmin() => Role == UserRole.Admin || Role == UserRole.SuperAdmin;

        // Verifica se usuário é super administrador
        public bool IsSuperAdmin() => Role == UserRole.SuperAdmin;

        // Verifica se usuário é cliente normal
        public bool IsCustomer() => Role == UserRole.Customer;

        // Retorna endereço padrão do usuário
        public Address? GetDefaultAddress() => Addresses.FirstOrDefault(a => a.IsDefault);

        // Verifica se usuário tem endereços cadastrados
        public bool HasAddresses() => Addresses.Count > 0;

        // Verifica se usuário tem pedidos realizados
        public bool HasOrders() => Orders.Count > 0;

        // Retorna quantidade de pedidos do usuário
        public int GetOrdersCount() => Orders.Count;

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define as propriedades do usuário durante criação
        private void SetUserProperties(string email, string firstName, string lastName, string passwordHash, UserRole role)
        {
            SetEmail(email);                        // Define email
            SetNames(firstName, lastName);          // Define nomes
            SetPasswordHash(passwordHash);          // Define hash da senha
            SetRole(role);                         // Define nível de acesso
        }

        // Define o email (normalizado)
        private void SetEmail(string email)
        {
            Email = NormalizeEmail(email);          // Converte para minúsculo e remove espaços
        }

        // Define nome e sobrenome (normalizados)
        private void SetNames(string firstName, string lastName)
        {
            FirstName = NormalizeName(firstName);   // Remove espaços extras
            LastName = NormalizeName(lastName);     // Remove espaços extras
        }

        // Define hash da senha
        private void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        // Define nível de acesso
        private void SetRole(UserRole role)
        {
            Role = role;
        }

        // Marca usuário como atualizado (para auditoria)
        private void MarkAsUpdated() => SetUpdatedAt();

        // Normaliza email (minúsculo e sem espaços)
        private static string NormalizeEmail(string email) => email.ToLowerInvariant().Trim();

        // Normaliza nome (remove espaços extras)
        private static string NormalizeName(string name) => name.Trim();

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida todos os dados durante criação do usuário
        private static void ValidateUserCreation(string email, string firstName, string lastName, string passwordHash)
        {
            ValidateEmail(email);                   // Valida email
            ValidateNames(firstName, lastName);     // Valida nomes
            ValidatePasswordHash(passwordHash);     // Valida hash da senha
        }

        // Valida email
        private static void ValidateEmail(string email)
        {
            ValidateEmailNotEmpty(email);           // Verifica se não está vazio
            ValidateEmailLength(email);             // Verifica tamanho
            ValidateEmailFormat(email);             // Verifica formato
        }

        // Valida se email não está vazio
        private static void ValidateEmailNotEmpty(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email é obrigatório", nameof(email));
        }

        // Valida tamanho do email
        private static void ValidateEmailLength(string email)
        {
            if (email.Length > MAX_EMAIL_LENGTH)
                throw new ArgumentException($"Email não pode exceder {MAX_EMAIL_LENGTH} caracteres", nameof(email));
        }

        // Valida formato do email usando regex
        private static void ValidateEmailFormat(string email)
        {
            if (!Regex.IsMatch(email, EMAIL_PATTERN))
                throw new ArgumentException("Formato de email inválido", nameof(email));
        }

        // Valida nome e sobrenome
        private static void ValidateNames(string firstName, string lastName)
        {
            ValidateName(firstName, nameof(firstName));   // Valida primeiro nome
            ValidateName(lastName, nameof(lastName));     // Valida sobrenome
        }

        // Valida um nome individual
        private static void ValidateName(string name, string paramName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{paramName} é obrigatório", paramName);

            if (name.Trim().Length < MIN_NAME_LENGTH)
                throw new ArgumentException($"{paramName} deve ter pelo menos {MIN_NAME_LENGTH} caracteres", paramName);

            if (name.Trim().Length > MAX_NAME_LENGTH)
                throw new ArgumentException($"{paramName} não pode exceder {MAX_NAME_LENGTH} caracteres", paramName);
        }

        // Valida hash da senha
        private static void ValidatePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Hash da senha é obrigatório", nameof(passwordHash));
        }

        #endregion
    }
}
