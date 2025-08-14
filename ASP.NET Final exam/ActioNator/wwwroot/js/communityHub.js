/**
 * Community Hub SignalR Client
 * Handles real-time updates for posts and comments
 */
"use strict";

// Create connection
const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
const token = tokenEl ? tokenEl.value : null;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/communityHub", {
        headers: {
            'X-CSRF-TOKEN': token || ''
        }
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Connection state management
connection.onreconnecting(error => {
    console.warn(`Connection lost due to error "${error}". Reconnecting...`);
});

connection.onreconnected(connectionId => {
    console.log(`Connection reestablished. Connected with ID: ${connectionId}`);
});

connection.onclose(error => {
    console.error(`Connection closed due to error: ${error}`);
});

// Event handlers for receiving messages
connection.on("ReceiveNewPost", post => {
    console.log("New post received:", post);
    addNewPostToFeed(post);
});

connection.on("ReceiveNewComment", comment => {
    console.log("New comment received:", comment);
    
    // Check if comment is an array (sometimes SignalR serializes it as an array)
    if (Array.isArray(comment)) {
        if (comment.length > 0) {
            comment = comment[0]; // Use the first item in the array
        } else {
            console.error("Received empty comment array");
            return;
        }
    }
    
    // Ensure the comment has a postId property
    if (!comment.postId && comment.postId !== 0) {
        // If postId is missing but PostId exists (case difference), use that instead
        if (comment.PostId) {
            comment.postId = comment.PostId;
        } else {
            console.error("Comment missing postId property:", comment);
            return;
        }
    }
    
    // Prevent double rendering: check if comment already exists in UI
    const postIdStr = comment.postId.toString();
    if (!window.postCommentsData) {
        window.postCommentsData = {};
    }
    if (!window.postCommentsData[postIdStr]) {
        window.postCommentsData[postIdStr] = [];
    }
    
    const postComments = window.postCommentsData[postIdStr];
    const existingCommentIndex = postComments.findIndex(c => c.id === comment.id);
    
    if (existingCommentIndex >= 0) {
        console.warn("Comment already exists, updating instead of adding:", comment);
        // Update the existing comment
        postComments[existingCommentIndex] = {
            ...postComments[existingCommentIndex],
            ...comment
        };
        
        // Dispatch a custom event for Alpine.js components to update
        document.dispatchEvent(new CustomEvent('comment-updated', {
            detail: { postId: postIdStr, commentId: comment.id }
        }));
    } else {
        // Add the new comment
        addNewCommentToPost(comment);
        
        // Dispatch a custom event for Alpine.js components to update
        document.dispatchEvent(new CustomEvent('comment-added', {
            detail: { postId: postIdStr, commentId: comment.id }
        }));
    }
});

connection.on("ReceivePostUpdate", (postId, likesCount) => {
    console.log(`Post ${postId} updated with ${likesCount} likes`);
    updatePostLikes(postId, likesCount);
});

connection.on("ReceiveCommentUpdate", (commentId, likesCount) => {
    // Some backends/serializers may send a single payload or array. Normalize here.
    try {
        if (Array.isArray(commentId) && likesCount === undefined) {
            // Format: [commentId, likesCount]
            const arr = commentId;
            commentId = arr[0];
            likesCount = arr[1];
        } else if (typeof commentId === 'string' && likesCount === undefined && commentId.includes(',')) {
            // Format: "<commentId>,<likesCount>"
            const parts = commentId.split(',');
            commentId = parts[0];
            likesCount = parseInt(parts[1], 10);
        }
    } catch (e) {
        console.warn('Failed to normalize ReceiveCommentUpdate payload', e);
    }
    console.log(`Comment ${commentId} updated with ${likesCount} likes`);
    updateCommentLikes(commentId, likesCount);
});

connection.on("ReceivePostDeletion", postId => {
    console.log(`Post ${postId} deleted`);
    removePostFromFeed(postId);
});

connection.on("ReceiveCommentDeletion", (commentId, postId) => {
    console.log(`Comment ${commentId} deleted from post ${postId}`);
    removeCommentFromPost(commentId, postId);
});

// Start connection
async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected");
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        setTimeout(startConnection, 5000);
    }
}

