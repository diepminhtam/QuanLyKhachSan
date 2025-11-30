// Admin Users Page Script
document.addEventListener("DOMContentLoaded", function () {
  // Select all checkboxes
  const selectAllCheckbox = document.getElementById("selectAll");
  const userCheckboxes = document.querySelectorAll("tbody .form-check-input");
  const bulkActionsBar = document.querySelector(".bulk-actions-bar");
  const selectedCountEl = document.querySelector(".selected-count");
  const clearSelectionBtn = document.getElementById("clearSelection");
  const viewUserBtns = document.querySelectorAll(".view-user");
  const editUserBtns = document.querySelectorAll(".edit-user");
  const lockUserBtns = document.querySelectorAll(".lock-user");
  const unlockUserBtns = document.querySelectorAll(".unlock-user");
  const verifyUserBtns = document.querySelectorAll(".verify-user");
  const editUserBtn = document.getElementById("editUserBtn");
  const saveUserBtn = document.getElementById("saveUserBtn");
  const addUserModal = document.getElementById("addUserModal");
  const userDetailModal = document.getElementById("userDetailModal");
  const bulkActionBtns = document.querySelectorAll(
    "#bulkVerify, #bulkActivate, #bulkDeactivate"
  );
  const confirmationModal = document.getElementById("confirmationModal");
  const confirmActionBtn = document.getElementById("confirmActionBtn");
  const togglePasswordBtns = document.querySelectorAll(".toggle-password");

  // User search form
  const userSearchForm = document.getElementById("userSearchForm");

  // Handle select all checkbox
  if (selectAllCheckbox) {
    selectAllCheckbox.addEventListener("change", function () {
      userCheckboxes.forEach((checkbox) => {
        checkbox.checked = this.checked;
      });
      updateBulkActionsBar();
    });
  }

  // Handle individual checkbox changes
  userCheckboxes.forEach((checkbox) => {
    checkbox.addEventListener("change", function () {
      updateBulkActionsBar();

      // Update "select all" checkbox state
      if (selectAllCheckbox) {
        const allChecked = [...userCheckboxes].every((cb) => cb.checked);
        const someChecked = [...userCheckboxes].some((cb) => cb.checked);

        selectAllCheckbox.checked = allChecked;
        selectAllCheckbox.indeterminate = someChecked && !allChecked;
      }
    });
  });

  // Update bulk actions bar visibility
  function updateBulkActionsBar() {
    const selectedCount = [...userCheckboxes].filter((cb) => cb.checked).length;

    if (selectedCount > 0) {
      bulkActionsBar.style.display = "block";
      selectedCountEl.textContent = selectedCount;
    } else {
      bulkActionsBar.style.display = "none";
    }
  }

  // Clear selection
  if (clearSelectionBtn) {
    clearSelectionBtn.addEventListener("click", function () {
      userCheckboxes.forEach((checkbox) => {
        checkbox.checked = false;
      });
      if (selectAllCheckbox) {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = false;
      }
      updateBulkActionsBar();
    });
  }

  // Toggle password visibility
  togglePasswordBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      const input = this.previousElementSibling;
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

  // View user details
  viewUserBtns.forEach((btn) => {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      const userId = this.getAttribute("data-user-id");

      // In a real application, you would fetch user data from the server
      // For now, we'll use the demo data that's already in the modal

      // Show the modal
      const userDetailModalInstance = new bootstrap.Modal(userDetailModal);
      userDetailModalInstance.show();
    });
  });

  // Edit user from detail modal
  if (editUserBtn) {
    editUserBtn.addEventListener("click", function () {
      // Close detail modal
      bootstrap.Modal.getInstance(userDetailModal).hide();

      // In a real application, you would populate the form with user data

      // Show edit modal
      document.getElementById("addUserModalLabel").textContent =
        "Chỉnh sửa thông tin khách hàng";
      const addUserModalInstance = new bootstrap.Modal(addUserModal);
      addUserModalInstance.show();
    });
  }

  // Edit user from table
  editUserBtns.forEach((btn) => {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      const userId = this.getAttribute("data-user-id");

      // In a real application, you would fetch user data and populate the form

      // Show edit modal
      document.getElementById("addUserModalLabel").textContent =
        "Chỉnh sửa thông tin khách hàng";
      const addUserModalInstance = new bootstrap.Modal(addUserModal);
      addUserModalInstance.show();
    });
  });

  // Save user (add/edit)
  if (saveUserBtn) {
    saveUserBtn.addEventListener("click", function () {
      const form = document.getElementById("userForm");

      // Basic form validation
      if (!form.checkValidity()) {
        form.reportValidity();
        return;
      }

      // Check if passwords match for new users
      const password = document.getElementById("password").value;
      const confirmPassword = document.getElementById("confirmPassword").value;

      if (password !== confirmPassword) {
        alert("Mật khẩu và xác nhận mật khẩu không khớp");
        return;
      }

      // In a real application, you would send the form data to the server

      // Show success message and close modal
      alert("Lưu thông tin thành công!");
      bootstrap.Modal.getInstance(addUserModal).hide();

      // Reset form for next use
      form.reset();
    });
  }

  // Handle lock/unlock/verify user
  function setupActionBtns(btns, action, message, warning, btnClass) {
    btns.forEach((btn) => {
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        const userId = this.getAttribute("data-user-id");

        // Setup confirmation modal
        document.getElementById("confirmationMessage").textContent = message;
        document.getElementById("warningText").textContent = warning;
        document.getElementById("warningMessage").style.display = warning
          ? "block"
          : "none";
        confirmActionBtn.className = `btn ${btnClass}`;

        // Store action data
        confirmActionBtn.setAttribute("data-action", action);
        confirmActionBtn.setAttribute("data-user-id", userId);

        // Show confirmation modal
        const confirmationModalInstance = new bootstrap.Modal(
          confirmationModal
        );
        confirmationModalInstance.show();
      });
    });
  }

  // Setup action buttons
  setupActionBtns(
    lockUserBtns,
    "lock",
    "Bạn có chắc chắn muốn khóa tài khoản này?",
    "Khóa tài khoản sẽ ngăn người dùng đăng nhập và sử dụng dịch vụ.",
    "btn-danger"
  );

  setupActionBtns(
    unlockUserBtns,
    "unlock",
    "Bạn có chắc chắn muốn mở khóa tài khoản này?",
    "",
    "btn-success"
  );

  setupActionBtns(
    verifyUserBtns,
    "verify",
    "Bạn có chắc chắn muốn xác thực tài khoản này?",
    "",
    "btn-primary"
  );

  // Handle bulk actions
  bulkActionBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      const action = this.id.replace("bulk", "").toLowerCase();
      let message, warning, btnClass;

      const selectedCount = [...userCheckboxes].filter(
        (cb) => cb.checked
      ).length;

      switch (action) {
        case "verify":
          message = `Bạn có chắc chắn muốn xác thực ${selectedCount} tài khoản đã chọn?`;
          warning = "";
          btnClass = "btn-primary";
          break;
        case "activate":
          message = `Bạn có chắc chắn muốn mở khóa ${selectedCount} tài khoản đã chọn?`;
          warning = "";
          btnClass = "btn-success";
          break;
        case "deactivate":
          message = `Bạn có chắc chắn muốn khóa ${selectedCount} tài khoản đã chọn?`;
          warning =
            "Khóa tài khoản sẽ ngăn người dùng đăng nhập và sử dụng dịch vụ.";
          btnClass = "btn-danger";
          break;
      }

      // Setup confirmation modal
      document.getElementById("confirmationMessage").textContent = message;
      document.getElementById("warningText").textContent = warning;
      document.getElementById("warningMessage").style.display = warning
        ? "block"
        : "none";
      confirmActionBtn.className = `btn ${btnClass}`;

      // Store action data
      confirmActionBtn.setAttribute("data-action", action);
      confirmActionBtn.setAttribute("data-bulk", "true");

      // Show confirmation modal
      const confirmationModalInstance = new bootstrap.Modal(confirmationModal);
      confirmationModalInstance.show();
    });
  });

  // Handle confirmation action
  if (confirmActionBtn) {
    confirmActionBtn.addEventListener("click", function () {
      const action = this.getAttribute("data-action");
      const isBulk = this.getAttribute("data-bulk") === "true";
      const userId = this.getAttribute("data-user-id");

      // In a real application, you would send the request to the server

      if (isBulk) {
        // Get selected user IDs (use checkbox values)
        const selectedUserIds = [...userCheckboxes]
          .filter((cb) => cb.checked)
          .map((cb) => cb.value);

        console.log(`Performing bulk ${action} on users:`, selectedUserIds);
      } else {
        console.log(`Performing ${action} on user:`, userId);
      }

      // Show success message
      alert("Thao tác thành công!");

      // Close modal
      bootstrap.Modal.getInstance(confirmationModal).hide();

      // Clear selection for bulk actions
      if (isBulk) {
        clearSelectionBtn.click();
      }

      // In a real application, you would refresh the data or update the UI
    });
  }

  // Search form submission: allow GET submissions to proceed, intercept only for POST/AJAX
  if (userSearchForm) {
    userSearchForm.addEventListener("submit", function (e) {
      if (this.method && this.method.toLowerCase() === "get") return; // allow normal submit
      e.preventDefault();
      const formData = new FormData(this);
      const searchParams = {};
      for (let [key, value] of formData.entries()) {
        if (value && String(value).trim()) searchParams[key] = value;
      }
      console.log("AJAX search params:", searchParams);
      alert("Tìm kiếm (demo AJAX)");
    });
  }

  // Page navigation links (server-rendered pager)
  document.querySelectorAll(".page-link-nav").forEach((a) => {
    a.addEventListener("click", function (e) {
      e.preventDefault();
      const page = this.getAttribute("data-page");
      const pageInput = document.getElementById("pageInput");
      const form =
        document.getElementById("userSearchForm") ||
        document.getElementById("filterForm");
      if (pageInput) pageInput.value = page;
      if (form) form.submit();
    });
  });
});
