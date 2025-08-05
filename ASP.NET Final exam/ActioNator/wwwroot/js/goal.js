// Global variables
let activeDropdown = null;
let activeModal = null;
let antiForgeryToken = null;
let deleteGoalId = null; // Store the ID of the goal to delete

// Global error handler
window.addEventListener('error', function(e) {
    console.error('Global error caught:', e.error);
});

// Close modal when clicking outside
function setupModalCloseOnOutsideClick() {
    document.addEventListener('click', function(event) {
        const modals = document.querySelectorAll('[id$="-modal"]:not(.hidden)');
        modals.forEach(modal => {
            if (event.target === modal) {
                closeModal(modal.id);
            }
        });
    });
}

// Initialize the page
document.addEventListener('DOMContentLoaded', function () {
    setupModalCloseOnOutsideClick();
    console.log('DOM fully loaded - Initialization starting');
    
    // Add event listeners for modal close buttons
    document.querySelectorAll('.modal-close-btn, [data-modal-close]').forEach(button => {
        button.addEventListener('click', function() {
            const modal = this.closest('.modal');
            if (modal) {
                closeModal(modal.id);
            }
        });
    });
    
    try {
        // Get anti-forgery token
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        console.log('Token element found:', tokenElement);
        if (tokenElement) {
            antiForgeryToken = tokenElement.value;
        } else {
            console.error('Anti-forgery token not found');
        }
        
        // Initialize filter buttons
        console.log('Initializing filter buttons');
        initializeFilters();

        // Initialize modals
        console.log('Initializing modals');
        // Using the shared modal partial, no need for separate initialization
        // Removing reference to undefined function

        // Initialize dropdowns
        console.log('Starting dropdown initialization');
        const dropdownTriggers = document.querySelectorAll('.goal-menu-btn.dropdown-trigger');
        console.log(`Found ${dropdownTriggers.length} dropdown triggers:`, dropdownTriggers);
        
        // Check for overlapping elements
        console.log('Checking for potential overlapping elements...');
        dropdownTriggers.forEach((trigger, i) => {
            const rect = trigger.getBoundingClientRect();
            console.log(`Trigger ${i} position:`, rect);
            console.log(`Trigger ${i} computed style:`, window.getComputedStyle(trigger));
            
            // Check if any element is overlapping this trigger
            const elementsAtPoint = document.elementsFromPoint(rect.left + rect.width/2, rect.top + rect.height/2);
            console.log(`Elements at trigger ${i} center point:`, elementsAtPoint);
            
            // Check if trigger is actually clickable
            console.log(`Trigger ${i} pointer-events:`, window.getComputedStyle(trigger).pointerEvents);
            console.log(`Trigger ${i} z-index:`, window.getComputedStyle(trigger).zIndex);
        });
        
        initializeDropdowns();

        // Set minimum due date to tomorrow for all date inputs
        console.log('Setting minimum due dates');
        setMinDueDates();
        
        // Load goals on page load
        console.log('Loading goals');
        loadGoals();

        // Initialize goal cards
        console.log('Initializing goal cards');
        initializeGoalCards();
        
        // Add direct click handlers as a fallback
        console.log('Adding direct click handlers as fallback');
        setTimeout(addDirectClickHandlers, 1000);
        
    } catch (error) {
        console.error('Error during initialization:', error);
    }
    
    // Direct event listener for the New Goal button
    const newGoalBtn = document.getElementById('new-goal-btn');
    console.log('Direct check for New Goal button:', newGoalBtn);
    if (newGoalBtn) {
        newGoalBtn.onclick = function() {
            console.log('New Goal button clicked directly');
            const modal = document.getElementById('goal-modal');
            if (modal) {
                console.log('Found goal modal, showing it');
                modal.style.display = 'flex';
                modal.classList.remove('hidden');
                modal.setAttribute('aria-hidden', 'false');
            } else {
                console.error('Goal modal not found');
            }
            return false; // Prevent default action
        };
    }
});

// Set minimum due date to tomorrow for all date inputs
function setMinDueDates() {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    const minDate = tomorrow.toISOString().split('T')[0];

    document.querySelectorAll('input[type="date"]').forEach(dateInput => {
        dateInput.min = minDate;
    });
}

// Initialize filter buttons
function initializeFilters() {
    const filterButtons = document.querySelectorAll('.filter-btn');

    filterButtons.forEach(button => {
        button.addEventListener('click', function () {
            // Remove active class from all buttons
            filterButtons.forEach(btn => btn.classList.remove('active'));

            // Add active class to clicked button
            this.classList.add('active');

            // Get filter value
            const filter = this.dataset.filter;

            // Load goals with the selected filter
            loadGoals(filter);
        });
    });
}