// DOM manipulation functions
function addNewPostToFeed(post) {
    const postsContainer = document.getElementById("posts-container");
    if (!postsContainer) return;

    // Check if we need to remove the "no posts" message
    const noPostsMessage = postsContainer.querySelector(".text-center");
    if (noPostsMessage) {
        postsContainer.innerHTML = "";
    }

    // Create a temporary container to hold the new post HTML
    const tempContainer = document.createElement("div");
    
    // Add null check for post and post.id
    if (!post || !post.id) {
        console.error("Invalid post object received:", post);
        return;
    }
    
    // Ensure post.id is treated as a string (in case it's a GUID)
    const postId = post.id.toString();
    
    // Fetch the post card partial view
    fetch(`/User/Community/GetPost/${postId}`, {
        headers: {
            'X-CSRF-TOKEN': token
        }
    })
        .then(response => response.text())
        .then(html => {
            tempContainer.innerHTML = html;
            
            // Add animation class
            const postElement = tempContainer.firstElementChild;
            postElement.classList.add("animate-fade-in");
            
            // Insert at the beginning of the feed
            postsContainer.insertBefore(postElement, postsContainer.firstChild);
            
            // Initialize Alpine.js components
            if (window.Alpine) {
                window.Alpine.initTree(postElement);
            }
        })
        .catch(error => console.error("Error fetching post:", error));
}

