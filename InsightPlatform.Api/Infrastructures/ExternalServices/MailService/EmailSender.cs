using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RazorLight;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

internal class EmailService : IEmailService
{
    private readonly IRazorLightEngine _emailTemplateEngine;
    private readonly EmailProviderSettings _emailSettings;
    private readonly string _templateBasePath;

    public EmailService(IOptions<EmailProviderSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
        _templateBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates/Views");
        _emailTemplateEngine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templateBasePath)
            .Build();
    }

    public async Task SendEmailAsync(
        string to,
        Modules module,
        EmailTemplateEnum templateName,
        CultureInfo culture,
        object data)
    {
        if (to.IsMissing())
            return;

        to = to.Replace(" ", string.Empty);

        var template = EmailTemplates.Data.TryGetValue(module, out var md) && md.TryGetValue(templateName, out var tpl) ? tpl : null;

        if (template == null)
            return;

        var cultureName = culture?.Name?.Contains('-') == true ? culture.Name.Split("-")[0] : culture?.Name ?? "vi";

        string subject = EmailTemplates.GetLocalizedSubject(module, template.Titles, cultureName, data);
        var templatePath = Path.Combine(module.ToString(), cultureName, template.Source);
        var body = await _emailTemplateEngine.CompileRenderAsync(templatePath, data);

        // Build the message using MimeKit
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_emailSettings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        if (template.Resources != null && template.Resources.Count != 0)
        {
            foreach (var rs in template.Resources)
            {
                var resource = builder.LinkedResources.Add(rs.Value.FileName);
                resource.ContentId = rs.Key;
                resource.ContentDisposition = new ContentDisposition(rs.Value.ContentDisposition.GetDescription());
                resource.IsAttachment = rs.Value.ContentDisposition == MailContentDisposition.Attachment;
            }
        }

        message.Body = builder.ToMessageBody();

        // Add headers
        message.Headers.Add("X-Mailer", "Microsoft .NET");
        message.Headers.Add("Precedence", "bulk");
        message.Headers.Add("List-Unsubscribe", $"<mailto:{_emailSettings.FromAddress}?subject=unsubscribe>");

        using var smtpClient = new MailKit.Net.Smtp.SmtpClient();

        await smtpClient.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
        await smtpClient.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
        await smtpClient.SendAsync(message);
        await smtpClient.DisconnectAsync(true);
    }
}