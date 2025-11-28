document.addEventListener('DOMContentLoaded', function () {
    // Set minimum date for check-in to today
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const formatDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    const checkinInput = document.getElementById('checkin');
    const checkoutInput = document.getElementById('checkout');

    if (checkinInput && checkoutInput) {
        checkinInput.min = formatDate(today);
        checkinInput.value = formatDate(today);

        checkoutInput.min = formatDate(tomorrow);
        checkoutInput.value = formatDate(tomorrow);

        // Update checkout min date when checkin changes
        checkinInput.addEventListener('change', function () {
            const newMinCheckout = new Date(checkinInput.value);
            newMinCheckout.setDate(newMinCheckout.getDate() + 1);
            checkoutInput.min = formatDate(newMinCheckout);

            // If current checkout date is before new minimum, update it
            if (new Date(checkoutInput.value) <= new Date(checkinInput.value)) {
                checkoutInput.value = formatDate(newMinCheckout);
            }
        });
    }

    // Search form submission
    const searchForm = document.getElementById('searchForm');
    if (searchForm) {
        searchForm.addEventListener('submit', function (event) {
            event.preventDefault();

            const checkin = checkinInput.value;
            const checkout = checkoutInput.value;
            const adults = document.getElementById('adults').value;
            const children = document.getElementById('children').value;

            // Redirect to rooms page with search parameters
            window.location.href = `/Home/Rooms?checkin=${checkin}&checkout=${checkout}&adults=${adults}&children=${children}`;
        });
    }

    // Newsletter form submission
    const newsletterForm = document.getElementById('newsletterForm');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', function (event) {
            event.preventDefault();
            const email = this.querySelector('input[type="email"]').value;

            // Here you would typically send this to your backend
            alert(`Cảm ơn bạn đã đăng ký với email: ${email}`);
            this.reset();
        });
    }

    // Animation on scroll
    const animateOnScroll = function () {
        const elements = document.querySelectorAll('.service-item, .room-card, .testimonial-item');

        elements.forEach(element => {
            const position = element.getBoundingClientRect().top;
            const screenHeight = window.innerHeight;

            if (position < screenHeight * 0.9) {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            }
        });
    };

    // Initial setup for animation
    document.querySelectorAll('.service-item, .room-card, .testimonial-item').forEach(element => {
        element.style.opacity = '0';
        element.style.transform = 'translateY(20px)';
        element.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
    });

    // Run on page load and scroll
    animateOnScroll();
    window.addEventListener('scroll', animateOnScroll);
});