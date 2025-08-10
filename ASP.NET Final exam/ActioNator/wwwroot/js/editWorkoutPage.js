/**
 * editWorkoutPage.js - Alpine.js component for workout editing
 * Handles workout editing, exercise management, and AJAX interactions
 */

// Store workout and exercise data globally to ensure it's accessible to Alpine components
window.workoutData = window.workoutData || {};
window.exerciseOptionsData = window.exerciseOptionsData || [];

// Shared API + toast helpers (re-uses global if present)
const Api = window.Api || {
    getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        if (el && el.value) return el.value;
        // Fallback to meta tags if present
        const meta = document.querySelector('meta[name="csrf-token"], meta[name="RequestVerificationToken"]');
        return meta ? meta.getAttribute('content') : null;
    },
    async withTimeout(promise, ms, controller) {
        let timeoutId;
        const timeout = new Promise((_, reject) => {
            timeoutId = setTimeout(() => {
                try { controller && controller.abort(); } catch (_) {}
                reject(new Error(`Timeout after ${ms}ms`));
            }, ms);
        });
        try {
            return await Promise.race([promise, timeout]);
        } finally {
            clearTimeout(timeoutId);
        }
    },
    async get(url, { headers = {}, timeoutMs = 10000 } = {}) {
        const token = this.getToken();
        const controller = new AbortController();
        const res = await this.withTimeout(fetch(url, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                ...(token ? { 'RequestVerificationToken': token } : {}),
                ...headers
            },
            signal: controller.signal
        }), timeoutMs, controller);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res;
    },
    async postForm(url, formData, { headers = {}, timeoutMs = 10000, redirect = 'manual' } = {}) {
        const token = this.getToken();
        const controller = new AbortController();
        const res = await this.withTimeout(fetch(url, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                ...(token ? { 'RequestVerificationToken': token } : {}),
                ...headers
            },
            redirect,
            signal: controller.signal
        }), timeoutMs, controller);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res;
    },
    async postUrlEncoded(url, data, { headers = {}, timeoutMs = 10000, redirect = 'manual' } = {}) {
        const token = this.getToken();
        const controller = new AbortController();
        const res = await this.withTimeout(fetch(url, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/x-www-form-urlencoded',
                ...headers
            },
            redirect,
            body: new URLSearchParams({
                ...(token ? { __RequestVerificationToken: token } : {}),
                ...data
            }).toString(),
            signal: controller.signal
        }), timeoutMs, controller);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res;
    },
    async postJson(url, json, { headers = {}, timeoutMs = 10000, redirect = 'manual' } = {}) {
        const token = this.getToken();
        const controller = new AbortController();
        const res = await this.withTimeout(fetch(url, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/json',
                // Send both common header names for maximum compatibility
                ...(token ? { 'RequestVerificationToken': token, 'X-CSRF-TOKEN': token } : {}),
                ...headers
            },
            redirect,
            // Also include the token in the JSON body as a fallback; extra props are ignored by model binding
            body: JSON.stringify(token ? { __RequestVerificationToken: token, ...json } : json),
            signal: controller.signal
        }), timeoutMs, controller);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res;
    }
};

let __emitToastGuard = false;
const emitToast = window.emitToast || function(type, message) {
    if (__emitToastGuard) return;
    __emitToastGuard = true;
    try {
        // Prefer direct showToast if available; otherwise, emit a single event.
        if (typeof window.showToast === 'function') {
            try {
                console.debug('[TOAST] showToast invoked:', { type, message });
                window.showToast(type, message);
            } catch (e) {
                console.warn('[TOAST] window.showToast failed, falling back to event:', e);
                window.dispatchEvent(new CustomEvent('show-toast', { detail: { type, message } }));
            }
        } else {
            console.debug('[TOAST] dispatching show-toast event:', { type, message });
            window.dispatchEvent(new CustomEvent('show-toast', { detail: { type, message } }));
        }
    } catch (err) {
        console.error('[TOAST] Error dispatching toast:', err);
        try { alert(`${String(type || 'info').toUpperCase()}: ${message}`); } catch (_) {}
    } finally {
        setTimeout(() => { __emitToastGuard = false; }, 0);
    }
};

