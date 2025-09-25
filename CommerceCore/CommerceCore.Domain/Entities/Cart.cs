using CommerceCore.Domain.Common;

namespace CommerceCore.Domain.Entities
{
    public class Cart : BaseEntity
    {
        #region Constants

        private const int MIN_QUANTITY = 1;         // Quantidade mínima por item
        private const int MAX_ITEMS_PER_CART = 50;  // Máximo de tipos diferentes de produtos

        #endregion

        #region Properties

        public Guid UserId { get; private set; }    // ID do usuário dono do carrinho

        #endregion

        #region Navigation Properties

        public User User { get; set; } = null!;           // Usuário dono do carrinho
        public List<CartItem> Items { get; set; } = [];   // Lista de itens no carrinho

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private Cart() { }

        // Cria um novo carrinho para o usuário
        public Cart(Guid userId)
        {
            ValidateUserId(userId); // Verifica se UserId é válido
            UserId = userId;        // Define o dono do carrinho
        }

        #endregion

        #region Public Methods

        // Adiciona um produto ao carrinho (ou aumenta quantidade se já existe)
        public void AddItem(Guid productId, int quantity = MIN_QUANTITY)
        {
            ValidateAddItemRequest(productId, quantity); // Valida dados de entrada
            ValidateCartCapacity();                      // Verifica se carrinho não está cheio

            var existingItem = FindItemByProduct(productId); // Busca se produto já está no carrinho
            if (existingItem != null)
            {
                UpdateExistingItem(existingItem, quantity); // Se existe, adiciona à quantidade atual
            }
            else
            {
                AddNewItem(productId, quantity);           // Se não existe, cria novo item
            }
            MarkAsUpdated(); // Marca carrinho como alterado
        }

        // Remove um produto completamente do carrinho
        public void RemoveItem(Guid productId)
        {
            ValidateProductId(productId);              // Valida ProductId
            var item = FindItemByProduct(productId);   // Busca o item
            if (item != null)
            {
                RemoveItemFromCart(item);              // Remove item da lista
                MarkAsUpdated();                       // Marca como alterado
            }
        }

        // Atualiza a quantidade de um item específico
        public void UpdateItemQuantity(Guid productId, int quantity)
        {
            ValidateUpdateQuantityRequest(productId, quantity); // Valida dados
            var item = FindRequiredItem(productId);             // Busca item (obrigatório existir)
            UpdateItemQuantityValue(item, quantity);            // Atualiza quantidade do item
            MarkAsUpdated();                                   // Marca como alterado
        }

        // Remove todos os itens do carrinho
        public void Clear()
        {
            ClearAllItems(); // Limpa lista de itens
            MarkAsUpdated(); // Marca como alterado
        }

        // Query Methods - Métodos para consultar estado do carrinho

        // Retorna quantidade total de itens (soma todas as quantidades)
        public int GetTotalItems() => CalculateTotalItems();

        // Retorna quantidade de tipos diferentes de produtos
        public int GetTotalUniqueItems() => Items.Count;

        // Verifica se carrinho está vazio
        public bool IsEmpty() => !HasAnyItems();

        // Verifica se carrinho tem itens
        public bool HasItems() => HasAnyItems();

        // Verifica se um produto específico está no carrinho
        public bool HasItem(Guid productId) => FindItemByProduct(productId) != null;

        // Retorna um item específico do carrinho (ou null se não existir)
        public CartItem? GetItem(Guid productId) => FindItemByProduct(productId);

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Busca um item pelo ID do produto
        private CartItem? FindItemByProduct(Guid productId) =>
            Items.FirstOrDefault(i => i.ProductId == productId);

        // Busca um item que deve existir (lança exceção se não encontrar)
        private CartItem FindRequiredItem(Guid productId)
        {
            var item = FindItemByProduct(productId);
            if (item == null)
                throw new InvalidOperationException("Item não encontrado no carrinho");
            return item;
        }

        // Atualiza item existente adicionando mais quantidade
        private void UpdateExistingItem(CartItem item, int additionalQuantity)
        {
            var newQuantity = item.Quantity + additionalQuantity; // Calcula nova quantidade
            item.UpdateQuantity(newQuantity);                     // Atualiza o item
        }

        // Adiciona novo item ao carrinho
        private void AddNewItem(Guid productId, int quantity)
        {
            var newItem = new CartItem(Id, productId, quantity); // Cria novo item
            Items.Add(newItem);                                  // Adiciona à lista
        }

        // Remove item da lista do carrinho
        private void RemoveItemFromCart(CartItem item) => Items.Remove(item);

        // Atualiza quantidade de um item específico
        private void UpdateItemQuantityValue(CartItem item, int quantity) =>
            item.UpdateQuantity(quantity);

        // Limpa todos os itens do carrinho
        private void ClearAllItems() => Items.Clear();

        // Marca carrinho como atualizado (para auditoria)
        private void MarkAsUpdated() => SetUpdatedAt();

        // Calcula total de itens somando todas as quantidades
        private int CalculateTotalItems() => Items.Sum(i => i.Quantity);

        // Verifica se carrinho tem pelo menos um item
        private bool HasAnyItems() => Items.Count > 0;

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida se UserId é válido
        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId é obrigatório", nameof(userId));
        }

        // Valida dados para adicionar item
        private static void ValidateAddItemRequest(Guid productId, int quantity)
        {
            ValidateProductId(productId); // Valida ProductId
            ValidateQuantity(quantity);   // Valida quantidade
        }

        // Valida dados para atualizar quantidade
        private static void ValidateUpdateQuantityRequest(Guid productId, int quantity)
        {
            ValidateProductId(productId); // Valida ProductId
            ValidateQuantity(quantity);   // Valida quantidade
        }

        // Valida se ProductId não é vazio
        private static void ValidateProductId(Guid productId)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("ProductId é obrigatório", nameof(productId));
        }

        // Valida se quantidade é válida
        private static void ValidateQuantity(int quantity)
        {
            if (quantity < MIN_QUANTITY)
                throw new ArgumentException($"Quantidade deve ser maior ou igual a {MIN_QUANTITY}", nameof(quantity));
        }

        // Valida se carrinho não excedeu capacidade máxima
        private void ValidateCartCapacity()
        {
            if (Items.Count >= MAX_ITEMS_PER_CART)
                throw new InvalidOperationException($"Carrinho não pode ter mais de {MAX_ITEMS_PER_CART} tipos de produtos diferentes");
        }

        #endregion
    }
}
