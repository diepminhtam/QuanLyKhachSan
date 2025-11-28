document.addEventListener('DOMContentLoaded', function () {
    // Password visibility toggle
    const passwordToggles = document.querySelectorAll('.password-toggle');

    passwordToggles.forEach(toggle => {
        toggle.addEventListener('click', function () {
            const passwordField = this.parentElement.querySelector('input');
            const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordField.setAttribute('type', type);

            // Toggle icon
            const icon = this.querySelector('i');
            icon.className = type === 'password' ? 'far fa-eye' : 'far fa-eye-slash';
        });
    });

    // Password strength meter
    const passwordField = document.getElementById('Password');
    const strengthMeter = document.getElementById('password-strength-meter');
    const strengthText = document.getElementById('password-strength-text');

    // Password hint elements
    const hintLength = document.querySelector('.hint-length i');
    const hintUppercase = document.querySelector('.hint-uppercase i');
    const hintLowercase = document.querySelector('.hint-lowercase i');
    const hintNumber = document.querySelector('.hint-number i');
    const hintSpecial = document.querySelector('.hint-special i');

    if (passwordField) {
        passwordField.addEventListener('input', function () {
            const password = this.value;
            let strength = 0;

            // Update password criteria hints
            const hasLength = password.length >= 8;
            const hasUppercase = /[A-Z]/.test(password);
            const hasLowercase = /[a-z]/.test(password);
            const hasNumber = /[0-9]/.test(password);
            const hasSpecial = /[^A-Za-z0-9]/.test(password);

            // Update hint icons
            updateHintIcon(hintLength, hasLength);
            updateHintIcon(hintUppercase, hasUppercase);
            updateHintIcon(hintLowercase, hasLowercase);
            updateHintIcon(hintNumber, hasNumber);
            updateHintIcon(hintSpecial, hasSpecial);

            // Calculate strength
            if (password.length > 0) strength += 1;
            if (password.length >= 8) strength += 1;
            if (hasUppercase) strength += 1;
            if (hasLowercase) strength += 1;
            if (hasNumber) strength += 1;
            if (hasSpecial) strength += 1;

            // Update strength meter
            let strengthPercentage = (strength / 6) * 100;
            let strengthClass = 'bg-danger';
            let strengthLabel = 'Rất yếu';

            if (strength === 0) {
                strengthPercentage = 0;
                strengthLabel = '';
            } else if (strength <= 2) {
                strengthLabel = 'Yếu';
                strengthClass = 'bg-danger';
            } else if (strength <= 4) {
                strengthLabel = 'Trung bình';
                strengthClass = 'bg-warning';
            } else if (strength <= 5) {
                strengthLabel = 'Mạnh';
                strengthClass = 'bg-info';
            } else {
                strengthLabel = 'Rất mạnh';
                strengthClass = 'bg-success';
            }

            strengthMeter.style.width = strengthPercentage + '%';
            strengthMeter.className = `progress-bar ${strengthClass}`;
            strengthText.textContent = strengthLabel;
        });
    }

    // Password confirmation validation
    const confirmPasswordField = document.getElementById('ConfirmPassword');

    if (confirmPasswordField && passwordField) {
        confirmPasswordField.addEventListener('input', function () {
            const password = passwordField.value;
            const confirmPassword = this.value;

            if (password === confirmPassword) {
                this.setCustomValidity('');
            } else {
                this.setCustomValidity('Mật khẩu xác nhận không khớp');
            }
        });
    }

    // Bootstrap form validation
    const forms = document.querySelectorAll('.needs-validation');

    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }

            form.classList.add('was-validated');
        }, false);
    });

    // Helper function to update hint icons
    function updateHintIcon(iconElement, isValid) {
        if (isValid) {
            iconElement.className = 'fas fa-check-circle';
            iconElement.style.color = 'var(--success-color)';
        } else {
            iconElement.className = 'fas fa-times-circle';
            iconElement.style.color = 'var(--danger-color)';
        }
    }

    // Phone number formatting
    const phoneInput = document.getElementById('PhoneNumber');

    if (phoneInput) {
        phoneInput.addEventListener('input', function (e) {
            let value = this.value.replace(/\D/g, '');

            // Format phone number as (XXX) XXX-XXXX or your local format
            if (value.length > 0) {
                if (value.length <= 3) {
                    this.value = value;
                } else if (value.length <= 6) {
                    this.value = value.slice(0, 3) + ' ' + value.slice(3);
                } else if (value.length <= 10) {
                    this.value = value.slice(0, 3) + ' ' + value.slice(3, 6) + ' ' + value.slice(6);
                } else {
                    this.value = value.slice(0, 3) + ' ' + value.slice(3, 6) + ' ' + value.slice(6, 10);
                }
            }
        });
    }
});