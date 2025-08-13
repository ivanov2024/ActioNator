namespace ActioNator.Services.Contracts.Goals
{
    public class CreateGoalDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
    }
}