// Initialize data when document is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Document ready, initializing workout page');

    // Log the available data from the server
    console.log('Server workout data:', window.workoutData);
    console.log('Server exercise options:', window.exerciseOptionsData);

    // Ensure data is available globally
    window.workoutData = window.workoutData || {};
    window.exerciseOptionsData = window.exerciseOptionsData || [];

    // Make sure exercises array exists
    if (!window.workoutData.exercises || !Array.isArray(window.workoutData.exercises)) {
        window.workoutData.exercises = [];
    }

    // Set up image preview functionality
    setupImagePreview();
    // Keep normal POST submission; do not hijack via AJAX. Only ensure hidden JSON is set.
    const editWorkoutForm = document.getElementById('editWorkoutForm');
    if (editWorkoutForm) {
        editWorkoutForm.addEventListener('submit', function() {
            try {
                const exercises = Array.isArray(window.workoutData?.exercises)
                    ? window.workoutData.exercises
                    : (Array.isArray(window.workoutData?.Exercises) ? window.workoutData.Exercises : []);
                const exercisesDataField = document.getElementById('exercisesData');
                if (exercisesDataField) {
                    exercisesDataField.value = JSON.stringify(exercises);
                }
            } catch (e) {
                console.warn('Could not prepare exercisesJson for submit:', e);
            }
        });
    }
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
        // Normalize potential application-root URLs like "~/images/..."
        const normalized = imageUrl.replace(/^~\//, '/');
        imagePreview.src = normalized;
        imagePreview.style.display = 'block';
    } else {
        imagePreview.style.display = 'none';
    }
}

