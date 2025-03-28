﻿using InvoiceApp.WebApi.Interfaces;
using InvoiceApp.WebApi.Models;
using System.Net.Mail;

namespace InvoiceApp.WebApi.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<IEmailService> _logger;
    private readonly IEmailSender _emailSender;

    public EmailService(ILogger<IEmailService> logger, IEmailSender emailSender)
    {
        _logger = logger;
        _emailSender = emailSender;
    }

    public (string to, string subject, string body) GenerateInvoiceEmail(Invoice invoice)
    {
        var to = invoice.Contact.Email;
        var subject = $"Invoice {invoice.InvoiceNumber} for {invoice.Contact.FirstName} {invoice.Contact.LastName}";
        var body = $"""
            Dear {invoice.Contact.FirstName} {invoice.Contact.LastName},

            Thank you for your business. Here are your invoice details:
            Invoice Number: {invoice.InvoiceNumber}
            Invoice Date: {invoice.InvoiceDate.LocalDateTime.ToShortDateString()}
            Invoice Amount: {invoice.Amount:C}
            Invoice Items:
            {string.Join(Environment.NewLine, invoice.InvoiceItems.Select(i => $"{i.Description} - {i.Quantity} x {i.UnitPrice:C}"))}

            Please pay by {invoice.DueDate.LocalDateTime.ToShortDateString()}. Thank you!

            Regards,
            InvoiceApp
            """;
        return (to, subject, body);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Mock the email sending process
        // In real world, you may use a third-party email service, such as SendGrid, MailChimp, Azure Logic Apps, etc.
        _logger.LogInformation($"Sending email to {to} with subject {subject} and body {body}");
        try
        {
            await _emailSender.SendEmailAsync(to, subject, body);
            _logger.LogInformation($"Email sent to {to} with subject {subject}");
        }
        catch (SmtpException e)
        {
            _logger.LogError(e, $"SmtpClient error occurs. Failed to send email to {to} with subject {subject}.");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to send email to {to} with subject {subject}.");
            throw;
        }
    }
}
