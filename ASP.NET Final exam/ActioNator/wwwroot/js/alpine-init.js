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
            if (postId) {
                if (window.postCommentsData[postId]) {
                    this.comments = window.postCommentsData[postId] || [];
                } else {
                    // Seed from data-comments attribute when available
                    const raw = postElement.dataset.comments;
                    if (raw) {
                        try {
                            const parsed = JSON.parse(raw);
                            this.comments = parsed || [];
                            window.postCommentsData[postId] = parsed || [];
                        } catch (e) {
                            console.error('Failed to parse data-comments for post', postId, e);
                            this.comments = [];
                        }
                    }
                }
            }
            
            console.log(`Initialized postCard for post ${postId}`, {
                liked: this.liked,
                likesCount: this.likesCount,
                comments: this.comments.length
            });

            // Cache the comments container element for faster/robust access
            this.$commentsEl = this.$el.querySelector('.comments-container');
            console.log('postCard comments container found?', !!this.$commentsEl);
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
            
            // Notify the comments handler component via event (use cached element if available)
            const commentsElement = this.$commentsEl || this.$el.querySelector('[x-data="postCommentsHandler()"], .comments-container[x-data]');
            if (commentsElement) {
                console.log('Dispatching toggle-comments to comments handler with visible =', this.commentsVisible);
                commentsElement.dispatchEvent(new CustomEvent('toggle-comments', {
                    detail: { visible: this.commentsVisible },
                    bubbles: true
                }));

                // Imperative fallback to ensure visibility toggles even if Alpine misses the event
                try {
                    commentsElement.style.display = this.commentsVisible ? 'block' : 'none';
                } catch {}
            } else {
                console.warn('Comments handler element not found under post card');
            }

            // Always dispatch a document-level event keyed by postId as a fallback
            const postId = this.$el.dataset.postId;
            if (postId) {
                document.dispatchEvent(new CustomEvent('post-toggle-comments', {
                    detail: { postId, visible: this.commentsVisible }
                }));
            }
            
            // If comments are now visible, ensure they're loaded
            if (this.commentsVisible) {
                if (postId && window.postCommentsData[postId]) {
                    this.comments = window.postCommentsData[postId] || [];
                    console.log(`Loaded ${this.comments.length} comments for post ${postId}`);
                    
                    // Also update the comments in the handler component via event
                    if (commentsElement) {
                        console.log('Dispatching set-comments to comments handler with', this.comments.length, 'comments');
                        commentsElement.dispatchEvent(new CustomEvent('set-comments', {
                            detail: { comments: this.comments },
                            bubbles: true
                        }));
                    }

                    // And dispatch a document-level event as a fallback
                    if (postId) {
                        document.dispatchEvent(new CustomEvent('post-set-comments', {
                            detail: { postId, comments: this.comments }
                        }));
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

        // Ensure each comment has an isOwner flag using the current user id on the post element
        applyOwnershipFromDataset() {
            try {
                const postElement = this.$el.closest('[data-post-id]');
                const me = (postElement && postElement.dataset && postElement.dataset.currentUserId) ? postElement.dataset.currentUserId.toLowerCase() : '';
                if (!Array.isArray(this.comments)) this.comments = [];
                this.comments = this.comments.map(c => {
                    const authorId = (c && (c.authorId ?? c.AuthorId) != null) ? String(c.authorId ?? c.AuthorId).toLowerCase() : '';
                    const isOwner = me && authorId && me === authorId;
                    // Preserve existing props and set isOwner
                    return Object.assign({}, c, { isOwner });
                });
            } catch (e) {
                console.warn('applyOwnershipFromDataset failed', e);
            }
        },

        setCommentsForPost(postId) {
            if (window.postCommentsData && window.postCommentsData[postId]) {
                // Make a deep copy to avoid reference issues
                this.comments = JSON.parse(JSON.stringify(window.postCommentsData[postId]));
                this.applyOwnershipFromDataset();
            } else {
                // Fallback: seed from the closest post element's data-comments
                const postElement = this.$el.closest('[data-post-id]');
                const raw = postElement && postElement.dataset ? postElement.dataset.comments : null;
                if (raw) {
                    try {
                        const parsed = JSON.parse(raw);
                        this.comments = parsed || [];
                        window.postCommentsData = window.postCommentsData || {};
                        window.postCommentsData[postId] = parsed || [];
                        this.applyOwnershipFromDataset();
                    } catch (e) {
                        console.error('Failed to parse data-comments in handler for post', postId, e);
                        this.comments = [];
                    }
                } else {
                    this.comments = [];
                }
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

                // Listen for visibility and comments updates from parent via direct events
                this.$el.addEventListener('toggle-comments', (e) => {
                    const next = !!(e.detail && e.detail.visible);
                    console.log('postCommentsHandler received toggle-comments, visible ->', next);
                    this.commentsVisible = next;
                    if (this.commentsVisible) {
                        this.$nextTick(() => {
                            if (this.$refs.commentTextarea) {
                                try { this.$refs.commentTextarea.focus(); } catch {}
                            }
                        });
                    }
                });
                this.$el.addEventListener('set-comments', (e) => {
                    const incoming = e.detail && e.detail.comments;
                    const list = Array.isArray(incoming) ? incoming : [];
                    console.log('postCommentsHandler received set-comments for', list.length, 'comments');
                    this.comments = list;
                    this.applyOwnershipFromDataset();
                });

                // Also listen at the document level keyed by postId (robust fallback)
                document.addEventListener('post-toggle-comments', (e) => {
                    if (!e.detail || e.detail.postId !== postId) return;
                    const next = !!e.detail.visible;
                    console.log('postCommentsHandler received DOCUMENT post-toggle-comments for post', postId, '->', next);
                    this.commentsVisible = next;
                    if (this.commentsVisible) {
                        this.$nextTick(() => {
                            if (this.$refs.commentTextarea) {
                                try { this.$refs.commentTextarea.focus(); } catch {}
                            }
                        });
                    }
                });
                document.addEventListener('post-set-comments', (e) => {
                    if (!e.detail || e.detail.postId !== postId) return;
                    const list = Array.isArray(e.detail.comments) ? e.detail.comments : [];
                    console.log('postCommentsHandler received DOCUMENT post-set-comments for post', postId, 'with', list.length, 'comments');
                    this.comments = list;
                    this.applyOwnershipFromDataset();
                });
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
            const raw = this.commentText == null ? '' : this.commentText;
            const trimmed = raw.trim();
            const MIN = 1;
            const MAX = 500; // mirrors ValidationConstants.Comment.ContentMaxLength
            if (trimmed.length < MIN) {
                // Already disabled in UI when empty, but guard here too
                return;
            }
            if (trimmed.length > MAX) {
                if (window.$store && window.$store.toast) {
                    window.$store.toast.show(`Comment is too long (${trimmed.length}/${MAX}).`, 'error');
                } else {
                    alert(`Comment is too long (${trimmed.length}/${MAX}). Max ${MAX} characters.`);
                }
                return;
            }
            
            // Only send comment to server; UI will be updated by SignalR real-time event.
            const postElement = this.$el.closest('[data-post-id]');
            const postId = postElement ? postElement.dataset.postId : null;
            if (!postId) {
                console.error('Could not find post ID');
                return;
            }
            
            let content = trimmed;
            let parentCommentId = null;
            
            if (this.replyingTo) {
                // Use a mention symbol instead of @ to avoid Razor conflicts
                content = '@' + this.replyingTo + ' ' + trimmed;
                // For now, we'll just use null as the parentCommentId
            }
            
            // Use the communityHub connection directly if available
            const doAjaxFallback = () => {
                // Fallback to traditional AJAX with safe token retrieval and UI update on success
                const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                const token = tokenEl ? tokenEl.value : '';
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
                })
                .then(resp => {
                    if (!resp.ok) throw new Error(`AJAX AddComment failed with status ${resp.status}`);
                    return resp.json();
                })
                .then(data => {
                    try {
                        if (data && data.success && data.comment) {
                            const c = data.comment;
                            const pid = ((c && (c.postId || c.PostId)) || postId).toString();
                            window.postCommentsData = window.postCommentsData || {};
                            window.postCommentsData[pid] = window.postCommentsData[pid] || [];
                            const exists = window.postCommentsData[pid].some(x => x && x.id === c.id);
                            if (!exists) {
                                window.postCommentsData[pid].push(c);
                            }
                            // Notify Alpine components to refresh their state for this post
                            document.dispatchEvent(new CustomEvent('comment-added', {
                                detail: { postId: pid, commentId: c.id }
                            }));
                        }
                    } catch (e) {
                        console.error('Error handling AddComment AJAX response:', e);
                    }
                })
                .catch(err => console.error('Error sending comment via AJAX:', err));
            };

            if (window.communityHub && typeof window.communityHub.invoke === 'function') {
                window.communityHub.invoke('AddComment', postId, content)
                    .catch(err => {
                        console.error('Error sending comment via SignalR:', err);
                        // Fallback if SignalR invoke fails
                        doAjaxFallback();
                    });
            } else {
                doAjaxFallback();
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

