/**
 * editWorkoutPage.js - Alpine.js component for workout editing
 * Handles workout editing, exercise management, and AJAX interactions
 */

// Store workout and exercise data globally to ensure it's accessible to Alpine components
window.workoutData = window.workoutModelData || {};
window.exerciseOptionsData = window.exerciseOptionsData || [];

// Initialize data when document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Ensure data is available globally
    window.workoutData = window.workoutModelData || {};
    window.exerciseOptionsData = window.exerciseOptionsData || [];
    
    // Set up image preview functionality
    setupImagePreview();
    
    // Set up workout form submission
    setupWorkoutFormSubmission();
});

// Setup image preview functionality
function setupImagePreview() {
    const exerciseSelect = document.getElementById('exerciseSelect');
    const imagePreview = document.getElementById('exerciseImagePreview');
    
    if (exerciseSelect && imagePreview) {
        // Update image when select changes
        exerciseSelect.addEventListener('change', function() {
            updateImagePreview(exerciseSelect, imagePreview);
        });
    }
}

// Update image preview based on selected exercise
function updateImagePreview(exerciseSelect, imagePreview) {
    const selectedOption = exerciseSelect.options[exerciseSelect.selectedIndex];
    const imageUrl = selectedOption ? selectedOption.getAttribute('data-img') : null;
    
    if (imageUrl) {
        imagePreview.src = imageUrl;
        imagePreview.style.display = 'block';
    } else {
        imagePreview.style.display = 'none';
    }
}

// Setup workout form submission via AJAX
function setupWorkoutFormSubmission() {
    const editWorkoutForm = document.getElementById('editWorkoutForm');
    const saveWorkoutBtn = document.getElementById('saveWorkoutBtn');
    const saveWorkoutBtnText = document.getElementById('saveWorkoutBtnText');
    const saveWorkoutBtnLoading = document.getElementById('saveWorkoutBtnLoading');
    
    if (editWorkoutForm && saveWorkoutBtn) {
        editWorkoutForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Show loading state
            saveWorkoutBtnText.classList.add('hidden');
            saveWorkoutBtnLoading.classList.remove('hidden');
            saveWorkoutBtn.disabled = true;
            
            // Get form data
            const formData = new FormData(editWorkoutForm);
            
            // Send AJAX request
            fetch(editWorkoutForm.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                redirect: 'error'
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.text();
            })
            .then(html => {
                // Parse the HTML response
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');
                
                // Extract toast data
                const toastData = doc.querySelector('#toast-data');
                if (!toastData) {
                    console.error('Toast data not found in response');
                    window.dispatchEvent(new CustomEvent('show-toast', {
                        detail: { type: 'error', message: 'An error occurred while saving the workout.' }
                    }));
                    return;
                }
                
                const success = toastData.dataset.success === 'true';
                const toastType = toastData.dataset.toastType || 'info';
                const toastMessage = toastData.dataset.toastMessage || 'Operation completed';
                
                // Show toast notification
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: toastType,
                        message: toastMessage
                    }
                }));
                
                if (success) {
                    // If successful, redirect to the workouts list after a short delay
                    setTimeout(() => {
                        window.location.href = '/User/Workout';
                    }, 1000);
                } else {
                    // Reset button state
                    saveWorkoutBtnText.classList.remove('hidden');
                    saveWorkoutBtnLoading.classList.add('hidden');
                    saveWorkoutBtn.disabled = false;
                    
                    // Handle validation errors
                    const validationErrors = doc.querySelectorAll('#validationErrors .validation-error');
                    if (validationErrors && validationErrors.length > 0) {
                        // Process validation errors - update the form with error messages
                        validationErrors.forEach(error => {
                            const field = error.dataset.field;
                            const message = error.dataset.message;
                            if (field && message) {
                                const errorSpan = document.querySelector(`[data-valmsg-for="${field}"]`);
                                if (errorSpan) {
                                    errorSpan.textContent = message;
                                }
                            }
                        });
                    }
                }
            })
            .catch(error => {
                console.error('Error saving workout:', error);
                
                // Reset button state
                saveWorkoutBtnText.classList.remove('hidden');
                saveWorkoutBtnLoading.classList.add('hidden');
                saveWorkoutBtn.disabled = false;
                
                // Show error toast
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: { type: 'error', message: 'An error occurred while saving the workout.' }
                }));
            });
        });
    }
}

