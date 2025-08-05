// Skeleton Loader for Goal Cards

// Create skeleton cards for loading state
function createSkeletonCards(container, count = 8) {
    console.log('Creating skeleton cards:', count);
    
    // Clear any existing skeleton cards
    container.innerHTML = '';
    
    // Create the specified number of skeleton cards
    for (let i = 0; i < count; i++) {
        const skeletonCard = document.createElement('div');
        skeletonCard.className = 'skeleton-card';
        
        // Create skeleton card structure that matches the real goal card structure
        skeletonCard.innerHTML = `
            <div class="skeleton-header">
                <div class="skeleton-title skeleton-pulse"></div>
                <div class="skeleton-menu skeleton-pulse"></div>
            </div>
            <div class="skeleton-description skeleton-pulse"></div>
            <div class="skeleton-footer">
                <div class="skeleton-date skeleton-pulse"></div>
                <div class="skeleton-status skeleton-pulse"></div>
            </div>
        `;
        
        container.appendChild(skeletonCard);
    }
}

// Show loading state
function showLoading() {
    const goalsContainer = document.getElementById('goals-container');
    const loadingSkeleton = document.getElementById('loading-skeleton');
    const emptyState = document.getElementById('empty-state');
    
    if (goalsContainer) goalsContainer.classList.add('hidden');
    if (emptyState) emptyState.classList.add('hidden');
    
    if (loadingSkeleton) {
        loadingSkeleton.classList.remove('hidden');
        createSkeletonCards(loadingSkeleton);
    }
}

// Hide loading state
function hideLoading() {
    const loadingSkeleton = document.getElementById('loading-skeleton');
    if (loadingSkeleton) {
        loadingSkeleton.classList.add('hidden');
        loadingSkeleton.innerHTML = '';
    }
}

// Initialize loading skeleton on page load
document.addEventListener('DOMContentLoaded', function() {
    const loadingSkeleton = document.getElementById('loading-skeleton');
    
    // Create initial skeleton cards (hidden by default)
    if (loadingSkeleton) {
        createSkeletonCards(loadingSkeleton);
        
        // Hide skeleton after initial page load is complete
        setTimeout(() => {
            hideLoading();
            document.getElementById('goals-container').classList.remove('hidden');
        }, 500);
    }
});