// Load goals from the server
function loadGoals(filter = 'all') {
    // Hide goals container while loading
    const goalsContainer = document.getElementById('goals-container');
    const emptyState = document.getElementById('empty-state');
    
    if (goalsContainer) goalsContainer.classList.add('hidden');
    if (emptyState) emptyState.classList.add('hidden');

    // Fetch goals from the server
    fetch(`/User/Goal/GetGoalPartial?filter=${filter}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(html => {
            // Update the goals container with the partial view HTML
            const goalsContainer = document.getElementById('goals-container');
            goalsContainer.innerHTML = html;
            goalsContainer.classList.remove('hidden');

            // Check if there are any goals
            const hasGoals = goalsContainer.querySelector('.goal-card') !== null;
            if (!hasGoals) {
                // Show empty state
                document.getElementById('empty-state').classList.remove('hidden');
                goalsContainer.classList.add('hidden');
            }

            // Initialize goal cards
            initializeGoalCards();
            
            // Initialize read more buttons
            if (typeof initializeReadMoreButtons === 'function') {
                console.log('Initializing read more buttons after loading goals');
                initializeReadMoreButtons();
            }
        })
        .catch(error => {
            console.error('Error loading goals:', error);
            showToast('Failed to load goals. Please try again.', 'error');

            // Hide loading skeleton
            document.getElementById('loading-skeleton').classList.add('hidden');
        });
}

// Create skeleton cards for loading state
function createSkeletonCards() {
    const skeletonContainer = document.getElementById('loading-skeleton');
    skeletonContainer.innerHTML = '';

    for (let i = 0; i < 4; i++) {
        const skeletonCard = document.createElement('div');
        skeletonCard.className = 'goal-card skeleton';
        skeletonCard.innerHTML = `
            <div class="goal-card-inner">
                <div class="goal-card-header">
                    <div class="skeleton-line w-3/4 h-5"></div>
                    <div class="skeleton-circle"></div>
                </div>
                <div class="goal-card-body">
                    <div class="skeleton-line w-full h-4 mb-2"></div>
                    <div class="skeleton-line w-2/3 h-4"></div>
                </div>
                <div class="goal-card-footer">
                    <div class="skeleton-line w-1/3 h-4"></div>
                    <div class="skeleton-badge"></div>
                </div>
            </div>
        `;
        skeletonContainer.appendChild(skeletonCard);
    }
}

// Initialize goal cards
function initializeGoalCards() {
    console.log('Initializing goal cards');
    
    // Initialize dropdowns
    initializeDropdowns();
    
    // Add direct click handlers as a fallback
    addDirectClickHandlers();
    
    // Force proper initial state for all dropdowns
    document.querySelectorAll('.dropdown-menu').forEach(dropdown => {
        console.log('Setting initial state for dropdown:', dropdown);
        dropdown.style.opacity = '0';
        dropdown.style.visibility = 'hidden';
        dropdown.style.transform = 'translateY(-10px)';
        dropdown.style.display = 'block';
        dropdown.style.pointerEvents = 'none';
    });

    // Initialize completed toggles
    initializeCompletedToggles();

    // Edit goal buttons
    document.querySelectorAll('.edit-goal-btn').forEach(button => {
        button.addEventListener('click', async function () {
            const goalId = this.dataset.id;
            const goalCard = this.closest('.goal-card');
            
            // First try to get data from the card directly (faster)
            if (goalCard) {
                const goal = {
                    id: goalId,
                    title: goalCard.querySelector('.goal-title')?.textContent.trim() || '',
                    description: goalCard.querySelector('.goal-description')?.textContent.trim() || '',
                    dueDate: goalCard.querySelector('[data-due-date]')?.getAttribute('data-due-date') || new Date().toISOString().split('T')[0],
                    priority: goalCard.getAttribute('data-priority') || 'medium',
                    isCompleted: goalCard.getAttribute('data-status') === 'completed'
                };
                openEditModal(goal);
                return;
            }
            
            // If we don't have the data in the DOM, fetch all goals and find the right one
            try {
                const response = await fetch('/User/Goal/GetGoals', {
                    headers: {
                        'Accept': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    }
                });
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                
                const goals = await response.json();
                const goal = Array.isArray(goals) ? goals.find(g => g.id === goalId) : null;
                
                if (goal) {
                    openEditModal(goal);
                } else {
                    throw new Error('Goal not found');
                }
            } catch (error) {
                console.error('Error fetching goals:', error);
                showToast('Failed to load goal details. Please try again.', 'error');
            }
        });
    });

    // Delete goal buttons
    document.querySelectorAll('.delete-goal-btn').forEach(button => {
        button.addEventListener('click', function () {
            const goalId = this.dataset.id;
            if (typeof openDeleteModal === 'function') {
                openDeleteModal(goalId);
            } else {
                console.error('openDeleteModal function not found');
            }
        });
    });

    // Empty state CTA button
    const emptyStateCta = document.getElementById('empty-state-cta');
    if (emptyStateCta) {
        emptyStateCta.addEventListener('click', function () {
            openModal('goal-modal');
        });
    }

    // Close modal buttons
    document.querySelectorAll('.modal-close-btn, .modal-cancel-btn').forEach(button => {
        button.addEventListener('click', function () {
            const modal = this.closest('.modal');
            if (modal) {
                closeModal(modal.id);
            }
        });
    });

    // Close modal when clicking outside content
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                const modal = overlay.closest('.modal');
                if (modal) {
                    closeModal(modal.id);
                }
            }
        });
    });

    // Additional handler for clicking on the modal background
    document.addEventListener('click', (e) => {
        if (e.target.classList.contains('modal-overlay')) {
            const modal = e.target.closest('.modal');
            if (modal) {
                closeModal(modal.id);
            }
        }
    });

    // Add keyboard navigation for modals (Escape key)
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && activeModal) {
            closeModal(activeModal);
        }
    });

    // Initialize goal cards functionality
    function initGoalCards() {
        console.log('Initializing goal cards functionality');
        
        // Add event listeners to goal cards
        document.querySelectorAll('.goal-card').forEach(card => {
            // Add click event listener to read more/less button
            const readMoreBtn = card.querySelector('.read-more-btn');
            if (readMoreBtn) {
                console.log('Found Read More button, attaching event listener');
                // Remove any existing event listeners by cloning
                const newReadMoreBtn = readMoreBtn.cloneNode(true);
                readMoreBtn.parentNode.replaceChild(newReadMoreBtn, readMoreBtn);
                
                // Add new event listener
                newReadMoreBtn.addEventListener('click', handleReadMoreClick);
            } else {
                console.warn('No Read More button found in card');
            }
            
            card.addEventListener('click', handleCardClick);
            
            // Add dropdown menu functionality
            const menuBtn = card.querySelector('.goal-menu-btn');
            if (menuBtn) {
                menuBtn.addEventListener('click', toggleDropdownMenu);
            }
            
            // Add edit button functionality
            const editBtn = card.querySelector('.edit-goal-btn');
            if (editBtn) {
                editBtn.addEventListener('click', handleEditButtonClick);
            }
            
            // Add delete button functionality
            const deleteBtn = card.querySelector('.delete-goal-btn');
            if (deleteBtn) {
                deleteBtn.addEventListener('click', handleDeleteButtonClick);
            }
            
            // Add click functionality for expand/collapse
            card.addEventListener('click', handleCardClick);
        });
        
        // Close dropdowns when clicking outside
        document.addEventListener('click', closeAllDropdowns);
    }

    // Handle card click for expand/collapse functionality
    function handleCardClick(event) {
        // Don't expand if clicking on menu buttons, dropdown items, or read more button
        if (event.target.closest('.goal-menu-btn') || 
            event.target.closest('.dropdown-menu') || 
            event.target.closest('.edit-goal-btn') || 
            event.target.closest('.delete-goal-btn') ||
            event.target.closest('.read-more-btn')) {
            return;
        }
        
        const card = event.currentTarget;
        toggleCardDescription(card);
    }
    
    // Handle read more/less button click
    function handleReadMoreClick(event) {
        event.preventDefault();
        event.stopPropagation();
        
        console.log('Read More/Less button clicked');
        
        const button = event.currentTarget;
        const card = button.closest('.goal-card');
        
        if (!card) {
            console.error('No parent goal card found for Read More button');
            return;
        }
        
        toggleCardDescription(card);
    }
    
    // Toggle card description expand/collapse
    function toggleCardDescription(card) {
        const description = card.querySelector('.goal-description');
        const readMoreBtn = card.querySelector('.read-more-btn');
        const readMoreText = readMoreBtn ? readMoreBtn.querySelector('.read-more') : null;
        const readLessText = readMoreBtn ? readMoreBtn.querySelector('.read-less') : null;
        
        if (!description) return;
        
        // Toggle expanded class
        card.classList.toggle('expanded');
        
        if (card.classList.contains('expanded')) {
            // Expand the description
            const descriptionHeight = description.scrollHeight;
            description.style.maxHeight = `${descriptionHeight}px`;
            description.style.overflow = 'visible';
            description.style.whiteSpace = 'normal';
            description.style.textOverflow = 'clip';
            
            // Update button text
            if (readMoreText && readLessText) {
                readMoreText.classList.add('hidden');
                readLessText.classList.remove('hidden');
            }
        } else {
            // Collapse the description
            description.style.maxHeight = '1.5em';
            description.style.overflow = 'hidden';
            description.style.whiteSpace = 'nowrap';
            description.style.textOverflow = 'ellipsis';
            
            // Update button text
            if (readMoreText && readLessText) {
                readMoreText.classList.remove('hidden');
                readLessText.classList.add('hidden');
            }
        }
    }

    // Form submissions
    const goalForm = document.getElementById('goal-form');
    if (goalForm) {
        goalForm.addEventListener('submit', handleCreateSubmit);
    }

    const editGoalForm = document.getElementById('edit-goal-form');
    if (editGoalForm) {
        editGoalForm.addEventListener('submit', handleEditSubmit);
    }

    // Delete confirmation
    const confirmDeleteBtn = document.getElementById('confirm-delete-btn');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', handleDeleteConfirm);
    }

    // Initialize the completed toggle
    const completedToggle = document.getElementById('edit-goal-completed');
    if (completedToggle) {
        // Add change event listener - the CSS handles the visual changes now
        completedToggle.addEventListener('change', function (e) {
            // We can add any additional logic here if needed
            console.log('Toggle state changed to:', e.target.checked);
        });
    }
}

// Initialize dropdowns
function initializeDropdowns() {
    console.log('Initializing dropdowns - function start');
    
    // Clear any existing global event listeners
    document.removeEventListener('click', documentClickHandler);
    document.removeEventListener('keydown', documentKeydownHandler);
    
    // Re-add document click and keydown handlers
    document.addEventListener('click', documentClickHandler);
    document.addEventListener('keydown', documentKeydownHandler);
    
    // First, ensure all dropdowns are in the correct initial state
    document.querySelectorAll('.dropdown-menu').forEach(dropdown => {
        dropdown.style.opacity = '0';
        dropdown.style.visibility = 'hidden';
        dropdown.style.transform = 'translateY(-10px)';
        dropdown.style.display = 'block';
        dropdown.style.pointerEvents = 'none';
    });
    
    // Remove any existing event listeners by cloning and replacing all dropdown buttons
    document.querySelectorAll('.goal-menu-btn').forEach((button) => {
        const newButton = button.cloneNode(true);
        if (button.parentNode) {
            button.parentNode.replaceChild(newButton, button);
            
            // Add click event listener to the new button
            newButton.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                console.log('Dropdown button clicked');
                
                // Find the dropdown menu
                const container = this.closest('.dropdown-container');
                if (!container) {
                    console.error('No dropdown container found');
                    return;
                }
                
                const dropdown = container.querySelector('.dropdown-menu');
                if (!dropdown) {
                    console.error('No dropdown menu found');
                    return;
                }
                
                console.log('Found dropdown:', dropdown);
                
                // Check if this dropdown is already open
                const isVisible = dropdown.style.visibility === 'visible' || dropdown.style.opacity === '1';
                console.log('Is dropdown visible?', isVisible);
                
                // Close any open dropdown first
                if (activeDropdown && activeDropdown !== dropdown) {
                    console.log('Closing active dropdown first');
                    closeDropdown(activeDropdown);
                }
                
                // Toggle this dropdown
                if (isVisible) {
                    console.log('Closing this dropdown');
                    closeDropdown(dropdown);
                    this.setAttribute('aria-expanded', 'false');
                } else {
                    console.log('Opening this dropdown');
                    openDropdown(dropdown);
                    this.setAttribute('aria-expanded', 'true');
                }
            });
        }
    })

// Document click handler for closing dropdowns
function documentClickHandler(e) {
    // Add a small delay to prevent the dropdown from closing immediately after opening
    // This allows the click event that opens the dropdown to complete first
    setTimeout(() => {
        if (activeDropdown && !e.target.closest('.dropdown-container') && !e.target.closest('.goal-menu-btn')) {
            console.log('Document click outside dropdown - closing');
            const openButton = activeDropdown.previousElementSibling;
            closeDropdown(activeDropdown, openButton);
            activeDropdown = null;
        }
    }, 10);
}

// Document keydown handler for closing dropdowns with Escape key
function documentKeydownHandler(e) {
    if (e.key === 'Escape' && activeDropdown) {
        console.log('Escape key pressed - closing dropdown');
        const openButton = activeDropdown.previousElementSibling;
        closeDropdown(activeDropdown, openButton);
        activeDropdown = null;
    }
}

// Open dropdown
function openDropdown(dropdown, trigger) {
    console.log('Opening dropdown');
    
    // First close all other dropdowns
    document.querySelectorAll('.dropdown-menu:not(.hidden)').forEach(openDropdown => {
        if (openDropdown !== dropdown) {
            closeDropdown(openDropdown, openDropdown.previousElementSibling);
        }
    });
    
    // Set initial state for animation
    dropdown.style.opacity = '0';
    dropdown.style.transform = 'translateY(-10px)';
    dropdown.style.display = 'block';
    dropdown.style.visibility = 'visible';
    dropdown.style.pointerEvents = 'auto';
    dropdown.classList.remove('hidden');
        
    // Force repaint to ensure animation works
    void dropdown.offsetHeight;
        
    // Trigger animation
    setTimeout(() => {
        dropdown.style.opacity = '1';
        dropdown.style.transform = 'translateY(0)';
    }, 10);
        
    if (trigger) {
        trigger.setAttribute('aria-expanded', 'true');
    }
        
    activeDropdown = dropdown;
        
    // Focus the first item in the dropdown
    setTimeout(() => {
        const firstItem = dropdown.querySelector('.dropdown-item');
        if (firstItem) {
            firstItem.focus();
        }
    }, 100);
}

// Close dropdown
function closeDropdown(dropdown, trigger) {
    if (!dropdown) return;
    
    console.log('Closing dropdown with animation');
    
    // Animate closing
    dropdown.style.opacity = '0';
    dropdown.style.transform = 'translateY(-10px)';
    dropdown.style.pointerEvents = 'none';
    
    // After animation completes, hide completely
    setTimeout(() => {
        dropdown.style.visibility = 'hidden';
    }, 200);
    
    if (trigger) {
        trigger.setAttribute('aria-expanded', 'false');
    }
}

// Close active dropdown
function closeActiveDropdown() {
    if (activeDropdown) {
        const trigger = activeDropdown.previousElementSibling;
        closeDropdown(activeDropdown, trigger);
    }
}

// Globally defined fallback for goal dropdowns
// Ensures all .goal-menu-btn.dropdown-trigger elements have robust click handlers
// Idempotent: safe to call after dynamic DOM updates (e.g., after loading new goals)
function addDirectClickHandlers() {
    console.log('Adding direct click handlers to ensure dropdown functionality');
    
    // Find all dropdown triggers
    document.querySelectorAll('.goal-menu-btn.dropdown-trigger').forEach((trigger, index) => {
        // Remove existing click listeners by cloning
        const newTrigger = trigger.cloneNode(true);
        trigger.parentNode.replaceChild(newTrigger, trigger);
        
        // Add click listener
        newTrigger.addEventListener('click', function(e) {
            e.stopPropagation();
            e.preventDefault();
            const dropdown = this.nextElementSibling;
            if (!dropdown) return;
            // Check if dropdown is visible
            const isVisible = window.getComputedStyle(dropdown).visibility === 'visible' || 
                             parseFloat(window.getComputedStyle(dropdown).opacity) > 0.1;
            // Close all other dropdowns
            document.querySelectorAll('.dropdown-menu').forEach(openDropdown => {
                if (openDropdown !== dropdown && (window.getComputedStyle(openDropdown).visibility === 'visible' || parseFloat(window.getComputedStyle(openDropdown).opacity) > 0.1)) {
                    openDropdown.style.opacity = '0';
                    openDropdown.style.transform = 'translateY(-10px)';
                    openDropdown.style.pointerEvents = 'none';
                    setTimeout(() => { openDropdown.style.visibility = 'hidden'; }, 200);
                }
            });
            if (!isVisible) {
                // Open this dropdown
                dropdown.style.display = 'block';
                dropdown.style.pointerEvents = 'auto';
                void dropdown.offsetHeight;
                dropdown.style.visibility = 'visible';
                dropdown.style.opacity = '1';
                dropdown.style.transform = 'translateY(0)';
                window.activeDropdown = dropdown;
            } else {
                // Close this dropdown
                dropdown.style.opacity = '0';
                dropdown.style.transform = 'translateY(-10px)';
                dropdown.style.pointerEvents = 'none';
                setTimeout(() => { dropdown.style.visibility = 'hidden'; }, 200);
                window.activeDropdown = null;
            }
        });
    });
    // Document click fallback to close dropdowns
    if (!window._addDirectClickHandlersDocListener) {
        document.addEventListener('click', function(e) {
            if (e.target.closest('.goal-menu-btn.dropdown-trigger')) return;
            document.querySelectorAll('.dropdown-menu').forEach(dropdown => {
                if (window.getComputedStyle(dropdown).visibility === 'visible' || parseFloat(window.getComputedStyle(dropdown).opacity) > 0.1) {
                    dropdown.style.opacity = '0';
                    dropdown.style.transform = 'translateY(-10px)';
                    dropdown.style.pointerEvents = 'none';
                    setTimeout(() => { dropdown.style.visibility = 'hidden'; }, 200);
                }
            });
            window.activeDropdown = null;
        });
        window._addDirectClickHandlersDocListener = true;
    }
}
    
    // Add direct document click handler to close dropdowns
    document.addEventListener('click', function(e) {
        // Skip this handler if the click was on a dropdown trigger
        // This prevents the dropdown from closing immediately after opening
        if (e.target.closest('.goal-menu-btn.dropdown-trigger')) {
            return;
        }
        
        const openDropdowns = document.querySelectorAll('.dropdown-menu');
        let hasOpenDropdown = false;
        
        openDropdowns.forEach(dropdown => {
            if (window.getComputedStyle(dropdown).visibility === 'visible' || 
                parseFloat(window.getComputedStyle(dropdown).opacity) > 0.1) {
                hasOpenDropdown = true;
            }
        });
        
        if (hasOpenDropdown && !e.target.closest('.dropdown-container') && !e.target.closest('.goal-menu-btn')) {
            console.log('Closing dropdowns via direct document click handler');
            openDropdowns.forEach(dropdown => {
                if (window.getComputedStyle(dropdown).visibility === 'visible' || 
                    parseFloat(window.getComputedStyle(dropdown).opacity) > 0.1) {
                    // Animate closing
                    dropdown.style.opacity = '0';
                    dropdown.style.transform = 'translateY(-10px)';
                    dropdown.style.pointerEvents = 'none';
                    
                    // After animation completes, hide completely
                    setTimeout(() => {
                        dropdown.style.visibility = 'hidden';
                    }, 200);
                }
            });
            
            activeDropdown = null;
        }
    });
}

// Open modal
function openModal(modalId) {
    console.log('Opening modal with ID:', modalId);
    const targetModal = document.getElementById(modalId);
    console.log('Target modal element:', targetModal);
    if (!targetModal) {
        console.error('Modal not found with ID:', modalId);
        return;
    }
    
    // Close any active dropdowns
    if (activeDropdown) {
        console.log('Closing active dropdown before opening modal');
        closeDropdown(activeDropdown);
        activeDropdown = null;
    }

    // Close any open modals first
    closeAllModals();

    // Make the page unscrollable
    document.body.style.overflow = 'hidden';
    document.body.style.paddingRight = '15px'; // Prevent layout shift when scrollbar disappears

    // Show the modal by removing the hidden class if it exists
    targetModal.classList.remove('hidden');
    targetModal.setAttribute('aria-hidden', 'false');
    activeModal = modalId;
    
    // Add modal-open class to body to hide dropdown triggers
    document.body.classList.add('modal-open');
    console.log('Modal opened:', modalId);

    // Set focus to the first focusable element
    setTimeout(() => {
        const focusableElements = targetModal.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
        if (focusableElements.length > 0) {
            focusableElements[0].focus();
        }
    }, 100);
}

// Close modal
function closeModal(modalId) {
    const targetModal = document.getElementById(modalId);
    if (!targetModal) return;

    // Restore scrolling
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';
    
    // Remove modal-open class from body to restore dropdown visibility
    document.body.classList.remove('modal-open');

    targetModal.setAttribute('aria-hidden', 'true');
    activeModal = null;

    // Reset form if it's the create goal modal
    if (targetModal.id === 'goal-modal') {
        resetForm();
    }
}

// Close all modals
function closeAllModals() {
    document.querySelectorAll('.modal').forEach(modal => {
        closeModal(modal.id);
    });
}

// Reset form
function resetForm() {
    const goalForm = document.getElementById('goal-form');
    if (goalForm) goalForm.reset();

    // Hide all error messages
    document.querySelectorAll('.text-red-600').forEach(el => {
        el.classList.add('hidden');
    });

    document.getElementById('form-error').classList.add('hidden');
}

// Open edit modal with the static modal from HTML
function openEditModal(goal) {
    const modal = document.getElementById('edit-goal-modal');
    const form = document.getElementById('edit-goal-form');
    
    if (!modal || !form) {
        console.error('Edit modal or form not found in the DOM');
        showToast('Error: Could not open edit form', 'error');
        return;
    }
    
    // Clear any previous validation errors
    clearValidationErrors('edit-');
    
    // Fill form with goal data
    document.getElementById('edit-goal-id').value = goal.id || '';
    document.getElementById('edit-goal-title').value = goal.title || '';
    document.getElementById('edit-goal-description').value = goal.description || '';
    
    // Set completed checkbox
    const completedCheckbox = document.getElementById('edit-goal-completed');
    if (completedCheckbox) {
        completedCheckbox.checked = goal.isCompleted || false;
    }
    
    // Format the due date if it exists
    console.log('Goal data received:', goal);
    console.log('Due date from goal:', goal.dueDate);
    
    if (goal.dueDate) {
        try {
            // Pure string manipulation approach - no Date objects
            if (typeof goal.dueDate === 'string') {
                // YYYY-MM-DD format (use directly)
                if (goal.dueDate.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    console.log('Using direct YYYY-MM-DD format:', goal.dueDate);
                    document.getElementById('edit-due-date').value = goal.dueDate;
                } 
                // ISO format with T (extract date part)
                else if (goal.dueDate.includes('T')) {
                    const datePart = goal.dueDate.split('T')[0];
                    console.log('Extracted date part from ISO format:', datePart);
                    document.getElementById('edit-due-date').value = datePart;
                }
                // MM/DD/YYYY format
                else if (goal.dueDate.match(/^\d{1,2}\/\d{1,2}\/\d{4}$/)) {
                    const parts = goal.dueDate.split('/');
                    const month = parts[0].padStart(2, '0');
                    const day = parts[1].padStart(2, '0');
                    const year = parts[2];
                    const formattedDate = `${year}-${month}-${day}`;
                    console.log('Converted MM/DD/YYYY to YYYY-MM-DD:', formattedDate);
                    document.getElementById('edit-due-date').value = formattedDate;
                }
                // Any other format - just use raw string
                else {
                    console.log('Using raw date string:', goal.dueDate);
                    document.getElementById('edit-due-date').value = goal.dueDate;
                }
            } else {
                // If it's not a string, convert to string and use as is
                const dateStr = String(goal.dueDate);
                console.log('Converted non-string date to string:', dateStr);
                document.getElementById('edit-due-date').value = dateStr;
            }
        } catch (error) {
            console.error('Error handling due date:', error, goal.dueDate);
            // Don't default to today if parsing fails, leave field empty
            document.getElementById('edit-due-date').value = '';
        }
    } else {
        console.log('No due date provided, leaving field empty');
        // Leave field empty if no due date provided
        document.getElementById('edit-due-date').value = '';
    }
    
    // Show the modal
    modal.setAttribute('aria-hidden', 'false');
    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
    
    // Set focus to the first input field
    const firstInput = form.querySelector('input:not([type="hidden"]), textarea, select');
    if (firstInput) {
        setTimeout(() => firstInput.focus(), 100);
    }
}

// Display validation errors in the form
function displayValidationErrors(errors, prefix = '') {
    // Clear any existing errors first
    clearValidationErrors(prefix);
    
    // Show the form error message container
    const formError = document.getElementById(`${prefix}form-error`);
    if (formError) {
        formError.classList.remove('hidden');
    }
    
    // Process each error and display it in the corresponding field
    for (const field in errors) {
        const errorMessages = errors[field];
        if (Array.isArray(errorMessages) && errorMessages.length > 0) {
            // Find the error element for this field
            const fieldName = field.charAt(0).toLowerCase() + field.slice(1); // Convert to camelCase
            const errorElement = document.getElementById(`${prefix}${fieldName}-error`);
            
            if (errorElement) {
                errorElement.textContent = errorMessages[0]; // Display the first error message
                errorElement.classList.remove('hidden');
            }
        }
    }
}

// Clear all validation errors
function clearValidationErrors(prefix = '') {
    // Hide all error messages
    document.querySelectorAll(`[id$="-error"]`).forEach(el => {
        if (el.id.startsWith(prefix)) {
            el.classList.add('hidden');
            el.textContent = ''; // Clear the error message
        }
    });
    
    // Hide the form error container
    const formError = document.getElementById(`${prefix}form-error`);
    if (formError) {
        formError.classList.add('hidden');
    }
}

// Open delete modal using the shared modal partial
function openDeleteModal(goalId) {
    // Use the shared modal system
    const modalConfig = {
        type: 'delete',
        title: 'Delete Goal',
        message: 'Are you sure you want to delete this goal? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel',
        onConfirm: () => handleDeleteGoal(goalId)
    };
    
    window.openModal(modalConfig);
}

// Handle edit form submission
async function handleEditFormSubmit(event) {
    event.preventDefault();
    
    const form = event.target;
    const formData = new FormData(form);
    const goalData = Object.fromEntries(formData.entries());
    
    try {
        const response = await fetch('/User/Goal/Update', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(goalData)
        });
        
        if (response.ok) {
            closeModal('edit-goal-form-modal');
            loadGoals(); // Refresh the goals list
            showToast('Goal updated successfully', 'success');
        } else {
            throw new Error('Failed to update goal');
        }
    } catch (error) {
        console.error('Error updating goal:', error);
        showToast('Error updating goal', 'error');
    }
}

// Handle goal deletion
async function handleDeleteGoal(goalId) {
    try {
        // Get the anti-forgery token from the form
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            throw new Error('Anti-forgery token not found');
        }
        
        const response = await fetch(`/User/Goal/Delete/${goalId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'  // Indicate this is an AJAX request
            },
            credentials: 'same-origin'  // Include cookies in the request
        });
        
        if (response.ok) {
            // Close the delete modal if it's open
            const modal = document.getElementById('delete-modal');
            if (modal) {
                closeModal(modal.id);
            }
            
            // Refresh the goals list
            loadGoals();
            
            // Show success message
            showToast('Goal deleted successfully', 'success');
        } else {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || 'Failed to delete goal');
        }
    } catch (error) {
        console.error('Error deleting goal:', error);
        showToast(error.message || 'Error deleting goal', 'error');
    }
}

