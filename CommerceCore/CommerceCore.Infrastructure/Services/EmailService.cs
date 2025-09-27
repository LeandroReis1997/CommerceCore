using CommerceCore.Application.Interfaces.Services;
using System.Net;
using System.Net.Mail;

namespace CommerceCore.Infrastructure.Services
{
    /// <summary>
    /// Implementação de IEmailService usando SmtpClient do .NET.
    /// Suporta envio de emails simples, com anexos e templates para e-commerce.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;
        private readonly string _fromAddress;

        public EmailService(string smtpHost, int smtpPort, string fromAddress, string smtpUser, string smtpPass)
        {
            _smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };
            _fromAddress = fromAddress;
        }

        // Envia email simples para um destinatário
        public async Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            var mail = new MailMessage(_fromAddress, to, subject, body)
            {
                IsBodyHtml = isHtml
            };
            await _smtpClient.SendMailAsync(mail, cancellationToken);
        }

        // Envia email para múltiplos destinatários
        public async Task SendAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(_fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            foreach (var recipient in to)
                mail.To.Add(recipient);

            await _smtpClient.SendMailAsync(mail, cancellationToken);
        }

        // Envia email com anexos
        public async Task SendWithAttachmentsAsync(string to, string subject, string body, IEnumerable<EmailAttachment> attachments, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            var mail = new MailMessage(_fromAddress, to, subject, body)
            {
                IsBodyHtml = isHtml
            };
            foreach (var attachment in attachments)
            {
                var mailAttachment = new Attachment(new MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType);
                mail.Attachments.Add(mailAttachment);
            }
            await _smtpClient.SendMailAsync(mail, cancellationToken);
        }

        // Email de boas-vindas para novo usuário
        public async Task SendWelcomeEmailAsync(string userEmail, string userName, CancellationToken cancellationToken = default)
        {
            var subject = "Bem-vindo ao CommerceCore!";
            var body = $"Olá {userName},<br/>Seja bem-vindo à nossa plataforma!";
            await SendAsync(userEmail, subject, body, true, cancellationToken);
        }

        // Email para redefinição de senha
        public async Task SendPasswordResetEmailAsync(string userEmail, string resetToken, CancellationToken cancellationToken = default)
        {
            var subject = "Redefinição de senha";
            var body = $"Para redefinir sua senha, utilize o token: <b>{resetToken}</b>";
            await SendAsync(userEmail, subject, body, true, cancellationToken);
        }

        // Confirmação de pedido
        public async Task SendOrderConfirmationEmailAsync(string userEmail, Guid orderId, CancellationToken cancellationToken = default)
        {
            var subject = "Confirmação de pedido";
            var body = $"Seu pedido <b>{orderId}</b> foi recebido com sucesso!";
            await SendAsync(userEmail, subject, body, true, cancellationToken);
        }

        // Atualização de status do pedido
        public async Task SendOrderStatusUpdateEmailAsync(string userEmail, Guid orderId, string newStatus, CancellationToken cancellationToken = default)
        {
            var subject = "Atualização de status do pedido";
            var body = $"O status do seu pedido <b>{orderId}</b> foi alterado para <b>{newStatus}</b>.";
            await SendAsync(userEmail, subject, body, true, cancellationToken);
        }

        // Email de carrinho abandonado
        public async Task SendAbandonedCartEmailAsync(string userEmail, Guid cartId, CancellationToken cancellationToken = default)
        {
            var subject = "Você esqueceu algo!";
            var body = $"Seu carrinho <b>{cartId}</b> está esperando por você. Volte e finalize sua compra!";
            await SendAsync(userEmail, subject, body, true, cancellationToken);
        }

        // Valida formato e existência do email
        public Task<bool> IsValidEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var addr = new MailAddress(email);
                return Task.FromResult(addr.Address == email);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        // Verifica se serviço de email está funcionando
        public Task<bool> IsEmailServiceAvailableAsync(CancellationToken cancellationToken = default)
        {
            // Testa conexão SMTP (simplesmente tenta conectar)
            try
            {
                _smtpClient.Send(new MailMessage(_fromAddress, _fromAddress, "Teste", "Teste de disponibilidade"));
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}