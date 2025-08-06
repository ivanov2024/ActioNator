using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Community
{
    public class PostViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(2000)]
        [Display(Name = "Content")]
        public string Content { get; set; } = null!;
        
        [Required]
        public Guid AuthorId { get; set; }
        
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; } = null!;
        
        public string ProfilePictureUrl { get; set; } = null!;
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }
        
        public DateTime? ModifiedAt { get; set; }
        
        [Display(Name = "Likes")]
        public int LikesCount { get; set; }
        
        [Display(Name = "Comments")]
        public int CommentsCount { get; set; }
        
        [Display(Name = "Shares")]
        public int SharesCount { get; set; }
        
        public string TimeAgo { get; set; } = null!;
        
        public bool IsAuthor { get; set; }
        
        public bool IsPublic { get; set; }
        
        public bool IsDeleted { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public bool IsCertified { get; set; }
        
        public virtual IEnumerable<PostImageViewModel> Images { get; set; } 
            = new HashSet<PostImageViewModel>();
        
        public virtual IEnumerable<PostCommentViewModel> Comments { get; set; }
            = new HashSet<PostCommentViewModel>();
    }
}
