using System.Dynamic;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using DMP.BL.Models.AppSettings;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorLight;

namespace DMP.BL.Services;

/// <summary>
/// Sends queued e-mails from the <c>Mail</c> table, rendering Razor templates when a template is attached.
/// </summary>
public class MailService(
    ILogger<MailService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IOptions<SmtpSettings> smtpSettings,
    IRazorLightEngine razorEngine) : IMailService
{
    private const int MaxAttempts = 10;

    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var mailDal = await context.Mails
                .OrderBy(m => m.UpdatedAt)
                .Include(m => m.EmailTemplate)
                .FirstOrDefaultAsync(m => m.Status < MailStatus.Sent, cancellationToken);

            if (mailDal is null)
            {
                break;
            }

            try
            {
                // Sending and recording the result are not cancelled midway to avoid duplicate or lost e-mails.
                await SendMailAsync(mailDal);
                mailDal.Status = MailStatus.Sent;
                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send mail {MailId}", mailDal.MailId);

                mailDal.Attempts += 1;
                mailDal.Status = mailDal.Attempts > MaxAttempts ? MailStatus.Error : MailStatus.Failed;

                context.Mails.Update(mailDal);
                await context.SaveChangesAsync(CancellationToken.None);

                throw;
            }
        }
    }

    private async Task SendMailAsync(MailDAL mailDal)
    {
        string body;
        string subject;
        if (mailDal.EmailTemplate is not null)
        {
            var model = JsonSerializer.Deserialize<ExpandoObject>(mailDal.Model!);
            body = await razorEngine.CompileRenderStringAsync(
                $"emailTemplate-{mailDal.EmailTemplateId}", mailDal.EmailTemplate.Body, model);
            subject = await razorEngine.CompileRenderStringAsync(
                $"emailSubjectTemplate-{mailDal.EmailTemplateId}", mailDal.EmailTemplate.Subject, model);
        }
        else
        {
            body = mailDal.Body!;
            subject = mailDal.Subject!;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(mailDal.From),
            Subject = subject,
            IsBodyHtml = true,
            Body = body,
        };
        mailMessage.To.Add(mailDal.To);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.Ssl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password),
        };
        await client.SendMailAsync(mailMessage);
    }
}
