document.addEventListener('DOMContentLoaded', function () {
    // Check if elements exist before accessing them
    const menuToggle = document.getElementById('menu-toggle');
    const closeMenu = document.getElementById('close-menu');
    const mobileNav = document.getElementById('mobile-nav');
    
    // Exit early if required elements don't exist
    if (!menuToggle || !closeMenu || !mobileNav) {
        console.warn('Navigation elements not found. Navigation functionality disabled.');
        return;
    }
    
    const mobileNavContent = mobileNav.querySelector('div');
    const navLinks = document.querySelectorAll('nav a');

    const focusableElements = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    let firstFocusableElement, lastFocusableElement;

    function toggleMenu(open) {
        const isOpening = typeof open === 'boolean' ? open : mobileNav.classList.contains('hidden');

        if (isOpening) {
            // Opening the menu
            mobileNav.classList.remove('hidden');
            document.body.style.overflow = 'hidden';
            // Force reflow to ensure the element is rendered before adding active class
            void mobileNav.offsetHeight;
            mobileNav.classList.add('active');

            // Set focus to first focusable element in the mobile menu
            const firstFocusable = mobileNavContent.querySelectorAll(focusableElements)[0];
            if (firstFocusable) firstFocusable.focus();
        } else {
            // Closing the menu
            mobileNav.classList.remove('active');
            document.body.style.overflow = '';

            // Wait for the transition to complete before hiding the element
            setTimeout(() => {
                if (!mobileNav.classList.contains('active')) {
                    mobileNav.classList.add('hidden');
                }
                // Return focus to menu toggle button when closing
                menuToggle.focus();
            }, 300);
        }

        // Update aria-expanded state
        menuToggle.setAttribute('aria-expanded', isOpening);
    }

    // Close menu when clicking outside
    mobileNav.addEventListener('click', function (e) {
        if (e.target === mobileNav) {
            toggleMenu(false);
        }
    });

    // Close menu when pressing Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !mobileNav.classList.contains('hidden')) {
            toggleMenu(false);
        }
    });

    menuToggle.addEventListener('click', () => toggleMenu());
    closeMenu.addEventListener('click', () => toggleMenu(false));

    // Close menu when clicking on a nav link
    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            if (!mobileNav.classList.contains('hidden')) {
                toggleMenu(false);
            }
        });
    });

    // Handle keyboard navigation for mobile menu
    mobileNavContent.addEventListener('keydown', function (e) {
        // Get all focusable elements in the mobile menu
        const focusableElements = Array.from(
            mobileNavContent.querySelectorAll(focusableElements)
        );

        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];

        // Handle Tab key
        if (e.key === 'Tab') {
            if (e.shiftKey) {
                // Shift + Tab
                if (document.activeElement === firstFocusable) {
                    e.preventDefault();
                    lastFocusable.focus();
                }
            } else {
                // Tab
                if (document.activeElement === lastFocusable) {
                    e.preventDefault();
                    firstFocusable.focus();
                }
            }
        }
    });

    // Update active link based on current URL
    function setActiveLink() {
        const currentPath = window.location.pathname;
        navLinks.forEach(link => {
            try {
                const linkPath = new URL(link.href).pathname;
                if (currentPath === linkPath) {
                    link.classList.add('active');
                } else {
                    link.classList.remove('active');
                }
            } catch (e) {
                console.warn('Error processing link:', e);
            }
        });
    }

    // Initialize
    setActiveLink();
});