// Close modal helper function
function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.setAttribute('aria-hidden', 'true');
        modal.classList.add('hidden');
        document.body.style.overflow = '';
        
        // Clear any error messages when closing
        const errorMessages = modal.querySelectorAll('.error-message');
        errorMessages.forEach(msg => msg.classList.add('hidden'));
    }
}

function validateForm(formId = 'goal-form') {
    const prefix = formId === 'edit-goal-form' ? 'edit-' : '';
    let isValid = true;

    // Validate title
    const titleInput = document.getElementById(`${prefix}goal-title`);
    const titleError = document.getElementById(`${prefix}title-error`);

    if (!titleInput.validity.valid) {
        titleError.classList.remove('hidden');
        isValid = false;
    } else {
        titleError.classList.add('hidden');
    }

    // Validate description (only if not empty)
    const descInput = document.getElementById(`${prefix}goal-description`);
    const descError = document.getElementById(`${prefix}description-error`);

    if (descInput.value.trim() !== '' && !descInput.validity.valid) {
        descError.classList.remove('hidden');
        isValid = false;
    } else {
        descError.classList.add('hidden');
    }

    // Validate due date
    const dueDateInput = document.getElementById(`${prefix}due-date`);
    const dueDateError = document.getElementById(`${prefix}due-date-error`);

    if (!dueDateInput.validity.valid) {
        dueDateError.classList.remove('hidden');
        isValid = false;
    } else {
        dueDateError.classList.add('hidden');
    }

    // Show form error if not valid
    const formError = document.getElementById(`${prefix}form-error`);
    if (!isValid) {
        formError.classList.remove('hidden');
    } else {
        formError.classList.add('hidden');
    }

    return isValid;
}

