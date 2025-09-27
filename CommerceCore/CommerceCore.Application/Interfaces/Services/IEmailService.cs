namespace CommerceCore.Application.Interfaces.Services
{
    public interface IEmailService
    {
        // Métodos básicos de envio
        Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default); // Envia email simples
        Task SendAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default); // Envia email para múltiplos destinatários
        Task SendWithAttachmentsAsync(string to, string subject, string body, IEnumerable<EmailAttachment> attachments, bool isHtml = true, CancellationToken cancellationToken = default); // Envia email com anexos

        // Templates de email específicos do e-commerce
        Task SendWelcomeEmailAsync(string userEmail, string userName, CancellationToken cancellationToken = default); // Email de boas-vindas para novo usuário
        Task SendPasswordResetEmailAsync(string userEmail, string resetToken, CancellationToken cancellationToken = default); // Email para redefinição de senha
        Task SendOrderConfirmationEmailAsync(string userEmail, Guid orderId, CancellationToken cancellationToken = default); // Confirmação de pedido
        Task SendOrderStatusUpdateEmailAsync(string userEmail, Guid orderId, string newStatus, CancellationToken cancellationToken = default); // Atualização de status do pedido
        Task SendAbandonedCartEmailAsync(string userEmail, Guid cartId, CancellationToken cancellationToken = default); // Email de carrinho abandonado

        // Métodos de validação
        Task<bool> IsValidEmailAsync(string email, CancellationToken cancellationToken = default); // Valida formato e existência do email
        Task<bool> IsEmailServiceAvailableAsync(CancellationToken cancellationToken = default); // Verifica se serviço de email está funcionando
    }

    // Classe auxiliar para anexos
    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty; // Nome do arquivo
        public byte[] Content { get; set; } = Array.Empty<byte>(); // Conteúdo do arquivo
        public string ContentType { get; set; } = "application/octet-stream"; // Tipo MIME do arquivo
    }
}