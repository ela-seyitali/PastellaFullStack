namespace Pastella.Backend.Core.DTOs
{
    public class CreateCommentDto
    {
        public int CakeId { get; set; }
        public int Rating { get; set; } // 1-5 arası
        public string Message { get; set; } = string.Empty;
    }
}