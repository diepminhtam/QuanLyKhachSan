document.addEventListener('DOMContentLoaded', function () {
    // Password visibility toggle
    const passwordField = document.getElementById('Password');

    if (passwordField) {
        // Create and append password toggle button
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'password-toggle';
        toggleButton.innerHTML = '<i class="far fa-eye"></i>';
        toggleButton.style.position = 'absolute';
        toggleButton.style.right = '10px';
        toggleButton.style.top = '50%';
        toggleButton.style.transform = 'translateY(-50%)';
        toggleButton.style.border = 'none';
        toggleButton.style.background = 'transparent';
        toggleButton.style.color = '#6c757d';
        toggleButton.style.cursor = 'pointer';

        // Append to parent element (with position relative)
        const parentElement = passwordField.parentElement;
        parentElement.style.position = 'relative';
        parentElement.appendChild(toggleButton);

        // Toggle password visibility
        toggleButton.addEventListener('click', function () {
            const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordField.setAttribute('type', type);

            // Toggle icon
            const icon = toggleButton.querySelector('i');
            icon.className = type === 'password' ? 'far fa-eye' : 'far fa-eye-slash';
        });
    }

    // Simple form validation enhancement
    const loginForm = document.querySelector('.login-form');

    if (loginForm) {
        loginForm.addEventListener('submit', function (event) {
            const emailInput = document.getElementById('Email');
            const passwordInput = document.getElementById('Password');

            let isValid = true;

            // Basic email validation
            if (!validateEmail(emailInput.value)) {
                showError(emailInput, 'Vui lòng nhập email hợp lệ');
                isValid = false;
            } else {
                clearError(emailInput);
            }

            // Password validation
            if (passwordInput.value.length < 6) {
                showError(passwordInput, 'Mật khẩu phải có ít nhất 6 ký tự');
                isValid = false;
            } else {
                clearError(passwordInput);
            }

            if (!isValid) {
                event.preventDefault();
            }
        });
    }

    // Helper functions
    function validateEmail(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(String(email).toLowerCase());
    }

    function showError(input, message) {
        const formGroup = input.closest('.form-floating');
        let errorElement = formGroup.querySelector('.text-danger');

        if (!errorElement) {
            errorElement = document.createElement('span');
            errorElement.className = 'text-danger';
            formGroup.appendChild(errorElement);
        }

        errorElement.textContent = message;
        input.classList.add('is-invalid');
    }

    function clearError(input) {
        const formGroup = input.closest('.form-floating');
        const errorElement = formGroup.querySelector('.text-danger');

        if (errorElement) {
            errorElement.textContent = '';
        }

        input.classList.remove('is-invalid');
    }
});