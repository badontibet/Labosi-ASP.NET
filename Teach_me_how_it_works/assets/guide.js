const searchInput = document.getElementById("fileSearch");
const fileNav = document.getElementById("fileNav");

if (searchInput && fileNav) {
  searchInput.addEventListener("input", () => {
    const query = searchInput.value.trim().toLowerCase();
    const groups = fileNav.querySelectorAll(".file-group");

    groups.forEach((group) => {
      let visibleCount = 0;
      group.querySelectorAll(".file-link").forEach((link) => {
        const visible = !query || link.dataset.search.includes(query);
        link.classList.toggle("hidden", !visible);
        if (visible) visibleCount += 1;
      });

      group.classList.toggle("hidden", visibleCount === 0);
      if (query && visibleCount > 0) {
        group.open = true;
      }
    });
  });
}