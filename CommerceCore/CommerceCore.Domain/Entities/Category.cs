using CommerceCore.Domain.Common;
using System.Text.RegularExpressions;

namespace CommerceCore.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public Guid? ParentId { get; private set; }
        public bool IsActive { get; private set; } = true;

        // Navigation Properties
        public Category? Parent { get; set; }
        public List<Category> Children { get; set; } = [];
        public List<Product> Products { get; set; } = [];

        // Constructors
        private Category() { } // Construtor vazio para ORMs (Dapper) e serialização JSON

        public Category(string name, Guid? parentId = null)
        {
            ValidateName(name);
            Name = name;
            ParentId = parentId;
            IsActive = true;
        }

        // Business Methods
        public void UpdateName(string name)
        {
            ValidateName(name);
            Name = name;
            SetUpdatedAt();
        }

        public void SetParent(Guid? parentId)
        {
            ParentId = parentId;
            SetUpdatedAt();
        }

        public void Activate()
        {
            IsActive = true;
            SetUpdatedAt();
        }

        public void Deactivate()
        {
            if (HasChildren())
                throw new InvalidOperationException("Não é possível desativar categoria que possui subcategorias");

            if (Products.Any(p => p.IsActive))
                throw new InvalidOperationException("Não é possível desativar categoria que possui produtos ativos");

            IsActive = false;
            SetUpdatedAt();
        }

        public bool HasChildren() => Children.Count > 0;
        public bool IsSubCategory() => ParentId.HasValue;
        public bool HasActiveProducts() => Products.Any(p => p.IsActive);

        // Private Validation Methods
        private static void ValidateName(string name)
        {
            // 1. Não pode ser vazio
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("O nome da categoria não pode ser vazio", nameof(name));

            // 2. Tamanho mínimo e máximo
            if (name.Length < 2)
                throw new ArgumentException("O nome da categoria deve ter pelo menos 2 caracteres", nameof(name));

            if (name.Length > 100)
                throw new ArgumentException("O nome da categoria não pode exceder 100 caracteres", nameof(name));

            // 3. Não pode ter números
            if (Regex.IsMatch(name, @"\d"))
                throw new ArgumentException("O nome da categoria não pode conter números", nameof(name));

            // 4. Não pode ter caracteres especiais (só letras, espaços e hífen)
            if (!Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ\s\-]+$"))
                throw new ArgumentException("O nome da categoria pode conter apenas letras, espaços e hífens", nameof(name));

            // 5. Não pode começar ou terminar com espaço
            if (name.Trim() != name)
                throw new ArgumentException("O nome da categoria não pode começar ou terminar com espaços", nameof(name));
        }
    }
}
