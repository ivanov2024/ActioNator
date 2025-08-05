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
                
                var newComment = {
                    author: 'Current User',
                    avatar: 'bg-purple-300',
                    text: this.commentText,
                    time: 'Just now',
                    likes: 0,
                    liked: false,
                    isCertified: false,
                    isAdmin: false,
                    isDeleted: false
                };
                
                if (this.replyingTo) {
                    // Use a mention symbol instead of @ to avoid Razor conflicts
                    newComment.text = '<span class="font-medium text-facebook-blue">@</span>' + 
                                      '<span class="font-medium text-facebook-blue">' + 
                                      this.replyingTo + '</span> ' + this.commentText;
                    this.replyingTo = null;
                }
                
                this.comments.push(newComment);
                this.commentText = '';
            },
            
            setReplyTo: function(author) {
                this.replyingTo = author;
            }
        };
    });
});
