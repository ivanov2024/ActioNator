namespace ActioNator.ViewModels.Posts
{
    public class PostImagesViewModel
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = null!;
        public Guid? PostId { get; set; }
    }
}
