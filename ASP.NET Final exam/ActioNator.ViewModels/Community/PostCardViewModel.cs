using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Community
{
    public class PostCardViewModel
    {
        public Guid Id { get; set; }
        
        [MaxLength(2000)]
        [Display(Name = "Content")]
        public string? Content { get; set; }
        
        public Guid AuthorId { get; set; }
        
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; } = null!;
        
        public string AuthorProfilePicture { get; set; } = null!;
        
        // Alias for view compatibility
        public string ProfilePictureUrl { get => AuthorProfilePicture; set => AuthorProfilePicture = value; }
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }
        
        [Display(Name = "Likes")]
        public int LikeCount { get; set; }
        
        // Alias for view compatibility
        public int LikesCount { get => LikeCount; set => LikeCount = value; }
        
        [Display(Name = "Comments")]
        public int CommentCount { get; set; }
        
        // Alias for view compatibility
        public int CommentsCount { get => CommentCount; set => CommentCount = value; }
        
        [Display(Name = "Shares")]
        public int SharesCount { get; set; }
        
        public string TimeAgo { get; set; } = null!;
        
        public bool IsAuthor { get; set; }
        
        public bool IsPublic { get; set; }
        
        public bool IsDeleted { get; set; }
        
        public bool IsLiked { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public virtual IEnumerable<PostImageViewModel> Images { get; set; } 
            = new HashSet<PostImageViewModel>();
        
        public virtual IEnumerable<PostCommentViewModel> Comments { get; set; }
            = new HashSet<PostCommentViewModel>();
    }
}
