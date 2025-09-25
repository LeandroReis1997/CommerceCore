using CommerceCore.Domain.Common;

namespace CommerceCore.Domain.Entities
{
    public class CartItem : BaseEntity
    {
        #region Constants

        private const int MIN_QUANTITY = 1;      // Quantidade mínima: 1 item
        private const int MAX_QUANTITY = 999;    // Quantidade máxima: 999 itens

        #endregion

        #region Properties

        public Guid CartId { get; private set; }     // ID do carrinho que contém este item
        public Guid ProductId { get; private set; }  // ID do produto
        public int Quantity { get; private set; }    // Quantidade deste produto no carrinho

        #endregion

        #region Navigation Properties

        public Cart Cart { get; set; } = null!;       // Carrinho que contém este item
        public Product Product { get; set; } = null!; // Produto referenciado

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private CartItem() { }

        // Cria um novo item no carrinho
        public CartItem(Guid cartId, Guid productId, int quantity)
        {
            ValidateCartItemCreation(cartId, productId, quantity); // Valida se dados estão corretos
            SetCartItemProperties(cartId, productId, quantity);    // Define as propriedades
        }

        #endregion

        #region Public Methods

        // Atualiza a quantidade total do item (substitui a quantidade atual)
        public void UpdateQuantity(int quantity)
        {
            ValidateQuantity(quantity);  // Verifica se quantidade é válida
            SetQuantity(quantity);       // Define nova quantidade
            MarkAsUpdated();            // Marca data de alteração
        }

        // Adiciona mais itens à quantidade existente (soma)
        public void AddQuantity(int additionalQuantity)
        {
            ValidateAdditionalQuantity(additionalQuantity);        // Verifica se quantidade a adicionar é válida
            var newQuantity = CalculateNewQuantity(additionalQuantity); // Calcula nova quantidade total
            ValidateQuantity(newQuantity);                         // Verifica se total não excede limite
            SetQuantity(newQuantity);                             // Define nova quantidade
            MarkAsUpdated();                                      // Marca data de alteração
        }

        // Remove itens da quantidade existente (subtrai)
        public void RemoveQuantity(int quantityToRemove)
        {
            ValidateQuantityToRemove(quantityToRemove);           // Verifica se quantidade a remover é válida
            ValidateRemovalIsPossible(quantityToRemove);          // Verifica se há itens suficientes para remover
            var newQuantity = CalculateQuantityAfterRemoval(quantityToRemove); // Calcula quantidade após remoção
            SetQuantity(newQuantity);                             // Define nova quantidade
            MarkAsUpdated();                                      // Marca data de alteração
        }

        // Incrementa em 1 item (atalho para adicionar 1)
        public void IncrementQuantity()
        {
            AddQuantity(MIN_QUANTITY); // Adiciona 1 item usando AddQuantity
        }

        // Decrementa em 1 item (atalho para remover 1)
        public void DecrementQuantity()
        {
            if (CanDecrementQuantity()) // Verifica se pode decrementar
            {
                RemoveQuantity(MIN_QUANTITY); // Remove 1 item usando RemoveQuantity
            }
        }

        // Query Methods - Métodos para consultar estado do item

        // Verifica se está na quantidade mínima (1 item)
        public bool IsAtMinimumQuantity() => Quantity == MIN_QUANTITY;

        // Verifica se está na quantidade máxima (999 itens)
        public bool IsAtMaximumQuantity() => Quantity == MAX_QUANTITY;

        // Verifica se pode adicionar mais itens (não está no máximo)
        public bool CanIncrementQuantity() => Quantity < MAX_QUANTITY;

        // Verifica se pode remover itens (tem mais de 1 item)
        public bool CanDecrementQuantity() => Quantity > MIN_QUANTITY;

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define as propriedades do item durante a criação
        private void SetCartItemProperties(Guid cartId, Guid productId, int quantity)
        {
            CartId = cartId;      // Define carrinho
            ProductId = productId; // Define produto
            Quantity = quantity;   // Define quantidade inicial
        }

        // Define apenas a quantidade (usado internamente)
        private void SetQuantity(int quantity)
        {
            Quantity = quantity;
        }

        // Marca que o item foi atualizado (para auditoria)
        private void MarkAsUpdated()
        {
            SetUpdatedAt(); // Atualiza campo UpdatedAt da BaseEntity
        }

        // Calcula nova quantidade após adição
        private int CalculateNewQuantity(int additionalQuantity) =>
            Quantity + additionalQuantity;

        // Calcula quantidade após remoção
        private int CalculateQuantityAfterRemoval(int quantityToRemove) =>
            Quantity - quantityToRemove;

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida todos os dados durante criação do item
        private static void ValidateCartItemCreation(Guid cartId, Guid productId, int quantity)
        {
            ValidateCartId(cartId);       // Verifica se CartId é válido
            ValidateProductId(productId); // Verifica se ProductId é válido
            ValidateQuantity(quantity);   // Verifica se quantidade é válida
        }

        // Valida se CartId não é vazio
        private static void ValidateCartId(Guid cartId)
        {
            if (cartId == Guid.Empty)
                throw new ArgumentException("CartId é obrigatório", nameof(cartId));
        }

        // Valida se ProductId não é vazio
        private static void ValidateProductId(Guid productId)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("ProductId é obrigatório", nameof(productId));
        }

        // Valida se quantidade está dentro dos limites permitidos
        private static void ValidateQuantity(int quantity)
        {
            if (quantity < MIN_QUANTITY)
                throw new ArgumentException($"Quantidade deve ser maior ou igual a {MIN_QUANTITY}", nameof(quantity));

            if (quantity > MAX_QUANTITY)
                throw new ArgumentException($"Quantidade não pode exceder {MAX_QUANTITY}", nameof(quantity));
        }

        // Valida se quantidade a adicionar é positiva
        private static void ValidateAdditionalQuantity(int additionalQuantity)
        {
            if (additionalQuantity <= 0)
                throw new ArgumentException("Quantidade adicional deve ser maior que zero", nameof(additionalQuantity));
        }

        // Valida se quantidade a remover é positiva
        private static void ValidateQuantityToRemove(int quantityToRemove)
        {
            if (quantityToRemove <= 0)
                throw new ArgumentException("Quantidade a remover deve ser maior que zero", nameof(quantityToRemove));
        }

        // Valida se é possível remover a quantidade solicitada
        private void ValidateRemovalIsPossible(int quantityToRemove)
        {
            if (quantityToRemove >= Quantity)
                throw new InvalidOperationException("Não é possível remover mais itens do que existe no carrinho");
        }

        #endregion
    }
}
