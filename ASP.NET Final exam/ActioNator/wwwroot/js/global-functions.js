/**
 * Global Functions for ActioNator
 * This file contains global functions that need to be available to Alpine.js components
 */

// Function to like a post via AJAX
window.likePost = function(postId) {
    console.log('Global likePost called for post:', postId);
    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    // Make the AJAX call to the controller
    fetch(`/User/Community/ToggleLike`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token
        },
        body: JSON.stringify({ PostId: postId })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        console.log('Post like toggled successfully:', data);
        // The UI is already updated optimistically by Alpine.js
        // SignalR will broadcast the update to other clients
    })
    .catch(error => {
        console.error('Error toggling post like:', error);
        // Revert the optimistic UI update if there was an error
        const postElement = document.querySelector(`[data-post-id="${postId}"]`);
        if (postElement && postElement.__x) {
            postElement.__x.$data.liked = !postElement.__x.$data.liked;
            if (postElement.__x.$data.liked) {
                postElement.__x.$data.likesCount++;
            } else {
                postElement.__x.$data.likesCount = Math.max(0, postElement.__x.$data.likesCount - 1);
            }
        }
    });
};

// Function to show delete post modal
window.showDeletePostModal = function(postId) {
    console.log('Global showDeletePostModal called for post:', postId);
    
    // Try multiple approaches to open the modal
    const openModalConfig = {
        type: 'delete',
        title: 'Delete Post',
        message: 'Are you sure you want to delete this post? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel'
    };
    
    // Store postId globally for the modal confirm action
    window.currentPostIdForDeletion = postId;
    
    // Try the direct modal access first
    if (window.openModalDirect && window.openModalDirect(openModalConfig)) {
        console.log('Modal opened using direct access');
        return;
    }
    
    // Try the standard openModal function
    if (window.openModal) {
        window.openModal(openModalConfig);
        console.log('Modal opened using standard openModal');
        return;
    }
    
    // Fallback: try to find and open modal directly
    const modalElement = document.querySelector('[x-data="modal"]');
    if (modalElement && modalElement.__x && modalElement.__x.$data) {
        const modalData = modalElement.__x.$data;
        modalData.type = 'delete';
        modalData.title = 'Delete Post';
        modalData.message = 'Are you sure you want to delete this post? This action cannot be undone.';
        modalData.confirmText = 'Delete';
        modalData.cancelText = 'Cancel';
        modalData.isOpen = true;
        document.body.style.overflow = 'hidden';
        console.log('Modal opened using direct element access');
        return;
    }
    
    console.error('Could not open modal - no modal system found');
};

// Function to show delete goal modal (reuses shared modal)
window.showDeleteGoalModal = function(goalId) {
    console.log('Global showDeleteGoalModal called for goal:', goalId);

    const openModalConfig = {
        type: 'delete',
        title: 'Delete Goal',
        message: 'Are you sure you want to delete this goal? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel'
    };

    // Store goalId globally for the modal confirm action
    window.currentGoalIdForDeletion = goalId;

    // Try the direct modal access first
    if (window.openModalDirect && window.openModalDirect(openModalConfig)) {
        console.log('Modal opened using direct access');
        return;
    }

    // Try the standard openModal function
    if (window.openModal) {
        window.openModal(openModalConfig);
        console.log('Modal opened using standard openModal');
        return;
    }

    // Fallback: try to find and open modal directly
    const modalElement = document.querySelector('[x-data="modal"]');
    if (modalElement && modalElement.__x && modalElement.__x.$data) {
        const modalData = modalElement.__x.$data;
        modalData.type = 'delete';
        modalData.title = 'Delete Goal';
        modalData.message = 'Are you sure you want to delete this goal? This action cannot be undone.';
        modalData.confirmText = 'Delete';
        modalData.cancelText = 'Cancel';
        modalData.isOpen = true;
        document.body.style.overflow = 'hidden';
        console.log('Modal opened using direct element access');
        return;
    }

    console.error('Could not open modal - no modal system found');
};