function addNewCommentToPost(comment) {
    // Add null check for comment
    if (!comment) {
        console.error("Null or undefined comment object received");
        return;
    }
    
    // Ensure comment has required properties
    if (!comment.id || (!comment.postId && comment.postId !== 0)) {
        console.error("Comment missing required properties:", comment);
        return;
    }
    
    // Ensure IDs are treated as strings (in case they're GUIDs)
    const commentIdStr = comment.id.toString();
    const postIdStr = comment.postId.toString();
    
    // Find the post element
    const postElement = document.querySelector(`[data-post-id="${postIdStr}"]`);
    if (!postElement) {
        console.error(`Post element not found for post ID ${postIdStr}`);
        return;
    }
    
    // First, update the global data store
    if (!window.postCommentsData) {
        window.postCommentsData = {};
    }
    if (!window.postCommentsData[postIdStr]) {
        window.postCommentsData[postIdStr] = [];
    }
    
    // Check if comment already exists in the data store
    const existingCommentIndex = window.postCommentsData[postIdStr].findIndex(c => c.id === comment.id);
    if (existingCommentIndex >= 0) {
        // Update existing comment
        window.postCommentsData[postIdStr][existingCommentIndex] = {
            ...window.postCommentsData[postIdStr][existingCommentIndex],
            ...comment
        };
    } else {
        // Add new comment
        window.postCommentsData[postIdStr].push(comment);
        
        // Update comment count for new comments only
        const commentCountElement = postElement.querySelector('.comments-count');
        if (commentCountElement) {
            const currentCount = parseInt(commentCountElement.textContent) || 0;
            commentCountElement.textContent = (currentCount + 1).toString();
        }
    }
    
    // Try to find the Alpine.js component for this post's comments
    const commentsContainer = postElement.querySelector('[x-data^="postCommentsHandler"]');
    if (commentsContainer && window.Alpine) {
        try {
            // Get the Alpine.js component instance
            const alpineComponent = window.Alpine.$data(commentsContainer);
            if (alpineComponent) {
                // Update the comments array in the Alpine.js component
                alpineComponent.setCommentsForPost(postIdStr);
                console.log("Updated Alpine.js comments for post", postIdStr);
                return; // Alpine.js will handle rendering
            }
        } catch (err) {
            console.error("Error updating Alpine.js component:", err);
            // Continue to fallback
        }
    }
    
    // Fallback: Manual DOM manipulation if Alpine.js is not available or failed
    console.warn("Alpine.js component not found or failed, falling back to manual DOM manipulation");
    
    // Check if this comment is already rendered in the DOM
    const existingCommentElement = postElement.querySelector(`[data-comment-id="${commentIdStr}"]`);
    if (existingCommentElement) {
        console.log("Comment already exists in DOM, updating content");
        updateExistingCommentInDOM(existingCommentElement, comment);
        return;
    }
    
    // Find the comments list container (preferred) or fall back to the outer container
    // Prefer appending into '.comments-list' so comments appear in the correct list area
    let fallbackCommentsContainer = postElement.querySelector('.comments-list');
    if (!fallbackCommentsContainer) {
        fallbackCommentsContainer = postElement.querySelector('.comments-container');
    }
    if (!fallbackCommentsContainer) {
        console.error("Comments container not found");
        return;
    }
    
    // Create a new comment element
    const commentElement = document.createElement('div');
    commentElement.classList.add('comment', 'mb-3', 'p-3', 'bg-white', 'rounded', 'shadow-sm');
    commentElement.dataset.commentId = commentIdStr;
    
    // Format the date
    const createdAt = new Date(comment.createdAt);
    const formattedDate = createdAt.toLocaleDateString() + ' ' + createdAt.toLocaleTimeString();
    
    // Determine if the user is an admin
    const isAdmin = comment.authorIsAdmin || false;
    
    // Create the comment HTML
    commentElement.innerHTML = `
        <div class="d-flex justify-content-between align-items-center mb-2">
            <div class="d-flex align-items-center">
                <img src="${comment.authorProfilePicture || '/img/default-avatar.png'}" 
                     class="rounded-circle me-2" 
                     style="width: 32px; height: 32px;" 
                     alt="${comment.authorName}">
                <div>
                    <div class="d-flex align-items-center">
                        <span class="fw-bold">${comment.authorName}</span>
                        ${isAdmin ? '<span class="badge bg-primary ms-1">Administrator</span>' : ''}
                        ${comment.isDeleted ? '<span class="badge bg-danger ms-1">Deleted</span>' : ''}
                    </div>
                    <small class="text-muted">${formattedDate}</small>
                </div>
            </div>
            <div class="dropdown">
                <button class="btn btn-sm btn-link dropdown-toggle" type="button" data-bs-toggle="dropdown">
                    <i class="bi bi-three-dots-vertical"></i>
                </button>
                <ul class="dropdown-menu dropdown-menu-end">
                    ${comment.canDelete ? `
                        <li><a class="dropdown-item" href="#" onclick="deleteComment('${commentIdStr}', '${postIdStr}'); return false;">
                            <i class="bi bi-trash me-2"></i>Delete
                        </a></li>
                    ` : ''}
                    ${comment.canRestore ? `
                        <li><a class="dropdown-item" href="#" onclick="restoreComment('${commentIdStr}', '${postIdStr}'); return false;">
                            <i class="bi bi-arrow-counterclockwise me-2"></i>Restore
                        </a></li>
                    ` : ''}
                    ${comment.canReport ? `
                        <li><a class="dropdown-item" href="#" onclick="reportComment('${commentIdStr}', '${postIdStr}'); return false;">
                            <i class="bi bi-flag me-2"></i>Report
                        </a></li>
                    ` : ''}
                </ul>
            </div>
        </div>
        <div class="comment-content ${comment.isDeleted ? 'text-muted fst-italic' : ''}">
            ${comment.isDeleted ? 'This comment has been deleted.' : comment.content}
        </div>
        <div class="d-flex justify-content-between align-items-center mt-2">
            <div>
                <button class="btn btn-sm btn-link p-0 me-3" data-action="like-comment" onclick="likeComment('${commentIdStr}', '${postIdStr}'); return false;">
                    <i class="bi bi-hand-thumbs-up${comment.isLikedByCurrentUser ? '-fill' : ''}"></i>
                    <span class="comment-likes-count">${comment.likesCount || 0}</span>
                </button>
                <button class="btn btn-sm btn-link p-0" onclick="replyToComment('${comment.authorName}', '${commentIdStr}'); return false;">
                    <i class="bi bi-reply"></i> Reply
                </button>
            
    `;
    
    // Add the comment to the DOM
    fallbackCommentsContainer.appendChild(commentElement);
    
    // Initialize Alpine.js components in the new comment element
    if (window.Alpine) {
        window.Alpine.initTree(commentElement);
    }
    
    // Add a brief highlight effect
    commentElement.classList.add('comment-highlight');
    setTimeout(() => {
        commentElement.classList.remove('comment-highlight');
    }, 2000);
}

function updatePostLikes(postId, likesCount) {
    // Ensure postId is treated as a string (in case it's a GUID)
    const postIdStr = postId.toString();
    
    // Find the post element
    const postElement = document.querySelector(`[data-post-id="${postIdStr}"]`);
    if (!postElement) return;
    
    // Find the like count element in the post stats section
    const likesElement = postElement.querySelector('.px-4.py-2.border-t.border-b .flex.justify-between span:first-child span:last-child');
    if (likesElement) {
        // Update the text content directly
        likesElement.textContent = likesCount.toString();
        
        // Add a brief highlight effect
        likesElement.classList.add("text-blue-600");
        setTimeout(() => {
            likesElement.classList.remove("text-blue-600");
        }, 1000);
    }
    
    // Also update Alpine.js state if available
    if (window.Alpine && postElement.__x) {
        try {
            const alpineData = postElement.__x.getUnobservedData();
            if (alpineData) {
                // Update the likesCount in Alpine data
                alpineData.likesCount = likesCount;
            }
        } catch (error) {
            console.error("Error updating Alpine.js state:", error);
        }
    }
}

