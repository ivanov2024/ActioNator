using System;

namespace ActioNator.ViewModels.Community
{
    public class CommentRequest
    {
        public Guid PostId { get; set; }
        public string Content { get; set; } = null!;
        public Guid? ParentCommentId { get; set; }
    }
}
