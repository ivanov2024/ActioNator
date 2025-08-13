/**
 * Journal Entry Management
 * Handles all journal entry operations including CRUD, validation, and UI interactions
 * Adapted for ASP.NET Core MVC with AJAX support
 */

// Helper function to dispatch journal modal events
function openJournalModal() {
    console.log('Journal: Dispatching open-journal-modal event');
    // Dispatch once on window to avoid duplicate handlers
    window.dispatchEvent(new CustomEvent('open-journal-modal'));
}

// Helper function to close journal modal
function closeJournalModal() {
    console.log('Journal: Dispatching close-journal-modal event');
    // Dispatch once on window to avoid duplicate handlers
    window.dispatchEvent(new CustomEvent('close-journal-modal'));
}

// Log when the script loads
console.log('Journal script loaded');

document.addEventListener('alpine:init', () => {
    console.log('Alpine.js initialized');
    
    Alpine.data('journalApp', () => ({
        // Data properties
        entries: [],
        filteredEntries: [],
        searchQuery: '',
        isLoading: true,
        isEditMode: false,
        activeMenu: null,
        validationAttempted: false,
        currentEntry: {
            id: '',
            title: '',
            content: '',
            mood: '',
            createdAt: new Date().toISOString()
        },
        entryToDelete: null,
        
        // Initialize the component
        init() {
            // Initialize validation flag
            this.validationAttempted = false;
            
            // Use server-provided data if available, otherwise use empty array
            if (window.initialJournalEntries && Array.isArray(window.initialJournalEntries)) {
                this.entries = window.initialJournalEntries;
                this.filteredEntries = [...this.entries]; // Initialize filtered entries
                // Ensure read-more buttons are initialized after initial render
                this.$nextTick(() => document.dispatchEvent(new Event('goals-refreshed')));
            } else {
                // Fetch entries from the server
                this.fetchEntries();
            }
            
            // Set up event listeners
            this.$el.addEventListener('edit-entry-requested', (e) => {
                this.editEntry(e.detail.entry);
            });
        },
        
        // Update filtered entries when search query changes
        updateFilteredEntries() {
            if (!this.searchQuery || this.searchQuery.trim() === '') {
                this.filteredEntries = [...this.entries];
            } else {
                const query = this.searchQuery.toLowerCase();
                this.filteredEntries = this.entries.filter(entry => 
                    entry.title.toLowerCase().includes(query) || 
                    entry.content.toLowerCase().includes(query) || 
                    (entry.moodTag && entry.moodTag.toLowerCase().includes(query))
                );
            }
            // Re-init read-more after list changes
            this.$nextTick(() => document.dispatchEvent(new Event('goals-refreshed')));
        },
        
        // Open modal to add new journal entry
        openAddModal() {
            console.log('Journal: Opening add journal entry modal');
            // First reset the entry data
            this.isEditMode = false;
            this.currentEntry = {
                id: '',
                title: '',
                content: '',
                mood: '',
                createdAt: new Date().toISOString()
            };
            this.validationAttempted = false;
            
            // Inform the modal of add mode and current entry before opening
            const addDetail = { isEditMode: false, entry: this.currentEntry };
            // Persist for modal open handler fallback
            window.__journalModalState = addDetail;
            // Emit a single window-scoped event to avoid duplicates
            window.dispatchEvent(new CustomEvent('journal-edit-mode', { detail: addDetail }));

            // Open the modal
            openJournalModal();
        },
        
        // Open modal to edit existing journal entry
        editEntry(entry) {
            console.log('Journal: Opening edit journal entry modal for entry:', entry.id);
            
            // Use the already loaded entry data to set up edit mode
            const existingEntry = this.entries.find(e => e.id === entry.id) || entry;
            if (!existingEntry) {
                console.error('Entry not found for editing:', entry.id);
                showToast && showToast('error', 'Could not load entry data');
                return;
            }

            this.setupEditMode(existingEntry);

            // Notify the modal that we're in edit mode and provide the entry, before opening
            const editDetail = { isEditMode: true, entry: this.currentEntry };
            // Persist for modal open handler fallback
            window.__journalModalState = editDetail;
            // Emit a single window-scoped event to avoid duplicates
            window.dispatchEvent(new CustomEvent('journal-edit-mode', { detail: editDetail }));

            // Open the modal after data is set up
            openJournalModal();
        },
        
        // Helper function to set up edit mode with entry data
        setupEditMode(entry) {
            console.log('Setting up edit mode with entry:', entry);
            // Set edit mode flag
            this.isEditMode = true;
            
            // Populate current entry with data
            this.currentEntry = {
                id: entry.id,
                title: entry.title,
                content: entry.content,
                mood: entry.moodTag || '',
                createdAt: entry.createdAt
            };
            
            // Reset validation
            this.validationAttempted = false;
        },
        
        // Validate the form before submission (client-side mirror of DataAnnotations)
        validateForm() {
            this.validationAttempted = true;

            // Title: required, 3-20
            if (!this.currentEntry.title ||
                this.currentEntry.title.length < 3 ||
                this.currentEntry.title.length > 20) {
                return false;
            }

            // Content: required, 1-150
            if (!this.currentEntry.content || this.currentEntry.content.length < 1 || this.currentEntry.content.length > 150) {
                return false;
            }

            // MoodTag: required, 1-50
            if (!this.currentEntry.mood || this.currentEntry.mood.length < 1 || this.currentEntry.mood.length > 50) {
                return false;
            }

            return true;
        },
        
        // Fetch entries from the server (JSON endpoint)
        async fetchEntries() {
            this.isLoading = true;
            try {
                // Use JSON List endpoint; fall back to preloaded entries only when not searching
                const url = '/User/Journal/List' + (this.searchQuery ? `?searchTerm=${encodeURIComponent(this.searchQuery)}` : '');
                const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });

                if (response.ok) {
                    const data = await response.json();
                    this.entries = Array.isArray(data) ? data : [];
                    this.filteredEntries = [...this.entries];
                    console.log(`Journal: Loaded ${this.entries.length} entries`);
                    // Re-init shared Read More buttonse
                    this.$nextTick(() => document.dispatchEvent(new Event('goals-refreshed')));
                } else {
                    console.error('Failed to fetch entries');
                }
            } catch (error) {
                console.error('Error fetching entries:', error);
            } finally {
                this.isLoading = false;
            }
        },
        
        // Format date for display
        formatDate(dateString) {
            const date = new Date(dateString);
            return new Intl.DateTimeFormat('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: 'numeric',
                minute: 'numeric'
            }).format(date);
        },
        // Save entry
        saveEntry() {
            console.log('Journal: Saving entry in mode:', this.isEditMode ? 'EDIT' : 'CREATE');
            
            const form = document.getElementById('journalEntryForm');
            if (window.jQuery && form) {
                const $form = jQuery(form);
                if (jQuery.validator && jQuery.validator.unobtrusive) {
                    jQuery.validator.unobtrusive.parse($form);
                }
                if ($form.valid && !$form.valid()) {
                    // Let jQuery validate render messages
                    this.validationAttempted = true;
                    showToast && showToast('error', 'Please fix validation errors and try again.');
                    return;
                }
            } else {
                // Fallback to lightweight Alpine checks
                if (!this.validateForm()) {
                    showToast && showToast('error', 'Please fix validation errors and try again.');
                    return;
                }
            }

            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const entryData = {
                // Use Guid.Empty for create, actual Id for edit to match backend expectations
                Id: this.isEditMode ? this.currentEntry.id : '00000000-0000-0000-0000-000000000000',
                Title: this.currentEntry.title,
                Content: this.currentEntry.content,
                MoodTag: this.currentEntry.mood,
                __RequestVerificationToken: token
            };
            console.log('Submitting to /User/Journal/Save with data: ', entryData);
            fetch('/User/Journal/Save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'X-CSRF-TOKEN': token
                },
                credentials: 'same-origin',
                body: JSON.stringify(entryData)
            })
                .then(async (response) => {
                    if (!response.ok) {
                        const contentType = response.headers.get('content-type');
                        if (contentType && contentType.includes('application/json')) {
                            const data = await response.json();
                            if (Array.isArray(data.errors) && data.errors.length) {
                                throw new Error(data.errors.join('\n'));
                            }
                            throw new Error(data.error || 'Failed to save journal entry');
                        }
                        throw new Error('Failed to save journal entry');
                    }
                    return response.json();
                })
                .then((data) => {
                    const actionText = this.isEditMode ? 'updated' : 'saved';
                    showToast('success', data.message || `Journal entry ${actionText} successfully!`);
                    // Close modal via single window-scoped event
                    closeJournalModal();
                    this.resetForm();
                    this.fetchEntries();
                })
                .catch((error) => {
                    console.error('Error saving entry:', error);
                    showToast('error', error.message || 'An error occurred while saving the entry');
                });
        },
        
        // Open delete confirmation modal using shared modal
        openDeleteModal(id) {
            console.log('Opening delete modal for entry ID:', id);
            
            // Validate the ID before proceeding
            if (!id || id === '00000000-0000-0000-0000-000000000000') {
                console.error('Invalid journal entry ID:', id);
                showToast('error', 'Invalid journal entry ID');
                return;
            }
            
            // Store the ID to delete
            this.entryToDelete = id;
            
            // Use the shared modal component
            window.openModal({
                type: 'delete',
                title: 'Delete Journal Entry',
                message: 'Are you sure you want to delete this journal entry? This action cannot be undone.',
                confirmText: 'Delete',
                cancelText: 'Cancel'
            });
            
            // Listen for modal confirmation
            window.addEventListener('modal-confirmed', (event) => {
                if (event.detail && event.detail.type === 'delete') {
                    this.confirmDelete();
                }
            }, { once: true });
        },
        
        // Delete the current entry
        confirmDelete() {
            this.isLoading = true;
            
            // Double-check ID validation before proceeding
            if (!this.entryToDelete || this.entryToDelete === '00000000-0000-0000-0000-000000000000') {
                console.error('Invalid journal entry ID:', this.entryToDelete);
                showToast('error', 'Invalid journal entry ID');
                this.isLoading = false;
                this.entryToDelete = null;
                return;
            }
            
            console.log('Deleting entry with ID:', this.entryToDelete);

            // Add anti-forgery token to the request
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            
            // Format the request body to match the DeleteEntryRequest class on the backend
            fetch('/User/Journal/Delete', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'X-CSRF-TOKEN': token
                },
                credentials: 'same-origin',
                body: JSON.stringify({
                    Id: this.entryToDelete, // Use capital 'I' to match C# property name
                    __RequestVerificationToken: token
                })
            })
            .then(response => {
                // Check if the response is JSON before parsing
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    return response.json().then(data => ({ isJson: true, data, status: response.status }));
                } else {
                    return response.text().then(text => ({ isJson: false, text, status: response.status }));
                }
            })
            .then(result => {
                // Handle both JSON and non-JSON responses
                if (result.isJson) {
                    const data = result.data;
                    if (data.success) {
                        // Use the shared toast component
                        showToast('success', data.message || 'Journal entry deleted successfully!');
                        // Refresh entries from server
                        this.fetchEntries();
                    } else {
                        showToast('error', data.error || 'Failed to delete entry');
                    }
                } else {
                    // Handle non-JSON response (likely HTML or error)
                    console.log('Received non-JSON response during delete operation');
                    if (result.status >= 200 && result.status < 300) {
                        showToast('success', 'Journal entry deleted successfully!');
                        this.fetchEntries();
                    } else {
                        showToast('error', 'Failed to delete entry');
                    }
                }
            })
            .catch(error => {
                console.error('Error deleting entry:', error);
                showToast('error', 'An error occurred while deleting the entry');
            })
            .finally(() => {
                this.isLoading = false;
                this.entryToDelete = null;
            });
        },
        
        // Toggle the dropdown menu for an entry
        toggleMenu(index) {
            this.activeMenu = this.activeMenu === index ? null : index;
        },
        
        // Close the dropdown menu
        closeMenu() {
            this.activeMenu = null;
        }
    }));
});
