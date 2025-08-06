// Tailwind configuration
tailwind.config = {
    theme: {
        extend: {
            colors: {
                "facebook-blue": "#1877F2",
                "facebook-bg": "#F0F2F5",
                "facebook-card": "#FFFFFF",
                "facebook-hover": "#E7F3FF",
            },
        },
    },
};

// Global variable to track the currently open dropdown
let currentOpenDropdown = null;

// Function to close all dropdowns
function closeAllDropdowns() {
    // Close standard dropdowns with the dropdown-menu class
    const dropdowns = document.querySelectorAll(".dropdown-menu:not(.hidden)");
    dropdowns.forEach((dropdown) => {
        dropdown.classList.add("hidden");
    });

    // Close Alpine.js dropdowns
    document.querySelectorAll("[x-data]").forEach(function (el) {
        if (
            el.__x &&
            el.__x.$data &&
            typeof el.__x.$data.showMenu !== "undefined"
        ) {
            el.__x.$data.showMenu = false;
        }
    });

    // Reset the currently open dropdown reference
    currentOpenDropdown = null;

    // Also reset the Alpine.js store's active dropdown
    if (window.Alpine && Alpine.store("dropdowns")) {
        Alpine.store("dropdowns").activeDropdown = null;
    }
}

// Close all dropdown menus when clicking outside
document.addEventListener("click", function (event) {
    // Don't close if clicking on a dropdown button or inside a dropdown
    if (
        event.target.closest(".dropdown-menu") ||
        event.target.closest(
            "button[x-ref], button[x-on\\:click], button[x-on\\:click\\.stop]"
        )
    ) {
        return;
    }

    closeAllDropdowns();
});

// Close all dropdown menus when scrolling
window.addEventListener("scroll", function () {
    closeAllDropdowns();
});

// Global event handler for all dropdown buttons
document.addEventListener("click", function (event) {
    // Check if this is a dropdown button click
    const dropdownButton = event.target.closest("button");
    if (!dropdownButton) return;

    // Check if this is a dropdown toggle button
    const isStandardDropdown =
        dropdownButton.nextElementSibling &&
        dropdownButton.nextElementSibling.classList.contains("dropdown-menu");

    const isAlpineDropdown =
        dropdownButton.hasAttribute("x-on:click") ||
        dropdownButton.hasAttribute("x-on:click.stop") ||
        dropdownButton.hasAttribute("@click") ||
        dropdownButton.hasAttribute("@click.stop");

    // If it's not a dropdown button - exit early
    if (!isStandardDropdown && !isAlpineDropdown) return;

    // Needed to check if it's a dropdown toggle
    if (isAlpineDropdown) {
        const alpineComponent = dropdownButton.closest("[x-data]");
        if (
            !alpineComponent ||
            !alpineComponent.__x ||
            !alpineComponent.__x.$data ||
            typeof alpineComponent.__x.$data.showMenu === "undefined"
        ) {
            return; // Not a dropdown toggle button
        }

        if (alpineComponent.__x.$data.showMenu) {
            return;
        }

        // If opening a dropdown, close all others first
        event.preventDefault(); // Prevent default Alpine.js behavior
        event.stopPropagation(); // Stop event propagation

        closeAllDropdowns();

        // Then open this dropdown
        setTimeout(() => {
            alpineComponent.__x.$data.showMenu = true;
            if (typeof alpineComponent.__x.$data.btnRect === "undefined") {
                alpineComponent.__x.$data.btnRect =
                    dropdownButton.getBoundingClientRect();
            }
            currentOpenDropdown = alpineComponent;
        }, 10);
    }

    // For standard dropdowns
    if (isStandardDropdown) {
        const clickedDropdown = dropdownButton.nextElementSibling;
        const isCurrentlyHidden = clickedDropdown.classList.contains("hidden");

        // If we're opening the dropdown
        if (isCurrentlyHidden) {
            event.preventDefault();
            event.stopPropagation();

            closeAllDropdowns();

            // Then open this dropdown
            setTimeout(() => {
                clickedDropdown.classList.remove("hidden");
                currentOpenDropdown = clickedDropdown;
            }, 10);
        }
    }
});

