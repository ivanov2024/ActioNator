/**
 * Community Hub SignalR Client
 * Handles real-time updates for posts and comments
 */
"use strict";

// Create connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/communityHub")
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
    
    addNewCommentToPost(comment);
});

connection.on("ReceivePostUpdate", (postId, likesCount) => {
    console.log(`Post ${postId} updated with ${likesCount} likes`);
    updatePostLikes(postId, likesCount);
});

connection.on("ReceiveCommentUpdate", (commentId, likesCount) => {
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
    fetch(`/User/Community/GetPost/${postId}`)
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
    let postId, commentId;
    try {
        // Handle different property name cases (postId vs PostId, id vs Id)
        postId = (comment.postId || comment.PostId);
        commentId = (comment.id || comment.Id);
        
        if (!postId || !commentId) {
            console.error("Comment missing required properties:", comment);
            return;
        }
        
        // Convert to string safely
        postId = String(postId);
        commentId = String(commentId);
    } catch (error) {
        console.error("Error processing comment properties:", error, comment);
        return;
    }
    
    // Check if this comment already exists in the DOM to avoid duplicates
    const existingComment = document.querySelector(`[data-comment-id="${commentId}"]`);
    if (existingComment) {
        console.log(`Comment ${commentId} already exists in the DOM, skipping render`);
        return;
    }
    
    // Find the comments container for this post
    const commentsContainer = document.querySelector(`[data-post-id="${postId}"] .space-y-3`);
    if (!commentsContainer) {
        console.error(`Comments container not found for post ${postId}`);
        return;
    }
    
    // Create a temporary container to hold the new comment HTML
    const tempContainer = document.createElement("div");
    
    // Fetch the comment partial view
    fetch(`/User/Community/GetComment/${commentId}`)
        .then(response => response.text())
        .then(html => {
            tempContainer.innerHTML = html;
            
            // Add animation class
            const commentElement = tempContainer.firstElementChild;
            if (!commentElement) {
                console.error("Comment element not found in response HTML");
                return;
            }
            
            commentElement.classList.add("animate-fade-in");
            
            // Append to comments container
            commentsContainer.appendChild(commentElement);
            
            // Update comment count
            const commentCountElement = document.querySelector(`[data-post-id="${postId}"] .comments-count`);
            if (commentCountElement) {
                const currentCount = parseInt(commentCountElement.textContent) || 0;
                commentCountElement.textContent = (currentCount + 1).toString();
            }
            
            // Initialize Alpine.js components
            if (window.Alpine) {
                window.Alpine.initTree(commentElement);
            }
        })
        .catch(error => console.error("Error fetching comment:", error));
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

function updateCommentLikes(commentId, likesCount) {
    // Ensure commentId is treated as a string (in case it's a GUID)
    const commentIdStr = commentId.toString();
    
    // Find the comment element
    const commentElement = document.querySelector(`[data-comment-id="${commentIdStr}"]`);
    if (!commentElement) return;
    
    // Find the like count element (span with data-like-count attribute)
    const likesElement = commentElement.querySelector(`span[data-like-count]`);
    if (likesElement) {
        // Update the text content
        likesElement.textContent = `(${likesCount})`;
        
        // Add a brief highlight effect
        likesElement.classList.add("text-blue-600");
        setTimeout(() => {
            likesElement.classList.remove("text-blue-600");
        }, 1000);
    }
    
    // Update the like button appearance based on the current user's like status
    const likeButton = commentElement.querySelector(`[data-action="like-comment"]`);
    if (likeButton) {
        // We can't determine if the current user liked it from this function,
        // but we can highlight the button briefly to indicate the update
        likeButton.classList.add("text-blue-600");
        setTimeout(() => {
            // Only remove the highlight if it's not already liked by the user
            if (!likeButton.classList.contains("text-blue-600")) {
                likeButton.classList.remove("text-blue-600");
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
document.addEventListener("DOMContentLoaded", startConnection);

// Export connection for external use
window.communityHub = connection;