// Function to handle post deletion when modal is confirmed
window.handlePostDeletion = function() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const goalId = window.currentGoalIdForDeletion;
    const postId = window.currentPostIdForDeletion;

    // If a Goal deletion was requested via the shared modal
    if (goalId) {
        console.log('Deleting goal:', goalId);
        fetch(`/User/Goal/Delete`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token
            },
            body: JSON.stringify({ Id: goalId, __RequestVerificationToken: token })
        })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json().catch(() => ({ success: true }));
        })
        .then(data => {
            console.log('Goal deleted response:', data);
            if (!data || data.success === false) throw new Error(data?.message || 'Failed to delete goal');
            // Remove the goal from the UI or refresh
            const goalElement = document.querySelector(`.goal-card[data-id="${goalId}"]`);
            if (goalElement) {
                goalElement.remove();
            }
            // Success toast
            try {
                if (window.GoalsPage && typeof window.GoalsPage.showToast === 'function') {
                    window.GoalsPage.showToast('Goal deleted successfully', 'success');
                } else {
                    window.dispatchEvent(new CustomEvent('show-toast', { detail: { message: 'Goal deleted successfully', type: 'success' } }));
                }
            } catch (e) {
                console.warn('Toast dispatch failed:', e);
            }
            if (window.__goalsReload) {
                window.__goalsReload();
            } else if (!goalElement) {
                window.location.reload();
            }
        })
        .catch(error => {
            console.error('Error deleting goal:', error);
            alert('Failed to delete goal. Please try again.');
            try {
                window.dispatchEvent(new CustomEvent('show-toast', { detail: { message: (error && error.message) ? error.message : 'Failed to delete goal', type: 'error' } }));
            } catch (_) {}
        })
        .finally(() => {
            window.currentGoalIdForDeletion = null;
        });
        return;
    }

    // Otherwise, proceed with Post deletion (existing behavior)
    if (postId) {
        console.log('Deleting post:', postId);
        fetch(`/User/Community/DeletePost`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token
            },
            body: JSON.stringify({ PostId: postId })
        })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => {
            console.log('Post deleted successfully:', data);
            const postElement = document.querySelector(`[data-post-id="${postId}"]`);
            if (postElement) {
                postElement.remove();
            } else {
                window.location.reload();
            }
        })
        .catch(error => {
            console.error('Error deleting post:', error);
            alert('Failed to delete post. Please try again.');
        })
        .finally(() => {
            window.currentPostIdForDeletion = null;
        });
        return;
    }

    console.error('No ID found for deletion');
};

// Function to restore a deleted post
window.restorePost = function(postId) {
    console.log('Global restorePost called for post:', postId);
    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    // Make the AJAX call to the controller
    fetch(`/User/Community/RestorePost`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token
        },
        body: JSON.stringify({ PostId: postId })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        console.log('Post restored successfully:', data);
        // Refresh the page to show the restored post
        window.location.reload();
    })
    .catch(error => {
        console.error('Error restoring post:', error);
    });
};

// Function to report a post
window.reportPost = function(postId) {
    console.log('Global reportPost called for post:', postId);
    // Store the post ID in a data attribute on the modal
    const modal = document.getElementById('reportPostModal');
    if (modal) {
        modal.dataset.postId = postId;
        
        // Show the modal using Bootstrap
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    } else {
        console.error('Report post modal not found in the DOM');
    }
};

// Function to report a user
window.reportUser = function(userId) {
    console.log('Global reportUser called for user:', userId);
    // Store the user ID in a data attribute on the modal
    const modal = document.getElementById('reportUserModal');
    if (modal) {
        modal.dataset.userId = userId;
        
        // Show the modal using Bootstrap
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    } else {
        console.error('Report user modal not found in the DOM');
    }
};
