using CommerceCore.Domain.Common;
using System.Text.RegularExpressions;

namespace CommerceCore.Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        #region Constants

        private const int MIN_URL_LENGTH = 10;
        private const int MAX_URL_LENGTH = 500;
        private const string URL_PATTERN = @"^https?:\/\/.+\.(jpg|jpeg|png|gif|webp)(\?.*)?$";

        #endregion

        #region Properties

        // ID do produto ao qual a imagem pertence
        public Guid ProductId { get; private set; }

        // URL da imagem (deve ser válida e acessível)
        public string Url { get; private set; } = string.Empty;

        // Define se é a imagem principal do produto
        public bool IsMain { get; private set; }

        #endregion

        #region Navigation Properties

        // Produto ao qual esta imagem pertence
        public Product Product { get; set; } = null!;

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private ProductImage() { }

        // Cria uma nova imagem para o produto
        public ProductImage(Guid productId, string url, bool isMain = false)
        {
            ValidateImageCreation(productId, url); // Valida dados da imagem
            SetImageProperties(productId, url, isMain); // Define propriedades
        }

        #endregion

        #region Public Methods

        // Atualiza a URL da imagem
        public void UpdateUrl(string url)
        {
            ValidateUrl(url);           // Valida nova URL
            SetUrl(url);               // Define nova URL
            MarkAsUpdated();           // Marca como alterado
        }

        // Define esta imagem como principal do produto
        public void SetAsMain()
        {
            IsMain = true;
            MarkAsUpdated();
        }

        // Remove esta imagem como principal do produto
        public void UnsetAsMain()
        {
            IsMain = false;
            MarkAsUpdated();
        }

        // Query Methods - Métodos para consultar informações da imagem

        // Verifica se a URL parece ser uma imagem válida
        public bool HasValidImageExtension() => IsValidImageUrl(Url);

        // Retorna extensão do arquivo da imagem
        public string GetFileExtension()
        {
            try
            {
                var uri = new Uri(Url);
                return Path.GetExtension(uri.LocalPath).ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        // Verifica se é uma imagem HTTPS (mais segura)
        public bool IsSecure() => Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define as propriedades da imagem durante criação
        private void SetImageProperties(Guid productId, string url, bool isMain)
        {
            ProductId = productId;     // Define produto
            SetUrl(url);              // Define URL
            IsMain = isMain;          // Define se é principal
        }

        // Define a URL (normalizada)
        private void SetUrl(string url)
        {
            Url = NormalizeUrl(url);   // Remove espaços e normaliza
        }

        // Marca imagem como atualizada (para auditoria)
        private void MarkAsUpdated() => SetUpdatedAt();

        // Normaliza URL removendo espaços
        private static string NormalizeUrl(string url) => url.Trim();

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida dados durante criação da imagem
        private static void ValidateImageCreation(Guid productId, string url)
        {
            ValidateProductId(productId);  // Valida ProductId
            ValidateUrl(url);             // Valida URL
        }

        // Valida se ProductId é válido
        private static void ValidateProductId(Guid productId)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("ProductId é obrigatório", nameof(productId));
        }

        // Valida URL da imagem
        private static void ValidateUrl(string url)
        {
            ValidateUrlNotEmpty(url);      // Verifica se não está vazia
            ValidateUrlLength(url);        // Verifica tamanho
            ValidateUrlFormat(url);        // Verifica formato
        }

        // Valida se URL não está vazia
        private static void ValidateUrlNotEmpty(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL é obrigatória", nameof(url));
        }

        // Valida tamanho da URL
        private static void ValidateUrlLength(string url)
        {
            var trimmedUrl = url.Trim();

            if (trimmedUrl.Length < MIN_URL_LENGTH)
                throw new ArgumentException($"URL deve ter pelo menos {MIN_URL_LENGTH} caracteres", nameof(url));

            if (trimmedUrl.Length > MAX_URL_LENGTH)
                throw new ArgumentException($"URL não pode exceder {MAX_URL_LENGTH} caracteres", nameof(url));
        }

        // Valida formato da URL (deve ser uma imagem válida)
        private static void ValidateUrlFormat(string url)
        {
            if (!IsValidImageUrl(url))
                throw new ArgumentException("URL deve ser uma imagem válida (jpg, jpeg, png, gif, webp)", nameof(url));
        }

        // Verifica se URL é uma imagem válida usando regex
        private static bool IsValidImageUrl(string url)
        {
            return Regex.IsMatch(url, URL_PATTERN, RegexOptions.IgnoreCase);
        }

        #endregion
    }
}
