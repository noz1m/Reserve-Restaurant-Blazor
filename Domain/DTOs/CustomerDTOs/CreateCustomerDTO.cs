using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.CustomerDTOs;

public class CreateCustomerDTO
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;
}
