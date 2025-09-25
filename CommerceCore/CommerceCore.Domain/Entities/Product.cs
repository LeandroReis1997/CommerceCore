using CommerceCore.Domain.Common;
using System.Text.RegularExpressions;

namespace CommerceCore.Domain.Entities
{
    public class Product : BaseEntity
    {
        #region Constants

        private const int MIN_NAME_LENGTH = 2;
        private const int MAX_NAME_LENGTH = 200;
        private const int MAX_DESCRIPTION_LENGTH = 2000;
        private const decimal MIN_PRICE = 0.01m;
        private const decimal MAX_PRICE = 999999.99m;
        private const int MIN_INVENTORY = 0;
        private const int MAX_INVENTORY = 999999;

        #endregion

        #region Properties

        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int Inventory { get; private set; }
        public Guid CategoryId { get; private set; }
        public Guid? BrandId { get; private set; }
        public bool IsActive { get; private set; } = true;

        #endregion

        #region Navigation Properties

        public Category Category { get; set; } = null!;
        public Brand? Brand { get; set; }
        public List<ProductImage> Images { get; set; } = [];
        public List<CartItem> CartItems { get; set; } = [];
        public List<OrderItem> OrderItems { get; set; } = [];

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private Product() { }

        public Product(string name, string description, decimal price, int inventory, Guid categoryId, Guid? brandId = null)
        {
            ValidateProductData(name, description, price, inventory);
            ValidateRelatedEntities(categoryId);

            Name = name;
            Description = description;
            Price = price;
            Inventory = inventory;
            CategoryId = categoryId;
            BrandId = brandId;
            IsActive = true;
        }

        #endregion

        #region Public Methods

        public void UpdateBasicInfo(string name, string description)
        {
            ValidateName(name);
            ValidateDescription(description);

            Name = name;
            Description = description;
            SetUpdatedAt();
        }

        public void UpdatePrice(decimal price)
        {
            ValidatePrice(price);
            Price = price;
            SetUpdatedAt();
        }

        public void AddStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero", nameof(quantity));

            var newInventory = Inventory + quantity;
            ValidateInventory(newInventory);

            Inventory = newInventory;
            SetUpdatedAt();
        }

        public void RemoveStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero", nameof(quantity));

            if (quantity > Inventory)
                throw new InvalidOperationException("Não há estoque suficiente para remover");

            Inventory -= quantity;
            SetUpdatedAt();
        }

        public void SetStock(int inventory)
        {
            ValidateInventory(inventory);
            Inventory = inventory;
            SetUpdatedAt();
        }

        public void ChangeCategory(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
                throw new ArgumentException("CategoryId não pode ser vazio", nameof(categoryId));

            CategoryId = categoryId;
            SetUpdatedAt();
        }

        public void ChangeBrand(Guid? brandId)
        {
            BrandId = brandId;
            SetUpdatedAt();
        }

        public void Activate()
        {
            IsActive = true;
            SetUpdatedAt();
        }

        public void Deactivate()
        {
            IsActive = false;
            SetUpdatedAt();
        }

        public bool IsInStock() => Inventory > 0;
        public bool IsOutOfStock() => Inventory <= 0;
        public bool HasBrand() => BrandId.HasValue;
        public bool HasImages() => Images.Count > 0;
        public ProductImage? GetMainImage() => Images.FirstOrDefault(i => i.IsMain);

        #endregion

        #region Private Validation Methods

        private static void ValidateProductData(string name, string description, decimal price, int inventory)
        {
            ValidateName(name);
            ValidateDescription(description);
            ValidatePrice(price);
            ValidateInventory(inventory);
        }

        private static void ValidateRelatedEntities(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
                throw new ArgumentException("CategoryId é obrigatório", nameof(categoryId));
        }

        private static void ValidateName(string name)
        {
            ValidateNameBasics(name);
            ValidateNameFormat(name);
        }

        private static void ValidateNameBasics(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("O nome do produto não pode ser vazio", nameof(name));

            if (name.Length < MIN_NAME_LENGTH)
                throw new ArgumentException($"O nome do produto deve ter pelo menos {MIN_NAME_LENGTH} caracteres", nameof(name));

            if (name.Length > MAX_NAME_LENGTH)
                throw new ArgumentException($"O nome do produto não pode exceder {MAX_NAME_LENGTH} caracteres", nameof(name));
        }
        private static void ValidateNameFormat(string name)
        {
            // Produtos podem ter letras, números, espaços e símbolos básicos
            if (!Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ0-9\s\-&\.'\(\)\/]+$"))
                throw new ArgumentException("O nome do produto contém caracteres não permitidos", nameof(name));

            if (name.Trim() != name)
                throw new ArgumentException("O nome do produto não pode começar ou terminar com espaços", nameof(name));
        }
        private static void ValidateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("A descrição do produto não pode ser vazia", nameof(description));

            if (description.Length > MAX_DESCRIPTION_LENGTH)
                throw new ArgumentException($"A descrição do produto não pode exceder {MAX_DESCRIPTION_LENGTH} caracteres", nameof(description));

            if (description.Trim() != description)
                throw new ArgumentException("A descrição do produto não pode começar ou terminar com espaços", nameof(description));
        }

        private static void ValidatePrice(decimal price)
        {
            if (price < MIN_PRICE)
                throw new ArgumentException($"O preço deve ser maior ou igual a {MIN_PRICE:C}", nameof(price));

            if (price > MAX_PRICE)
                throw new ArgumentException($"O preço não pode exceder {MAX_PRICE:C}", nameof(price));
        }

        private static void ValidateInventory(int inventory)
        {
            if (inventory < MIN_INVENTORY)
                throw new ArgumentException($"O estoque não pode ser menor que {MIN_INVENTORY}", nameof(inventory));

            if (inventory > MAX_INVENTORY)
                throw new ArgumentException($"O estoque não pode exceder {MAX_INVENTORY}", nameof(inventory));
        }

        #endregion
    }
}
