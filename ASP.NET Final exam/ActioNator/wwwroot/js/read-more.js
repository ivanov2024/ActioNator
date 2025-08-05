// Read More Button Functionality
document.addEventListener('DOMContentLoaded', function() {
    console.log('Initializing Read More buttons');
    initializeReadMoreButtons();
    
    // Re-initialize read more buttons when goals are refreshed
    document.addEventListener('goals-refreshed', function() {
        console.log('Goals refreshed, reinitializing Read More buttons');
        initializeReadMoreButtons();
    });
});

// Initialize all read more buttons in the document
function initializeReadMoreButtons() {
    const readMoreButtons = document.querySelectorAll('.read-more-btn');
    console.log(`Found ${readMoreButtons.length} read more buttons`);
    
    readMoreButtons.forEach(button => {
        // Remove any existing event listeners to prevent duplicates
        button.removeEventListener('click', handleReadMoreClick);
        // Add the click event listener
        button.addEventListener('click', handleReadMoreClick);
    });
}

// Handle read more button click
function handleReadMoreClick(event) {
    const button = event.currentTarget;
    const container = button.closest('.goal-description-container');
    const description = container.querySelector('.goal-description');
    const readMoreText = button.querySelector('.read-more');
    const readLessText = button.querySelector('.read-less');
    const goalCard = button.closest('.goal-card');
    
    console.log('Read More button clicked', description);
    
    // Toggle expanded state
    if (description.classList.contains('expanded')) {
        // Collapse
        description.classList.remove('expanded');
        if (goalCard) goalCard.classList.remove('expanded');
        readMoreText.classList.remove('hidden');
        readLessText.classList.add('hidden');
    } else {
        // Expand
        description.classList.add('expanded');
        if (goalCard) goalCard.classList.add('expanded');
        readMoreText.classList.add('hidden');
        readLessText.classList.remove('hidden');
    }
}
