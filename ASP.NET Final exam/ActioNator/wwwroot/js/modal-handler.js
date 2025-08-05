// Modal Handler for ActioNator
// This file handles the shared modal functionality across the application

// Helper function to close all open dropdowns
function closeAllDropdowns() {
    console.log('Closing all open dropdowns');
    
    // Remove active class from all dropdown triggers
    document.querySelectorAll('.dropdown-trigger').forEach(trigger => {
        trigger.classList.remove('active');
    });
    
    // Hide all dropdown menus
    document.querySelectorAll('.dropdown-menu').forEach(menu => {
        menu.style.opacity = '0';
        menu.style.visibility = 'hidden';
        menu.style.transform = 'translateY(-10px)';
        menu.style.pointerEvents = 'none';
    });
    
    // Reset global active dropdown if it exists
    if (typeof window.activeDropdown !== 'undefined') {
        window.activeDropdown = null;
    }
}

document.addEventListener('DOMContentLoaded', function() {
    console.log('Modal handler initialized');
    
    // Listen for the modal-ready event
    window.addEventListener('modal-ready', function(event) {
        console.log('Modal ready event received');
    });
    
    // Listen for modal-confirmed event
    window.addEventListener('modal-confirmed', function(event) {
        console.log('Modal confirmed event received', event.detail);
        
        // Handle goal deletion
        if (event.detail.type === 'delete' && event.detail.title === 'Delete Goal') {
            if (window.deleteGoalId) {
                console.log('Confirming deletion of goal:', window.deleteGoalId);
                deleteGoal(window.deleteGoalId);
            } else {
                console.error('No goal ID found for deletion');
            }
        }
    });
});

// Open the delete confirmation modal
function openDeleteModal(goalId) {
    console.log('Opening delete modal for goal:', goalId);
    
    // Store the goal ID globally for the confirmation handler
    window.deleteGoalId = goalId;
    
    // Check if the modal system is initialized
    if (!window.modalSystem || !window.modalSystem.initialized) {
        console.error('Modal system not initialized');
        return;
    }
    
    // Configure and open the modal
    const modal = window.modalSystem.instance;
    if (modal) {
        modal.type = 'delete';
        modal.title = 'Delete Goal';
        modal.message = 'Are you sure you want to delete this goal? This action cannot be undone.';
        modal.confirmText = 'Delete';
        modal.cancelText = 'Cancel';
        
        // Set up the confirmation handler
        modal.onConfirm = function() {
            deleteGoal(goalId);
        };
        
        // Close any open dropdowns before opening the modal
        closeAllDropdowns();
        
        // Open the modal
        modal.isOpen = true;
        document.body.style.overflow = 'hidden'; // Prevent scrolling
        console.log('Delete modal opened');
    } else {
        console.error('Modal instance not found');
    }
}

// Delete goal function
function deleteGoal(goalId) {
    console.log('Deleting goal:', goalId);
    
    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        console.error('Anti-forgery token not found');
        return;
    }
    
    // Send delete request to the server
    fetch('/User/Goal/Delete', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token
        },
        body: JSON.stringify({ id: goalId })
    })
    .then(async response => {
        // Check if there's content to parse
        const contentType = response.headers.get('content-type');
        
        // Always try to parse JSON response regardless of status code
        if (contentType && contentType.includes('application/json')) {
            const data = await response.json();
            
            // If response is not OK, throw an error with the message from the server
            if (!response.ok) {
                console.error('Server response not OK:', response.status, data);
                throw new Error(data.message || 'An error occurred while deleting the goal');
            }
            
            return data;
        } else if (!response.ok) {
            // Non-JSON error response
            console.error('Server response not OK:', response.status, response.statusText);
            throw new Error('Network response was not ok');
        } else {
            return { success: true }; // Default success response if no JSON
        }
    })
    .then(data => {
        console.log('Goal deleted successfully:', data);
        
        // Show success toast
        window.dispatchEvent(new CustomEvent('show-toast', {
            detail: {
                type: 'success',
                title: 'Goal Deleted',
                message: 'Your goal has been successfully deleted.'
            }
        }));
        
        // Reload goals
        if (typeof loadGoals === 'function') {
            loadGoals();
        }
    })
    .catch(error => {
        console.error('Error deleting goal:', error);
        
        // Show error toast with the specific error message
        window.dispatchEvent(new CustomEvent('show-toast', {
            detail: {
                type: 'error',
                title: 'Error',
                message: error.message || 'Failed to delete goal. Please try again.'
            }
        }));
    });
}
