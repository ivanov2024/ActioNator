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
            console.log('Journal: Opening edit journal entry modal');
            // First set up the entry data
            this.isEditMode = true;
            this.currentEntry = {
                id: entry.id,
                title: entry.title,
                content: entry.content,
                mood: entry.moodTag || '',
                createdAt: entry.createdAt
            };
            this.validationAttempted = false;
            
            // Dispatch journal-specific event to open the modal
            openJournalModal();
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
                    // For a full page reload, we'd get HTML back, not JSON
                    // This is just a fallback in case we implement AJAX search later
                    if (response.headers.get('content-type')?.includes('application/json')) {
                        const data = await response.json();
                        this.entries = data;
                        this.filteredEntries = [...this.entries]; // Update filtered entries
                    }
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
            if (!this.validateForm()) {
                return;
            }

            this.isLoading = true;
            
            // Add anti-forgery token to the request
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            
            // Map the currentEntry to the format expected by the server
            const entryData = {
                id: this.currentEntry.id || '00000000-0000-0000-0000-000000000000', // Empty GUID if new entry
                title: this.currentEntry.title,
                content: this.currentEntry.content,
                moodTag: this.currentEntry.mood, // Map mood to moodTag
                createdAt: this.currentEntry.createdAt,
                __RequestVerificationToken: token
            };

            fetch('/User/Journal/Save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(entryData)
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    console.log('Journal: Save successful, closing journal modal');
                    // Close the journal modal using our helper function
                    closeJournalModal();
                    
                    // Use the shared toast component
                    showToast('success', data.message || 'Journal entry saved successfully!');
                    // Refresh entries from server
                    this.fetchEntries();
                } else {
                    const errorMessage = data.errors ? Object.values(data.errors).flat().join(', ') : 'Failed to save entry';
                    showToast('error', errorMessage);
                }
            })
            .catch(error => {
                console.error('Error saving entry:', error);
                showToast('error', 'An error occurred while saving the entry');
            })
            .finally(() => {
                this.isLoading = false;
            });
        },
        
        // Open delete confirmation modal using shared modal
        openDeleteModal(id) {
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

            // Add anti-forgery token to the request
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            
            fetch('/User/Journal/Delete', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    id: this.entryToDelete,
                    __RequestVerificationToken: token
                })
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Use the shared toast component
                    showToast('success', data.message);
                    // Refresh entries from server
                    this.fetchEntries();
                } else {
                    showToast('error', data.error || 'Failed to delete entry');
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
