// Define the main workouts page Alpine component
// Shared helpers for API requests, timeouts, and toast emission
const Api = {
    getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : null;
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
        const controller = new AbortController();
        const res = await this.withTimeout(fetch(url, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
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
    }
    ,
    async postJson(url, data, { headers = {}, timeoutMs = 10000, redirect = 'manual' } = {}) {
        const token = this.getToken();
        const controller = new AbortController();
        const res = await this.withTimeout(fetch(url, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/json; charset=utf-8',
                // Custom antiforgery filter expects X-CSRF-TOKEN for JSON requests
                ...(token ? { 'X-CSRF-TOKEN': token } : {}),
                ...headers
            },
            redirect,
            body: JSON.stringify(data ?? {}),
            signal: controller.signal
        }), timeoutMs, controller);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res;
    }
};

// Centralized toast emission with re-entrancy guard
let __toastEmitting = false;
/**
 * Emit a toast via a single 'show-toast' event and optional global handler.
 * Falls back to alert() on failure.
 * @param {'success'|'error'|'info'|'warning'} type
 * @param {string} message
 */
function emitToast(type, message) {
    if (__toastEmitting) return;
    __toastEmitting = true;
    try {
        window.dispatchEvent(new CustomEvent('show-toast', {
            detail: { type, message }
        }));
        if (typeof window.showToast === 'function') {
            try { window.showToast(type, message); } catch (e) { console.warn('[TOAST] window.showToast failed:', e); }
        }
    } catch (err) {
        console.error('[TOAST] Error dispatching toast:', err);
        try { alert(`${String(type || 'info').toUpperCase()}: ${message}`); } catch (_) {}
    } finally {
        setTimeout(() => { __toastEmitting = false; }, 0);
    }
}

