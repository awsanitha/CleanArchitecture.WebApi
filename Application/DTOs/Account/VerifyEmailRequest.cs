using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Account
{
    public class VerifyEmailRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
