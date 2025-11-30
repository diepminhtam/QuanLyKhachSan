(function () {
  function toast(msg) {
    alert(msg);
  }

  function fetchList() {
    fetch("/Admin/Promotions/List")
      .then((r) => r.text())
      .then((html) => {
        document.getElementById("promotionsContainer").innerHTML = html;
        attachDelete();
      })
      .catch(() => {});
  }

  function attachDelete() {
    document.querySelectorAll(".delete-promo").forEach((btn) => {
      btn.addEventListener("click", function () {
        const id = this.getAttribute("data-id");
        if (!confirm("Xóa khuyến mãi này?")) return;
        fetch("/Admin/Promotions/Delete/" + id, { method: "POST" })
          .then((r) => r.json())
          .then((j) => {
            if (j.success) {
              toast("Đã xóa");
              fetchList();
            } else toast("Lỗi");
          })
          .catch(() => toast("Lỗi mạng"));
      });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    document
      .getElementById("openCreatePromo")
      ?.addEventListener("click", function () {
        new bootstrap.Modal(document.getElementById("createPromoModal")).show();
      });
    document
      .getElementById("promoForm")
      ?.addEventListener("submit", function (e) {
        e.preventDefault();
        const form = new FormData(this);
        fetch("/Admin/Promotions/Create", { method: "POST", body: form })
          .then((r) => r.json())
          .then((j) => {
            if (j.success) {
              toast(j.message);
              bootstrap.Modal.getInstance(
                document.getElementById("createPromoModal")
              ).hide();
              fetchList();
            } else toast("Lỗi");
          })
          .catch(() => toast("Lỗi mạng"));
      });

    fetchList();
  });
})();
