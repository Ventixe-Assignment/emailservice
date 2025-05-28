using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using Presentation.Interfaces;
using Presentation.Models;
using System.Diagnostics;

namespace Presentation.Services;

public class EmailService(IConfiguration configuration, EmailClient emailClient, IMemoryCache cache) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly EmailClient _emailClient = emailClient;
    private readonly IMemoryCache _cache = cache;
    private static readonly Random _random = new();

    public async Task<EmailVerificationResult> SendVerificationCodeAsync(EmailVerificationCodeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return new EmailVerificationResult { Succeeded = false, Error = "E-mail address is required, in order to send verification code." };

            var verificationCode = _random.Next(100000, 999999).ToString();
            var subject = $"Your code is {verificationCode}";
            var plainTextContent = $@"
            Verify your email address

            Top o' the morning,

            Thank you for registering with us! To complete your registration, please verify your email address by entering the following code:

            {verificationCode}

            Alternatively, you can click the link below to verify your email address:
            https://ventixe.com/verify-email?email={Uri.EscapeDataString(request.Email)}&code={verificationCode} 


            If you did not request this verification, please ignore this email.           

            Best regards,
            The Ventixe Team
            
            All rights reserved. Ventixe Inc. {DateTime.UtcNow.Year}
            ";

            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
              <head>
                <meta charset='UTF-8'>
                <title>Verify Your Email</title>
              </head>
              <body style='margin: 0; padding: 0; background-color: #FDEDFE; font-family: 'Inter', sans-serif;'>
                <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px; background-color: #FFFFFF; margin: 20px auto; border-radius: 10px; box-shadow: 0 2px 5px rgba(0,0,0,0.1);'>
                  <tr>
                    <td style='padding: 30px; background-color: #FCD3FE; color: #3D004D; font-size: 24px; font-weight: bold; border-top-left-radius: 10px; border-top-right-radius: 10px;'>
                      Verify your email address
                    </td>
                  </tr>
                  <tr>
                    <td style='padding: 30px; color: #333333; font-size: 16px;'>
                      <p style='margin-top: 0;'>Top o' the morning,</p>

                      <p>Thank you for registering with us! To complete your registration, please verify your email address by entering the following code:</p>

                      <p style='font-size: 24px; font-weight: bold; color: #F26CF9; text-align: center; background-color: #FCE2FE; padding: 15px; border-radius: 8px;'>
                        {verificationCode}
                      </p>

                      <p style='text-align: center; margin: 30px 0;'>
                        <a href='https://ventixe.com/verify-email?email={Uri.EscapeDataString(request.Email)}&code={verificationCode}'
                           style='background-color: #D3DAF9; color: #3D004D; padding: 12px 25px; border-radius: 5px; text-decoration: none; font-weight: bold;'>
                          Verify Email Address
                        </a>
                      </p>

                      <p>If you did not request this verification, please ignore this email.</p>

                      <p style='margin-bottom: 0;'>Best regards,<br>
                      The Ventixe Team</p>
                    </td>
                  </tr>
                  <tr>
                    <td style='padding: 20px; background-color: #ABB4DD; text-align: center; color: #3D004D; font-size: 12px; border-bottom-left-radius: 10px; border-bottom-right-radius: 10px;'>
                      All rights reserved. Ventixe Inc. {DateTime.UtcNow.Year}
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            ";

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAddress"],
                recipients: new EmailRecipients([new(request.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                });

            var sendEmail = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            SaveVerificationCode(new SaveVerificationCodeRequest { Email = request.Email, Code = verificationCode, Expiration = TimeSpan.FromMinutes(5) });

            return new EmailVerificationResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending email: {ex.Message}");
            return new EmailVerificationResult { Succeeded = false, Error = $"An error occurred while sending the verification code: {ex.Message}" };
        }
    }

    /* Save to cache, short-term RandomAccessMemory. */
    public void SaveVerificationCode(SaveVerificationCodeRequest request)
    {
        _cache.Set(request.Email.ToLowerInvariant(), request.Code, request.Expiration);
    }

    /* Validate if code from cache is equal to the code provided from user. */
    public EmailVerificationResult VerifyCode(VerifyEmailRequest request)
    {
        var key = request.Email.ToLowerInvariant();

        if (_cache.TryGetValue(key, out string? storedCode))
        {
            if (storedCode == request.Code)
            {
                _cache.Remove(key);
                return new EmailVerificationResult { Succeeded = true };
            }
        }

        return new EmailVerificationResult { Succeeded = false, Error = "Verification code is invalid or expired." };
    }
}