// Show toast message using the shared toast partial
function showToast(message, type = 'info') {
    console.log('Showing toast:', message, type);
    
    // Map 'info' type to 'success' since the shared toast partial only supports 'success' and 'error'
    const mappedType = type === 'info' ? 'success' : type;
    
    // Dispatch event for Alpine.js toast system
    try {
        const event = new CustomEvent('show-toast', {
            detail: {
                message: message,
                type: mappedType
            }
        });
        
        // Dispatch the event to the window
        window.dispatchEvent(event);
        console.log('Toast event dispatched');
    } catch (error) {
        console.error('Error dispatching toast event:', error);
        
        // Fallback toast if event dispatch fails
        alert(`${mappedType.toUpperCase()}: ${message}`);
    }
}

// Add direct click handlers as a fallback
function addDirectClickHandlers() {
    console.log('Adding direct click handlers');
    
    // Handle edit button clicks
    document.querySelectorAll('.edit-goal-btn').forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            const goalId = this.getAttribute('data-id');
            const goalCard = this.closest('.goal-card');
            
            if (goalCard) {
                const goal = {
                    id: goalId,
                    title: goalCard.querySelector('.goal-title')?.textContent.trim() || '',
                    description: goalCard.querySelector('.goal-description')?.textContent.trim() || '',
                    dueDate: goalCard.querySelector('[data-due-date]')?.getAttribute('data-due-date') || new Date().toISOString().split('T')[0],
                    priority: 'medium', // Default priority
                    isCompleted: goalCard.getAttribute('data-status') === 'completed'
                };
                openEditModal(goal);
            }
        });
    });

    // Handle delete button clicks
    document.querySelectorAll('.delete-goal-btn').forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            const goalId = this.getAttribute('data-id');
            if (goalId) {
                openDeleteModal(goalId);
            }
        });
    });
}