// Setup workout form submission via AJAX
function setupWorkoutFormSubmission() {
    // Disabled: Keep normal form POST submission as per project preference.
    const editWorkoutForm = document.getElementById('editWorkoutForm');
    if (editWorkoutForm) {
        console.info('[EDIT WORKOUT] Using normal form POST; AJAX hijack disabled.');
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
                const src = window.workoutData || {};
                const normalized = {
                    id: (src.id ?? src.Id ?? ""),
                    title: (src.title ?? src.Title ?? ""),
                    duration: (src.duration ?? src.Duration ?? 0),
                    exercises: Array.isArray(src.exercises ?? src.Exercises) ? (src.exercises ?? src.Exercises) : []
                };
                this.workout = normalized;
                
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

            // Ensure the shared delete modal can invoke the correct handler on this page.
            // The shared _ModalPartial calls window.handlePostDeletion() when type === 'delete'.
            // Bind it here so it calls this.confirmDelete() in the context of this Alpine component.
            try {
                window.handlePostDeletion = () => {
                    try {
                        console.debug('[EDIT WORKOUT] handlePostDeletion invoked');
                        this.confirmDelete();
                    } catch (e) {
                        console.error('[EDIT WORKOUT] Error executing confirmDelete from handlePostDeletion:', e);
                    }
                };
                console.log('[EDIT WORKOUT] Bound global handlePostDeletion for exercise deletion');
            } catch (e) {
                console.error('[EDIT WORKOUT] Failed to bind handlePostDeletion:', e);
            }
        },

        // Helper: normalize URLs that may include application root prefix (~/)
        _normalizeUrl(url) {
            if (!url || typeof url !== 'string') return url;
            return url.replace(/^~\//, '/');
        },

        // Fetch latest workout state from server and update local state
        async refreshWorkoutFromServer() {
            if (!this.workout?.id) return;
            try {
                const res = await Api.get(`/User/Workout/GetWorkoutJson?id=${encodeURIComponent(this.workout.id)}`);
                const data = await res.json();
                if (data && data.success && data.workout) {
                    const w = data.workout;
                    // Ensure exercises array and normalize any image URLs if present
                    const exercises = Array.isArray(w.exercises) ? w.exercises.map(ex => ({
                        ...ex,
                        imageUrl: this._normalizeUrl(ex.imageUrl)
                    })) : [];
                    this.workout = {
                        id: w.id || this.workout.id,
                        title: w.title || this.workout.title,
                        duration: typeof w.duration === 'number' ? w.duration : (this.workout.duration || 0),
                        notes: w.notes || this.workout.notes,
                        date: w.date || this.workout.date,
                        completedAt: w.completedAt || this.workout.completedAt,
                        exercises
                    };
                }
            } catch (err) {
                console.error('[WORKOUT] Failed to refresh workout from server:', err);
            } finally {
                // Always recalc duration from exercises to keep UI consistent
                this.recalculateDuration();
            }
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
        async saveExercise() {
            // Validate the form
            if (!this.validateExercise()) return;
            if (this.savingExercise) return;
            this.savingExercise = true;
            try {
                console.debug('[saveExercise] starting, mode:', this.modalFormMode);
                if (!this.workout) {
                    this.workout = {
                        id: window.workoutData.id || "",
                        title: window.workoutData.title || "",
                        duration: window.workoutData.duration || 0,
                        exercises: []
                    };
                }
                // Prepare payload
                const selectedOpt = this.exerciseOptions.find(o => o.Id === this.exerciseModal.exerciseTemplateId);
                const exerciseData = { ...this.exerciseModal };
                exerciseData.workoutId = this.workout.id;
                // Include both casings for robust model binding
                exerciseData.ExerciseTemplateId = this.exerciseModal.exerciseTemplateId;
                exerciseData.exerciseTemplateId = this.exerciseModal.exerciseTemplateId;
                if (this.modalFormMode === 'add') {
                    exerciseData.name = selectedOpt ? selectedOpt.Name : 'Unnamed Exercise';
                } else {
                    exerciseData.name = selectedOpt ? selectedOpt.Name : this.exerciseModal.name;
                }
                let url = '';
                if (this.modalFormMode === 'add') {
                    url = '/User/Workout/AddExercise';
                } else if (this.modalFormMode === 'edit') {
                    url = '/User/Workout/UpdateExercise';
                    exerciseData.id = this.workout.exercises[this.editIdx]?.id;
                }
                console.debug('[saveExercise] posting to:', url, 'payload:', {
                    ...exerciseData,
                    notes: exerciseData.notes ? '[len ' + String(exerciseData.notes).length + ']' : ''
                });
                const response = await Api.postJson(url, exerciseData, { redirect: 'manual' });
                console.debug('[saveExercise] response status:', response.status);
                const text = await response.text();
                let data;
                try {
                    data = JSON.parse(text);
                } catch (e) {
                    throw new Error('Invalid JSON response');
                }
                console.debug('[saveExercise] response json:', data);
                // Emit toast exactly once
                emitToast(data.toastType || 'info', data.toastMessage || 'Operation completed');
                if (data.success) {
                    console.debug('[saveExercise] success received, refreshing workout from server...');
                    // Refresh full workout state from server to ensure canonical data (incl. normalized image URLs)
                    try {
                        await this.refreshWorkoutFromServer();
                        console.debug('[saveExercise] workout refreshed successfully');
                    } catch (rfErr) {
                        console.error('[saveExercise] refreshWorkoutFromServer failed:', rfErr);
                    } finally {
                        // Close the modal after a successful server response regardless of refresh outcome
                        this.showExerciseModal = false;
                        console.debug('[saveExercise] modal closed');
                    }
                    // Clear validation errors
                    this.validationErrors = {};
                    // Optionally reset modal fields after close (kept minimal to not disrupt next open)
                } else if (data.errors) {
                    // Handle validation errors from server
                    this.validationErrors = {};
                    Object.entries(data.errors).forEach(([field, messages]) => {
                        if (messages && messages.length > 0) {
                            this.validationErrors[String(field).toLowerCase()] = messages[0];
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
                    console.debug('[saveExercise] validation errors set:', this.validationErrors);
                }
            } catch (error) {
                console.error('Error saving exercise:', error);
                emitToast('error', 'An error occurred while saving the exercise.');
            } finally {
                this.savingExercise = false;
                console.debug('[saveExercise] finished');
            }
        },

        // Confirm exercise deletion
        async confirmDelete() {
            if (this.deleteIndex === null || !this.workout.exercises[this.deleteIndex]) return;
            const exerciseId = this.workout.exercises[this.deleteIndex].id;
            try {
                const response = await Api.postJson('/User/Workout/DeleteExercise', { id: exerciseId }, { redirect: 'manual' });
                const text = await response.text();
                let data;
                try {
                    data = JSON.parse(text);
                } catch (e) {
                    throw new Error('Invalid JSON response');
                }
                const success = data.success === true;
                emitToast(data.toastType || 'info', data.toastMessage || 'Operation completed');
                if (success) {
                    await this.refreshWorkoutFromServer();
                } else {
                    console.error('Failed to delete exercise:', data);
                }
            } catch (error) {
                console.error('Error deleting exercise:', error);
                emitToast('error', 'Error deleting exercise');
            } finally {
                this.deleteIndex = null;
            }
        },

        // Recalculate workout duration based on exercise durations
        recalculateDuration() {
            // Sum all durations
            this.workout.duration = this.workout.exercises.reduce((sum, ex) => sum + (ex.duration || 0), 0);
        }
    }));
});