using Microsoft.Extensions.Options;
using RazorLight;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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
        _emailTemplateEngine = new RazorLightEngineBuilder().UseFileSystemProject(_templateBasePath).Build();
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
            return; ;

        var cultureName = culture?.Name?.Contains('-') == true ? culture.Name.Split("-")[0] : culture?.Name ?? "vi";       

        string subject = EmailTemplates.GetLocalizedSubject(module, template.Titles, cultureName, data);
        var templatePath = Path.Combine(module.ToString(), cultureName, template.Source);
        var body = await _emailTemplateEngine.CompileRenderAsync(templatePath, data);

        var fromMailAddress = new MailAddress(_emailSettings.FromAddress);
        var toMailAddress = new MailAddress(to);
        var mailMessage = new MailMessage(fromMailAddress, toMailAddress)
        {
            Subject = subject,
            IsBodyHtml = true
        };

        var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

        if (template.Resources != null && template.Resources.Count != 0)
            foreach (var rs in template.Resources)
                htmlView.LinkedResources.Add(new LinkedResource(rs.Value.FileName, rs.Value.ContentType)
                {
                    ContentId = rs.Key,
                });

        mailMessage.AlternateViews.Add(htmlView);

        using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
            EnableSsl = true
        };

        // Anti-spam: Set headers
        mailMessage.Headers.Add("X-Mailer", "Microsoft .NET");
        mailMessage.Headers.Add("Precedence", "bulk");
        mailMessage.Headers.Add("List-Unsubscribe", $"<mailto:{_emailSettings.FromAddress}?subject=unsubscribe>");

        await smtpClient.SendMailAsync(mailMessage);
    }
}