// Initialize completion toggle functionality
function initializeCompletedToggles() {
    console.log('Initializing completed toggles');
    
    document.querySelectorAll('.goal-card [data-action="toggle-complete"]').forEach(toggle => {
        toggle.addEventListener('click', async function(e) {
            e.stopPropagation();
            const card = this.closest('.goal-card');
            const goalId = card?.dataset?.id;
            const isCompleted = card?.dataset?.status === 'completed';
            
            if (!goalId) {
                console.error('No goal ID found for toggle');
                return;
            }

            try {
                const response = await fetch(`/User/Goal/ToggleComplete/${goalId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    }
                });

                if (response.ok) {
                    // Toggle the UI state
                    const newStatus = isCompleted ? 'active' : 'completed';
                    card.dataset.status = newStatus;
                    
                    // Update the status badge
                    const statusBadge = card.querySelector('.status-badge');
                    if (statusBadge) {
                        if (isCompleted) {
                            statusBadge.className = 'status-badge active bg-blue-100 text-blue-800 text-xs px-2.5 py-0.5 rounded-full';
                            statusBadge.innerHTML = '<i class="fas fa-spinner mr-1"></i>Active';
                        } else {
                            statusBadge.className = 'status-badge completed bg-green-100 text-green-800 text-xs px-2.5 py-0.5 rounded-full';
                            statusBadge.innerHTML = '<i class="fas fa-check-circle mr-1"></i>Completed';
                        }
                    }
                    
                    // Show feedback
                    showToast(`Goal marked as ${newStatus}`, 'success');
                } else {
                    throw new Error('Failed to update goal status');
                }
            } catch (error) {
                console.error('Error toggling goal completion:', error);
                showToast('Error updating goal status', 'error');
            }
        });
    });
}

// Handle edit goal form submission
async function handleEditSubmit(event) {
    event.preventDefault();
    
    // Get form data
    const form = event.target;
    const goalId = form.querySelector('input[name="id"]').value;
    const title = form.querySelector('input[name="title"]').value;
    const description = form.querySelector('textarea[name="description"]').value;
    const dueDate = form.querySelector('input[name="dueDate"]').value;
    const isCompleted = form.querySelector('input[name="completed"]').checked;
    
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    // Create model with PascalCase properties to match C# model
    const modelData = {
        Id: goalId,
        Title: title,
        Description: description,
        DueDate: dueDate,
        Completed: isCompleted,
        __RequestVerificationToken: token  // Include token in body
    };
    
    // Disable submit button and show loading state
    const submitButton = form.querySelector('button[type="submit"]');
    const saveText = document.getElementById('save-text');
    const saveLoading = document.getElementById('save-loading');
    
    if (submitButton) submitButton.disabled = true;
    if (saveText) saveText.classList.add('hidden');
    if (saveLoading) saveLoading.classList.remove('hidden');
    
    try {
        const response = await fetch('/User/Goal/Update', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token,        // Primary header name from config
                'RequestVerificationToken': token,  // Fallback header name
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(modelData),
            credentials: 'same-origin'
        });
        
        // Re-enable submit button and restore button text
        if (submitButton) submitButton.disabled = false;
        if (saveText) saveText.classList.remove('hidden');
        if (saveLoading) saveLoading.classList.add('hidden');
        
        if (!response.ok) {
            throw new Error(`Server error: ${response.status}`);
        }
        
        const result = await response.json();
        if (result.success) {
            // Close the modal
            closeModal('edit-goal-modal');
            
            // Show success toast
            showToast(result.message || 'Goal updated successfully!', 'success');
            
            // Reload goals to refresh the UI
            loadGoals();
        } else {
            // Handle validation errors
            showToast(result.message || 'Failed to update goal', 'error');
            
            // Display validation errors if available
            if (result.errors) {
                displayValidationErrors(result.errors, 'edit-');
            }
        }
    } catch (error) {
        console.error('Error updating goal:', error);
        showToast('Failed to update goal. Please try again.', 'error');
        
        // Re-enable submit button and restore button text if not already done
        if (submitButton) submitButton.disabled = false;
        if (saveText) saveText.classList.remove('hidden');
        if (saveLoading) saveLoading.classList.add('hidden');
    }
}
// Handle create goal form submission
async function handleCreateSubmit(event) {
    event.preventDefault();
    
    const form = event.target;
    if (!validateForm('goal-form')) {
        return;
    }
    
    // Disable submit button and show loading state
    const submitButton = form.querySelector('button[type="submit"]');
    const createText = document.getElementById('create-text');
    const createLoading = document.getElementById('create-loading');
    
    if (submitButton) submitButton.disabled = true;
    if (createText) createText.classList.add('hidden');
    if (createLoading) createLoading.classList.remove('hidden');
    
    const formData = new FormData(form);
    const goalData = Object.fromEntries(formData.entries());
    
    // Add any additional processing if needed
    if (!goalData.dueDate) {
        goalData.dueDate = new Date().toISOString().split('T')[0];
    }
    
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    try {
        // Convert form data to JSON with PascalCase keys to match C# model
        const modelData = {
            Title: goalData.title,
            Description: goalData.description,
            DueDate: goalData.dueDate,
            Completed: goalData.completed === 'on',
            __RequestVerificationToken: token // Include token in body
        };
        
        const response = await fetch('/User/Goal/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token,
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(modelData),
            credentials: 'same-origin'
        });
        
        // Re-enable submit button and restore button text
        if (submitButton) submitButton.disabled = false;
        if (createText) createText.classList.remove('hidden');
        if (createLoading) createLoading.classList.add('hidden');
        
        if (!response.ok) {
            throw new Error(`Server error: ${response.status}`);
        }
        
        const result = await response.json();
        if (result.success) {
            // Close the modal
            closeModal('goal-modal');
            
            // Reset the form
            form.reset();
            
            // Show success toast
            showToast(result.message || 'Goal created successfully!', 'success');
            
            // Reload goals to refresh the UI
            loadGoals();
        } else {
            // Handle validation errors
            showToast(result.message || 'Failed to create goal', 'error');
            
            // Display validation errors if available
            if (result.errors) {
                displayValidationErrors(result.errors);
            }
        }
    } catch (error) {
        console.error('Error creating goal:', error);
        showToast('Failed to create goal. Please try again.', 'error');
        
        // Re-enable submit button and restore button text if not already done
        if (submitButton) submitButton.disabled = false;
        if (createText) createText.classList.remove('hidden');
        if (createLoading) createLoading.classList.add('hidden');
    }
}

// Initialize the app
function init() {
    // Load goals on page load
    loadGoals();

    // Initialize goal cards
    initializeGoalCards();
    
    // Set up event listeners for modal confirmations
    window.addEventListener('modal-confirmed', function(event) {
        const detail = event.detail;
        
        if (detail.type === 'delete') {
            const goalId = document.getElementById('delete-goal-id').value;
            
            if (!goalId) {
                console.error('No goal ID found for deletion');
                return;
            }
            
            // Submit to server
            fetch('/User/Goal/Delete', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                body: JSON.stringify({ id: goalId })
            })
            .then(response => {
                if (!response.ok) {
                    if (response.status === 400) {
                        return response.json().then(errors => {
                            throw new Error('Validation failed: ' + JSON.stringify(errors));
                        });
                    } else if (response.status === 403) {
                        throw new Error('You do not have permission to delete this goal');
                    }
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    // Reload goals
                    loadGoals();
                    
                    // Show success message
                    showToast('Goal deleted successfully', 'success');
                } else {
                    showToast(data.message || 'Failed to delete goal. Please try again.', 'error');
                }
            })
            .catch(error => {
                console.error('Error deleting goal:', error);
                showToast(error.message || 'Failed to delete goal. Please try again.', 'error');
            });
        }
    });
    
    // Set up form submission handlers
    const goalForm = document.getElementById('goal-form');
    if (goalForm) {
        goalForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Validate form
            if (!validateForm()) {
                return;
            }
            
            // Get form data
            const formData = {
                title: document.getElementById('goal-title').value,
                description: document.getElementById('goal-description').value,
                dueDate: document.getElementById('due-date').value,
                priority: document.getElementById('priority').value
            };
            
            // Show loading state
            document.getElementById('submit-text').classList.add('hidden');
            document.getElementById('submit-loading').classList.remove('hidden');
            
            // Submit to server
            fetch('/User/Goal/Create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                body: JSON.stringify(formData)
            })
            .then(response => {
                if (!response.ok) {
                    if (response.status === 400) {
                        return response.json().then(errors => {
                            throw new Error('Validation failed: ' + JSON.stringify(errors));
                        });
                    }
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    // Reset form
                    resetForm();
                    
                    // Close modal
                    closeModal('goal-modal');
                    
                    // Reload goals
                    loadGoals();
                    
                    // Show success message
                    showToast('Goal created successfully', 'success');
                } else {
                    showToast(data.message || 'Failed to create goal. Please try again.', 'error');
                }
            })
            .catch(error => {
                console.error('Error creating goal:', error);
                showToast(error.message || 'Failed to create goal. Please try again.', 'error');
            })
            .finally(() => {
                // Hide loading state
                document.getElementById('submit-text').classList.remove('hidden');
                document.getElementById('submit-loading').classList.add('hidden');
            });
        });
    }
    
    const editGoalForm = document.getElementById('edit-goal-form');
    if (editGoalForm) {
        editGoalForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Validate form
            if (!validateForm('edit-goal-form')) {
                return;
            }
            
            const isCompletedCheckbox = document.getElementById('edit-is-completed');
            const dueDateInput = document.getElementById('edit-due-date');

            if (isCompletedCheckbox.checked) {
                const today = new Date().toISOString().split('T')[0];
                dueDateInput.value = today;
            }

            // Get form data
            const formData = {
                id: document.getElementById('edit-goal-id').value,
                title: document.getElementById('edit-goal-title').value,
                description: document.getElementById('edit-goal-description').value,
                dueDate: dueDateInput.value,
                priority: document.getElementById('edit-priority').value,
                isCompleted: isCompletedCheckbox.checked
            };
            
            // Show loading state
            document.getElementById('edit-submit-text').classList.add('hidden');
            document.getElementById('edit-submit-loading').classList.remove('hidden');
            
            // Submit to server
            fetch('/User/Goal/Edit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                body: JSON.stringify(formData)
            })
            .then(response => {
                if (!response.ok) {
                    if (response.status === 400) {
                        return response.json().then(errors => {
                            throw new Error('Validation failed: ' + JSON.stringify(errors));
                        });
                    } else if (response.status === 403) {
                        throw new Error('You do not have permission to edit this goal');
                    }
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    // Close modal
                    closeModal('edit-goal-modal');
                    
                    // Reload goals
                    loadGoals();
                    
                    // Show success message
                    showToast('Goal updated successfully', 'success');
                } else {
                    showToast(data.message || 'Failed to update goal. Please try again.', 'error');
                }
            })
            .catch(error => {
                console.error('Error updating goal:', error);
                showToast(error.message || 'Failed to update goal. Please try again.', 'error');
            })
            .finally(() => {
                // Hide loading state
                document.getElementById('edit-submit-text').classList.remove('hidden');
                document.getElementById('edit-submit-loading').classList.add('hidden');
            });
        });
    }
}