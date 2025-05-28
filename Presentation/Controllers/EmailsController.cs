using Microsoft.AspNetCore.Mvc;
using Presentation.Interfaces;
using Presentation.Models;

namespace Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailsController(IEmailService emailService) : ControllerBase
{
    private readonly IEmailService _emailService = emailService;

    [HttpPost("send")]
    public async Task<IActionResult> Send(EmailVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Recipient email not provided!" });
 
        var result = await _emailService.SendVerificationCodeAsync(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);
    }

    [HttpPost("verify")]
    public IActionResult Verify(VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Code is invalid or expired" });

        var result = _emailService.VerifyCode(request);
        return result.Succeeded
            ? Ok(result)
            : StatusCode(500, result);
    }
}
