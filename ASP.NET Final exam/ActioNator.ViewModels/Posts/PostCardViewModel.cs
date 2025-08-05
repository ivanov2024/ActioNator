namespace ActioNator.ViewModels.Posts
{
    public class PostCardViewModel
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = null!;
        public string ProfilePictureUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public int SharesCount { get; set; }
        public string TimeAgo { get; set; } = null!;
        public bool IsAuthor { get; set; }
        public bool IsPublic { get; set; } 
        public bool IsDeleted { get; set; }
        public string? ImageUrl { get; set; }

        public virtual IEnumerable<PostImagesViewModel> Images { get; set; } 
            = new HashSet<PostImagesViewModel>();

        public virtual IEnumerable<PostCommentsViewModel> Comments { get; set; }
            = new HashSet<PostCommentsViewModel>();
    }
}