// Register Alpine.js component
document.addEventListener('alpine:init', function() {
    Alpine.data('editWorkoutPage', () => ({
        // Component state
        workout: {
            id: "",
            title: "",
            duration: 0,
            exercises: []
        },
        exerciseOptions: [],
        exerciseModal: {
            exerciseTemplateId: "",
            name: "",
            sets: 3,
            reps: 10,
            weight: 0,
            duration: 10,
            notes: ""
        },
        validationErrors: {},
        editIdx: null,
        deleteIndex: null,
        modalFormMode: '', // 'add' or 'edit'
        showExerciseModal: false,
        savingExercise: false,

        // Initialize component
        init() {
            // Load workout data from server with defensive initialization
            try {
                // Initialize workout with safe defaults first
                this.workout = {
                    id: window.workoutData.id || "",
                    title: window.workoutData.title || "",
                    duration: window.workoutData.duration || 0,
                    exercises: []
                };
                
                // Then try to load from window data if available
                if (window.workoutData) {
                    this.workout = {
                        id: window.workoutData.id || this.workout.id,
                        title: window.workoutData.title || this.workout.title,
                        duration: window.workoutData.duration || this.workout.duration,
                        exercises: Array.isArray(window.workoutData.exercises) ? 
                            window.workoutData.exercises : []
                    };
                }
                
                // Load exercise options with defensive check
                this.exerciseOptions = [];
                if (window.exerciseOptionsData && Array.isArray(window.exerciseOptionsData)) {
                    this.exerciseOptions = window.exerciseOptionsData;
                }
                
                console.log('Workout initialized:', this.workout);
                console.log('Exercise options:', this.exerciseOptions);
            } catch (error) {
                console.error('Error initializing workout data:', error);
                // Ensure workout is always defined with safe defaults
                this.workout = {
                    id: workoutData.id || "",
                    title: workoutData.title || "",
                    duration: workoutData.duration || 0,
                    exercises: []
                };
                this.exerciseOptions = [];
            }
            
            // Listen for modal confirmation events (only for delete)
            window.addEventListener('modal-confirmed', (e) => {
                if (e.detail.type === 'delete' && e.detail.title === 'Delete Exercise') {
                    this.confirmDelete();
                }
            });

            // Watch for modal open events to update image preview
            this.$watch('showExerciseModal', (value) => {
                if (value === true) {
                    // Modal opened, update image after a short delay
                    setTimeout(() => {
                        const exerciseSelect = document.getElementById('exerciseSelect');
                        const imagePreview = document.getElementById('exerciseImagePreview');
                        if (exerciseSelect && imagePreview) {
                            updateImagePreview(exerciseSelect, imagePreview);
                        }
                    }, 100);
                }
            });
        },

        // Reset exercise modal to default state
        resetExerciseModal() {
            this.exerciseModal = {
                exerciseTemplateId: this.exerciseOptions.length > 0 ? this.exerciseOptions[0].Id : '',
                name: '',
                sets: 3,
                reps: 10,
                weight: 0,
                duration: 10,
                notes: ''
            };
            this.editIdx = null;
            this.validationErrors = {};
            
            // Clear validation error messages
            this.$nextTick(() => {
                document.querySelectorAll('[data-valmsg-for]').forEach(el => {
                    el.textContent = '';
                });
            });
        },

        // Open modal to add a new exercise
        openAddExerciseModal() {
            this.modalFormMode = 'add';
            this.resetExerciseModal();
            this.showExerciseModal = true;
            this.$nextTick(() => {
                const firstInput = document.querySelector('#addExerciseForm input, #addExerciseForm select');
                if (firstInput) firstInput.focus();
            });
        },

        // Open modal to edit an existing exercise
        editExercise(idx) {
            this.editIdx = idx;
            this.modalFormMode = 'edit';
            this.exerciseModal = { ...this.workout.exercises[idx] };
            // Ensure exerciseTemplateId is set for select
            if (!this.exerciseModal.exerciseTemplateId && this.exerciseOptions.length > 0) {
                const match = this.exerciseOptions.find(o => o.Name === this.exerciseModal.name);
                if (match) this.exerciseModal.exerciseTemplateId = match.Id;
            }
            this.showExerciseModal = true;
            this.$nextTick(() => {
                const firstInput = document.querySelector('#addExerciseForm input, #addExerciseForm select');
                if (firstInput) firstInput.focus();
            });
        },

        // Show confirmation modal for exercise deletion
        deleteExercise(idx) {
            this.deleteIndex = idx;
            openModal({
                type: 'delete',
                title: 'Delete Exercise',
                message: 'Are you sure you want to delete this exercise? This action cannot be undone.',
                confirmText: 'Delete',
                cancelText: 'Cancel'
            });
        },

        // Validate exercise form data
        validateExercise() {
            let isValid = true;
            this.validationErrors = {};
            
            // Validate exercise name/template
            if (!this.exerciseModal.exerciseTemplateId) {
                this.validationErrors.exerciseTemplateId = 'Please select an exercise';
                isValid = false;
            }
            
            // Validate sets
            if (!this.exerciseModal.sets) {
                this.validationErrors.sets = 'Sets are required';
                isValid = false;
            } else if (this.exerciseModal.sets < 1 || this.exerciseModal.sets > 100) {
                this.validationErrors.sets = 'Sets must be between 1 and 100';
                isValid = false;
            }
            
            // Validate reps
            if (!this.exerciseModal.reps) {
                this.validationErrors.reps = 'Reps are required';
                isValid = false;
            } else if (this.exerciseModal.reps < 1 || this.exerciseModal.reps > 1000) {
                this.validationErrors.reps = 'Reps must be between 1 and 1000';
                isValid = false;
            }
            
            // Validate weight
            if (this.exerciseModal.weight < 0 || this.exerciseModal.weight > 1000) {
                this.validationErrors.weight = 'Weight must be between 0 and 1000';
                isValid = false;
            }
            
            // Validate duration
            if (!this.exerciseModal.duration) {
                this.validationErrors.duration = 'Duration is required';
                isValid = false;
            } else if (this.exerciseModal.duration < 1 || this.exerciseModal.duration > 300) {
                this.validationErrors.duration = 'Duration must be between 1 and 300 minutes';
                isValid = false;
            }
            
            // Update validation messages in the UI
            this.$nextTick(() => {
                Object.keys(this.validationErrors).forEach(field => {
                    const errorSpan = document.querySelector(`[data-valmsg-for="${field}"]`);
                    if (errorSpan) {
                        errorSpan.textContent = this.validationErrors[field];
                    }
                });
            });
            
            return isValid;
        },
        
        // Save exercise (add new or update existing)
        saveExercise() {
            // Validate the form
            if (!this.validateExercise()) {
                return;
            }
            if (this.savingExercise) return;
            this.savingExercise = true;
            if (!this.workout) {
                this.workout = { 
                    id: window.workoutData.id || "",
                    title: window.workoutData.title || "",
                    duration: window.workoutData.duration || 0,
                    exercises: [] 
                };
            }
            // Find selected exercise name for payload
            const selectedOpt = this.exerciseOptions.find(o => o.Id === this.exerciseModal.exerciseTemplateId);
            const exerciseData = { ...this.exerciseModal };
            
            // Ensure proper casing for server-side model binding
            exerciseData.workoutId = this.workout.id;
            
            // Debug: Log the exercise template ID to verify it's being set correctly
            console.log('Exercise Template ID:', this.exerciseModal.exerciseTemplateId);
            console.log('Selected Template:', selectedOpt);
            
            // Make sure both property casings are included to handle any model binding issues
            exerciseData.ExerciseTemplateId = this.exerciseModal.exerciseTemplateId; // Capital E for model binding
            exerciseData.exerciseTemplateId = this.exerciseModal.exerciseTemplateId; // Lowercase e as backup
            
            // When adding a new exercise, we'll let the server derive the name from the template
            // This matches the behavior in the Edit modal where the name comes from the template
            if (this.modalFormMode === 'add') {
                // For new exercises, set the name from the template directly on the client side
                // This ensures the name is always present in the payload
                if (selectedOpt) {
                    exerciseData.name = selectedOpt.Name;
                    console.log('Setting name from template:', exerciseData.name);
                } else {
                    exerciseData.name = 'Unnamed Exercise'; // Fallback name
                    console.log('Using fallback name');
                }
            } else {
                // For editing, use the existing name or derive from template
                exerciseData.name = selectedOpt ? selectedOpt.Name : this.exerciseModal.name;
            }
            
            // Debug: Log the final payload
            console.log('Exercise payload:', exerciseData);
            let url = '';
            if (this.modalFormMode === 'add') {
                url = '/User/Workout/AddExercise';
            } else if (this.modalFormMode === 'edit') {
                url = '/User/Workout/UpdateExercise';
                exerciseData.id = this.workout.exercises[this.editIdx].id;
            }
            
            // Prevent form submission if we're already handling it via AJAX
            const form = document.getElementById('addExerciseForm');
            if (form) {
                form.addEventListener('submit', function(e) {
                    e.preventDefault();
                });
            }
            fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(exerciseData),
                // Important: Ensure we don't follow redirects that would cause page replacement
                redirect: 'error'
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Network response was not ok: ${response.status}`);
                }
                // Log the response for debugging
                return response.text().then(text => {
                    try {
                        // Try to parse as JSON
                        const data = JSON.parse(text);
                        console.log('[WORKOUT] Exercise save response:', data);
                        return data;
                    } catch (e) {
                        // If not valid JSON, throw error with the text
                        console.error('[WORKOUT] Failed to parse response as JSON:', text);
                        throw new Error(`Invalid JSON response: ${e.message}`);
                    }
                });
            })
            .then(data => {
                this.savingExercise = false;
                
                // Show toast notification
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: data.toastType || 'info',
                        message: data.toastMessage || 'Operation completed'
                    }
                }));
                
                if (data.success) {
                    // Use the exercise data directly from the JSON response
                    const exerciseResult = data.exercise;
                    
                    if (this.modalFormMode === 'add') {
                        // Add new exercise to the list
                        if (!this.workout.exercises) this.workout.exercises = [];
                        this.workout.exercises.push({
                            id: exerciseResult?.id || exerciseData.id || crypto.randomUUID(),
                            ...exerciseData,
                            ...exerciseResult
                        });
                    } else if (this.modalFormMode === 'edit') {
                        // Update existing exercise
                        this.workout.exercises[this.editIdx] = {
                            id: this.workout.exercises[this.editIdx].id,
                            ...exerciseData,
                            ...exerciseResult
                        };
                    }
                    this.showExerciseModal = false;
                    // Clear validation errors
                    this.validationErrors = {};
                    
                    // Update workout duration if provided
                    if (exerciseResult?.duration) {
                        this.recalculateDuration();
                    }
                } else {
                    // Handle validation errors from server
                    if (data.errors) {
                        // Reset validation errors
                        this.validationErrors = {};
                        
                        // Process validation errors
                        Object.entries(data.errors).forEach(([field, messages]) => {
                            if (messages && messages.length > 0) {
                                this.validationErrors[field.toLowerCase()] = messages[0];
                            }
                        });
                        
                        // Update validation messages in the UI
                        this.$nextTick(() => {
                            Object.keys(this.validationErrors).forEach(field => {
                                const errorSpan = document.querySelector(`[data-valmsg-for="${field}"]`);
                                if (errorSpan) {
                                    errorSpan.textContent = this.validationErrors[field];
                                }
                            });
                        });
                    }
                }
            })
            .catch(error => {
                console.error('Error saving exercise:', error);
                this.savingExercise = false;
                
                // Create a more descriptive error message
                let errorMessage = 'An error occurred while saving the exercise.';
                if (error.message) {
                    errorMessage += ' ' + error.message;
                }
                
                // Dispatch toast event with circuit breaker to prevent infinite loops
                let toastAttempts = window._toastAttempts || 0;
                if (toastAttempts < 3) { // Prevent infinite toast loops
                    window._toastAttempts = toastAttempts + 1;
                    window.dispatchEvent(new CustomEvent('show-toast', {
                        detail: { type: 'error', message: errorMessage }
                    }));
                    
                    // Reset toast attempts counter after a delay
                    setTimeout(() => {
                        window._toastAttempts = 0;
                    }, 2000);
                }
            });
        },

        // Confirm exercise deletion
        confirmDelete() {
            if (this.deleteIndex === null || !this.workout.exercises[this.deleteIndex]) return;
            const exerciseId = this.workout.exercises[this.deleteIndex].id;
            const deleteIndex = this.deleteIndex; // Store for closure
            
            fetch('/User/Workout/DeleteExercise', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': document.querySelector('input[name=__RequestVerificationToken]').value
                },
                body: JSON.stringify({ id: exerciseId }),
                // Important: Ensure we don't follow redirects that would cause page replacement
                redirect: 'error'
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                // Log the response for debugging
                return response.text().then(text => {
                    try {
                        // Try to parse as JSON
                        const data = JSON.parse(text);
                        console.log('[WORKOUT] Exercise delete response:', data);
                        return data;
                    } catch (e) {
                        // If not valid JSON, throw error with the text
                        console.error('[WORKOUT] Failed to parse response as JSON:', text);
                        throw new Error(`Invalid JSON response: ${e.message}`);
                    }
                });
            })
            .then(data => {
                // Use the JSON response data directly
                const success = data.success === true;
                const toastType = data.toastType || 'info';
                const toastMessage = data.toastMessage || 'Operation completed';
                
                // Show toast notification
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: {
                        type: toastType,
                        message: toastMessage
                    }
                }));
                
                if (success) {
                    // Remove the exercise from the list
                    if (this.workout.exercises && this.deleteIndex !== null) {
                        this.workout.exercises.splice(this.deleteIndex, 1);
                        this.deleteIndex = null;
                        
                        // Recalculate workout duration
                        this.recalculateDuration();
                    }
                } else {
                    console.error('Failed to delete exercise:', data);
                }
                this.deleteIndex = null;
            })
            .catch(error => {
                console.error('Error deleting exercise:', error);
                window.dispatchEvent(new CustomEvent('show-toast', {
                    detail: { type: 'error', message: 'Error deleting exercise' }
                }));
                this.deleteIndex = null;
            });
        },

        // Recalculate workout duration based on exercise durations
        recalculateDuration() {
            // Sum all durations
            this.workout.duration = this.workout.exercises.reduce((sum, ex) => sum + (ex.duration || 0), 0);
        }
    }));
});