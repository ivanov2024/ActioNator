document.addEventListener('DOMContentLoaded', function () {
    const signUpButton = document.getElementById('signUp');
    const signInButton = document.getElementById('signIn');
    const container = document.getElementById('container');
    const toggleButtons = document.querySelectorAll('.toggle-form');

    // Desktop view - overlay panel buttons
    if (signUpButton && signInButton) {
        signUpButton.addEventListener('click', () => {
            container.classList.add("right-panel-active");
        });

        signInButton.addEventListener('click', () => {
            container.classList.remove("right-panel-active");
        });
    }

    // Mobile view - toggle buttons
    let isSignUpActive = false;

    toggleButtons.forEach(button => {
        button.addEventListener('click', (e) => {
            e.preventDefault();

            // Prevent multiple rapid clicks
            if (container.classList.contains('transitioning')) return;

            // Add transitioning class to prevent multiple clicks
            container.classList.add('transitioning');

            // Wait for the transition to complete
            setTimeout(() => {
                container.classList.remove('transitioning');
            }, 500); // Match the CSS transition duration

            if (isSignUpActive) {
                container.classList.remove('right-panel-active');
                isSignUpActive = false;
            } else {
                container.classList.add('right-panel-active');
                isSignUpActive = true;
            }
        });
    });

    // Handle window resize to ensure proper state
    let isMobile = window.innerWidth <= 769;

    function handleResize() {
        const newIsMobile = window.innerWidth <= 769;

        if (isMobile !== newIsMobile) {
            isMobile = newIsMobile;

            // Reset the container state when switching between mobile and desktop
            if (!isMobile) {
                container.classList.remove('right-panel-active');
            }
        }
    }

    // Initial check
    handleResize();

    // Add resize listener
    window.addEventListener('resize', handleResize);
});