/**
 * Alpine.js Global Component Initialization
 * This file ensures all Alpine.js components are properly registered
 * before they are used in the HTML.
 */

// Initialize global data store for post comments
window.postCommentsData = window.postCommentsData || {};

// IMPORTANT: Define Alpine components directly on window.Alpine.data BEFORE Alpine loads
// This ensures components are available immediately when Alpine processes the DOM

// Create a queue of component registrations to run when Alpine is available
window.alpineComponentQueue = window.alpineComponentQueue || [];

// Define the postCard component
const postCardComponent = function(isLiked = false) {
    return {
        isDetailed: false,
        liked: isLiked === true || isLiked === 'true',
        likesCount: 0,
        commentsVisible: false,
        isDeleted: false,
        isAdmin: false,
        isCertified: false,
        comments: [],
        
        init() {
            console.log('postCard init called');
            const postElement = this.$el;
            if (!postElement) return;
            
            // Get data attributes
            this.liked = postElement.dataset.liked === 'true';
            this.likesCount = parseInt(postElement.dataset.likesCount || '0');
            this.isDeleted = postElement.dataset.isDeleted === 'true';
            this.isAdmin = postElement.dataset.isAdmin === 'true';
            this.isCertified = postElement.dataset.isCertified === 'true';
            
            // Get post ID
            const postId = postElement.dataset.postId;
            if (postId && window.postCommentsData[postId]) {
                this.comments = window.postCommentsData[postId] || [];
            }
            
            console.log(`Initialized postCard for post ${postId}`, {
                liked: this.liked,
                likesCount: this.likesCount,
                comments: this.comments.length
            });
        },
        
        toggleLike() {
            console.log('toggleLike called, current state:', this.liked);
            this.liked = !this.liked;
            this.likesCount += this.liked ? 1 : -1;
            if (this.likesCount < 0) this.likesCount = 0;
            
            // If likePost function exists globally, call it
            if (typeof window.likePost === 'function') {
                const postId = this.$el.dataset.postId;
                if (postId) {
                    window.likePost(postId);
                }
            }
        },
        
        toggleComments() {
            console.log('toggleComments called, current state:', this.commentsVisible);
            this.commentsVisible = !this.commentsVisible;
            
            // Synchronize with the comments handler component
            const commentsElement = this.$el.querySelector('[x-data="postCommentsHandler()"]');
            if (commentsElement && commentsElement.__x && commentsElement.__x.$data) {
                commentsElement.__x.$data.commentsVisible = this.commentsVisible;
                console.log('Synchronized comments visibility with handler component');
            }
            
            // If comments are now visible, ensure they're loaded
            if (this.commentsVisible) {
                const postId = this.$el.dataset.postId;
                if (postId && window.postCommentsData[postId]) {
                    this.comments = window.postCommentsData[postId] || [];
                    console.log(`Loaded ${this.comments.length} comments for post ${postId}`);
                    
                    // Also update the comments in the handler component
                    if (commentsElement && commentsElement.__x && commentsElement.__x.$data) {
                        commentsElement.__x.$data.comments = this.comments;
                    }
                }
            }
        }
    };
};

// Define the postCommentsHandler component
const postCommentsHandlerComponent = function() {
    return {
        comments: [],
        commentText: '',
        replyingTo: null,
        commentsVisible: false,

        setCommentsForPost(postId) {
            if (window.postCommentsData && window.postCommentsData[postId]) {
                // Make a deep copy to avoid reference issues
                this.comments = JSON.parse(JSON.stringify(window.postCommentsData[postId]));
            } else {
                this.comments = [];
            }
        },
        
        init() {
            console.log('postCommentsHandler init called');
            // Set comments for this post
            const postElement = this.$el.closest('[data-post-id]');
            const postId = postElement ? postElement.dataset.postId : null;
            
            if (postId) {
                this.setCommentsForPost(postId);
                console.log(`Initialized postCommentsHandler for post ${postId} with ${this.comments.length} comments`);
                
                // Get commentsVisible state from parent postCard component if available
                const postCardEl = this.$el.closest('[x-data^="postCard"]');
                if (postCardEl && postCardEl.__x) {
                    this.commentsVisible = postCardEl.__x.getUnobservedData().commentsVisible || false;
                }
            }
            
            // Listen for comment updates from SignalR
            document.addEventListener('comment-added', (e) => {
                if (e.detail && e.detail.postId === postId) {
                    this.setCommentsForPost(postId);
                }
            });
            
            document.addEventListener('comment-deleted', (e) => {
                if (e.detail && e.detail.postId === postId) {
                    this.setCommentsForPost(postId);
                }
            });
            
            document.addEventListener('comment-updated', (e) => {
                if (e.detail && e.detail.postId === postId) {
                    this.setCommentsForPost(postId);
                }
            });
        },
        
        toggleComments() {
            this.commentsVisible = !this.commentsVisible;
        },
        
        addComment() {
            if (this.commentText.trim() === '') return;
            
            // Only send comment to server; UI will be updated by SignalR real-time event.
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
                // For now, we'll just use null as the parentCommentId
            }
            
            // Use the communityHub connection directly if available
            if (window.communityHub) {
                window.communityHub.invoke('AddComment', postId, content)
                    .catch(err => console.error('Error sending comment via SignalR:', err));
            } else {
                // Fallback to traditional AJAX
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                fetch(`/User/Community/AddComment`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-CSRF-TOKEN': token
                    },
                    body: JSON.stringify({
                        postId: postId,
                        content: content
                    })
                }).catch(err => console.error('Error sending comment via AJAX:', err));
            }
            
            // Clear the comment input field after submission
            this.commentText = '';
            this.replyingTo = null;
        },
        
        replyToComment(author) {
            this.replyingTo = author;
            // Focus the comment textarea
            this.$nextTick(() => {
                const textarea = this.$refs.commentTextarea;
                if (textarea) {
                    textarea.focus();
                }
            });
        }
    };
};

// Add components to the registration queue
window.alpineComponentQueue.push({
    name: 'postCard',
    component: postCardComponent
});

window.alpineComponentQueue.push({
    name: 'postCommentsHandler',
    component: postCommentsHandlerComponent
});

// Function to register all components
function registerAlpineComponents() {
    if (!window.Alpine || !window.Alpine.data) {
        console.error("Alpine.js is not available yet");
        return false;
    }
    
    console.log("Registering Alpine.js components");
    
    // Process the queue
    window.alpineComponentQueue.forEach(item => {
        window.Alpine.data(item.name, item.component);
        console.log(`Registered component: ${item.name}`);
    });
    
    console.log("All Alpine.js components registered successfully");
    return true;
}

// CRITICAL: Make the components available globally for direct access
window.postCard = postCardComponent;
window.postCommentsHandler = postCommentsHandlerComponent;

// Register components when Alpine initializes
document.addEventListener('alpine:init', () => {
    console.log("Alpine:init event triggered");
    registerAlpineComponents();
});

// Immediate attempt to register components if Alpine is already available
if (window.Alpine && window.Alpine.data) {
    console.log("Alpine.js already available, registering components immediately");
    registerAlpineComponents();
}

// Backup: Register when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    console.log("DOM loaded, checking Alpine.js status");
    if (window.Alpine && window.Alpine.data) {
        registerAlpineComponents();
    } else {
        console.warn("Alpine.js not available at DOMContentLoaded, will try again");
        // Try again after a short delay
        setTimeout(() => {
            if (window.Alpine && window.Alpine.data) {
                registerAlpineComponents();
            } else {
                console.error("Alpine.js still not available after delay");
            }
        }, 500);
    }
});