// Helper function to update an existing comment in the DOM
function updateExistingCommentInDOM(commentElement, comment) {
    if (!commentElement || !comment) return;
    
    // Update content
    const contentElement = commentElement.querySelector('.comment-content');
    if (contentElement) {
        if (comment.isDeleted) {
            contentElement.classList.add('text-muted', 'fst-italic');
            contentElement.textContent = 'This comment has been deleted.';
        } else {
            contentElement.classList.remove('text-muted', 'fst-italic');
            contentElement.innerHTML = comment.content;
        }
    }
    
    // Update likes count
    const likesCountElement = commentElement.querySelector('.comment-likes-count');
    if (likesCountElement) {
        likesCountElement.textContent = comment.likesCount || 0;
    }
    
    // Update like button icon
    const likeIcon = commentElement.querySelector('.bi-hand-thumbs-up, .bi-hand-thumbs-up-fill');
    if (likeIcon) {
        if (comment.isLikedByCurrentUser) {
            likeIcon.classList.remove('bi-hand-thumbs-up');
            likeIcon.classList.add('bi-hand-thumbs-up-fill');
        } else {
            likeIcon.classList.remove('bi-hand-thumbs-up-fill');
            likeIcon.classList.add('bi-hand-thumbs-up');
        }
    }
    
    // Update badges
    const badgesContainer = commentElement.querySelector('.d-flex.align-items-center');
    if (badgesContainer) {
        // Update admin badge
        let adminBadge = badgesContainer.querySelector('.badge.bg-primary');
        if (comment.authorIsAdmin && !adminBadge) {
            const newAdminBadge = document.createElement('span');
            newAdminBadge.classList.add('badge', 'bg-primary', 'ms-1');
            newAdminBadge.textContent = 'Administrator';
            badgesContainer.appendChild(newAdminBadge);
        } else if (!comment.authorIsAdmin && adminBadge) {
            adminBadge.remove();
        }
        
        // Update deleted badge
        let deletedBadge = badgesContainer.querySelector('.badge.bg-danger');
        if (comment.isDeleted && !deletedBadge) {
            const newDeletedBadge = document.createElement('span');
            newDeletedBadge.classList.add('badge', 'bg-danger', 'ms-1');
            newDeletedBadge.textContent = 'Deleted';
            badgesContainer.appendChild(newDeletedBadge);
        } else if (!comment.isDeleted && deletedBadge) {
            deletedBadge.remove();
        }
    }
    
    // Update dropdown menu options based on permissions
    const dropdownMenu = commentElement.querySelector('.dropdown-menu');
    if (dropdownMenu) {
        // Clear existing items
        dropdownMenu.innerHTML = '';
        
        const commentIdStr = comment.id.toString();
        const postIdStr = comment.postId.toString();
        
        // Add delete option if allowed
        if (comment.canDelete) {
            const deleteItem = document.createElement('li');
            deleteItem.innerHTML = `<a class="dropdown-item" href="#" onclick="deleteComment('${commentIdStr}', '${postIdStr}'); return false;">
                <i class="bi bi-trash me-2"></i>Delete
            </a>`;
            dropdownMenu.appendChild(deleteItem);
        }
        
        // Add restore option if allowed
        if (comment.canRestore) {
            const restoreItem = document.createElement('li');
            restoreItem.innerHTML = `<a class="dropdown-item" href="#" onclick="restoreComment('${commentIdStr}', '${postIdStr}'); return false;">
                <i class="bi bi-arrow-counterclockwise me-2"></i>Restore
            </a>`;
            dropdownMenu.appendChild(restoreItem);
        }
        
        // Add report option if allowed
        if (comment.canReport) {
            const reportItem = document.createElement('li');
            reportItem.innerHTML = `<a class="dropdown-item" href="#" onclick="reportComment('${commentIdStr}', '${postIdStr}'); return false;">
                <i class="bi bi-flag me-2"></i>Report
            </a>`;
            dropdownMenu.appendChild(reportItem);
        }
    }
}

// Helper function to update comment count
function updateCommentCount(postElement, increment) {
    if (!postElement) return;
    
    const commentCountElement = postElement.querySelector('.comments-count');
    if (commentCountElement) {
        const currentCount = parseInt(commentCountElement.textContent) || 0;
        const newCount = currentCount + increment;
        commentCountElement.textContent = Math.max(0, newCount).toString();
    }
}

