// Profile page functionality
document.addEventListener("DOMContentLoaded", function () {
  // Edit profile functionality
  const editProfileBtn = document.getElementById("edit-profile-btn");
  const cancelEditBtn = document.getElementById("cancel-edit-btn");
  const profileForm = document.getElementById("profile-form");
  const actionButtons = document.querySelector(".profile-action-buttons");

  if (editProfileBtn) {
    editProfileBtn.addEventListener("click", function () {
      enableFormEditing();
    });
  }

  if (cancelEditBtn) {
    cancelEditBtn.addEventListener("click", function () {
      disableFormEditing();
    });
  }

  function enableFormEditing() {
    if (!profileForm) return;
    const inputs = profileForm.querySelectorAll("input, select");
    inputs.forEach((input) => {
      if (input.id !== "email") {
        // Don't enable email field
        input.disabled = false;
      }
    });
    if (actionButtons) actionButtons.style.display = "flex";
    if (editProfileBtn) editProfileBtn.style.display = "none";
  }

  function disableFormEditing() {
    if (!profileForm) return;
    const inputs = profileForm.querySelectorAll("input, select");
    inputs.forEach((input) => {
      input.disabled = true;
    });
    if (actionButtons) actionButtons.style.display = "none";
    if (editProfileBtn) editProfileBtn.style.display = "block";
  }

  // Password toggle functionality
  const passwordToggles = document.querySelectorAll(".password-toggle");
  passwordToggles.forEach((toggle) => {
    toggle.addEventListener("click", function () {
      const input = this.parentElement.querySelector("input");
      const icon = this.querySelector("i");

      if (input.type === "password") {
        input.type = "text";
        icon.classList.remove("fa-eye");
        icon.classList.add("fa-eye-slash");
      } else {
        input.type = "password";
        icon.classList.remove("fa-eye-slash");
        icon.classList.add("fa-eye");
      }
    });
  });

  // Password strength meter
  const newPasswordInput = document.getElementById("newPassword");
  if (newPasswordInput) {
    newPasswordInput.addEventListener("input", function () {
      updatePasswordStrength(this.value);
    });
  }

  function updatePasswordStrength(password) {
    const meter = document.getElementById("password-strength-meter");
    const text = document.getElementById("password-strength-text");

    let strength = 0;
    let feedback = "";

    if (password.length >= 8) strength += 25;
    if (/[A-Z]/.test(password)) strength += 25;
    if (/[0-9]/.test(password)) strength += 25;
    if (/[^A-Za-z0-9]/.test(password)) strength += 25;

    meter.style.width = strength + "%";

    if (strength < 50) {
      meter.className = "progress-bar bg-danger";
      feedback = "Mật khẩu yếu";
    } else if (strength < 75) {
      meter.className = "progress-bar bg-warning";
      feedback = "Mật khẩu trung bình";
    } else {
      meter.className = "progress-bar bg-success";
      feedback = "Mật khẩu mạnh";
    }

    text.textContent = feedback;
  }

  // Review functionality
  const addReviewBtns = document.querySelectorAll(".add-review-btn");
  const reviewModal = new bootstrap.Modal(
    document.getElementById("add-review-modal")
  );
  const ratingStars = document.querySelectorAll(".rating-input i");
  const ratingValue = document.getElementById("rating-value");
  const submitReviewBtn = document.getElementById("submit-review");

  addReviewBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      const bookingId = this.getAttribute("data-id");
      const roomName = this.getAttribute("data-room");

      document.getElementById("booking-id").value = bookingId;
      document.getElementById("review-room-name").value = roomName;

      // Reset rating
      ratingStars.forEach((star) => (star.className = "far fa-star"));
      ratingValue.value = "";
      document.getElementById("review-comment").value = "";

      reviewModal.show();
    });
  });

  ratingStars.forEach((star) => {
    star.addEventListener("click", function () {
      const rating = parseInt(this.getAttribute("data-rating"));
      ratingValue.value = rating;

      ratingStars.forEach((s, index) => {
        if (index < rating) {
          s.className = "fas fa-star";
        } else {
          s.className = "far fa-star";
        }
      });
    });
  });

  // Form submissions
  // Let the form submit normally so server-side binding + antiforgery works
  // Keep client-side validation if needed
  if (profileForm) {
    // Keep default submit behavior (no preventDefault)
  }

  const changePasswordForm = document.getElementById("change-password-form");
  if (changePasswordForm) {
    changePasswordForm.addEventListener("submit", function (e) {
      e.preventDefault();
      // Handle password change
      changePassword();
    });
  }

  // Booking filters
  const statusFilter = document.getElementById("booking-status-filter");
  const dateFrom = document.getElementById("booking-date-from");
  const dateTo = document.getElementById("booking-date-to");

  if (statusFilter) {
    statusFilter.addEventListener("change", filterBookings);
  }
  if (dateFrom) {
    dateFrom.addEventListener("change", filterBookings);
  }
  if (dateTo) {
    dateTo.addEventListener("change", filterBookings);
  }

  function filterBookings() {
    const status = statusFilter.value;
    const fromDate = dateFrom.value;
    const toDate = dateTo.value;

    // Implement filtering logic here
    console.log("Filtering bookings:", { status, fromDate, toDate });
  }

  // Remove favorite from profile favorites list (updates cookie)
  document.addEventListener("click", function (e) {
    var el = e.target.closest(".remove-favorite-btn");
    if (!el) return;
    var id = el.getAttribute("data-id") || el.getAttribute("data-room-id");
    if (!id) return;
    // Read cookie
    try {
      const current =
        (document.cookie.match("(^|;)\\s*favorites\\s*=\\s*([^;]+)") ||
          [])[2] || "";
      const ids = current
        .split(",")
        .filter((s) => s)
        .map((s) => parseInt(s, 10));
      const newIds = ids.filter((i) => i !== parseInt(id, 10));
      document.cookie = "favorites=" + newIds.join(",") + "; path=/";
      // Remove DOM element
      const card =
        el.closest(".col-md-6") ||
        el.closest(".favorite-room-item") ||
        el.closest(".list-group-item");
      if (card) card.remove();
      RoomDetailsUtils &&
        RoomDetailsUtils.showNotification("Đã xóa khỏi yêu thích", "info");
    } catch (e) {
      console.warn(e);
    }
  });
});

// API calls
async function updateProfile() {
  const formData = {
    firstName: document.getElementById("firstName").value,
    lastName: document.getElementById("lastName").value,
    phone: document.getElementById("phone").value,
    birthdate: document.getElementById("birthdate").value,
    gender: document.getElementById("gender").value,
    address: document.getElementById("address").value,
    city: document.getElementById("city").value,
    state: document.getElementById("state").value,
    zipCode: document.getElementById("zipCode").value,
  };

  try {
    const response = await fetch("/Account/UpdateProfile", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(formData),
    });

    if (response.ok) {
      showAlert("Cập nhật thông tin thành công!", "success");
      location.reload();
    } else {
      showAlert("Có lỗi xảy ra khi cập nhật thông tin", "error");
    }
  } catch (error) {
    console.error("Error updating profile:", error);
    showAlert("Có lỗi xảy ra khi cập nhật thông tin", "error");
  }
}

function showAlert(message, type) {
  // Implement alert display
  alert(message);
}
