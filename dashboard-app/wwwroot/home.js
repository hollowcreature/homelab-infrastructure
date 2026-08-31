function updateGreeting() {
  const hour = new Date().getHours();
  let greeting = "Good evening";
  if (hour < 12) greeting = "Good morning";
  else if (hour < 18) greeting = "Good afternoon";

  document.getElementById("greeting").textContent = `${greeting}, YOUR_NAME`;
  document.getElementById("datetime").textContent = new Date().toLocaleString(
    "en-US",
    {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    },
  );
}

function weatherIcon(code) {
  if (code === 0) return "☀️";
  if (code <= 3) return "⛅";
  if (code <= 48) return "🌫️";
  if (code <= 67) return "🌧️";
  if (code <= 77) return "❄️";
  if (code <= 82) return "🌦️";
  return "⛈️";
}

async function fetchWeather(lat, lon) {
  try {
    const weatherResponse = await fetch(
      `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lon}&current=temperature_2m,weather_code`,
    );
    const weatherData = await weatherResponse.json();
    const temp = Math.round(weatherData.current.temperature_2m);
    document.getElementById("weather").textContent = `${temp}°C`;
    document.getElementById("weather-icon").textContent = weatherIcon(
      weatherData.current.weather_code,
    );

    const geoResponse = await fetch(
      `https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${lat}&longitude=${lon}&localityLanguage=en`,
    );
    const geoData = await geoResponse.json();
    document.getElementById("weather-label").textContent =
      geoData.city || geoData.locality || "Your location";
  } catch (error) {
    console.error("Weather fetch failed:", error);
    document.getElementById("weather").textContent = "--";
    document.getElementById("weather-label").textContent = "Unavailable";
  }
}

function loadWeather() {

  // change this to whatever
  const FALLBACK_LAT = 0;
  const FALLBACK_LON = 0;

  if (!navigator.geolocation) {
    fetchWeather(FALLBACK_LAT, FALLBACK_LON);
    return;
  }

  navigator.geolocation.getCurrentPosition(
    (position) => {
      fetchWeather(
        position.coords.latitude,
        position.coords.longitude,
        "Your location",
      );
    },
    () => {
      fetchWeather(FALLBACK_LAT, FALLBACK_LON);
    },
  );
}

function loadWordOfTheDay() {
  const words = [
    { word: "Ephemeral", meaning: "lasting for a very short time" },
    { word: "Ubiquitous", meaning: "present everywhere" },
    {
      word: "Serendipity",
      meaning: "finding something good without looking for it",
    },
    { word: "Resilient", meaning: "able to recover quickly from difficulties" },
    { word: "Meticulous", meaning: "showing great attention to detail" }
  ];

  const dayOfYear = Math.floor(
    (new Date() - new Date(new Date().getFullYear(), 0, 0)) / 86400000,
  );
  const chosen = words[dayOfYear % words.length];

  document.getElementById("word").innerHTML =
    `<span class="font-semibold">${chosen.word}</span> - ${chosen.meaning}`;
}

async function loadQuickServices() {
  try {
    const response = await fetch("services");
    const services = await response.json();
    const container = document.getElementById("quick-services");
    container.innerHTML = "";

    services.forEach((service) => {
      const statusColor =
        service.lastKnownStatus === true
          ? "bg-green-500"
          : service.lastKnownStatus === false
            ? "bg-red-500"
            : "bg-gray-500";

      const tile = document.createElement("a");
      tile.href = service.url;
      tile.target = "_blank";
      tile.className =
        "bg-white/5 backdrop-blur border border-white/10 rounded-lg p-3 hover:bg-white/10 transition flex flex-col items-center gap-1";
      tile.innerHTML = `
                <span class="w-2 h-2 rounded-full ${statusColor}"></span>
                <span class="text-xs text-gray-300 truncate w-full text-center">${service.name}</span>
            `;
      container.appendChild(tile);
    });
  } catch {
  }
}

updateGreeting();
loadWeather();
loadWordOfTheDay();
loadQuickServices();
