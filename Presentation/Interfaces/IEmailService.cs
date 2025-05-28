using Presentation.Models;

namespace Presentation.Interfaces
{
    public interface IEmailService
    {
        void SaveVerificationCode(SaveVerificationCodeRequest request);
        Task<EmailVerificationResult> SendVerificationCodeAsync(EmailVerificationCodeRequest request);
        EmailVerificationResult VerifyCode(VerifyEmailRequest request);
    }
}