// Fallback helper to reply to a comment when Alpine component is not available
window.replyToComment = function(authorName, commentId) {
    try {
        const commentEl = document.querySelector(`[data-comment-id="${commentId}"]`);
        const postEl = commentEl ? commentEl.closest('[data-post-id]') : null;
        if (!postEl) return;

        // Try Alpine component first
        const compEl = postEl.querySelector('[x-data^="postCommentsHandler"]');
        if (compEl && window.Alpine) {
            try {
                const comp = window.Alpine.$data(compEl);
                if (comp) {
                    comp.replyingTo = authorName;
                    // Focus the textarea
                    if (comp.$refs && comp.$refs.commentTextarea) {
                        comp.$refs.commentTextarea.focus();
                    } else {
                        const ta = compEl.querySelector('textarea');
                        if (ta) ta.focus();
                    }
                    return;
                }
            } catch {}
        }

        // Fallback: manipulate DOM directly
        const container = postEl.querySelector('.comments-container');
        const textarea = container ? container.querySelector('textarea') : null;
        if (textarea) {
            const mention = `@${authorName} `;
            if (!textarea.value.startsWith(mention)) {
                textarea.value = mention + (textarea.value || '');
            }
            textarea.focus();
            // Move caret to end
            const len = textarea.value.length;
            textarea.setSelectionRange(len, len);
        }
    } catch (e) {
        console.error('replyToComment fallback failed', e);
    }
};

function updateCommentLikes(commentId, likesCount) {
    // Ensure commentId is treated as a string (in case it's a GUID)
    const commentIdStr = commentId.toString();
    const nextLikes = Number.isFinite(Number(likesCount)) ? Number(likesCount) : 0;

    // Find the comment element
    const commentElement = document.querySelector(`[data-comment-id="${commentIdStr}"]`);
    
    // Update global Alpine data store so UI re-renders
    try {
        // Try to determine postId from DOM first
        const postIdStr = commentElement?.closest('[data-post-id]')?.dataset?.postId
            || Object.keys(window.postCommentsData || {}).find(pid =>
                Array.isArray(window.postCommentsData[pid]) && window.postCommentsData[pid].some(c => (c && (c.id || c.Id)) === commentIdStr)
            );
        if (postIdStr) {
            window.postCommentsData = window.postCommentsData || {};
            const arr = window.postCommentsData[postIdStr];
            if (Array.isArray(arr)) {
                const idx = arr.findIndex(c => (c && (c.id || c.Id)) === commentIdStr);
                if (idx >= 0) {
                    const c = arr[idx] || {};
                    arr[idx] = {
                        ...c,
                        likesCount: nextLikes,
                        likes: nextLikes
                        // Do not change isLikedByCurrentUser here; server may not send it
                    };
                    // Notify Alpine components to refresh from store
                    document.dispatchEvent(new CustomEvent('comment-updated', { detail: { postId: postIdStr, commentId: commentIdStr } }));
                    // Also directly dispatch set-comments to the handler element if present
                    try {
                        const commentsElement = document.querySelector(`[data-post-id="${postIdStr}"] .comments-container`);
                        if (commentsElement) {
                            commentsElement.dispatchEvent(new CustomEvent('set-comments', {
                                detail: { comments: arr.slice() },
                                bubbles: true
                            }));
                        }
                    } catch (e2) {
                        console.warn('Failed to dispatch set-comments to element', e2);
                    }
                }
            }
        }
    } catch (e) {
        console.warn('Failed to sync Alpine store in updateCommentLikes', e);
    }

    if (!commentElement) return;

    // Fallback: update the like count text in DOM
    const likesElement = commentElement.querySelector('.comment-likes-count');
    if (likesElement) {
        likesElement.textContent = nextLikes > 0 ? String(nextLikes) : '';
        likesElement.classList.add('text-blue-600');
        setTimeout(() => likesElement.classList.remove('text-blue-600'), 1000);
    }

    // Briefly highlight the like button to indicate an update
    const likeButton = commentElement.querySelector('[data-action="like-comment"]');
    if (likeButton) {
        likeButton.classList.add('text-blue-600');
        setTimeout(() => {
            // If Alpine toggled it based on isLikedByCurrentUser, leave it; otherwise remove flash
            if (!likeButton.classList.contains('text-blue-600')) {
                likeButton.classList.remove('text-blue-600');
            }
        }, 1000);
    }
}