// Textarea auto-resize function
function autoResizeTextarea(el) {
    el.style.height = "auto";
    el.style.height = el.scrollHeight + "px";
}

// Alpine.js initialization
document.addEventListener("alpine:init", () => {
    // Debug Alpine.js initialization
    console.log("Alpine.js initialized");

    // Global posts store for managing posts and loading state
    Alpine.store("posts", {
        isLoading: true,
        loadingCount: 3,
        posts: [],

        init() {
            // Show loading skeleton
            this.showLoadingSkeleton();

            // Simulate loading posts
            setTimeout(() => {
                this.hideLoadingSkeleton();
            }, 2000); // Show loading state for 2 seconds
        },

        // Show loading skeleton
        showLoadingSkeleton() {
            this.isLoading = true;
            const loadingSkeleton = document.getElementById("loading-skeleton");
            if (loadingSkeleton) {
                loadingSkeleton.innerHTML = Array(this.loadingCount)
                    .fill(
                        `
                    <div class="skeleton-card p-4 mb-4">
                        <div class="flex justify-between items-start mb-3">
                            <div class="flex items-center">
                                <div class="skeleton w-10 h-10 rounded-full mr-3"></div>
                                <div>
                                    <div class="skeleton w-32 h-4 rounded mb-2"></div>
                                    <div class="skeleton w-24 h-3 rounded"></div>
                                </div>
                            </div>
                            <div class="skeleton w-8 h-8 rounded-full"></div>
                        </div>
                        <div class="skeleton w-full h-4 rounded mb-3"></div>
                        <div class="skeleton w-2/3 h-4 rounded mb-4"></div>
                        <div class="skeleton w-full h-48 rounded mb-3"></div>
                        <div class="flex justify-between items-center py-2 border-t border-b border-gray-200 mb-2">
                            <div class="skeleton w-16 h-4 rounded"></div>
                            <div class="skeleton w-20 h-4 rounded"></div>
                        </div>
                        <div class="flex justify-between items-center pt-2">
                            <div class="skeleton w-24 h-8 rounded"></div>
                            <div class="skeleton w-24 h-8 rounded"></div>
                            <div class="skeleton w-24 h-8 rounded"></div>
                        </div>
                    </div>
                `
                    )
                    .join("");
                loadingSkeleton.classList.remove("hidden");
            }
        },

        // Hide loading skeleton
        hideLoadingSkeleton() {
            this.isLoading = false;
            const loadingSkeleton = document.getElementById("loading-skeleton");
            const postsContainer = document.getElementById("posts-container");
            if (loadingSkeleton) {
                loadingSkeleton.classList.add("hidden");
            }
            if (postsContainer) {
                postsContainer.classList.remove("hidden");
            }
            console.log("Loading complete, posts shown");
        },
    });

    // Global dropdowns store for managing all dropdowns
    Alpine.store("dropdowns", {
        activeDropdown: null,
        idCounter: 0,

        // Track which dropdown is currently open
        setActiveDropdown(dropdown) {
            // Close previous dropdown if it's not the same one
            if (this.activeDropdown && this.activeDropdown !== dropdown) {
                this.activeDropdown.showMenu = false;
                if (this.activeDropdown.btnRect) {
                    this.activeDropdown.btnRect = null;
                }
            }
            this.activeDropdown = dropdown;
        },

        closeAll() {
            this.activeDropdown = null;
        },
        setActive(id) {
            this.activeDropdown = id;
        },
        generateId() {
            return `dropdown-${++this.idCounter}`;
        },
    });

    // Global modal store
    Alpine.store("modal", {
        modalOpen: false,
        modalType: "default", // default, create-post, image-preview
        modalTitle: "",
        modalMessage: "",
        modalConfirmText: "OK",
        modalCancelText: "Cancel",
        modalConfirmAction: () => { },
        postText: "",
        postImages: [],
        taggedFriends: [],
        searchQuery: "",
        friendsList: [],
        taggedFriends: [],
        friendSearchQuery: "",
        availableFriends: [
            { id: 1, name: "Jane Smith", avatar: "bg-blue-300" },
            { id: 2, name: "Mike Johnson", avatar: "bg-green-300" },
            { id: 3, name: "Sarah Williams", avatar: "bg-yellow-300" },
            { id: 4, name: "David Brown", avatar: "bg-red-300" },
        ],
        showFriendsList: false,

        openCreatePostModal() {
            console.log("Store: openCreatePostModal called");
            this.modalTitle = "Create Post";
            this.modalConfirmText = "Post";
            this.modalCancelText = "Cancel";
            this.modalType = "create-post";
            this.postText = "";
            this.postPrivacy = "public";
            this.postImages = [];
            this.taggedFriends = [];
            this.modalOpen = true;
        },

        closeModal() {
            console.log("Store: closeModal called");
            this.modalOpen = false;
            this.postText = "";
            this.postImages = [];
            this.taggedFriends = [];
            this.friendSearchQuery = "";
            this.showFriendsList = false;
        },

        confirmModal() {
            if (this.modalType === "create-post") {
                this.createPost();
            } else if (this.modalConfirmAction) {
                this.modalConfirmAction();
            }
            this.closeModal();
        },

        createPost() {
            console.log("Creating post with:", {
                text: this.postText,
                privacy: this.postPrivacy,
                images:
                    this.postImages.length > 0
                        ? `${this.postImages.length} images attached`
                        : "No images",
                taggedFriends: this.taggedFriends.map((f) => f.name),
            });
            alert("Post created successfully!");
        },

        handleImageUpload(event) {
            const files = event.target.files;
            if (files && files.length > 0) {
                // Check if adding new images would exceed the limit of 10
                if (this.postImages.length + files.length > 10) {
                    alert("You can only upload up to 10 images");
                    return;
                }

                // Process each selected file
                Array.from(files).forEach((file) => {
                    const reader = new FileReader();
                    reader.onload = (e) => {
                        this.postImages.push({
                            id: Date.now() + Math.random().toString(36).substring(2, 15),
                            src: e.target.result,
                        });
                    };
                    reader.readAsDataURL(file);
                });
            }
        },

        removeImage(imageId) {
            this.postImages = this.postImages.filter((img) => img.id !== imageId);
        },

        toggleFriendsList() {
            this.showFriendsList = !this.showFriendsList;
        },

        tagFriend(friend) {
            if (!this.taggedFriends.find((f) => f.id === friend.id)) {
                this.taggedFriends.push(friend);
            }
            this.showFriendsList = false;
            this.friendSearchQuery = "";
        },

        untagFriend(friendId) {
            this.taggedFriends = this.taggedFriends.filter((f) => f.id !== friendId);
        },

        filteredFriends() {
            return this.availableFriends.filter(
                (friend) =>
                    friend.name
                        .toLowerCase()
                        .includes(this.friendSearchQuery.toLowerCase()) &&
                    !this.taggedFriends.find((f) => f.id === friend.id)
            );
        },
    });

    // Create a global Alpine.js component for the modal
    Alpine.data("globalModal", () => ({
        modalOpen: false,
        modalTitle: "",
        modalMessage: "",
        modalConfirmText: "Confirm",
        modalCancelText: "Cancel",
        modalConfirmAction: null,
        modalType: "default", // Added modalType to handle different modal types
        postText: "",
        postPrivacy: "public",
        postImage: null,
        taggedFriends: [],
        friendSearchQuery: "",
        availableFriends: [
            { id: 1, name: "Jane Smith", avatar: "bg-blue-300" },
            { id: 2, name: "Mike Johnson", avatar: "bg-green-300" },
            { id: 3, name: "Sarah Williams", avatar: "bg-yellow-300" },
            { id: 4, name: "David Brown", avatar: "bg-red-300" },
        ],
        showFriendsList: false,

        init() {
            // Listen for the create-post event
            this.$watch("modalOpen", (value) => {
                console.log("modalOpen changed:", value);
            });

            // Listen for the open-create-post-modal event at the window level
            window.addEventListener("open-create-post-modal", () => {
                console.log("open-create-post-modal event received");
                this.openCreatePostModal();
            });
        },

        openModal(
            title,
            message,
            confirmText = "Confirm",
            cancelText = "Cancel",
            confirmAction = null
        ) {
            console.log("Global openModal called", { title, message });
            this.modalTitle = title;
            this.modalMessage = message;
            this.modalConfirmText = confirmText;
            this.modalCancelText = cancelText;
            this.modalConfirmAction = confirmAction;
            this.modalType = "default";
            this.modalOpen = true;
        },

        openCreatePostModal() {
            console.log("openCreatePostModal called");
            this.modalTitle = "Create Post";
            this.modalConfirmText = "Post";
            this.modalCancelText = "Cancel";
            this.modalType = "create-post";
            this.postText = "";
            this.postPrivacy = "public";
            this.postImage = null;
            this.taggedFriends = [];
            this.modalOpen = true;
        },

        closeModal() {
            console.log("Global closeModal called");
            this.modalOpen = false;
            this.postText = "";
            this.postImage = null;
            this.taggedFriends = [];
            this.friendSearchQuery = "";
            this.showFriendsList = false;
        },

        confirmModal() {
            if (this.modalType === "create-post") {
                this.createPost();
            } else if (this.modalConfirmAction) {
                this.modalConfirmAction();
            }
            this.closeModal();
        },

        createPost() {
            console.log("Creating post with:", {
                text: this.postText,
                privacy: this.postPrivacy,
                image: this.postImage ? "Image attached" : "No image",
                taggedFriends: this.taggedFriends.map((f) => f.name),
            });

            alert("Post created successfully!");
            // In a real app, you would add the post to the feed here
        },

        handleImageUpload(event) {
            const file = event.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    this.postImage = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        },

        removeImage() {
            this.postImage = null;
            // Reset the file input
            document.getElementById("post-image-upload").value = "";
        },

        toggleFriendsList() {
            this.showFriendsList = !this.showFriendsList;
        },

        tagFriend(friend) {
            if (!this.taggedFriends.some((f) => f.id === friend.id)) {
                this.taggedFriends.push(friend);
            }
            this.showFriendsList = false;
            this.friendSearchQuery = "";
        },

        untagFriend(friendId) {
            this.taggedFriends = this.taggedFriends.filter((f) => f.id !== friendId);
        },

        filteredFriends() {
            const query = this.friendSearchQuery.toLowerCase();
            return this.availableFriends.filter(
                (friend) =>
                    friend.name.toLowerCase().includes(query) &&
                    !this.taggedFriends.some((f) => f.id === friend.id)
            );
        },
    }));

    // Post Card component with Dynamic Image Grid
    // Post liking functionality
    // Make likePost function globally accessible
    window.likePost = function(postId) {
        likePost(postId);
    };
    
    // Comment liking functionality
    function likeComment(commentId) {
        // Call the API to like/unlike the comment
        fetch(`/User/Community/LikeComment/${commentId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Update the UI to reflect the like/unlike action
                const commentElement = document.querySelector(`[data-comment-id="${commentId}"]`);
                if (commentElement) {
                    const likeButton = commentElement.querySelector('[data-action="like-comment"]');
                    const likeCountElement = commentElement.querySelector('[data-like-count]');
                    
                    if (likeButton) {
                        // Toggle the liked state
                        const isLiked = likeButton.classList.contains('text-blue-600');
                        
                        if (isLiked) {
                            likeButton.classList.remove('text-blue-600');
                            likeButton.classList.add('text-gray-500');
                        } else {
                            likeButton.classList.remove('text-gray-500');
                            likeButton.classList.add('text-blue-600');
                        }
                        
                        // Update the like count if available
                        if (likeCountElement && data.likesCount !== undefined) {
                            likeCountElement.textContent = `(${data.likesCount})`;
                        }
                    }
                }
            } else {
                throw new Error(data.message || 'Failed to like comment');
            }
        })
        .catch(error => {
            console.error('Error liking comment:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to like comment. Please try again.'
                }
            }));
        });
    }
    
    // Make likeComment function globally accessible
    window.likeComment = function(commentId) {
        likeComment(commentId);
    };
    
    // Comment creation functionality
    function addComment(postId, content, parentCommentId = null) {
        // Call the API to add a comment to the post
        fetch(`/User/Community/AddComment`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                postId: postId,
                content: content,
                parentCommentId: parentCommentId
            })
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // If the comment was added successfully, you can either:
                // 1. Refresh the comments section
                // 2. Add the new comment to the UI directly
                
                // Option 1: Refresh the page or comments section
                // window.location.reload();
                
                // Option 2: Add the new comment to the UI
                // This requires the server to return the new comment data
                if (data.comment) {
                    // Dispatch an event that the postCommentsHandler can listen for
                    window.dispatchEvent(new CustomEvent('comment-added', {
                        detail: {
                            comment: data.comment
                        }
                    }));
                }
                
                // Show success message
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: 'success',
                        message: 'Comment added successfully.'
                    }
                }));
            } else {
                throw new Error(data.message || 'Failed to add comment');
            }
        })
        .catch(error => {
            console.error('Error adding comment:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to add comment. Please try again.'
                }
            }));
        });
    }
    
    // Make addComment function globally accessible
    window.addComment = function(postId, content, parentCommentId = null) {
        addComment(postId, content, parentCommentId);
    };
    
    // TODO: Add image upload functionality
    // This should include:
    // 1. Client-side image selection and preview
    // 2. Image validation (size, type, dimensions)
    // 3. Image upload to server via API
    // 4. Handling upload progress and errors
    // 5. Updating UI after successful upload
    
    function likePost(postId) {
        // Call the API to like/unlike the post
        fetch(`/User/Community/LikePost/${postId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Update the UI to reflect the like/unlike action
                const postElement = document.querySelector(`[data-post-id="${postId}"]`);
                if (postElement) {
                    const likeButton = postElement.querySelector('[data-action="like"]');
                    const likeCountElement = postElement.querySelector('[data-like-count]');
                    
                    if (likeButton) {
                        // Toggle the liked state
                        const isLiked = likeButton.classList.contains('text-blue-600');
                        
                        if (isLiked) {
                            likeButton.classList.remove('text-blue-600');
                            likeButton.classList.add('text-gray-500');
                        } else {
                            likeButton.classList.remove('text-gray-500');
                            likeButton.classList.add('text-blue-600');
                        }
                        
                        // Update the like count if available
                        if (likeCountElement && data.likesCount !== undefined) {
                            likeCountElement.textContent = data.likesCount;
                        }
                    }
                }
            } else {
                throw new Error(data.message || 'Failed to like post');
            }
        })
        .catch(error => {
            console.error('Error liking post:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to like post. Please try again.'
                }
            }));
        });
    }
    
    // Post deletion and reporting functions
    function deletePost(postId) {
        // Call the API to delete the post
        fetch(`/User/Community/DeletePost/${postId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Show success message or update UI
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: 'success',
                        message: 'Post deleted successfully.'
                    }
                }));
                
                // Refresh the page or update the UI
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                throw new Error(data.message || 'Failed to delete post');
            }
        })
        .catch(error => {
            console.error('Error deleting post:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to delete post. Please try again.'
                }
            }));
        });
    }

    function reportPost(postId) {
        // Call the API to report the post
        fetch(`/User/Community/ReportPost/${postId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Show success message
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: 'success',
                        message: 'Post reported successfully.'
                    }
                }));
            } else {
                throw new Error(data.message || 'Failed to report post');
            }
        })
        .catch(error => {
            console.error('Error reporting post:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to report post. Please try again.'
                }
            }));
        });
    }

    function deleteComment(commentId) {
        // Call the API to delete the comment
        fetch(`/User/Community/DeleteComment/${commentId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Show success message or update UI
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: 'success',
                        message: 'Comment deleted successfully.'
                    }
                }));
                
                // Update the UI to mark the comment as deleted
                // This could be done by finding the comment in the DOM and updating it
                // or by refreshing the comments section
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                throw new Error(data.message || 'Failed to delete comment');
            }
        })
        .catch(error => {
            console.error('Error deleting comment:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to delete comment. Please try again.'
                }
            }));
        });
    }

    function reportComment(commentId) {
        // Call the API to report the comment
        fetch(`/User/Community/ReportComment/${commentId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Show success message
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: 'success',
                        message: 'Comment reported successfully.'
                    }
                }));
            } else {
                throw new Error(data.message || 'Failed to report comment');
            }
        })
        .catch(error => {
            console.error('Error reporting comment:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to report comment. Please try again.'
                }
            }));
        });
    }

    function reportUser(userId) {
        // Call the API to report the user
        fetch(`/User/Community/ReportUser/${userId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // Show success message
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: 'success',
                        message: 'User reported successfully.'
                    }
                }));
            } else {
                throw new Error(data.message || 'Failed to report user');
            }
        })
        .catch(error => {
            console.error('Error reporting user:', error);
            window.dispatchEvent(new CustomEvent('show-toast', {
                detail: {
                    type: 'error',
                    message: 'Failed to report user. Please try again.'
                }
            }));
        });
    }

    // Modal trigger functions
    window.showDeletePostModal = function(postId) {
        window.openModal({
            type: 'delete',
            title: 'Delete Post',
            message: 'Are you sure you want to delete this post? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel',
            onConfirm: function() {
                deletePost(postId);
            }
        });
    };

    window.showReportPostModal = function(postId) {
        window.openModal({
            type: 'report',
            title: 'Report Post',
            message: 'Are you sure you want to report this post? This will notify moderators for review.',
            confirmText: 'Report',
            cancelText: 'Cancel',
            onConfirm: function() {
                reportPost(postId);
            }
        });
    };

    window.showDeleteCommentModal = function(commentId) {
        window.openModal({
            type: 'delete',
            title: 'Delete Comment',
            message: 'Are you sure you want to delete this comment? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel',
            onConfirm: function() {
                deleteComment(commentId);
            }
        });
    };

    window.showReportCommentModal = function(commentId) {
        window.openModal({
            type: 'report',
            title: 'Report Comment',
            message: 'Are you sure you want to report this comment? This will notify moderators for review.',
            confirmText: 'Report',
            cancelText: 'Cancel',
            onConfirm: function() {
                reportComment(commentId);
            }
        });
    };

    window.showReportUserModal = function(userId) {
        window.openModal({
            type: 'report',
            title: 'Report User',
            message: 'Are you sure you want to report this user? This will notify moderators for review.',
            confirmText: 'Report',
            cancelText: 'Cancel',
            onConfirm: function() {
                reportUser(userId);
            }
        });
    };

    // Listen for modal confirmation events
    window.addEventListener('modal-confirmed', function(event) {
        const detail = event.detail;
        console.log('Modal confirmed:', detail);
        
        // Handle different modal confirmations based on the title
        if (detail.title === 'Delete Post' && detail.type === 'delete') {
            // The postId should be stored in a data attribute or closure
            const postId = document.querySelector('[data-post-id]')?.dataset.postId;
            if (postId) {
                deletePost(postId);
            }
        } else if (detail.title === 'Report Post' && detail.type === 'report') {
            const postId = document.querySelector('[data-post-id]')?.dataset.postId;
            if (postId) {
                reportPost(postId);
            }
        } else if (detail.title === 'Delete Comment' && detail.type === 'delete') {
            const commentId = document.querySelector('[data-comment-id]')?.dataset.commentId;
            if (commentId) {
                deleteComment(commentId);
            }
        } else if (detail.title === 'Report Comment' && detail.type === 'report') {
            const commentId = document.querySelector('[data-comment-id]')?.dataset.commentId;
            if (commentId) {
                reportComment(commentId);
            }
        } else if (detail.title === 'Report User' && detail.type === 'report') {
            const userId = document.querySelector('[data-user-id]')?.dataset.userId;
            if (userId) {
                reportUser(userId);
            }
        }
    });

    Alpine.data("postCard", () => ({
        liked: false,
        showComments: false,
        commentText: "",
        comments: [
            {
                author: "Alice Johnson",
                text: "Great post!",
                avatar: "bg-purple-300",
                isCertified: true,
                isDeleted: false,
                likes: 0,
                liked: false,
                time: "July 25 at 10:15 AM",
                isAdmin: true,
            },
            {
                author: "Bob Smith",
                text: "Thanks for sharing!",
                avatar: "bg-green-300",
                isCertified: false,
                isDeleted: false,
                likes: 0,
                liked: false,
                time: "July 25 at 10:20 AM",
            },
            {
                author: "Charlie Davis",
                text: "This comment has been deleted.",
                avatar: "bg-red-300",
                isCertified: false,
                isDeleted: true,
                likes: 0,
                liked: false,
                time: "July 25 at 10:25 AM",
            },
        ],
        shareModalOpen: false,
        replyingTo: null,
        isCertified: true,
        isDeleted: false,
        isAdmin: true,

        // Dynamic Image Grid Properties
        postImages: [
            {
                id: 1,
                url: "https://images.pexels.com/photos/2662116/pexels-photo-2662116.jpeg",
                alt: "Beautiful mountain landscape with snow-capped peaks",
                loading: false,
                error: false,
            },
            {
                id: 2,
                url: "https://images.pexels.com/photos/3052361/pexels-photo-3052361.jpeg",
                alt: "Autumn road with colorful trees",
                loading: false,
                error: false,
            },
            {
                id: 3,
                url: "https://images.pexels.com/photos/2387873/pexels-photo-2387873.jpeg",
                alt: "Happy woman with glasses in urban setting",
                loading: false,
                error: false,
            },
            {
                id: 4,
                url: "https://images.pexels.com/photos/1640777/pexels-photo-1640777.jpeg",
                alt: "Delicious food photography",
                loading: false,
                error: false,
            },
            {
                id: 5,
                url: "https://images.pexels.com/photos/1366919/pexels-photo-1366919.jpeg",
                alt: "Sunset over ocean waves",
                loading: false,
                error: false,
            },
            {
                id: 6,
                url: "https://images.pexels.com/photos/1323550/pexels-photo-1323550.jpeg",
                alt: "Modern architecture building",
                loading: false,
                error: false,
            },
        ],

        // Computed property for displayed images (max 3)
        get displayedImages() {
            return this.postImages.slice(0, Math.min(3, this.postImages.length));
        },

        // Get grid CSS class based on number of images
        getGridClass() {
            const imageCount = this.displayedImages.length;
            if (imageCount === 1) return "grid-single";
            if (imageCount === 2) return "grid-double";
            if (imageCount >= 3) return "grid-triple";
            return "";
        },

        // Get individual image container class
        getImageContainerClass(index) {
            const imageCount = this.displayedImages.length;
            if (imageCount === 1) return "image-single";
            if (imageCount === 2) {
                return index === 0 ? "image-double-1" : "image-double-2";
            }
            if (imageCount >= 3) {
                if (index === 0) return "image-triple-1";
                if (index === 1) return "image-triple-2";
                if (index === 2) return "image-triple-3";
            }
            return "";
        },

        // Handle image loading errors
        handleImageError(event, index) {
            console.log("Image failed to load:", this.postImages[index]?.url);
            if (this.postImages[index]) {
                this.postImages[index].error = true;
                this.postImages[index].loading = false;
                // Replace with placeholder
                event.target.src =
                    "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgdmlld0JveD0iMCAwIDIwMCAyMDAiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIyMDAiIGhlaWdodD0iMjAwIiBmaWxsPSIjRjNGNEY2Ii8+CjxwYXRoIGQ9Ik04NyA3NEg5M1Y4MEg4N1Y3NFoiIGZpbGw9IiM5Q0EzQUYiLz4KPC9zdmc+";
            }
        },

        // Open image modal/lightbox using global modal system
        openImageModal(startIndex = 0) {
            // Dispatch event to global modal - works for both desktop and mobile
            window.dispatchEvent(
                new CustomEvent("open-image-modal", {
                    detail: {
                        images: this.postImages,
                        startIndex: startIndex,
                    },
                })
            );
        },

        toggleLike() {
            console.log("toggleLike called");
            this.liked = !this.liked;
        },

        toggleComments() {
            console.log("toggleComments called");
            this.showComments = !this.showComments;
        },

        addComment() {
            if (this.commentText.trim() !== "") {
                let commentText = this.commentText;

                // If replying to someone, add their name to the comment
                if (this.replyingTo) {
                    commentText =
                        '<span class="font-medium">@' +
                        this.replyingTo +
                        "</span> " +
                        commentText;
                }

                // Convert line breaks to <br> tags for proper display
                commentText = commentText.replace(/\n/g, "<br>");

                this.comments.push({
                    author: "You",
                    avatar: "bg-purple-300",
                    text: commentText,
                    time: "Just now",
                    likes: 0,
                    isCertified: false,
                    isDeleted: false,
                });

                this.commentText = "";
                this.replyingTo = null;

                // Reset textarea height to default
                if (this.$refs.commentTextarea) {
                    this.$refs.commentTextarea.style.height = "38px";
                }

                // Auto-expand comments when adding a new one
                this.showComments = true;

                // Scroll to the new comment
                this.$nextTick(() => {
                    const commentsContainer = document.querySelector(".space-y-3");
                    if (commentsContainer) {
                        commentsContainer.scrollTop = commentsContainer.scrollHeight;
                    }
                });
            }
        },

        setReplyTo(author) {
            this.replyingTo = author;
        },

        openShareModal() {
            console.log("openShareModal called");
            this.shareModalOpen = true;
        },

        closeShareModal() {
            console.log("closeShareModal called");
            this.shareModalOpen = false;
        },

        copyLink() {
            alert("Link copied to clipboard!");
            this.closeShareModal();
        },

        shareToFeed() {
            alert("Post shared to your feed!");
            this.closeShareModal();
        },

        // Use global modal via custom event
        openGlobalModal(
            title,
            message,
            confirmText = "Confirm",
            cancelText = "Cancel",
            confirmAction = null
        ) {
            console.log("Dispatching openModal event", { title, message });
            window.dispatchEvent(
                new CustomEvent("open-modal", {
                    detail: {
                        title,
                        message,
                        confirmText,
                        cancelText,
                        confirmAction,
                    },
                })
            );
        },

        // Initialize component
        init() {
            // Animate grid on load
            this.$nextTick(() => {
                const gridElement = this.$el.querySelector(".dynamic-image-grid");
                if (gridElement && this.postImages.length > 0) {
                    gridElement.style.opacity = "0";
                    gridElement.style.transform = "scale(0.95)";

                    setTimeout(() => {
                        gridElement.style.transition =
                            "opacity 0.3s ease, transform 0.3s ease";
                        gridElement.style.opacity = "1";
                        gridElement.style.transform = "scale(1)";
                    }, 100);
                }
            });

            // Handle window resize - close modal if switching to mobile
            window.addEventListener("resize", () => {
                if (window.innerWidth < 769) {
                    // Dispatch close event to global modal
                    window.dispatchEvent(
                        new CustomEvent("keydown", { detail: { key: "Escape" } })
                    );
                }
            });
        },

        // Comment action methods
        deleteComment(index) {
            console.log("deleteComment called for index:", index);
            this.openGlobalModal(
                "Delete Comment",
                "Are you sure you want to delete this comment?",
                "Delete",
                "Cancel",
                () => {
                    console.log("Deleting comment at index:", index);
                    this.comments.splice(index, 1);
                }
            );
        },

        restoreComment(index) {
            console.log("restoreComment called for index:", index);
            this.openGlobalModal(
                "Restore Comment",
                "Restore this comment to the original feed?",
                "Restore",
                "Cancel",
                () => {
                    alert("Comment restored!");
                }
            );
        },

        reportComment(index) {
            console.log("reportComment called for index:", index);
            this.openGlobalModal(
                "Report Comment",
                "Do you want to report this comment for inappropriate content?",
                "Report",
                "Cancel",
                () => {
                    alert("Comment reported!");
                }
            );
        },

        reportUser(author) {
            console.log("reportUser called for author:", author);
            this.openGlobalModal(
                "Report User",
                `Do you want to report ${author} for inappropriate behavior?`,
                "Report",
                "Cancel",
                () => {
                    alert("User reported!");
                }
            );
        },
    }));
});
