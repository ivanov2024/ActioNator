// Post comments handler for Alpine.js
document.addEventListener('alpine:init', function() {
    // Define the comments handler component
    Alpine.data('postCommentsHandler', function() {
        return {
            comments: window.postCommentsData || [],
            commentText: '',
            replyingTo: null,
            
            init: function() {
                // Ensure the parent component has these methods
                if (!this.toggleComments) {
                    this.toggleComments = function() {
                        this.showComments = !this.showComments;
                    };
                }
            },
            
            addComment: function() {
                if (this.commentText.trim() === '') return;
                
                // Get the post ID from the parent element
                const postElement = this.$el.closest('[data-post-id]');
                const postId = postElement ? postElement.dataset.postId : null;
                
                if (!postId) {
                    console.error('Could not find post ID');
                    return;
                }
                
                let content = this.commentText;
                let parentCommentId = null;
                
                if (this.replyingTo) {
                    // Use a mention symbol instead of @ to avoid Razor conflicts
                    content = '@' + this.replyingTo + ' ' + this.commentText;
                    
                    // If we're replying to a specific comment, get its ID
                    // This assumes the replyingTo contains the comment ID or we have a way to map username to comment ID
                    // For now, we'll just use null as the parentCommentId
                }
                
                // Call the server-side function to add the comment
                window.addComment(postId, content, parentCommentId);
                
                // Create a temporary comment to show immediately in the UI
                var newComment = {
                    author: 'Current User',
                    avatar: 'bg-purple-300',
                    text: content,
                    time: 'Just now',
                    likes: 0,
                    liked: false,
                    isCertified: false,
                    isAdmin: false,
                    isDeleted: false
                };
                
                this.comments.push(newComment);
                this.commentText = '';
                this.replyingTo = null;
            },
            
            setReplyTo: function(author) {
                this.replyingTo = author;
            }
        };
    });
});
