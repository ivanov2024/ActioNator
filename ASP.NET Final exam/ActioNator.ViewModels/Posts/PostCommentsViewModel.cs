namespace ActioNator.ViewModels.Posts
{
    public class PostCommentsViewModel
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public string ProfilePictureUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int LikesCount { get; set; }
        public string TimeAgo { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public bool IsAuthor { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
