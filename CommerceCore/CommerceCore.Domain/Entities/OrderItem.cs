using CommerceCore.Domain.Common;

namespace CommerceCore.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        #region Constants

        private const int MIN_QUANTITY = 1;
        private const int MAX_QUANTITY = 999;
        private const decimal MIN_UNIT_PRICE = 0.01m;
        private const decimal MAX_UNIT_PRICE = 999999.99m;
        private const int MIN_PRODUCT_NAME_LENGTH = 2;
        private const int MAX_PRODUCT_NAME_LENGTH = 200;

        #endregion

        #region Properties

        // ID do pedido ao qual este item pertence
        public Guid OrderId { get; private set; }

        // ID do produto (referência)
        public Guid ProductId { get; private set; }

        // Nome do produto (snapshot no momento da compra)
        public string ProductName { get; private set; } = string.Empty;

        // Quantidade comprada
        public int Quantity { get; private set; }

        // Preço unitário no momento da compra
        public decimal UnitPrice { get; private set; }

        // Preço total (Quantity * UnitPrice)
        public decimal TotalPrice { get; private set; }

        #endregion

        #region Navigation Properties

        // Pedido ao qual este item pertence
        public Order Order { get; set; } = null!;

        // Produto referenciado
        public Product Product { get; set; } = null!;

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private OrderItem() { }

        // Cria um novo item no pedido
        public OrderItem(Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            ValidateOrderItemCreation(orderId, productId, productName, quantity, unitPrice); // Valida dados
            SetOrderItemProperties(orderId, productId, productName, quantity, unitPrice);    // Define propriedades
            CalculateAndSetTotalPrice();                                                     // Calcula preço total
        }

        #endregion

        #region Public Methods

        // Atualiza quantidade do item (recalcula preço total)
        public void UpdateQuantity(int quantity)
        {
            ValidateQuantity(quantity);      // Valida nova quantidade
            SetQuantity(quantity);          // Define nova quantidade
            RecalculateTotalPrice();        // Recalcula preço total
            MarkAsUpdated();               // Marca como alterado
        }

        // Atualiza preço unitário (recalcula preço total)
        public void UpdateUnitPrice(decimal unitPrice)
        {
            ValidateUnitPrice(unitPrice);   // Valida novo preço
            SetUnitPrice(unitPrice);       // Define novo preço
            RecalculateTotalPrice();       // Recalcula preço total
            MarkAsUpdated();              // Marca como alterado
        }

        // Atualiza quantidade e preço unitário juntos
        public void UpdateQuantityAndPrice(int quantity, decimal unitPrice)
        {
            ValidateQuantity(quantity);     // Valida quantidade
            ValidateUnitPrice(unitPrice);   // Valida preço
            SetQuantity(quantity);         // Define quantidade
            SetUnitPrice(unitPrice);       // Define preço
            RecalculateTotalPrice();       // Recalcula total
            MarkAsUpdated();              // Marca como alterado
        }

        // Atualiza nome do produto (se produto for renomeado)
        public void UpdateProductName(string productName)
        {
            ValidateProductName(productName); // Valida novo nome
            SetProductName(productName);      // Define novo nome
            MarkAsUpdated();                 // Marca como alterado
        }

        #endregion

        #region Query Methods - Métodos para consultar informações do item

        // Verifica se é um item de quantidade única
        public bool IsSingleItem() => Quantity == MIN_QUANTITY;

        // Verifica se é um item de quantidade múltipla
        public bool IsMultipleItems() => Quantity > MIN_QUANTITY;

        // Verifica se o preço unitário é considerado alto (acima de R$ 1000)
        public bool IsExpensiveItem() => UnitPrice > 1000m;

        // Calcula desconto por quantidade (se houver)
        public decimal GetQuantityDiscountPercentage()
        {
            return Quantity switch
            {
                >= 10 => 0.10m,  // 10% desconto para 10+ itens
                >= 5 => 0.05m,   // 5% desconto para 5+ itens
                _ => 0m           // Sem desconto
            };
        }

        // Retorna preço total com desconto por quantidade
        public decimal GetTotalPriceWithQuantityDiscount()
        {
            var discountPercentage = GetQuantityDiscountPercentage();
            return TotalPrice * (1 - discountPercentage);
        }

        // Retorna valor economizado com desconto por quantidade
        public decimal GetQuantityDiscountAmount()
        {
            return TotalPrice - GetTotalPriceWithQuantityDiscount();
        }

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define propriedades do item durante criação
        private void SetOrderItemProperties(Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            OrderId = orderId;                          // Define pedido
            ProductId = productId;                      // Define produto
            SetProductName(productName);                // Define nome do produto
            SetQuantity(quantity);                      // Define quantidade
            SetUnitPrice(unitPrice);                   // Define preço unitário
        }

        // Define nome do produto (normalizado)
        private void SetProductName(string productName)
        {
            ProductName = NormalizeProductName(productName);
        }

        // Define quantidade
        private void SetQuantity(int quantity)
        {
            Quantity = quantity;
        }

        // Define preço unitário
        private void SetUnitPrice(decimal unitPrice)
        {
            UnitPrice = unitPrice;
        }

        // Calcula e define preço total
        private void CalculateAndSetTotalPrice()
        {
            RecalculateTotalPrice();
        }

        // Recalcula preço total baseado na quantidade e preço unitário atuais
        private void RecalculateTotalPrice()
        {
            TotalPrice = CalculateTotalPrice(Quantity, UnitPrice);
        }

        // Marca item como atualizado (para auditoria)
        private void MarkAsUpdated() => SetUpdatedAt();

        // Normaliza nome do produto removendo espaços extras
        private static string NormalizeProductName(string productName) => productName.Trim();

        // Calcula preço total (quantidade * preço unitário)
        private static decimal CalculateTotalPrice(int quantity, decimal unitPrice) => quantity * unitPrice;

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida dados durante criação do item
        private static void ValidateOrderItemCreation(Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            ValidateOrderId(orderId);           // Valida OrderId
            ValidateProductId(productId);       // Valida ProductId
            ValidateProductName(productName);   // Valida nome do produto
            ValidateQuantity(quantity);         // Valida quantidade
            ValidateUnitPrice(unitPrice);       // Valida preço unitário
        }

        // Valida OrderId
        private static void ValidateOrderId(Guid orderId)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("OrderId é obrigatório", nameof(orderId));
        }

        // Valida ProductId
        private static void ValidateProductId(Guid productId)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("ProductId é obrigatório", nameof(productId));
        }

        // Valida nome do produto
        private static void ValidateProductName(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Nome do produto é obrigatório", nameof(productName));

            var trimmed = productName.Trim();
            if (trimmed.Length < MIN_PRODUCT_NAME_LENGTH)
                throw new ArgumentException($"Nome do produto deve ter pelo menos {MIN_PRODUCT_NAME_LENGTH} caracteres", nameof(productName));

            if (trimmed.Length > MAX_PRODUCT_NAME_LENGTH)
                throw new ArgumentException($"Nome do produto não pode exceder {MAX_PRODUCT_NAME_LENGTH} caracteres", nameof(productName));
        }

        // Valida quantidade
        private static void ValidateQuantity(int quantity)
        {
            if (quantity < MIN_QUANTITY)
                throw new ArgumentException($"Quantidade deve ser maior ou igual a {MIN_QUANTITY}", nameof(quantity));

            if (quantity > MAX_QUANTITY)
                throw new ArgumentException($"Quantidade não pode exceder {MAX_QUANTITY}", nameof(quantity));
        }

        // Valida preço unitário
        private static void ValidateUnitPrice(decimal unitPrice)
        {
            if (unitPrice < MIN_UNIT_PRICE)
                throw new ArgumentException($"Preço unitário deve ser maior ou igual a {MIN_UNIT_PRICE:C}", nameof(unitPrice));

            if (unitPrice > MAX_UNIT_PRICE)
                throw new ArgumentException($"Preço unitário não pode exceder {MAX_UNIT_PRICE:C}", nameof(unitPrice));
        }

        #endregion
    }
}
