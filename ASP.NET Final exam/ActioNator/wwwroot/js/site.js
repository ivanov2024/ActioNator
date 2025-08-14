// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * Toggle like for a comment (fallback implementation).
 * Only defines if a global likeComment is not already provided (e.g., by communityHub.js).
 * @param {string} commentId - The ID of the comment to like/unlike
 * @param {string} [postId] - Unused here; kept for signature compatibility
 */
if (typeof window.likeComment !== 'function') {
    window.likeComment = function(commentId, postId) {
        if (!commentId) {
            console.error('Comment ID is required');
            return;
        }

        // Get the CSRF token
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        const csrf = tokenEl ? tokenEl.value : null;
        if (!csrf) {
            console.error('Anti-forgery token not found');
            return;
        }

        // Send the request to toggle the like (MVC JSON endpoint)
        fetch('/User/Community/ToggleLikeComment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrf
            },
            body: JSON.stringify({ commentId })
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data && data.success) {
                console.log(`Comment ${commentId} like toggled successfully (fallback)`);
                // Optionally reflect count immediately if helper exists
                if (typeof updateCommentLikes === 'function') {
                    updateCommentLikes(commentId, data.likesCount);
                }
            } else {
                console.error('Failed to toggle comment like:', data && data.message);
            }
        })
        .catch(error => {
            console.error('Error toggling comment like:', error);
        });
    };
}

/**
 * Toggle like for a post
 * @param {string} postId - The ID of the post to like/unlike
 */
window.likePost = function(postId) {
    if (!postId) {
        console.error('Post ID is required');
        return;
    }

    // Get the CSRF token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Send the request to toggle the like
    fetch('/User/Community/ToggleLike', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `postId=${postId}`
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            // The SignalR hub will handle updating the UI
            console.log(`Post ${postId} like toggled successfully`);
            
            // Toggle the like button appearance immediately for better UX
            const likeButton = document.querySelector(`[data-post-id="${postId}"] [data-action="like-post"]`);
            if (likeButton) {
                likeButton.classList.toggle('text-blue-600');
            }
        } else {
            console.error('Failed to toggle post like:', data.message);
        }
    })
    .catch(error => {
        console.error('Error toggling post like:', error);
    });
};
