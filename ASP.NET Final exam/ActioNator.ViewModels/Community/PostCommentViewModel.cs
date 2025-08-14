using System;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Community
{
    public class PostCommentViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(1000)]
        [Display(Name = "Content")]
        public string Content { get; set; } = null!;
        
        public Guid AuthorId { get; set; }
        
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; } = null!;
        
        public string AuthorProfilePicture { get; set; } = null!;
        
        // Alias for view compatibility
        public string ProfilePictureUrl { get => AuthorProfilePicture; set => AuthorProfilePicture = value; }
        
        // Author role flags for UI badges
        public bool AuthorIsAdmin { get; set; }
        public bool AuthorIsCoach { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }
        
        [Display(Name = "Likes")]
        public int LikeCount { get; set; }
        
        // Alias for view compatibility
        public int LikesCount { get => LikeCount; set => LikeCount = value; }
        
        public string TimeAgo { get; set; } = null!;
        
        public bool IsDeleted { get; set; }
        
        public bool IsAuthor { get; set; }
        
        public bool IsLiked { get; set; }
        
        // Alias for view compatibility
        public bool IsLikedByCurrentUser { get => IsLiked; set => IsLiked = value; }
        
        public Guid PostId { get; set; }
    }
}
