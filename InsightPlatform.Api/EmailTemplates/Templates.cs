using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;

public enum MailContentDisposition
{
    [Description("inline")]
    Inline,

    [Description("attachment")]
    Attachment,

    [Description("form-data")]
    FormData
}

public class EmailTemplateItem
{
    public Dictionary<string, string> Titles { get; set; }

    public string Source { get; set; }   

    public Dictionary<string, LinkedResourceItem> Resources { get; set; } = [];

    public object Data { get; set; }
}

public class LinkedResourceItem
{
    public string FileName { get; set; }

    public string ContentType { get; set; } = null;

    public MailContentDisposition ContentDisposition { get; set; } = MailContentDisposition.Inline;
}

public static class EmailTemplates
{
    public static readonly Dictionary<Modules, Dictionary<EmailTemplateEnum, EmailTemplateItem>> Data = new()
    {
        [Modules.BocMenh] = new()
        {
            [EmailTemplateEnum.EmailOtpForRegister] = new EmailTemplateItem
            {
                Titles = new Dictionary<string, string>
                {
                    ["vi"] = "Xác thực email",
                    ["en"] = "Email Verification",
                },
                Source = "EmailOtpForRegister.cshtml",
                Resources = new Dictionary<string, LinkedResourceItem>
                {
                    ["logo"] = new LinkedResourceItem
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates/Resources/logo.png"),
                        ContentType = MediaTypeNames.Image.Png,
                        ContentDisposition = MailContentDisposition.Inline
                    }
                }
            },
            [EmailTemplateEnum.EmailOtpForPasswordRecovery] = new EmailTemplateItem
            {
                Titles = new Dictionary<string, string>
                {
                    ["vi"] = "Mã xác thực để đặt lại mật khẩu của bạn",
                    ["en"] = "Authentication code to reset your password",
                },
                Source = "EmailOtpForPasswordRecovery.cshtml",
                Resources = new Dictionary<string, LinkedResourceItem>
                {
                    ["logo"] = new LinkedResourceItem
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates/Resources/logo.png"),
                        ContentType = MediaTypeNames.Image.Png,
                        ContentDisposition = MailContentDisposition.Inline
                    }
                }
            },
        }
    };

    public static string GetLocalizedSubject(
        Modules module
        , Dictionary<string, string> titles
        , string cultureName
        , object model)
    {
        if (titles.TryGetValue(cultureName, out var subjectTemplate))
        {
            var subject = subjectTemplate;
            foreach (var prop in model.GetType().GetProperties())
            {
                subject = subject.Replace($"{{{prop.Name}}}", prop.GetValue(model)?.ToString());
            }
            return $"{subject} - {module.GetDescription().Split("|")[1]}";
        }

        return titles.FirstOrDefault().Value;
    }
}