using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Threading.Tasks;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        Modules module,
        EmailTemplateEnum templateName,
        CultureInfo culture,
        object data);
}

