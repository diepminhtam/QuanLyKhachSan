// Room Details Page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Initialize components
    initializeDateInputs();
    initializeBookingCalculation();
    initializeFavoriteButton();
    initializeShareButton();
    initializeImageGallery();
    initializeScrollAnimations();

    // Date input initialization
    function initializeDateInputs() {
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);

        const formatDate = (date) => {
            return date.toISOString().split('T')[0];
        };

        const checkinInput = document.getElementById('checkin-date');
        const checkoutInput = document.getElementById('checkout-date');

        if (checkinInput && checkoutInput) {
            // Set minimum dates
            checkinInput.min = formatDate(today);
            checkinInput.value = formatDate(today);
            checkoutInput.min = formatDate(tomorrow);
            checkoutInput.value = formatDate(tomorrow);

            // Update checkout min date when checkin changes
            checkinInput.addEventListener('change', function () {
                const newCheckinDate = new Date(this.value);
                const newMinCheckoutDate = new Date(newCheckinDate);
                newMinCheckoutDate.setDate(newMinCheckoutDate.getDate() + 1);

                checkoutInput.min = formatDate(newMinCheckoutDate);

                // If current checkout date is before new min date, update it
                const currentCheckoutDate = new Date(checkoutInput.value);
                if (currentCheckoutDate <= newCheckinDate) {
                    checkoutInput.value = formatDate(newMinCheckoutDate);
                }

                updateBookingSummary();
            });

            checkoutInput.addEventListener('change', updateBookingSummary);

            // Initial calculation
            updateBookingSummary();
        }
    }

    // Booking calculation
    function initializeBookingCalculation() {
        const guestsSelect = document.getElementById('guests');
        if (guestsSelect) {
            guestsSelect.addEventListener('change', updateBookingSummary);
        }
    }

    function updateBookingSummary() {
        const checkinInput = document.getElementById('checkin-date');
        const checkoutInput = document.getElementById('checkout-date');

        if (!checkinInput || !checkoutInput || !checkinInput.value || !checkoutInput.value) {
            return;
        }

        const checkin = new Date(checkinInput.value);
        const checkout = new Date(checkoutInput.value);
        const diffTime = Math.abs(checkout - checkin);
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

        // Get base price from element
        const priceElement = document.querySelector('.current-price');
        if (!priceElement) return;

        const priceText = priceElement.textContent;
        const price = parseInt(priceText.replace(/[^\d]/g, ''));

        if (isNaN(price)) return;

        // Update calculations
        updatePriceDisplay(price, diffDays);
    }

    function updatePriceDisplay(price, nights) {
        const nightsElement = document.getElementById('nights-count');
        const roomTotalElement = document.getElementById('room-total-price');
        const totalPriceElement = document.getElementById('total-price');

        if (nightsElement) nightsElement.textContent = nights;

        const roomTotal = price * nights;

        if (roomTotalElement) roomTotalElement.textContent = formatCurrency(roomTotal);
        if (totalPriceElement) totalPriceElement.textContent = formatCurrency(roomTotal);
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'decimal',
            maximumFractionDigits: 0
        }).format(value) + ' VNĐ';
    }

    // Favorite button functionality
    function initializeFavoriteButton() {
        const favoriteBtn = document.querySelector('.favorite-btn');
        if (favoriteBtn) {
            favoriteBtn.addEventListener('click', function () {
                const icon = this.querySelector('i');
                const roomId = this.getAttribute('data-room-id');

                if (icon.classList.contains('far')) {
                    icon.classList.remove('far');
                    icon.classList.add('fas');
                    showNotification('Đã thêm phòng vào danh sách yêu thích', 'success');
                } else {
                    icon.classList.remove('fas');
                    icon.classList.add('far');
                    showNotification('Đã xóa phòng khỏi danh sách yêu thích', 'info');
                }

                // Here you would send an AJAX request to update favorites
                console.log('Toggle favorite for room:', roomId);
            });
        }
    }

    // Share button functionality
    function initializeShareButton() {
        const shareBtn = document.querySelector('.share-btn');
        if (shareBtn) {
            shareBtn.addEventListener('click', function () {
                const shareModal = new bootstrap.Modal(document.getElementById('shareModal'));
                shareModal.show();
            });
        }

        // Copy link functionality
        const copyLinkBtn = document.getElementById('copy-link-btn');
        if (copyLinkBtn) {
            copyLinkBtn.addEventListener('click', function () {
                const shareLink = document.getElementById('share-link');

                // Modern clipboard API
                if (navigator.clipboard) {
                    navigator.clipboard.writeText(shareLink.value).then(() => {
                        showCopySuccess(this);
                    });
                } else {
                    // Fallback for older browsers
                    shareLink.select();
                    document.execCommand('copy');
                    showCopySuccess(this);
                }
            });
        }
    }

    function showCopySuccess(button) {
        const originalText = button.textContent;
        button.textContent = 'Đã sao chép!';
        button.classList.add('btn-success');
        button.classList.remove('btn-outline-secondary');

        setTimeout(() => {
            button.textContent = originalText;
            button.classList.remove('btn-success');
            button.classList.add('btn-outline-secondary');
        }, 2000);
    }

    // Image gallery functionality
    function initializeImageGallery() {
        const thumbnails = document.querySelectorAll('.thumbnail img');
        thumbnails.forEach(thumb => {
            thumb.addEventListener('click', function () {
                const imageUrl = this.src;
                changeMainImage(imageUrl);
            });
        });
    }

    // Scroll animations
    function initializeScrollAnimations() {
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        }, observerOptions);

        // Observe elements for animation
        const animatedElements = document.querySelectorAll('.room-description, .room-features-section, .room-specs, .room-policies, .room-reviews');
        animatedElements.forEach(el => {
            el.style.opacity = '0';
            el.style.transform = 'translateY(20px)';
            el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
            observer.observe(el);
        });
    }

    // Booking form submission
    const bookingForm = document.getElementById('booking-form');
    if (bookingForm) {
        bookingForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const checkin = document.getElementById('checkin-date').value;
            const checkout = document.getElementById('checkout-date').value;
            const guests = document.getElementById('guests').value;

            // Get room ID from URL or data attribute
            const urlParams = new URLSearchParams(window.location.search);
            const roomId = urlParams.get('id') || window.location.pathname.split('/').pop();

            // Show loading state
            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn.textContent;
            submitBtn.textContent = 'Đang xử lý...';
            submitBtn.disabled = true;

            // Simulate processing time
            setTimeout(() => {
                // Redirect to booking page with parameters
                window.location.href = `/Bookings/Create?roomId=${roomId}&checkin=${checkin}&checkout=${checkout}&guests=${guests}`;
            }, 1000);
        });
    }

    // Notification system
    function showNotification(message, type = 'success') {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.custom-notification');
        existingNotifications.forEach(notification => notification.remove());

        // Create notification element
        const notification = document.createElement('div');
        notification.className = `custom-notification alert alert-${type}`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            max-width: 300px;
            padding: 15px 20px;
            border-radius: 10px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.2);
            transform: translateX(100%);
            transition: transform 0.3s ease;
        `;

        notification.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="fas fa-${type === 'success' ? 'check-circle' : 'info-circle'} me-2"></i>
                <span>${message}</span>
                <button type="button" class="btn-close btn-close-white ms-auto" onclick="this.parentElement.parentElement.remove()"></button>
            </div>
        `;

        document.body.appendChild(notification);

        // Show notification
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);

        // Auto remove after 4 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.style.transform = 'translateX(100%)';
                setTimeout(() => {
                    notification.remove();
                }, 300);
            }
        }, 4000);
    }

    // Initialize tooltips for accessibility
    const tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipElements.forEach(element => {
        new bootstrap.Tooltip(element);
    });

    // Smooth scrolling for internal links
    const internalLinks = document.querySelectorAll('a[href^="#"]');
    internalLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);

            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Lazy loading for images
    const lazyImages = document.querySelectorAll('img[data-src]');
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    observer.unobserve(img);
                }
            });
        });

        lazyImages.forEach(img => imageObserver.observe(img));
    } else {
        // Fallback for browsers without IntersectionObserver
        lazyImages.forEach(img => {
            img.src = img.dataset.src;
        });
    }
});

// Global function for changing main image (called from inline onclick)
function changeMainImage(imageUrl) {
    const mainImage = document.getElementById('main-room-image');
    if (mainImage) {
        // Add loading effect
        mainImage.style.opacity = '0.5';

        setTimeout(() => {
            mainImage.src = imageUrl;
            mainImage.style.opacity = '1';
        }, 150);
    }

    // Update active thumbnail
    document.querySelectorAll('.thumbnail').forEach(thumb => {
        thumb.classList.remove('active');
    });

    const clickedThumbnail = document.querySelector(`.thumbnail img[src='${imageUrl}']`);
    if (clickedThumbnail) {
        clickedThumbnail.parentElement.classList.add('active');
    }
}

// Handle browser back button
window.addEventListener('popstate', function () {
    // You can add custom handling here if needed
});

// Export utility functions for external use
window.RoomDetailsUtils = {
    updateBookingSummary: function () {
        // Trigger booking summary update
        const event = new Event('change');
        const checkinInput = document.getElementById('checkin-date');
        if (checkinInput) checkinInput.dispatchEvent(event);
    },

    showNotification: function (message, type) {
        // External access to notification system
        showNotification(message, type);
    }
};