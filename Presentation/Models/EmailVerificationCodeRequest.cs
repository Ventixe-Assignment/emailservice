using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class EmailVerificationCodeRequest
{
    [Required]
    public string Email { get; set; } = null!;

}
