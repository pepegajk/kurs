namespace kursecond1.Models;

public class ReviewDto
{
    public int Id { get; set; }
    public int DealId { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public string? ModeratedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CarInfo { get; set; }
}

