using CommerceCore.Domain.Common;
using System.Text.RegularExpressions;

namespace CommerceCore.Domain.Entities
{
    public class Brand : BaseEntity
    {
        #region Constants

        private const int MIN_NAME_LENGTH = 2;
        private const int MAX_NAME_LENGTH = 100;

        #endregion

        #region Properties

        public string Name { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        #endregion

        #region Navigation Properties

        public List<Product> Products { get; set; } = [];

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private Brand() { }

        public Brand(string name)
        {
            ValidateName(name);
            Name = name;
            IsActive = true;
        }

        #endregion

        #region Public Methods

        public void UpdateName(string name)
        {
            ValidateName(name);
            Name = name;
            SetUpdatedAt();
        }

        public void Activate()
        {
            IsActive = true;
            SetUpdatedAt();
        }

        public void Deactivate()
        {
            if (HasActiveProducts())
                throw new InvalidOperationException("Não é possível desativar marca que possui produtos ativos");

            IsActive = false;
            SetUpdatedAt();
        }

        public bool HasActiveProducts() => Products.Any(p => p.IsActive);

        #endregion

        #region Private Validation Methods

        private static void ValidateName(string name)
        {
            ValidateNameBasics(name);
            ValidateNameFormat(name);
        }

        private static void ValidateNameBasics(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("O nome da marca não pode ser vazio", nameof(name));

            if (name.Length < MIN_NAME_LENGTH)
                throw new ArgumentException($"O nome da marca deve ter pelo menos {MIN_NAME_LENGTH} caracteres", nameof(name));

            if (name.Length > MAX_NAME_LENGTH)
                throw new ArgumentException($"O nome da marca não pode exceder {MAX_NAME_LENGTH} caracteres", nameof(name));
        }

        private static void ValidateNameFormat(string name)
        {
            // Marcas podem ter números e & (ex: "3M", "H&M")
            if (!Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ0-9\s\-&]+$"))
                throw new ArgumentException("O nome da marca pode conter apenas letras, números, espaços, hífens e &", nameof(name));

            // Não pode começar ou terminar com espaços
            if (name.Trim() != name)
                throw new ArgumentException("O nome da marca não pode começar ou terminar com espaços", nameof(name));
        }

        #endregion
    }
}