window.workoutsPage = function() {
    return {
        // Core properties for workout list and modals
        showModal: false,
        showDeleteModal: false,
        deleteWorkoutId: null,
        modal: { title: '', date: '', notes: '' },
        validationErrors: { title: '', date: '' },
        _isAfterDeletion: false, // Flag to track post-deletion state
        // Pagination state
        currentPage: 1,
        pageSize: 3,
        totalPages: 1,
        
        // Initialize the component
        init() {
            console.log('[WORKOUT] Initializing workouts page Alpine component');
            this.setupEventListeners();
            // Initialize pagination state from DOM/model
            this.syncPageStateFromDom();
            // Ensure the shared modal's delete-confirm callback exists on this page.
            // The shared _ModalPartial calls window.handlePostDeletion() when type === 'delete'.
            // Define a local shim only if not already present (to avoid overriding community pages).
            if (typeof window.handlePostDeletion !== 'function') {
                window.handlePostDeletion = () => {
                    if (this.deleteWorkoutId) {
                        // Route to workout deletion flow
                        this.confirmDelete();
                    } else {
                        // Avoid double-dispatching 'modal-confirmed' which may have null detail
                        // This can trigger errors in listeners expecting a detail object.
                        console.debug('[WORKOUT] handlePostDeletion without deleteWorkoutId; ignoring.');
                    }
                };
                console.log('[WORKOUT] Installed handlePostDeletion shim for workout deletion');
            }
        },
        
        // Pagination helpers
        syncPageStateFromDom() {
            try {
                const el = document.getElementById('paginationContainer');
                if (el) {
                    const p = Number(el.getAttribute('data-current-page')) || 1;
                    const tp = Number(el.getAttribute('data-total-pages')) || 1;
                    this.currentPage = Math.max(1, p);
                    this.totalPages = Math.max(1, tp);
                    // Page size is constant from server contract (3) unless overridden
                    this.pageSize = 3;
                } else if (window.workoutModelData) {
                    const data = window.workoutModelData;
                    this.currentPage = Math.max(1, Number(data.Page) || 1);
                    this.totalPages = Math.max(1, Number(data.TotalPages) || 1);
                    this.pageSize = Math.max(1, Number(data.PageSize) || 3);
                }
                console.log('[WORKOUT] Pagination state', { page: this.currentPage, totalPages: this.totalPages, pageSize: this.pageSize });
            } catch (e) {
                console.warn('[WORKOUT] Unable to read pagination state from DOM', e);
            }
        },
        // Navigate to a specific page
        goToPage(p) {
            const page = Math.max(1, Math.min(Number(p) || 1, this.totalPages || 1));
            if (page === this.currentPage) return;
            this.currentPage = page;
            this.refreshWorkoutsList();
        },
        // Next page
        nextPage() {
            if (this.currentPage < this.totalPages) {
                this.currentPage += 1;
                this.refreshWorkoutsList();
            }
        },
        // Previous page
        prevPage() {
            if (this.currentPage > 1) {
                this.currentPage -= 1;
                this.refreshWorkoutsList();
            }
        },
        // Build URL with page query params
        _buildPageUrl(base) {
            const url = new URL(base, window.location.origin);
            url.searchParams.set('page', String(this.currentPage || 1));
            url.searchParams.set('pageSize', String(this.pageSize || 3));
            return url.toString().replace(window.location.origin, '');
        },

        // Set up event listeners
        setupEventListeners() {
            // Observe toast events for debugging (do not re-dispatch to avoid loops)
            window.addEventListener('show-toast', (event) => {
                if (event?.detail?.type && event?.detail?.message) {
                    console.debug('[WORKOUT] show-toast observed:', event.detail);
                }
            });
            
            // Add global error handler to catch any unhandled errors
            window.addEventListener('error', (event) => {
                console.error('[WORKOUT] Unhandled error caught:', event.error);
                this.showToast('error', 'An unexpected error occurred. Please refresh the page.');
            });
        },
        
        /**
         * Show toast using the shared toast partial and optional global handler.
         * Dispatches a single 'show-toast' event and safeguards against loops.
         * @param {'success'|'error'|'info'|'warning'} type
         * @param {string} message
         */
        showToast(type, message) {
            emitToast(type, message);
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
                this.validationErrors.date = 'Please enter a valid date';
                isValid = false;
            } else {
                // Additional client-side validation: ensure the date string is a valid date
                const parsed = new Date(this.modal.date);
                if (isNaN(parsed.getTime())) {
                    this.validationErrors.date = 'Please enter a valid date';
                    isValid = false;
                }
            }
            
            return isValid;
        },
        
        // Handle form submission with dynamic update (no page reload)
        async saveWorkout(event) {
            if (event?.preventDefault) {
                event.preventDefault();
                event.stopPropagation();
            }
            console.log('[WORKOUT] Starting workout save process');
            if (!this.validateForm()) {
                console.log('[WORKOUT] Form validation failed', this.validationErrors);
                this.showToast('error', 'Please fix the validation errors');
                return false;
            }
            const form = document.getElementById('createWorkoutForm');
            if (!form) {
                console.error('[WORKOUT] Form element not found in DOM');
                this.showToast('error', 'System error: Form not found');
                return false;
            }
            // Prepare JSON payload to match server's [FromBody] model binder
            let exercises = [];
            try {
                const raw = Array.isArray(window.exercisesData) ? window.exercisesData : [];
                const isGuid = (val) => typeof val === 'string' && /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/.test(val);
                exercises = raw.map(ex => {
                    const base = {
                        Name: ex.name ?? ex.Name ?? '',
                        Sets: Number(ex.sets ?? ex.Sets) || 3,
                        Reps: Number(ex.reps ?? ex.Reps) || 10,
                        Weight: Number(ex.weight ?? ex.Weight) || 0,
                        Duration: Number(ex.duration ?? ex.Duration) || 10,
                        Notes: ex.notes ?? ex.Notes ?? ''
                    };
                    const tpl = ex.exerciseTemplateId ?? ex.ExerciseTemplateId;
                    if (isGuid(tpl)) {
                        base.ExerciseTemplateId = tpl;
                    }
                    return base;
                });
            } catch (error) {
                console.error('[WORKOUT] Error preparing exercises array:', error);
            }
            // Build payload carefully to match server types:
            // - Omit Duration entirely (server calculates TimeSpan)
            // - Only include Date if non-empty (empty string would break DateTime binding)
            const payload = {
                Title: this.modal.title || '',
                Notes: this.modal.notes || '',
                Exercises: exercises
            };
            if (this.modal?.date) {
                // Send as ISO-compatible date-time to help System.Text.Json parse reliably
                payload.Date = `${this.modal.date}T00:00:00`;
            }
            console.debug('[WORKOUT] Payload for CreateWorkout:', payload);
            const submitButton = form.querySelector('button[type="button"]');
            if (submitButton) submitButton.disabled = true;
            try {
                console.log('[WORKOUT] Sending AJAX request to /User/Workout/CreateWorkout as JSON');
                const res = await Api.postJson('/User/Workout/CreateWorkout', payload, { redirect: 'manual' });
                const contentType = res.headers.get('content-type') || '';
                const data = contentType.includes('application/json') ? await res.json() : await res.text().then(t => { try { return JSON.parse(t); } catch { return { text: t }; } });
                console.log('[WORKOUT] Processing create response:', data);
                this.validationErrors = { title: '', date: '', notes: '' };
                if (data.success === false) {
                    this.showToast(data.toastType || 'error', data.toastMessage || 'Please fix the validation errors');
                    if (data.validationErrors && typeof data.validationErrors === 'object') {
                        Object.keys(data.validationErrors).forEach(key => {
                            const errors = data.validationErrors[key];
                            if (errors?.length) {
                                const lowerKey = key.toLowerCase();
                                this.validationErrors[lowerKey] = errors[0];
                                const field = document.getElementById(key);
                                if (field) field.classList.add('border-red-500');
                            }
                        });
                    } else {
                        this.validationErrors.title = 'Please provide a title for the workout';
                        const titleField = document.getElementById('Title');
                        if (titleField) titleField.classList.add('border-red-500');
                    }
                    return false;
                }
                if (data.success) {
                    this.showToast(data.toastType || 'success', data.toastMessage || 'Workout created successfully!');
                    const workoutsContainer = document.getElementById('workoutsContainer');
                    if (workoutsContainer && (data.workouts || data.workoutsHtml)) {
                        const oldContent = workoutsContainer.innerHTML;
                        try {
                            if (data.workoutsHtml) {
                                workoutsContainer.innerHTML = data.workoutsHtml;
                                // Sync pagination state with newly rendered partial
                                if (typeof this.syncPageStateFromDom === 'function') {
                                    this.syncPageStateFromDom();
                                }
                            } else if (data.workouts) {
                                const workoutCards = data.workouts.map(workout => `
                                    <div class="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow duration-300 workout-card" data-workout-id="${workout.id}">
                                        <div class="p-6">
                                            <div class="flex justify-between items-center mb-4">
                                                <h3 class="text-xl font-semibold text-gray-800">${workout.title}</h3>
                                                <div class="flex space-x-2">
                                                    <a href="/User/Workout/Edit/${workout.id}" class="text-blue-500 hover:text-blue-700">
                                                        <i class="fas fa-edit"></i>
                                                    </a>
                                                    <button type="button" @click="showDeleteConfirmation('${workout.id}')" class="text-red-500 hover:text-red-700">
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
                                            </div>` : ''}
                                            ${workout.notes ? `
                                            <div class="mt-4 text-gray-600">
                                                <p class="text-sm">${workout.notes}</p>
                                            </div>` : ''}
                                            <div class="mt-6">
                                                <a href="/User/Workout/Edit/${workout.id}" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                                                    <i class="fas fa-dumbbell mr-2"></i>
                                                    View Exercises
                                                </a>
                                            </div>
                                        </div>
                                    </div>`).join('');
                                workoutsContainer.innerHTML = `<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mt-8">${workoutCards}</div>`;
                            }
                            if (window.Alpine?.initTree) {
                                setTimeout(() => {
                                    try { window.Alpine.initTree(workoutsContainer); } catch (e) { console.error('[WORKOUT] Alpine init error:', e); }
                                }, 0);
                            }
                        } catch (updateError) {
                            console.error('[WORKOUT] Error updating workout list container:', updateError);
                            workoutsContainer.innerHTML = oldContent;
                            return false;
                        }
                    } else if (!workoutsContainer) {
                        console.error('[WORKOUT] Workout list container not found in DOM');
                        this.showToast('error', 'Error updating workout list. Please refresh the page.');
                        return false;
                    }
                    // Close modal and reset state
                    this.showModal = false;
                    this.modal = { title: '', date: '', notes: '' };
                }
            } catch (error) {
                console.error('[WORKOUT] Error creating workout:', error);
                this.showToast('error', 'Error creating workout. Please try again.');
            } finally {
                if (submitButton) submitButton.disabled = false;
            }
            return false;
        },
        
        // Helper method to refresh the workouts list via AJAX
        async refreshWorkoutsList() {
            console.log('[WORKOUT] Refreshing workouts list via AJAX');
            try {
                // Ensure page within bounds before request
                if (this.currentPage <= 0) this.currentPage = 1;
                if (this.pageSize <= 0) this.pageSize = 3;
                const url = this._buildPageUrl('/User/Workout/GetWorkouts');
                const res = await Api.get(url, { timeoutMs: 10000 });
                const html = await res.text();
                if (!html || html.trim() === '') {
                    console.warn('[WORKOUT] Empty response received from GetWorkouts, reloading page');
                    window.location.reload();
                    return;
                }
                const workoutsContainer = document.getElementById('workoutsContainer');
                if (workoutsContainer) {
                    try {
                        const tempContainer = document.createElement('div');
                        tempContainer.innerHTML = html;
                        const hasValidContent = tempContainer.querySelector('.workout-card') !== null;
                        if (!hasValidContent) {
                            console.warn('[WORKOUT] Invalid HTML structure received, missing workout cards');
                            window.location.reload();
                            return;
                        }
                        workoutsContainer.innerHTML = html;
                        // After update, sync pagination from the new DOM
                        this.syncPageStateFromDom();
                        // If current page is now out of bounds (e.g., after deletion), fetch the last page
                        if (this.currentPage > this.totalPages) {
                            this.currentPage = this.totalPages;
                            const url2 = this._buildPageUrl('/User/Workout/GetWorkouts');
                            const res2 = await Api.get(url2, { timeoutMs: 10000 });
                            const html2 = await res2.text();
                            workoutsContainer.innerHTML = html2;
                            this.syncPageStateFromDom();
                        }
                        console.log('[WORKOUT] Workouts list refreshed successfully');
                        if (window.Alpine?.initTree) {
                            setTimeout(() => {
                                try { window.Alpine.initTree(workoutsContainer); }
                                catch (alpineError) { console.error('[WORKOUT] Error initializing Alpine components:', alpineError); }
                            }, 0);
                        }
                    } catch (parseError) {
                        console.error('[WORKOUT] Error parsing or inserting HTML:', parseError);
                        setTimeout(() => window.location.reload(), 1000);
                    }
                } else {
                    console.error('[WORKOUT] Workouts container not found in DOM');
                    setTimeout(() => window.location.reload(), 1000);
                }
            } catch (error) {
                console.error('[WORKOUT] Error refreshing workouts list:', error);
                setTimeout(() => window.location.reload(), 1000);
            }
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
        async confirmDelete() {
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
            
            // Get the anti-forgery token (validate early even though Api helper also includes it)
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenElement) {
                console.error('[WORKOUT] Anti-forgery token not found in DOM');
                this.showToast('error', 'System error: Security token not found');
                return;
            }
            
            console.log('[WORKOUT] Sending AJAX request to delete workout');
            try {
                const response = await Api.postUrlEncoded(`/User/Workout/DeleteWorkout/${workoutIdToDelete}`, {}, { redirect: 'manual' });
                console.log(`[WORKOUT] Delete response received: ${response.status}`);
                const html = await response.text(); // Allow handling JSON or HTML partials
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
                                        workoutsContainer.innerHTML = `
                                            <div class="text-center py-8">
                                                <p class="text-gray-500">No workouts found. Create your first workout!</p>
                                            </div>
                                        `;
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
            } catch (error) {
                console.error('[WORKOUT] Error deleting workout:', error);
                this.showToast('error', 'Error deleting workout');
            }
        }
    };
};


/**
 * Open the edit modal for an exercise by index.
 * Reads and writes to the Alpine component 'editWorkoutPage'.
 * @param {number} idx Zero-based index of the exercise
 */
// Removed: editExerciseFixed (now handled within editWorkoutPage Alpine component)