// Admin Panel Global JS

document.addEventListener('DOMContentLoaded', function () {
    // Sidebar Toggle
    const sidebarToggles = document.querySelectorAll('.sidebar-toggle, #desktop-sidebar-toggle');
    const adminWrapper = document.querySelector('.admin-wrapper');

    if (sidebarToggles && adminWrapper) {
        sidebarToggles.forEach(toggle => {
            toggle.addEventListener('click', function () {
                adminWrapper.classList.toggle('sidebar-collapsed');

                // Save state to localStorage
                const isCollapsed = adminWrapper.classList.contains('sidebar-collapsed');
                localStorage.setItem('sidebar-collapsed', isCollapsed);
            });
        });

        // Restore sidebar state from localStorage
        const isCollapsed = localStorage.getItem('sidebar-collapsed') === 'true';
        if (isCollapsed) {
            adminWrapper.classList.add('sidebar-collapsed');
        }
    }

    // Highlight active sidebar item based on current URL
    const currentPath = window.location.pathname;
    const sidebarLinks = document.querySelectorAll('.sidebar-link');

    sidebarLinks.forEach(link => {
        const href = link.getAttribute('href');

        // Check if current path matches the link href
        if (href === currentPath || (href !== '/' && currentPath.startsWith(href))) {
            link.classList.add('active');

            // If the link is in a dropdown, expand the dropdown
            const parentCollapse = link.closest('.collapse');
            if (parentCollapse) {
                parentCollapse.classList.add('show');

                const triggerLink = document.querySelector(`[data-bs-toggle="collapse"][href="#${parentCollapse.id}"]`);
                if (triggerLink) {
                    triggerLink.classList.add('active');
                    triggerLink.setAttribute('aria-expanded', 'true');
                }
            }
        } else {
            link.classList.remove('active');
        }
    });

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Initialize dropdowns (on hover for desktop)
    if (window.innerWidth >= 992) {
        document.querySelectorAll('.navbar .dropdown').forEach(function (dropdown) {
            dropdown.addEventListener('mouseenter', function () {
                this.querySelector('.dropdown-toggle').click();
            });

            dropdown.addEventListener('mouseleave', function () {
                this.querySelector('.dropdown-toggle').click();
            });
        });
    }
});