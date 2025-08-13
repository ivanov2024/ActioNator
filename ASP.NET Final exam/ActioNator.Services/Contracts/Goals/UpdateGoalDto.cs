namespace ActioNator.Services.Contracts.Goals
{
    public class UpdateGoalDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool Completed { get; set; }
    }
}
