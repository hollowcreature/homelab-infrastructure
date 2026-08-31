async function loadServices() {
  const response = await fetch("services");
  const services = await response.json();

  const container = document.getElementById("services");
  container.innerHTML = "";

  services.forEach((service) => {
    const card = document.createElement("div");
    card.className = "bg-gray-800 rounded-lg p-4 shadow";

    const statusColor =
      service.lastKnownStatus === true
        ? "bg-green-500"
        : service.lastKnownStatus === false
          ? "bg-red-500"
          : "bg-gray-500";

    console.log(service);
    card.innerHTML = `
        <div class="flex items-center justify-between mb-2">
            <h2 class="text-xl font-semibold">${service.name}</h2>
            <span class="w-3 h-3 rounded-full ${statusColor}"></span>
        </div>
        <p class="text-gray-400 text-sm mb-3">${service.description ?? ""}</p>
        <a href="${service.url}" target="_blank" class="text-blue-400 hover:underline text-sm">${service.url}</a>
        <div class="mt-3 text-xs text-gray-500">
            ${service.lastCheckedAt ? `Checked ${timeAgo(service.lastCheckedAt)}` : "Not checked yet"}
            ${service.lastResponseTimeMs != null ? ` · ${service.lastResponseTimeMs}ms` : ""}
            ${service.uptimePercent24h != null ? ` · ${service.uptimePercent24h}% uptime (24h)` : ""}
        </div>
        
        <button data-id="${service.id}" data-name="${service.name}" class="delete-btn mt-3 text-xs text-red-400 hover:text-red-300"> Delete </button>
    `;

    container.appendChild(card);
  });

  document.querySelectorAll(".delete-btn").forEach((button) => {
    button.addEventListener("click", async () => {
      const confirmed = confirm(`Delete "${button.dataset.name}"?`);
      if (!confirmed) return;

      const id = button.dataset.id;
      await fetch(`services/${id}`, { method: "DELETE" });
      loadServices();
    });
  });
}

function timeAgo(dateString) {
  const seconds = Math.floor((new Date() - new Date(dateString)) / 1000);
  if (seconds < 60) return `${seconds}s ago`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ago`;
}

document
  .getElementById("add-service-form")
  .addEventListener("submit", async (event) => {
    event.preventDefault();

    const name = document.getElementById("input-name").value;
    const url = document.getElementById("input-url").value;
    const description = document.getElementById("input-description").value;

    await fetch("services", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, url, description }),
    });

    event.target.reset();
    loadServices();
  });

loadServices();
setInterval(loadServices, 30000);
