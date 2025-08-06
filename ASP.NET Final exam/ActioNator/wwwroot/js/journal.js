/**
 * Journal Entry Management
 * Handles all journal entry operations including CRUD, validation, and UI interactions
 * Adapted for ASP.NET Core MVC with AJAX support
 */

// Helper function to dispatch journal modal events
function openJournalModal() {
    console.log('Journal: Dispatching open-journal-modal event');
    // Dispatch both as document and window events to ensure it's captured
    document.dispatchEvent(new CustomEvent('open-journal-modal'));
    window.dispatchEvent(new CustomEvent('open-journal-modal'));
}

// Helper function to close journal modal
function closeJournalModal() {
    console.log('Journal: Dispatching close-journal-modal event');
    // Dispatch both as document and window events to ensure it's captured
    document.dispatchEvent(new CustomEvent('close-journal-modal'));
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
            
            // Dispatch journal-specific event to open the modal
            openJournalModal();
        },
        
        // Open modal to edit existing journal entry
        editEntry(entry) {
            console.log('Journal: Opening edit journal entry modal for entry:', entry.id);
            
            // Fetch the latest entry data from the server to ensure we have the most up-to-date content
            fetch(`/User/Journal/Edit?id=${entry.id}`, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(response => {
                if (response.ok) {
                    return response.json().catch(() => response.text());
                } else {
                    console.error('Failed to fetch entry for editing');
                    showToast('error', 'Failed to load entry for editing');
                    throw new Error('Failed to fetch entry');
                }
            })
            .then(data => {
                // If we got JSON data, use it directly
                if (typeof data === 'object' && data !== null) {
                    this.setupEditMode(data);
                } else {
                    // Otherwise, try to find the entry in our existing entries
                    const existingEntry = this.entries.find(e => e.id === entry.id);
                    if (existingEntry) {
                        this.setupEditMode(existingEntry);
                    } else {
                        showToast('error', 'Could not load entry data');
                        return;
                    }
                }
                
                // Open the modal after data is set up
                openJournalModal();
                
                // Dispatch an event to notify the modal that we're in edit mode
                document.dispatchEvent(new CustomEvent('journal-edit-mode', { 
                    detail: { isEditMode: true }
                }));
            })
            .catch(error => {
                console.error('Error fetching entry for editing:', error);
                showToast('error', 'An error occurred while loading the entry');
            });
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
        
        // Validate the form before submission
        validateForm() {
            this.validationAttempted = true;
            
            // Check title length
            if (!this.currentEntry.title || 
                this.currentEntry.title.length < 3 || 
                this.currentEntry.title.length > 20) {
                return false;
            }
            
            // Check content length
            if (this.currentEntry.content && this.currentEntry.content.length > 150) {
                return false;
            }
            
            // Check mood tag length
            if (this.currentEntry.mood && this.currentEntry.mood.length > 50) {
                return false;
            }
            
            return true;
        },
        
        // Fetch entries from the server
        async fetchEntries() {
            this.isLoading = true;
            try {
                const response = await fetch('/User/Journal/Index' + (this.searchQuery ? `?searchTerm=${encodeURIComponent(this.searchQuery)}` : ''));
                
                if (response.ok) {
                    // Check if we have server-side data already available
                    if (window.initialJournalEntries && Array.isArray(window.initialJournalEntries) && !this.searchQuery) {
                        console.log('Using server-provided entries:', window.initialJournalEntries.length);
                        this.entries = window.initialJournalEntries;
                        this.filteredEntries = [...this.entries];
                    }
                    // For AJAX requests, we'd get JSON back
                    else if (response.headers.get('content-type')?.includes('application/json')) {
                        const data = await response.json();
                        this.entries = data;
                        this.filteredEntries = [...this.entries]; // Update filtered entries
                    }
                    // Log the number of entries for debugging
                    console.log(`Journal: Loaded ${this.entries.length} entries`);
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
            
            // Get the CSRF token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            
            // Prepare the data
            const entryData = {
                id: this.isEditMode ? this.currentEntry.id : null,
                title: this.currentEntry.title,
                content: this.currentEntry.content,
                moodTag: this.currentEntry.mood,
                __RequestVerificationToken: token
            };
            
            // Determine the endpoint based on mode
            const endpoint = this.isEditMode ? '/User/Journal/Update' : '/User/Journal/Save';
            
            console.log(`Submitting to ${endpoint} with data:`, entryData);
            
            // Send the data to the server
            fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(entryData)
            })
            .then(response => {
                if (response.ok) {
                    return response.text();
                } else {
                    throw new Error('Failed to save journal entry');
                }
            })
            .then(responseText => {
                try {
                    // Try to parse as JSON
                    const data = JSON.parse(responseText);
                    
                    if (data.success) {
                        // Show success message
                        const actionText = this.isEditMode ? 'updated' : 'saved';
                        showToast('success', data.message || `Journal entry ${actionText} successfully!`);
                        
                        // Close the modal
                        document.dispatchEvent(new CustomEvent('close-journal-modal'));
                        
                        // If we have HTML for the new entry, update the entries list
                        if (data.entryHtml) {
                            if (this.isEditMode) {
                                // Find and update the existing entry
                                const index = this.entries.findIndex(e => e.id === this.currentEntry.id);
                                if (index !== -1) {
                                    // Create a temporary container to parse the HTML
                                    const tempContainer = document.createElement('div');
                                    tempContainer.innerHTML = data.entryHtml;
                                    
                                    // Extract entry data from the HTML
                                    const entryElement = tempContainer.firstElementChild;
                                    const entryId = entryElement.getAttribute('data-entry-id');
                                    const entryTitle = entryElement.querySelector('.entry-title').textContent;
                                    const entryContent = entryElement.querySelector('.entry-content').textContent;
                                    const entryDate = entryElement.getAttribute('data-created-at');
                                    const entryMood = entryElement.getAttribute('data-mood-tag') || '';
                                    
                                    // Update the entry in the array
                                    this.entries[index] = {
                                        id: entryId,
                                        title: entryTitle,
                                        content: entryContent,
                                        createdAt: entryDate,
                                        moodTag: entryMood
                                    };
                                    
                                    // Update filtered entries
                                    this.updateFilteredEntries();
                                }
                            } else {
                                // Create a temporary container to parse the HTML
                                const tempContainer = document.createElement('div');
                                tempContainer.innerHTML = data.entryHtml;
                                
                                // Extract entry data from the HTML
                                const entryElement = tempContainer.firstElementChild;
                                const entryId = entryElement.getAttribute('data-entry-id');
                                const entryTitle = entryElement.querySelector('.entry-title').textContent;
                                const entryContent = entryElement.querySelector('.entry-content').textContent;
                                const entryDate = entryElement.getAttribute('data-created-at');
                                const entryMood = entryElement.getAttribute('data-mood-tag') || '';
                                
                                // Add the new entry to the array
                                this.entries.unshift({
                                    id: entryId,
                                    title: entryTitle,
                                    content: entryContent,
                                    createdAt: entryDate,
                                    moodTag: entryMood
                                });
                                
                                // Update filtered entries
                                this.updateFilteredEntries();
                            }
                        } else {
                            // If no HTML was returned, refresh entries from server
                            this.fetchEntries();
                        }
                        
                        // Reset the form
                        this.resetForm();
                    } else {
                        // Show error message
                        showToast('error', data.error || 'Failed to save journal entry');
                    }
                } catch (e) {
                    // Not JSON, might be HTML with validation errors
                    console.error('Error parsing response:', e);
                    showToast('error', 'Failed to save journal entry');
                }
            })
            .catch(error => {
                console.error('Error saving entry:', error);
                showToast('error', 'An error occurred while saving the entry');
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
                    'X-Requested-With': 'XMLHttpRequest'
                },
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

/**
 * Toast Notification Component
 * Displays success, error, and info messages
 */
document.addEventListener('alpine:init', () => {
    Alpine.data('toastNotification', () => ({
        notifications: [],
        
        init() {
            window.addEventListener('toast', (e) => {
                this.addNotification(e.detail);
            });
        },
        
        addNotification({ message, type = 'info', duration = 3000 }) {
            const id = Date.now();
            this.notifications.push({ id, message, type });
            
            setTimeout(() => {
                this.removeNotification(id);
            }, duration);
        },
        
        removeNotification(id) {
            this.notifications = this.notifications.filter(n => n.id !== id);
        }
    }));
});
