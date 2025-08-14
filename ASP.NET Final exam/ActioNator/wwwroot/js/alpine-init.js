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

// Define the postImagesHandler component
const postImagesHandlerComponent = function(initial = []) {
    return {
        postImages: [],
        displayedImages: [],
        
        init() {
            try {
                let raw = initial;
                // Prefer data-images attribute on the container if present
                if (!Array.isArray(raw) || raw.length === 0) {
                    const dataAttr = this.$el?.dataset?.images;
                    if (dataAttr) {
                        try { raw = JSON.parse(dataAttr); } catch { raw = []; }
                    }
                }
                this.setImages(raw);
                
                // Subtle entrance animation for the grid
                this.$nextTick(() => {
                    const gridElement = this.$el.querySelector('.dynamic-image-grid');
                    if (gridElement && this.postImages && this.postImages.length > 0) {
                        gridElement.style.opacity = '0';
                        gridElement.style.transform = 'scale(0.95)';
                        setTimeout(() => {
                            gridElement.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                            gridElement.style.opacity = '1';
                            gridElement.style.transform = 'scale(1)';
                        }, 100);
                    }
                });
            } catch (e) {
                console.warn('postImagesHandler init failed', e);
            }
        },
        
        setImages(list) {
            const norm = Array.isArray(list)
                ? list.map((it) => {
                    const url = (it && (it.url || it.imageUrl || it.ImageUrl)) || '';
                    return url ? { url, alt: it?.alt || 'Post image', loading: false } : null;
                }).filter(Boolean)
                : [];
            this.postImages = norm;
            this.displayedImages = norm.length <= 3 ? norm : [norm[0], norm[1], norm[2]];
        },
        
        getGridClass() {
            const c = this.postImages.length;
            if (c <= 1) return 'grid grid-cols-1';
            if (c === 2) return 'grid grid-cols-2 gap-2 md:gap-3';
            // 3 or more
            return 'grid grid-cols-2 grid-rows-2 gap-2 md:gap-3';
        },
        
        getImageContainerClass(index) {
            const c = this.postImages.length;
            if (c <= 2) return 'col-span-1 row-span-1';
            // 3 or more: make the first image tall
            return index === 0 ? 'row-span-2 col-span-1' : 'col-span-1 row-span-1';
        },
        
        openImageModal(startIndex) {
            try {
                document.dispatchEvent(new CustomEvent('open-image-modal', {
                    detail: { images: this.postImages, index: startIndex }
                }));
            } catch {}
        },
        
        handleImageError(event, index) {
            try {
                if (this.displayedImages[index]) {
                    this.displayedImages[index].url = '/images/placeholder-image.png';
                    this.displayedImages[index].loading = false;
                }
            } catch {}
        }
    };
};

// Register postImagesHandler in the queue
window.alpineComponentQueue.push({
    name: 'postImagesHandler',
    component: postImagesHandlerComponent
});

// Define the imageGalleryOverlay component
const imageGalleryOverlayComponent = function() {
    return {
        open: false,
        images: [],
        index: 0,
        animating: false,
        phase: 'in', // 'out'|'in'
        transitionDir: 0, // -1 left, 1 right
        _prevOverflow: '',

        init() {
            // Listen for global open event dispatched by postImagesHandler
            document.addEventListener('open-image-modal', (e) => {
                try {
                    const arr = (e.detail && Array.isArray(e.detail.images)) ? e.detail.images : [];
                    const idx = (e.detail && typeof e.detail.index === 'number') ? e.detail.index : 0;
                    this.openWith(arr, idx);
                } catch {}
            });

            // Keyboard controls when gallery is open
            window.addEventListener('keydown', (ev) => {
                if (!this.open) return;
                if (ev.key === 'Escape') {
                    ev.preventDefault();
                    this.close();
                } else if (ev.key === 'ArrowRight') {
                    ev.preventDefault();
                    this.next();
                } else if (ev.key === 'ArrowLeft') {
                    ev.preventDefault();
                    this.prev();
                }
            });
        },

        openWith(arr, startIndex) {
            const mapped = Array.isArray(arr)
                ? arr.map((it) => {
                    const url = (it && (it.url || it.imageUrl || it.ImageUrl)) || '';
                    const alt = (it && (it.alt || it.Alt)) || 'Image';
                    return url ? { url, alt } : null;
                }).filter(Boolean)
                : [];
            if (mapped.length === 0) return;

            this.images = mapped;
            const idx = Number.isFinite(startIndex) ? startIndex : 0;
            this.index = Math.min(Math.max(0, idx), this.images.length - 1);
            this.open = true;
            this._lockScroll();
            this.$nextTick(() => this.preloadAround());
        },

        close() {
            this.open = false;
            this._unlockScroll();
        },

        currentImage() {
            return this.images[this.index] || { url: '', alt: '' };
        },

        next() {
            if (this.images.length <= 1) return;
            this._goto(this.index + 1, 1);
        },

        prev() {
            if (this.images.length <= 1) return;
            this._goto(this.index - 1, -1);
        },

        _goto(nextIndex, dir) {
            if (this.animating) return;
            const len = this.images.length;
            this.transitionDir = dir >= 0 ? 1 : -1;
            const normalized = (nextIndex % len + len) % len;

            this.animating = true;
            this.phase = 'out';
            // Allow CSS to apply the out transition first
            requestAnimationFrame(() => {
                setTimeout(() => {
                    this.index = normalized;
                    this.phase = 'in';
                    this.$nextTick(() => {
                        this.preloadAround();
                        setTimeout(() => { this.animating = false; }, 180);
                    });
                }, 180);
            });
        },

        preloadAround() {
            try {
                const len = this.images.length;
                if (len <= 1) return;
                const left = this.images[(this.index - 1 + len) % len]?.url;
                const right = this.images[(this.index + 1) % len]?.url;
                [left, right].forEach(u => {
                    if (!u) return;
                    const img = new Image();
                    img.src = u;
                });
            } catch {}
        },

        imageTransitionClass() {
            if (!this.animating) return 'opacity-100 translate-x-0';
            if (this.phase === 'out') {
                return this.transitionDir > 0 ? 'opacity-0 -translate-x-6' : 'opacity-0 translate-x-6';
            }
            // phase === 'in'
            return this.transitionDir > 0 ? 'opacity-0 translate-x-6' : 'opacity-0 -translate-x-6';
        },

        counterText() {
            return `${this.index + 1}/${this.images.length}`;
        },

        _lockScroll() {
            try {
                const el = document.documentElement;
                this._prevOverflow = el.style.overflow || '';
                el.style.overflow = 'hidden';
            } catch {}
        },

        _unlockScroll() {
            try {
                const el = document.documentElement;
                el.style.overflow = this._prevOverflow || '';
            } catch {}
        }
    };
};

// Register imageGalleryOverlay in the queue
window.alpineComponentQueue.push({
    name: 'imageGalleryOverlay',
    component: imageGalleryOverlayComponent
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
window.postImagesHandler = postImagesHandlerComponent;
window.imageGalleryOverlay = imageGalleryOverlayComponent;

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

