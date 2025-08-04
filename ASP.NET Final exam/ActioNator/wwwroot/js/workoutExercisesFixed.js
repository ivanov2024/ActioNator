// Enhanced Exercise CRUD functionality for ActioNator
// Fixed version to prevent frontend freezes after workout deletion
// Added safety checks and improved error handling

// Define the main workouts page Alpine component
window.workoutsPage = function() {
    return {
        // Core properties for workout list and modals
        showModal: false,
        showDeleteModal: false,
        deleteWorkoutId: null,
        modal: { title: '', date: '', notes: '' },
        validationErrors: { title: '', date: '' },
        _isAfterDeletion: false, // Flag to track post-deletion state
        
        // Initialize the component
        init() {
            console.log('[WORKOUT] Initializing workouts page Alpine component');
            this.setupEventListeners();
        },
        
        // Set up event listeners
        setupEventListeners() {
            // Listen for toast events
            window.addEventListener('show-toast', (event) => {
                if (event.detail) {
                    this.showToast(event.detail.type, event.detail.message);
                }
            });
            
            // Add global error handler to catch any unhandled errors
            window.addEventListener('error', (event) => {
                console.error('[WORKOUT] Unhandled error caught:', event.error);
                this.showToast('error', 'An unexpected error occurred. Please refresh the page.');
            });
        },
        
        // Circuit breaker to prevent infinite toast loops
        _toastCount: 0,
        _toastTimestamp: 0,
        _toastMessage: '',
        
        // Show toast notification using the shared toast partial
        showToast(type, message) {
            // CIRCUIT BREAKER: Prevent infinite toast loops
            const now = Date.now();
            
            // If this is the same message within a short time period, increment counter
            if (message === this._toastMessage && now - this._toastTimestamp < 1000) {
                this._toastCount++;
                
                // If we've shown the same toast too many times in a row, break the loop
                if (this._toastCount > 2) {
                    console.error(`[WORKOUT] Circuit breaker activated - toast loop detected for message: ${message}`);
                    return; // Break the loop
                }
            } else {
                // Reset counter for new message or after time threshold
                this._toastCount = 0;
                this._toastMessage = message;
            }
            
            // Update timestamp
            this._toastTimestamp = now;
            
            console.log(`[WORKOUT] Showing toast: ${type} - ${message}`);
            
            // Dispatch the show-toast event that the Alpine component in _ToastPartial.cshtml listens for
            try {
                console.log('[WORKOUT] Dispatching show-toast event:', { type, message });
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: type,
                        message: message
                    }
                }));
            } catch (error) {
                console.error('[WORKOUT] Error dispatching toast event:', error);
            }
        },
        
        // Validate the form
        validateForm() {
            let isValid = true;
            this.validationErrors = { title: '', date: '' };
            
            if (!this.modal.title || this.modal.title.trim() === '') {
                this.validationErrors.title = 'Title is required';
                isValid = false;
            }
            
            if (!this.modal.date || this.modal.date.trim() === '') {
                this.validationErrors.date = 'Date is required';
                isValid = false;
            }
            
            return isValid;
        },
        
        // Handle form submission with dynamic update (no page reload)
        saveWorkout(event) {
            // Prevent default form submission and stop propagation
            if (event && event.preventDefault) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            console.log('[WORKOUT] Starting workout save process');
            
            // Check if the form is valid
            if (!this.validateForm()) {
                console.log('[WORKOUT] Form validation failed', this.validationErrors);
                this.showToast('error', 'Please fix the validation errors');
                return false;
            }
            
            console.log('[WORKOUT] Form validation passed, submitting data:', this.modal);
            
            // Create form data from the form
            const form = document.getElementById('createWorkoutForm');
            if (!form) {
                console.error('[WORKOUT] Form element not found in DOM');
                this.showToast('error', 'System error: Form not found');
                return false;
            }
            
            // Manually create FormData from Alpine model to ensure we get the right data
            const formData = new FormData();
            formData.append('Title', this.modal.title || '');
            formData.append('Date', this.modal.date || '');
            formData.append('Notes', this.modal.notes || '');
            formData.append('Duration', '0');
            
            // Get the anti-forgery token
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenElement) {
                console.error('[WORKOUT] Anti-forgery token not found in DOM');
                this.showToast('error', 'System error: Security token not found');
                return false;
            }
            formData.append('__RequestVerificationToken', tokenElement.value);
            
            // Disable form elements during submission to prevent double-submit
            const submitButton = form.querySelector('button[type="button"]');
            if (submitButton) submitButton.disabled = true;
            
            // Submit the form via fetch API
            console.log('[WORKOUT] Sending AJAX request to /User/Workout/Create');
            fetch('/User/Workout/Create', {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                // Ensure we don't follow redirects which could cause page reload
                redirect: 'manual'
            })
            .then(response => {
                console.log(`[WORKOUT] Create response received: ${response.status}`);
                
                // Re-enable the submit button regardless of response
                const form = document.getElementById('createWorkoutForm');
                if (form) {
                    const submitButton = form.querySelector('button[type="button"]');
                    if (submitButton) submitButton.disabled = false;
                }
                
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                
                // Parse the response as JSON
                return response.json();
            })
            .then(data => {
                console.log('[WORKOUT] Processing create response:', data);
                
                try {
                    // Check if the response contains success flag
                    if (data.success) {
                        console.log('[WORKOUT] Workout created successfully');
                        
                        // Show toast notification for success
                        this.showToast(data.toastType || 'success', data.toastMessage || 'Workout created successfully!');
                        
                        // Check if we have workouts data or HTML for the workouts container
                        if (data.workouts || data.workoutsHtml) {
                            console.log('[WORKOUT] Updating workouts container with new data');
                            
                            // Update only the workout list container with the new content
                            const workoutsContainer = document.getElementById('workoutsContainer');
                            if (workoutsContainer) {
                                // Store the old content in case we need to revert
                                const oldContent = workoutsContainer.innerHTML;
                                
                                try {
                                    // If we have HTML directly, use it
                                    if (data.workoutsHtml) {
                                        workoutsContainer.innerHTML = data.workoutsHtml;
                                    } 
                                    // Otherwise, refresh the workouts list via direct AJAX call
                                    // but don't call refreshWorkoutsList() which would trigger another toast
                                    else if (data.workouts) {
                                        // Instead of calling this.refreshWorkoutsList() which would show another toast,
                                        // we'll update the UI directly with the data we already have
                                        const workoutCards = data.workouts.map(workout => {
                                            return `
                                            <div class="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow duration-300 workout-card" data-workout-id="${workout.id}">
                                                <div class="p-6">
                                                    <div class="flex justify-between items-center mb-4">
                                                        <h3 class="text-xl font-semibold text-gray-800">${workout.title}</h3>
                                                        <div class="flex space-x-2">
                                                            <a href="/User/Workout/Edit/${workout.id}" class="text-blue-500 hover:text-blue-700">
                                                                <i class="fas fa-edit"></i>
                                                            </a>
                                                            <button type="button" 
                                                                    @click="showDeleteConfirmation('${workout.id}')" 
                                                                    class="text-red-500 hover:text-red-700">
                                                                <i class="fas fa-trash-alt"></i>
                                                            </button>
                                                        </div>
                                                    </div>
                                                    
                                                    <div class="flex items-center text-gray-600 mb-2">
                                                        <i class="far fa-clock mr-2"></i>
                                                        <span>${workout.durationDisplay || 'N/A'}</span>
                                                    </div>
                                                    
                                                    ${workout.completedDateDisplay ? `
                                                    <div class="flex items-center text-green-600 mb-2">
                                                        <i class="far fa-calendar-check mr-2"></i>
                                                        <span>Completed: ${workout.completedDateDisplay}</span>
                                                    </div>
                                                    ` : ''}
                                                    
                                                    ${workout.notes ? `
                                                    <div class="mt-4 text-gray-600">
                                                        <p class="text-sm">${workout.notes}</p>
                                                    </div>
                                                    ` : ''}
                                                    
                                                    <div class="mt-6">
                                                        <a href="/User/Workout/NewEditWorkout/${workout.id}" 
                                                           class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                                                            <i class="fas fa-dumbbell mr-2"></i>
                                                            View Exercises
                                                        </a>
                                                    </div>
                                                </div>
                                            </div>`;
                                        }).join('');
                                        
                                        workoutsContainer.innerHTML = `<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mt-8">${workoutCards}</div>`;
                                    }
                                    console.log('[WORKOUT] Workout list container updated successfully');
                                    
                                    // Initialize Alpine components in the new content
                                    if (window.Alpine && typeof window.Alpine.initTree === 'function') {
                                        setTimeout(() => {
                                            try {
                                                window.Alpine.initTree(workoutsContainer);
                                                console.log('[WORKOUT] Alpine components initialized in new content');
                                            } catch (alpineError) {
                                                console.error('[WORKOUT] Error initializing Alpine components:', alpineError);
                                            }
                                        }, 0);
                                    }
                                } catch (updateError) {
                                    console.error('[WORKOUT] Error updating workout list container:', updateError);
                                    
                                    // Revert to old content if update fails
                                    workoutsContainer.innerHTML = oldContent;
                                    
                                    return false;
                                }
                            } else {
                                console.error('[WORKOUT] Workout list container not found in DOM');
                                this.showToast('error', 'Error updating workout list. Please refresh the page.');
                                return false;
                            }
                        }
                        
                        // Close the modal
                        this.showModal = false;
                        this.modal = { title: '', date: '', notes: '' };
                    }
                } catch (error) {
                    console.error('[WORKOUT] Error processing create response:', error);
                    this.showToast('error', 'Error processing server response');
                }
            })
            .catch(error => {
                console.error('[WORKOUT] Error creating workout:', error);
                this.showToast('error', 'Error creating workout. Please try again.');
            });
        },
        
        // Helper method to refresh the workouts list via AJAX
        refreshWorkoutsList() {
            console.log('[WORKOUT] Refreshing workouts list via AJAX');
            
            // Set a safety timeout to reload the page if the AJAX call takes too long
            const safetyTimeout = setTimeout(() => {
                console.warn('[WORKOUT] Safety timeout triggered for GetWorkouts, reloading page');
                window.location.reload();
            }, 10000); // 10 seconds timeout
            
            // Get the anti-forgery token
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenElement) {
                console.error('[WORKOUT] Anti-forgery token not found in DOM');
                // Don't show toast here as it might cause a loop
                return;
            }
            
            // Make the AJAX request to get the updated workouts list
            fetch('/User/Workout/GetWorkouts', {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': tokenElement.value
                }
            })
            .then(response => {
                // Clear the safety timeout since we got a response
                clearTimeout(safetyTimeout);
                
                if (!response.ok) {
                    throw new Error(`Network response was not ok: ${response.status}`);
                }
                return response.text();
            })
            .then(html => {
                // Clear the safety timeout since we got a response
                clearTimeout(safetyTimeout);
                
                // Check if the response is empty or invalid
                if (!html || html.trim() === '') {
                    console.warn('[WORKOUT] Empty response received from GetWorkouts, reloading page');
                    window.location.reload();
                    return;
                }
                
                console.log('[WORKOUT] Processing GetWorkouts response');
                
                // Update the workouts container with the new HTML
                const workoutsContainer = document.getElementById('workoutsContainer');
                if (workoutsContainer) {
                    try {
                        // Use a more controlled approach to update the DOM
                        // First, create a temporary container to validate the HTML
                        const tempContainer = document.createElement('div');
                        tempContainer.innerHTML = html;
                        
                        // Verify that the HTML contains valid workout elements
                        const hasValidContent = tempContainer.querySelector('.workout-card') !== null;
                        
                        if (!hasValidContent) {
                            console.warn('[WORKOUT] Invalid HTML structure received, missing workout cards');
                            window.location.reload();
                            return;
                        }
                        
                        // Now update the actual container
                        workoutsContainer.innerHTML = html;
                        console.log('[WORKOUT] Workouts list refreshed successfully');
                        
                        // Use a more cautious approach to Alpine initialization
                        if (window.Alpine && typeof window.Alpine.initTree === 'function') {
                            try {
                                console.log('[WORKOUT] Initializing Alpine components in new content');
                                // Use setTimeout to ensure this runs after the current execution context
                                setTimeout(() => {
                                    window.Alpine.initTree(workoutsContainer);
                                    console.log('[WORKOUT] Alpine initialization completed successfully');
                                }, 0);
                            } catch (alpineError) {
                                console.error('[WORKOUT] Error initializing Alpine components:', alpineError);
                                // Don't reload immediately, just log the error
                            }
                        }
                    } catch (parseError) {
                        console.error('[WORKOUT] Error parsing or inserting HTML:', parseError);
                        // Don't show toast here as it might cause a loop
                        setTimeout(() => window.location.reload(), 1000);
                    }
                } else {
                    console.error('[WORKOUT] Workouts container not found in DOM');
                    // Don't show toast here as it might cause a loop
                    setTimeout(() => window.location.reload(), 1000);
                }
            })
            .catch(error => {
                // Clear the safety timeout
                clearTimeout(safetyTimeout);
                
                console.error('[WORKOUT] Error refreshing workouts list:', error);
                // Don't show toast here as it might cause a loop
                
                // If there's an error, reload the page to ensure UI consistency
                setTimeout(() => window.location.reload(), 1000);
            });
        },
        
        // Show delete confirmation modal using shared modal partial
        showDeleteConfirmation(id) {
            console.log(`[WORKOUT] Showing delete confirmation for workout ID: ${id}`);
            
            // Store the workout ID to delete
            this.deleteWorkoutId = id;
            
            // Remove any existing event listeners to prevent memory leaks
            if (this.handleModalConfirmed) {
                console.log('[WORKOUT] Removing existing modal-confirmed event listener');
                window.removeEventListener('modal-confirmed', this.handleModalConfirmed);
            }
            
            // Create a bound handler function that we can reference for removal
            this.handleModalConfirmed = () => {
                console.log('[WORKOUT] Modal confirmed event received, calling confirmDelete');
                this.confirmDelete();
            };
            
            // Set up event listener for modal confirmation with the bound handler
            console.log('[WORKOUT] Adding modal-confirmed event listener with once:true option');
            window.addEventListener('modal-confirmed', this.handleModalConfirmed, { once: true });
            
            // Open the modal using the shared modal partial
            console.log('[WORKOUT] Opening delete confirmation modal');
            window.openModal({
                type: 'delete',
                title: 'Delete Workout',
                message: 'Are you sure you want to delete this workout? This action cannot be undone.',
                confirmText: 'Delete',
                cancelText: 'Cancel'
            });
        },
        
        // Confirm workout deletion with optimized DOM update (no full page reload)
        confirmDelete() {
            console.log('[WORKOUT] Starting workout deletion process');
            
            if (!this.deleteWorkoutId) {
                console.warn('[WORKOUT] No workout ID to delete, aborting');
                return;
            }
            
            // Store the ID locally to prevent issues with async operations
            const workoutIdToDelete = this.deleteWorkoutId;
            console.log(`[WORKOUT] Deleting workout ID: ${workoutIdToDelete}`);
            
            // Find the workout element before deletion to enable smooth removal
            const workoutElement = document.querySelector(`.workout-card[data-workout-id="${workoutIdToDelete}"]`);
            if (!workoutElement) {
                console.warn(`[WORKOUT] Could not find workout element with ID ${workoutIdToDelete} in DOM`);
            }
            
            // Clear the ID immediately to prevent duplicate operations
            this.deleteWorkoutId = null;
            
            // Ensure body overflow is reset to allow scrolling
            console.log('[WORKOUT] Resetting body overflow style');
            document.body.style.overflow = '';
            
            // Clean up event listener to prevent memory leaks
            if (this.handleModalConfirmed) {
                console.log('[WORKOUT] Removing modal-confirmed event listener');
                window.removeEventListener('modal-confirmed', this.handleModalConfirmed);
                this.handleModalConfirmed = null;
            }
            
            // Get the anti-forgery token
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenElement) {
                console.error('[WORKOUT] Anti-forgery token not found in DOM');
                this.showToast('error', 'System error: Security token not found');
                return;
            }
            
            console.log('[WORKOUT] Sending AJAX request to delete workout');
            fetch(`/User/Workout/Delete/${workoutIdToDelete}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: `__RequestVerificationToken=${tokenElement.value}`
            })
            .then(response => {
                console.log(`[WORKOUT] Delete response received: ${response.status}`);
                
                if (!response.ok) {
                    throw new Error(`Network response was not ok: ${response.status}`);
                }
                return response.text(); // Get the full response text to process HTML partials
            })
            .then(html => {
                console.log('[WORKOUT] Delete response processed');
                
                try {
                    // Create a temporary container to parse the response
                    const tempContainer = document.createElement('div');
                    tempContainer.innerHTML = html;
                    
                    // Look for toast data container
                    const toastData = tempContainer.querySelector('#toast-data');
                    if (toastData) {
                        const success = toastData.getAttribute('data-success') === 'true';
                        const toastType = toastData.getAttribute('data-toast-type') || 'success';
                        const toastMessage = toastData.getAttribute('data-toast-message') || 'Workout deleted successfully';
                        
                        // Show toast notification
                        this.showToast(toastType, toastMessage);
                        
                        // If deletion was successful, remove the workout element from DOM
                        if (success && workoutElement) {
                            console.log(`[WORKOUT] Removing workout element with ID ${workoutIdToDelete} from DOM`);
                            
                            // Add fade-out animation
                            workoutElement.style.transition = 'opacity 0.5s ease';
                            workoutElement.style.opacity = '0';
                            
                            // Remove element after animation completes
                            setTimeout(() => {
                                workoutElement.remove();
                                
                                // Check if there are no more workouts and show empty state if needed
                                const remainingWorkouts = document.querySelectorAll('.workout-card');
                                if (remainingWorkouts.length === 0) {
                                    const workoutsContainer = document.getElementById('workoutsContainer');
                                    if (workoutsContainer) {
                                        workoutsContainer.innerHTML = '<div class="text-center py-8"><p class="text-gray-500">No workouts found. Create your first workout!</p></div>';
                                    }
                                }
                            }, 500);
                        } else if (success) {
                            // If we couldn't find the element but deletion was successful, refresh the list
                            console.log('[WORKOUT] Could not find workout element, refreshing list');
                            this.refreshWorkoutsList();
                        }
                    } else {
                        // Try to parse as JSON if no toast data found
                        try {
                            const jsonData = JSON.parse(html);
                            if (jsonData.success) {
                                this.showToast(jsonData.toastType || 'success', jsonData.toastMessage || 'Workout deleted successfully');
                                
                                // Remove the workout element if found
                                if (workoutElement) {
                                    workoutElement.style.transition = 'opacity 0.5s ease';
                                    workoutElement.style.opacity = '0';
                                    setTimeout(() => workoutElement.remove(), 500);
                                } else {
                                    this.refreshWorkoutsList();
                                }
                            } else {
                                this.showToast(jsonData.toastType || 'error', jsonData.toastMessage || 'Error deleting workout');
                            }
                        } catch (jsonError) {
                            console.error('[WORKOUT] Error parsing response as JSON:', jsonError);
                            this.showToast('success', 'Workout deleted successfully');
                            
                            // Fallback to removing the element directly
                            if (workoutElement) {
                                workoutElement.remove();
                            } else {
                                this.refreshWorkoutsList();
                            }
                        }
                    }
                    
                    // Look for toast partial in the response and inject it if found
                    const toastPartial = tempContainer.querySelector('#toastContainer');
                    if (toastPartial) {
                        console.log('[WORKOUT] Toast partial found in response, injecting into DOM');
                        
                        const existingToastContainer = document.getElementById('toastContainer');
                        if (existingToastContainer) {
                            existingToastContainer.innerHTML = toastPartial.innerHTML;
                            
                            // Initialize Alpine components in the toast container
                            if (window.Alpine && typeof window.Alpine.initTree === 'function') {
                                setTimeout(() => {
                                    try {
                                        window.Alpine.initTree(existingToastContainer);
                                        console.log('[WORKOUT] Alpine components initialized in new content');
                                    } catch (alpineError) {
                                        console.error('[WORKOUT] Error initializing Alpine components:', alpineError);
                                    }
                                }, 0);
                            }
                        }
                    }
                } catch (error) {
                    console.error('[WORKOUT] Error processing delete response:', error);
                    this.showToast('error', 'Error processing server response');
                }
            })
            .catch(error => {
                console.error('[WORKOUT] Error deleting workout:', error);
                this.showToast('error', 'Error deleting workout');
            });
        }
    };
};

// Initialize the Alpine component when the DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('Initializing fixed workout exercise handlers');
    
    // NEVER initialize Alpine.js here - it's already initialized in _Layout.cshtml
    
    // Wait for Alpine to be fully initialized before setting up exercise handlers
    setTimeout(function() {
        try {
            console.log('Setting up exercise handlers');
            
            // Check if handlers are already initialized to prevent duplicate initialization
            if (window.exerciseHandlersInitialized) {
                console.log('Exercise handlers already initialized, skipping');
                return;
            }
            
            // Set the flag to prevent duplicate initialization
            window.exerciseHandlersInitialized = true;
            
            // Use delegated event binding for dynamic content
            function setupDelegatedEventHandlers() {
                console.log('Setting up delegated event handlers for exercise buttons');
                document.body.addEventListener('click', handleExerciseButtonClick);
            }
            
            // Handle clicks on exercise buttons through event delegation
            function handleExerciseButtonClick(event) {
                // Check if the click was on an edit button
                if (event.target.matches('.edit-exercise-btn') || 
                    event.target.closest('.edit-exercise-btn')) {
                    
                    const button = event.target.matches('.edit-exercise-btn') ? 
                        event.target : event.target.closest('.edit-exercise-btn');
                    
                    // Find the index by traversing up to the exercise-item and getting its position
                    const exerciseItem = button.closest('.exercise-item');
                    if (!exerciseItem) return;
                    
                    const exerciseItems = Array.from(document.querySelectorAll('.exercise-item'));
                    const idx = exerciseItems.indexOf(exerciseItem);
                    
                    if (idx !== -1) {
                        console.log('Edit button clicked for exercise at index:', idx);
                        event.preventDefault();
                        event.stopPropagation();
                        editExerciseFixed(idx);
                    }
                }
                
                // Check if the click was on a delete button
                if (event.target.matches('.delete-exercise-btn') || 
                    event.target.closest('.delete-exercise-btn')) {
                    
                    const button = event.target.matches('.delete-exercise-btn') ? 
                        event.target : event.target.closest('.delete-exercise-btn');
                    
                    // Find the index by traversing up to the exercise-item and getting its position
                    const exerciseItem = button.closest('.exercise-item');
                    if (!exerciseItem) return;
                    
                    const exerciseItems = Array.from(document.querySelectorAll('.exercise-item'));
                    const idx = exerciseItems.indexOf(exerciseItem);
                    
                    if (idx !== -1) {
                        console.log('Delete button clicked for exercise at index:', idx);
                        event.preventDefault();
                        event.stopPropagation();
                        deleteExerciseFixed(idx);
                    }
                }
            }
            
            // Apply the delegated event handlers once
            setupDelegatedEventHandlers();
            
            // Handle modal-confirmed event for exercise deletion
            document.addEventListener('modal-confirmed', function(e) {
                console.log('Modal confirmed event received:', e.detail);
                
                // Get the Alpine component
                const alpineEl = document.querySelector('[x-data="editWorkoutPage()"]');
                if (alpineEl && alpineEl.__x) {
                    const alpine = alpineEl.__x.$data;
                    if (alpine && typeof alpine.confirmDelete === 'function') {
                        console.log('Calling confirmDelete method');
                        alpine.confirmDelete();
                    }
                }
            }, { once: false });
            
            console.log('Exercise handlers initialized successfully');
        } catch (error) {
            console.error('Error initializing exercise handlers:', error);
        }
    }, 1000); // Wait for Alpine to be fully initialized
});

// EDIT EXERCISE FUNCTIONALITY
function editExerciseFixed(idx) {
    console.log('Editing exercise at index:', idx);
    
    // Safely get Alpine instance
    const alpineEl = document.querySelector('[x-data="editWorkoutPage()"]');
    if (!alpineEl || !alpineEl.__x) {
        console.error('Alpine component not found');
        return;
    }
    
    const alpine = alpineEl.__x.$data;
    if (!alpine || !alpine.exercises || !alpine.exercises[idx]) {
        console.error('Exercise not found at index:', idx);
        return;
    }
    
    try {
        // Set current exercise for editing
        alpine.currentExerciseIndex = idx;
        alpine.exerciseForm = { ...alpine.exercises[idx] };
        
        // Open the exercise modal
        alpine.showExerciseModal = true;
        
        // Update exercise select if it exists
        setTimeout(() => {
            const exerciseSelect = document.getElementById('exerciseSelect');
            if (exerciseSelect) {
                // Set the selected value
                exerciseSelect.value = alpine.exerciseForm.exerciseId;
                
                // Trigger change event to update image preview
                const changeEvent = new Event('change');
                exerciseSelect.dispatchEvent(changeEvent);
            }
        }, 100);
    } catch (error) {
        console.error('Error in editExerciseFixed:', error);
    }
}

// DELETE EXERCISE FUNCTIONALITY
function deleteExerciseFixed(idx) {
    console.log('Deleting exercise at index:', idx);
    
    // Safely get Alpine instance
    const alpineEl = document.querySelector('[x-data="editWorkoutPage()"]');
    if (!alpineEl || !alpineEl.__x) {
        console.error('Alpine component not found');
        return;
    }
    
    const alpine = alpineEl.__x.$data;
    if (!alpine || !alpine.exercises || !alpine.exercises[idx]) {
        console.error('Exercise not found at index:', idx);
        return;
    }
    
    try {
        // Set exercise for deletion
        alpine.currentExerciseIndex = idx;
        
        // Show delete confirmation modal
        alpine.showDeleteConfirmation = function(workoutId) {
            console.log(`[WORKOUT] Showing delete confirmation for workout ${workoutId}`);
            this.deleteWorkoutId = workoutId;
            this.showDeleteModal = true;
        };
        
        // Handle workout deletion with AJAX
        alpine.confirmDelete = function(workoutId) {
            console.log(`[WORKOUT] Confirming deletion of workout ${workoutId || this.deleteWorkoutId}`);
            
            // Use the provided ID or the one stored in the component
            const id = workoutId || this.deleteWorkoutId;
            if (!id) {
                console.error('[WORKOUT] No workout ID provided for deletion');
                this.showToast('error', 'Error: No workout identified for deletion');
                return false;
            }
            
            // Hide the delete confirmation modal
            this.showDeleteModal = false;
            
            // Get the anti-forgery token
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenElement) {
                console.error('[WORKOUT] Anti-forgery token not found');
                this.showToast('error', 'Security token not found. Please refresh the page.');
                return false;
            }
            
            // Find the workout element to remove
            const workoutElement = document.querySelector(`.workout-card[data-workout-id="${id}"]`);
            if (!workoutElement) {
                console.warn(`[WORKOUT] Workout element with ID ${id} not found in DOM`);
            } else {
                console.log(`[WORKOUT] Found workout element to delete: `, workoutElement);
            }
            
            // Create form data for the request
            const formData = new FormData();
            formData.append('id', id);
            formData.append('__RequestVerificationToken', tokenElement.value);
            
            // Send the delete request
            fetch(`/User/Workout/Delete/${id}`, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                // Prevent redirects that could cause page reload
                redirect: 'manual'
            })
            .then(response => {
                console.log(`[WORKOUT] Delete response received: ${response.status}`);
                
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('[WORKOUT] Delete response data:', data);
                
                // Show the toast message
                this.showToast(data.toastType || 'success', data.toastMessage || 'Workout deleted successfully');
                
                // Remove the workout from the DOM with animation if found
                if (workoutElement) {
                    // Add fade-out animation
                    workoutElement.style.transition = 'opacity 0.5s, transform 0.5s';
                    workoutElement.style.opacity = '0';
                    workoutElement.style.transform = 'scale(0.95)';
                    
                    // Remove from DOM after animation completes
                    setTimeout(() => {
                        try {
                            workoutElement.remove();
                            console.log('[WORKOUT] Workout element removed from DOM');
                        } catch (error) {
                            console.error('[WORKOUT] Error removing workout element:', error);
                        }
                    }, 500);
                } else {
                    // If we couldn't find the specific element, refresh the whole list
                    console.warn(`[WORKOUT] Could not find workout element with ID ${id}, refreshing list`);
                    this.refreshWorkoutsList();
                }
            })
            .catch(error => {
                console.error('[WORKOUT] Error deleting workout:', error);
                this.showToast('error', 'Error deleting workout. Please try again.');
            });
            
            // Prevent default behavior and stop event propagation
            return false;
        };
        
        // Show toast notification using _ToastPartial.cshtml Alpine.js component
        alpine.showToast = function(type, message) {
            console.log(`[TOAST] Showing ${type} toast: ${message}`);
            
            // Use Alpine.js event system to trigger the toast in _ToastPartial.cshtml
            // This dispatches an event that the toast component listens for
            try {
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: type,
                        message: message
                    }
                }));
                console.log('[TOAST] Toast event dispatched successfully');
            } catch (error) {
                console.error('[TOAST] Error dispatching toast event:', error);
                
                // Fallback method if the event dispatch fails
                alert(`${type.toUpperCase()}: ${message}`);
            }
        };
        
        // Open confirmation modal
        window.openModal({
            type: 'delete',
            title: 'Delete Exercise',
            message: 'Are you sure you want to delete this exercise? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel'
        });
    } catch (error) {
        console.error('Error in deleteExerciseFixed:', error);
    }
}
