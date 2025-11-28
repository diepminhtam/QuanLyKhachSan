// Room List Page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Initialize components
    initializeDateInputs();
    initializePriceSlider();
    initializeFilters();
    initializeSorting();
    initializeLoadingStates();
    initializeImageLazyLoading();
    initializeAccessibility();

    // Initialize date inputs with defaults
    function initializeDateInputs() {
        const checkinInput = document.querySelector('input[name="checkin"]');
        const checkoutInput = document.querySelector('input[name="checkout"]');

        if (checkinInput && checkoutInput) {
            const today = new Date();
            const tomorrow = new Date(today);
            tomorrow.setDate(tomorrow.getDate() + 1);

            // Set minimum dates
            checkinInput.min = formatDate(today);
            checkoutInput.min = formatDate(tomorrow);

            // Set default values if empty
            if (!checkinInput.value) {
                checkinInput.value = formatDate(today);
            }
            if (!checkoutInput.value) {
                checkoutInput.value = formatDate(tomorrow);
            }

            // Update checkout min date when checkin changes
            checkinInput.addEventListener('change', function () {
                const newCheckinDate = new Date(this.value);
                const newMinCheckoutDate = new Date(newCheckinDate);
                newMinCheckoutDate.setDate(newMinCheckoutDate.getDate() + 1);

                checkoutInput.min = formatDate(newMinCheckoutDate);

                if (new Date(checkoutInput.value) <= newCheckinDate) {
                    checkoutInput.value = formatDate(newMinCheckoutDate);
                }
            });
        }
    }

    // Initialize price slider
    function initializePriceSlider() {
        const priceSlider = document.getElementById('priceSlider');
        if (!priceSlider) return;

        const minPrice = parseFloat(priceSlider.dataset.min || '0');
        const maxPrice = parseFloat(priceSlider.dataset.max || '10000000');
        const currentMinPrice = parseFloat(document.getElementById('minPrice').value || minPrice);
        const currentMaxPrice = parseFloat(document.getElementById('maxPrice').value || maxPrice);

        noUiSlider.create(priceSlider, {
            start: [currentMinPrice, currentMaxPrice],
            connect: true,
            step: 100000,
            range: {
                'min': minPrice,
                'max': maxPrice
            },
            format: {
                to: function (value) {
                    return Math.round(value);
                },
                from: function (value) {
                    return parseFloat(value);
                }
            },
            tooltips: [
                {
                    to: function (value) {
                        return formatCurrency(value);
                    }
                },
                {
                    to: function (value) {
                        return formatCurrency(value);
                    }
                }
            ]
        });

        priceSlider.noUiSlider.on('update', function (values, handle) {
            const value = values[handle];

            if (handle === 0) {
                document.getElementById('minPrice').value = value;
                document.getElementById('priceMin').textContent = formatCurrency(value);
            } else {
                document.getElementById('maxPrice').value = value;
                document.getElementById('priceMax').textContent = formatCurrency(value);
            }
        });

        // Auto-submit after slider change with debounce
        let sliderTimeout;
        priceSlider.noUiSlider.on('change', function () {
            clearTimeout(sliderTimeout);
            sliderTimeout = setTimeout(() => {
                document.getElementById('filterForm').dispatchEvent(new Event('submit'));
            }, 500);
        });
    }

    // Initialize filters
    function initializeFilters() {
        const filterForm = document.getElementById('filterForm');
        const filterInputs = filterForm.querySelectorAll('input, select');

        // Auto-submit form on filter change (except price slider)
        filterInputs.forEach(input => {
            if (input.name !== 'minPrice' && input.name !== 'maxPrice') {
                let timeout;
                input.addEventListener('change', function () {
                    clearTimeout(timeout);
                    timeout = setTimeout(() => {
                        showLoadingState();
                        filterForm.submit();
                    }, input.type === 'text' ? 500 : 0);
                });
            }
        });

        // Handle form submission
        filterForm.addEventListener('submit', function (e) {
            showLoadingState();
            // Form will submit normally, but we show loading state
        });

        // Clear filters functionality
        const clearFiltersBtn = document.querySelector('.clear-filters');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', function (e) {
                e.preventDefault();
                clearAllFilters();
            });
        }
    }

    // Initialize sorting
    function initializeSorting() {
        const sortSelect = document.getElementById('sortOptions');
        if (sortSelect) {
            sortSelect.addEventListener('change', function () {
                updateSort(this.value);
            });
        }
    }

    // Initialize loading states
    function initializeLoadingStates() {
        // Add loading skeletons if needed
        const roomList = document.querySelector('.room-list');
        if (roomList && roomList.children.length === 0) {
            showSkeletonLoading();
        }
    }

    // Initialize lazy loading for images
    function initializeImageLazyLoading() {
        const images = document.querySelectorAll('.room-img img');

        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src || img.src;
                        img.classList.remove('lazy');
                        observer.unobserve(img);
                    }
                });
            });

            images.forEach(img => {
                if (img.dataset.src) {
                    imageObserver.observe(img);
                }
            });
        }
    }

    // Initialize accessibility features
    function initializeAccessibility() {
        // Add keyboard navigation for room cards
        const roomCards = document.querySelectorAll('.room-card-horizontal');
        roomCards.forEach(card => {
            card.setAttribute('tabindex', '0');
            card.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    const detailLink = card.querySelector('.room-title a');
                    if (detailLink) {
                        detailLink.click();
                    }
                }
            });
        });

        // Announce filter results to screen readers
        const resultsInfo = document.querySelector('.room-sorting p');
        if (resultsInfo) {
            resultsInfo.setAttribute('aria-live', 'polite');
        }
    }

    // Utility functions
    function formatDate(date) {
        return date.toISOString().split('T')[0];
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'decimal',
            maximumFractionDigits: 0
        }).format(value) + ' VNĐ';
    }

    function updateSort(value) {
        const form = document.getElementById('filterForm');
        const sortInput = form.querySelector('input[name="sortBy"]');
        if (!sortInput) {
            const hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = 'sortBy';
            form.appendChild(hiddenInput);
        }
        form.querySelector('input[name="sortBy"]').value = value;
        showLoadingState();
        form.submit();
    }

    function clearAllFilters() {
        const form = document.getElementById('filterForm');
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);

        // Reset form fields
        form.querySelector('input[name="checkin"]').value = formatDate(today);
        form.querySelector('input[name="checkout"]').value = formatDate(tomorrow);
        form.querySelector('select[name="adults"]').value = '2';
        form.querySelector('select[name="children"]').value = '0';
        form.querySelector('select[name="roomType"]').value = '';

        // Reset price slider
        const priceSlider = document.getElementById('priceSlider');
        if (priceSlider && priceSlider.noUiSlider) {
            const minPrice = parseFloat(priceSlider.dataset.min || '0');
            const maxPrice = parseFloat(priceSlider.dataset.max || '10000000');
            priceSlider.noUiSlider.set([minPrice, maxPrice]);
        }

        showLoadingState();
        form.submit();
    }

    function showLoadingState() {
        const roomList = document.querySelector('.room-list');
        const sortingSection = document.querySelector('.room-sorting');

        if (roomList) {
            roomList.style.opacity = '0.6';
            roomList.style.pointerEvents = 'none';
        }

        if (sortingSection) {
            sortingSection.style.opacity = '0.6';
        }

        // Show loading spinner
        showLoadingSpinner();
    }

    function showLoadingSpinner() {
        const existingSpinner = document.querySelector('.loading-spinner');
        if (existingSpinner) return;

        const spinner = document.createElement('div');
        spinner.className = 'loading-spinner';
        spinner.innerHTML = `
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Đang tải...</span>
            </div>
        `;
        spinner.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            z-index: 9999;
            background: rgba(255, 255, 255, 0.9);
            padding: 20px;
            border-radius: 10px;
            box-shadow: 0 5px 20px rgba(0, 0, 0, 0.2);
        `;

        document.body.appendChild(spinner);

        // Remove spinner after 3 seconds max
        setTimeout(() => {
            const spinnerElement = document.querySelector('.loading-spinner');
            if (spinnerElement) {
                spinnerElement.remove();
            }
        }, 3000);
    }

    function showSkeletonLoading() {
        const roomList = document.querySelector('.room-list');
        if (!roomList) return;

        const skeletonHTML = `
            <div class="room-card-horizontal loading-skeleton">
                <div class="row">
                    <div class="col-md-4">
                        <div class="room-img" style="background: #f0f0f0; height: 250px;"></div>
                    </div>
                    <div class="col-md-8">
                        <div class="room-info p-3">
                            <div style="background: #f0f0f0; height: 24px; width: 70%; margin-bottom: 15px; border-radius: 4px;"></div>
                            <div style="background: #f0f0f0; height: 16px; width: 30%; margin-bottom: 20px; border-radius: 4px;"></div>
                            <div style="background: #f0f0f0; height: 16px; width: 100%; margin-bottom: 10px; border-radius: 4px;"></div>
                            <div style="background: #f0f0f0; height: 16px; width: 80%; margin-bottom: 20px; border-radius: 4px;"></div>
                            <div style="background: #f0f0f0; height: 40px; width: 200px; border-radius: 4px;"></div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        roomList.innerHTML = skeletonHTML.repeat(3);
    }

    // Room card hover effects
    const roomCards = document.querySelectorAll('.room-card-horizontal');
    roomCards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-8px)';
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });

    // Smooth scroll to results after filter
    function scrollToResults() {
        const roomsSection = document.querySelector('.room-list');
        if (roomsSection) {
            roomsSection.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    }

    // Initialize tooltips for better UX
    function initializeTooltips() {
        const tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipElements.forEach(element => {
            new bootstrap.Tooltip(element);
        });
    }

    // Call tooltip initialization
    initializeTooltips();

    // Handle browser back button
    window.addEventListener('popstate', function () {
        location.reload();
    });

    // Auto-save search preferences
    function saveSearchPreferences() {
        const form = document.getElementById('filterForm');
        const formData = new FormData(form);
        const preferences = {};

        for (let [key, value] of formData.entries()) {
            preferences[key] = value;
        }

        localStorage.setItem('roomSearchPreferences', JSON.stringify(preferences));
    }

    // Load saved search preferences
    function loadSearchPreferences() {
        const saved = localStorage.getItem('roomSearchPreferences');
        if (!saved) return;

        try {
            const preferences = JSON.parse(saved);
            const form = document.getElementById('filterForm');

            Object.entries(preferences).forEach(([key, value]) => {
                const input = form.querySelector(`[name="${key}"]`);
                if (input && !input.value) {
                    input.value = value;
                }
            });
        } catch (e) {
            console.warn('Could not load search preferences:', e);
        }
    }

    // Initialize search preferences
    loadSearchPreferences();

    // Save preferences on form change
    const filterForm = document.getElementById('filterForm');
    if (filterForm) {
        filterForm.addEventListener('change', saveSearchPreferences);
    }
});

// Export functions for external use
window.RoomListUtils = {
    updateSort: function (value) {
        // This function is already defined above
        const form = document.getElementById('filterForm');
        const sortInput = form.querySelector('input[name="sortBy"]');
        if (!sortInput) {
            const hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = 'sortBy';
            form.appendChild(hiddenInput);
        }
        form.querySelector('input[name="sortBy"]').value = value;
        form.submit();
    }
};