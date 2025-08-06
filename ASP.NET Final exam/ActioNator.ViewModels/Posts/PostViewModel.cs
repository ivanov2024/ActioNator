using System;
using System.Collections.Generic;

namespace ActioNator.ViewModels.Posts
{
    public class PostViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public int SharesCount { get; set; }
        public bool IsCertified { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public IEnumerable<PostImagesViewModel> Images { get; set; }
        public IEnumerable<PostCommentsViewModel> Comments { get; set; }
    }
}
