using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.TableDTOs;

public class CreateTableDTO
{
    [Range(1, 100000)]
    public int Number { get; set; }
    [Range(1, 30)]
    public int Seats { get; set; }
}