function removePostFromFeed(postId) {
    // Ensure postId is treated as a string (in case it's a GUID)
    const postIdStr = postId.toString();
    
    const postElement = document.querySelector(`[data-post-id="${postIdStr}"]`);
    if (postElement) {
        // Add fade-out animation
        postElement.classList.add("animate-fade-out");
        
        // Remove after animation completes
        setTimeout(() => {
            postElement.remove();
            
            // Check if there are no more posts
            const postsContainer = document.getElementById("posts-container");
            if (postsContainer && postsContainer.children.length === 0) {
                postsContainer.innerHTML = `
                    <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6 text-center">
                        <p class="text-gray-500 dark:text-gray-400">No posts yet. Be the first to share something!</p>
                    </div>
                `;
            }
        }, 300);
    }
}

function removeCommentFromPost(commentId, postId) {
    // Ensure IDs are treated as strings (in case they're GUIDs)
    const commentIdStr = commentId.toString();
    const postIdStr = postId.toString();
    
    // Update the global data store
    if (window.postCommentsData && window.postCommentsData[postIdStr]) {
        const commentIndex = window.postCommentsData[postIdStr].findIndex(c => c.id === commentId);
        if (commentIndex >= 0) {
            // Mark as deleted or remove from array
            window.postCommentsData[postIdStr][commentIndex].isDeleted = true;
            
            // Dispatch a custom event for Alpine.js components to update
            document.dispatchEvent(new CustomEvent('comment-deleted', {
                detail: { postId: postIdStr, commentId: commentIdStr }
            }));
        }
    }
    
    const commentElement = document.querySelector(`[data-comment-id="${commentIdStr}"]`);
    if (commentElement) {
        // Add fade-out animation
        commentElement.classList.add("animate-fade-out");
        
        // Remove after animation completes
        setTimeout(() => {
            commentElement.remove();
            
            // Update comment count
            const commentCountElement = document.querySelector(`[data-post-id="${postIdStr}"] .comments-count`);
            if (commentCountElement) {
                const currentCount = parseInt(commentCountElement.textContent);
                if (currentCount > 0) {
                    commentCountElement.textContent = (currentCount - 1).toString();
                }
            }
        }, 300);
    }
}

// Start the connection when the document is ready
if (document.readyState === 'loading') {
    document.addEventListener("DOMContentLoaded", startConnection);
} else {
    // In case this script is loaded after DOMContentLoaded (e.g., dynamic navigation), start immediately
    startConnection();
}

// Export connection for external use
window.communityHub = connection;

// Comment action functions

/**
 * Like a comment via SignalR
 * @param {string} commentId - The ID of the comment to like
 * @param {string} postId - The ID of the post containing the comment
 */
window.likeComment = function(commentId, postId) {
    if (!commentId || !postId) {
        console.error('Missing required parameters for likeComment:', { commentId, postId });
        return;
    }

    // Optimistic UI update: toggle like locally for instant feedback
    try {
        const pid = postId.toString();
        window.postCommentsData = window.postCommentsData || {};
        const arr = window.postCommentsData[pid];
        if (Array.isArray(arr)) {
            const idx = arr.findIndex(c => (c && (c.id || c.Id)) === commentId);
            if (idx >= 0) {
                const c = arr[idx] || {};
                const prevLiked = !!(c.isLikedByCurrentUser || c.isLiked);
                const prevLikes = (c.likesCount ?? c.likes ?? 0) | 0;
                const nextLikes = prevLiked ? Math.max(0, prevLikes - 1) : prevLikes + 1;
                arr[idx] = {
                    ...c,
                    likesCount: nextLikes,
                    likes: nextLikes,
                    isLikedByCurrentUser: !prevLiked,
                    isLiked: !prevLiked
                };
                document.dispatchEvent(new CustomEvent('comment-updated', { detail: { postId: pid, commentId } }));
            }
        }
    } catch (e) {
        console.warn('Optimistic like update failed', e);
    }

    // DOM fallback for instant visual feedback if Alpine store path was unavailable
    try {
        const commentEl = document.querySelector(`[data-post-id="${postId}"] [data-comment-id="${commentId}"]`);
        if (commentEl) {
            const icon = commentEl.querySelector('i');
            const countEl = commentEl.querySelector('.comment-likes-count');
            if (icon) {
                // Toggle filled/outlined icon classes and color
                const filled = icon.classList.contains('bi-hand-thumbs-up-fill');
                icon.classList.toggle('bi-hand-thumbs-up-fill', !filled);
                icon.classList.toggle('bi-hand-thumbs-up', filled);
                icon.classList.toggle('text-blue-600', !filled);
            }
            if (countEl) {
                const current = parseInt(countEl.textContent || '0', 10) || 0;
                const next = (icon && icon.classList.contains('bi-hand-thumbs-up-fill')) ? current + 1 : Math.max(0, current - 1);
                countEl.textContent = next > 0 ? String(next) : '';
            }
        }
    } catch (e) {
        console.warn('Optimistic DOM like toggle failed', e);
    }
    
    // Send the like request via SignalR
    connection.invoke('LikeComment', commentId)
        .then(() => {
            console.log(`Like request sent for comment ${commentId}`);
            // Server will broadcast ReceiveCommentUpdate; updateCommentLikes will sync count
        })
        .catch(err => {
            console.error('Error liking comment:', err);
            // Fallback to AJAX (MVC endpoint) if SignalR fails
            try {
                const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                const csrf = tokenEl ? tokenEl.value : null;
                if (!csrf) {
                    console.error('Anti-forgery token not found for likeComment fallback');
                    return;
                }
                fetch('/User/Community/ToggleLikeComment', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-CSRF-TOKEN': csrf
                    },
                    body: JSON.stringify({ commentId })
                })
                .then(response => {
                    if (!response.ok) throw new Error('Network response was not ok');
                    return response.json();
                })
                .then(data => {
                    if (data && data.success) {
                        console.log('Comment like toggled via AJAX fallback:', data);
                        // Sync with server-provided likes count
                        updateCommentLikes(commentId, data.likesCount);
                    } else {
                        console.error('AJAX fallback likeComment failed:', data);
                    }
                })
                .catch(error => console.error('Error in AJAX fallback for liking comment:', error));
            } catch (fallbackErr) {
                console.error('likeComment AJAX fallback encountered an error:', fallbackErr);
            }
        });
};

