// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * Toggle like for a comment
 * @param {string} commentId - The ID of the comment to like/unlike
 */
window.likeComment = function(commentId) {
    if (!commentId) {
        console.error('Comment ID is required');
        return;
    }

    // Get the CSRF token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Send the request to toggle the like
    fetch('/User/Community/ToggleLikeComment', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: `commentId=${commentId}`
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
            console.log(`Comment ${commentId} like toggled successfully`);
            
            // Toggle the like button appearance immediately for better UX
            const likeButton = document.querySelector(`[data-comment-id="${commentId}"] [data-action="like-comment"]`);
            if (likeButton) {
                likeButton.classList.toggle('text-blue-600');
            }
        } else {
            console.error('Failed to toggle comment like:', data.message);
        }
    })
    .catch(error => {
        console.error('Error toggling comment like:', error);
    });
};

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
