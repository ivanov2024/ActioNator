// Post comments handler - REMOVED DUPLICATE DEFINITION
// The Alpine.js component is now defined in alpine-init.js to avoid conflicts
// This file now only contains helper functions for comment management

// Helper function to ensure comments data is properly initialized
function initializeCommentsData() {
    if (!window.postCommentsData) {
        window.postCommentsData = {};
    }
}

// Initialize on DOM load
document.addEventListener('DOMContentLoaded', function() {
    initializeCommentsData();
    console.log('Post comments helper functions initialized');
});

// Global function to add a comment (used by external scripts)
window.addComment = function(postId, content, parentCommentId, options) {
    if (window.communityHub && window.communityHub.connection.state === 'Connected') {
        return window.communityHub.invoke('AddComment', postId, content, parentCommentId);
    } else {
        // Fallback to traditional AJAX
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        return fetch(`/User/Community/AddComment`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token,
                ...(options?.headers || {})
            },
            body: JSON.stringify({
                postId: postId,
                content: content,
                parentCommentId: parentCommentId
            })
        });
    }
};