/**
 * Delete a comment via SignalR
 * @param {string} commentId - The ID of the comment to delete
 * @param {string} postId - The ID of the post containing the comment
 */
window.deleteComment = function(commentId, postId) {
    if (!commentId || !postId) {
        console.error('Missing required parameters for deleteComment:', { commentId, postId });
        return;
    }
    
    // Show confirmation modal
    if (window.$store && window.$store.modal) {
        window.$store.modal.modalTitle = 'Delete Comment';
        window.$store.modal.modalMessage = 'Are you sure you want to delete this comment?';
        window.$store.modal.modalConfirmText = 'Delete';
        window.$store.modal.modalCancelText = 'Cancel';
        window.$store.modal.modalType = 'danger';
        window.$store.modal.modalConfirmAction = () => {
            // Send the delete request via SignalR
            connection.invoke('DeleteComment', commentId)
                .then(() => {
                    console.log(`Delete request sent for comment ${commentId}`);
                    // UI will be updated by SignalR event handler
                })
                .catch(err => {
                    console.error('Error deleting comment:', err);
                    // Fallback to AJAX if SignalR fails
                    fetch(`/api/comments/${commentId}`, {
                        method: 'DELETE',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-CSRF-TOKEN': token
                        }
                    })
                    .then(response => {
                        if (!response.ok) throw new Error('Network response was not ok');
                        // Manually update UI since SignalR failed
                        removeCommentFromPost(commentId, postId);
                    })
                    .catch(error => console.error('Error in AJAX fallback for deleting comment:', error));
                });
        };
        window.$store.modal.modalOpen = true;
    } else {
        // If modal system not available, use browser confirm
        if (confirm('Are you sure you want to delete this comment?')) {
            connection.invoke('DeleteComment', commentId)
                .catch(err => {
                    console.error('Error deleting comment:', err);
                    // Fallback to AJAX
                    fetch(`/api/comments/${commentId}`, {
                        method: 'DELETE',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-CSRF-TOKEN': token
                        }
                    })
                    .then(response => {
                        if (!response.ok) throw new Error('Network response was not ok');
                        removeCommentFromPost(commentId, postId);
                    })
                    .catch(error => console.error('Error in AJAX fallback for deleting comment:', error));
                });
        }
    }
};

/**
 * Restore a deleted comment via SignalR
 * @param {string} commentId - The ID of the comment to restore
 * @param {string} postId - The ID of the post containing the comment
 */
