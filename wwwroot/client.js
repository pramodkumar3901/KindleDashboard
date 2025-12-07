/* ---- ROTATION FOR KINDLE ---- */
(function () {
  const rot = document.getElementById("rotator");
  function fit() {
    const vw = window.innerWidth, vh = window.innerHeight;
    rot.style.width = vh + "px";
    rot.style.height = vw + "px";
    rot.style.transform = "rotate(-90deg) translate(" + (-vh) + "px,0)";
  }
  fit();
  window.addEventListener("resize", fit);
})();

/* ---- CLOCK ---- */
function updateClock() {
  const now = new Date();
  let h = now.getHours();
  const m = String(now.getMinutes()).padStart(2, '0');
  const ampm = (h >= 12) ? "PM" : "AM";
  h = h % 12 || 12;

  document.getElementById("clock").textContent = h + ":" + m;
  document.getElementById("ampm").textContent = ampm;

  // Date under clock
  document.getElementById("date-text").textContent =
    now.toLocaleDateString(undefined, { weekday: "long", month: "short", day: "numeric" });

  // Top bar date
  const opts = { month: "short", day: "numeric", year: "numeric" };
  document.getElementById("date-display").textContent = now.toLocaleDateString(undefined, opts);
  document.getElementById("day-display").textContent = now.toLocaleDateString(undefined, { weekday: "long" });

  // Clock theme
  const box = document.getElementById("clock-box");
  if (ampm === "PM") { box.classList.remove("am"); box.classList.add("pm"); }
  else { box.classList.remove("pm"); box.classList.add("am"); }
}
updateClock();
setInterval(updateClock, 60000);

/* ---- COMPACT CALENDAR ---- */
function renderCalendar() {
  const now = new Date();
  const y = now.getFullYear(), m = now.getMonth();
  const first = new Date(y, m, 1).getDay();
  const days = new Date(y, m + 1, 0).getDate();

  const tbl = document.getElementById(
