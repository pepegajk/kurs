using System.ComponentModel.DataAnnotations;

namespace kursecondapi.Models;

public class CreateReviewRequestDto
{
    [Required(ErrorMessage = "ID сделки обязателен")]
    public int DealId { get; set; }

    [Required(ErrorMessage = "Рейтинг обязателен")]
    [Range(1, 5, ErrorMessage = "Рейтинг должен быть от 1 до 5")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Комментарий не может быть длиннее 1000 символов")]
    public string? Comment { get; set; }
}