window.restoreComment = function(commentId, postId) {
    if (!commentId || !postId) {
        console.error('Missing required parameters for restoreComment:', { commentId, postId });
        return;
    }
    
    // Show confirmation modal
    if (window.$store && window.$store.modal) {
        window.$store.modal.modalTitle = 'Restore Comment';
        window.$store.modal.modalMessage = 'Do you want to restore this comment?';
        window.$store.modal.modalConfirmText = 'Restore';
        window.$store.modal.modalCancelText = 'Cancel';
        window.$store.modal.modalType = 'success';
        window.$store.modal.modalConfirmAction = () => {
            // Send the restore request via SignalR
            connection.invoke('RestoreComment', commentId)
                .then(() => {
                    console.log(`Restore request sent for comment ${commentId}`);
                    // Update the global data store
                    if (window.postCommentsData && window.postCommentsData[postId]) {
                        const comment = window.postCommentsData[postId].find(c => c.id === commentId);
                        if (comment) {
                            comment.isDeleted = false;
                            // Dispatch event for Alpine.js components
                            document.dispatchEvent(new CustomEvent('comment-updated', {
                                detail: { postId, commentId }
                            }));
                        }
                    }
                })
                .catch(err => {
                    console.error('Error restoring comment:', err);
                    // Fallback to AJAX if SignalR fails
                    fetch(`/api/comments/${commentId}/restore`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-CSRF-TOKEN': token
                        }
                    })
                    .then(response => {
                        if (!response.ok) throw new Error('Network response was not ok');
                        // Update UI manually
                        const commentElement = document.querySelector(`[data-comment-id="${commentId}"]`);
                        if (commentElement) {
                            // Remove deleted styling
                            commentElement.classList.remove('opacity-50');
                            const contentElement = commentElement.querySelector('.text-muted');
                            if (contentElement && window.postCommentsData && window.postCommentsData[postId]) {
                                const comment = window.postCommentsData[postId].find(c => c.id === commentId);
                                if (comment) {
                                    contentElement.innerHTML = comment.content;
                                    contentElement.classList.remove('text-muted', 'fst-italic');
                                }
                            }
                        }
                    })
                    .catch(error => console.error('Error in AJAX fallback for restoring comment:', error));
                });
        };
        window.$store.modal.modalOpen = true;
    } else {
        // If modal system not available, use browser confirm
        if (confirm('Do you want to restore this comment?')) {
            connection.invoke('RestoreComment', commentId)
                .catch(err => {
                    console.error('Error restoring comment:', err);
                    // Fallback to AJAX
                    fetch(`/api/comments/${commentId}/restore`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-CSRF-TOKEN': token
                        }
                    })
                    .catch(error => console.error('Error in AJAX fallback for restoring comment:', error));
                });
        }
    }
};

/**
 * Report a comment via SignalR
 * @param {string} commentId - The ID of the comment to report
 * @param {string} postId - The ID of the post containing the comment
 */
window.reportComment = function(commentId, postId) {
    if (!commentId) {
        console.error('Missing required parameter commentId for reportComment');
        return;
    }
    
    // Show confirmation modal
    if (window.$store && window.$store.modal) {
        window.$store.modal.modalTitle = 'Report Comment';
        window.$store.modal.modalMessage = 'Do you want to report this comment for inappropriate content?';
        window.$store.modal.modalConfirmText = 'Report';
        window.$store.modal.modalCancelText = 'Cancel';
        window.$store.modal.modalType = 'warning';
        window.$store.modal.modalConfirmAction = () => {
            // Send the report request via AJAX (typically reports don't need real-time updates)
            fetch(`/api/comments/${commentId}/report`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token
                }
            })
            .then(response => {
                if (!response.ok) throw new Error('Network response was not ok');
                return response.json();
            })
            .then(data => {
                console.log('Comment reported successfully:', data);
                // Show success message
                if (window.$store && window.$store.toast) {
                    window.$store.toast.show('Comment reported successfully', 'success');
                } else {
                    alert('Comment reported successfully');
                }
            })
            .catch(error => {
                console.error('Error reporting comment:', error);
                if (window.$store && window.$store.toast) {
                    window.$store.toast.show('Error reporting comment', 'error');
                } else {
                    alert('Error reporting comment');
                }
            });
        };
        window.$store.modal.modalOpen = true;
    } else {
        // If modal system not available, use browser confirm
        if (confirm('Do you want to report this comment for inappropriate content?')) {
            fetch(`/api/comments/${commentId}/report`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token
                }
            })
            .then(response => {
                if (!response.ok) throw new Error('Network response was not ok');
                alert('Comment reported successfully');
            })
            .catch(error => {
                console.error('Error reporting comment:', error);
                alert('Error reporting comment');
            });
        }
    }
};
