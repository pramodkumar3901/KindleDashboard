/* CLOCK */
function updateClock() {
    const now = new Date();
    let h = now.getHours();
    const m = String(now.getMinutes()).padStart(2, '0');
    const ampm = h >= 12 ? "PM" : "AM";
    h = h % 12 || 12;

    document.getElementById("clock").textContent = h + ":" + m;
    document.getElementById("dateText").textContent =
        now.toLocaleDateString(undefined, { weekday: "long", month: "short", day: "numeric" });

    const cb = document.getElementById("clockBox");
    cb.classList.toggle("pm", ampm === "PM");
    cb.classList.toggle("am", ampm === "AM");
}
updateClock();
setInterval(updateClock, 60000);

// Add navigation to clock.html
document.getElementById("clockBox").style.cursor = "pointer";
document.getElementById("clockBox").onclick = () => {
    window.location.href = "clock.html";
};

// Add navigation to calendar.html
const calBox = document.querySelector(".cal-box");
if (calBox) {
    calBox.style.cursor = "pointer";
    calBox.onclick = () => {
        window.location.href = "calendar.html";
    };
}

/* CALENDAR */
function renderCalendar() {
    const now = new Date();
    const y = now.getFullYear(), m = now.getMonth();
    const first = new Date(y, m, 1).getDay();
    const days = new Date(y, m + 1, 0).getDate();
    const tbl = document.getElementById("calendar");
    tbl.innerHTML = "";

    const head = document.createElement("tr");
    ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].forEach(d => {
        const th = document.createElement("th");
        th.textContent = d; head.appendChild(th);
    });
    tbl.appendChild(head);

    let row = document.createElement("tr");
    for (let i = 0; i < first; i++) row.appendChild(document.createElement("td"));

    for (let d = 1; d <= days; d++) {
        if (row.children.length === 7) {
            tbl.appendChild(row); row = document.createElement("tr");
        }
        const cell = document.createElement("td");
        cell.textContent = d;
        if (d === now.getDate()) cell.classList.add("today");
        row.appendChild(cell);
    }
    if (row.children.length) tbl.appendChild(row);
}
renderCalendar();

/* BRIGHTNESS POPUP */
let brightness = 0;
const brightBtn = document.getElementById("brightBtn");
const brightValue = document.getElementById("brightValue");
const popup = document.getElementById("popup-bg");
const slider = document.getElementById("brightnessSlider");
const textBox = document.getElementById("brightnessText");

function updateBrightness() {
    brightValue.textContent = brightness;

    if (brightness > 0) {
        brightBtn.classList.add("active");
    } else {
        brightBtn.classList.remove("active");
    }
}

brightBtn.onclick = () => {
    slider.value = brightness;
    textBox.value = brightness;
    popup.style.display = "flex";
};

document.getElementById("minus").onclick = () => {
    brightness = Math.max(0, brightness - 1);
    slider.value = brightness; textBox.value = brightness;
};
document.getElementById("plus").onclick = () => {
    brightness = Math.min(100, brightness + 1);
    slider.value = brightness; textBox.value = brightness;
};
slider.oninput = () => {
    brightness = parseInt(slider.value);
    textBox.value = brightness;
};
textBox.oninput = () => {
    const v = parseInt(textBox.value);
    if (!isNaN(v)) {
        brightness = Math.max(0, Math.min(100, v));
        slider.value = brightness;
    }
};
document.getElementById("setBtn").onclick = async () => {
    await fetch(`/api/brightness/set?value=${brightness}`);
    updateBrightness();
    popup.style.display = "none";
};
popup.onclick = e => {
    if (e.target === popup) popup.style.display = "none";
};
document.getElementById("offBtn").onclick = async () => {
    brightness = 0;
    slider.value = 0;
    textBox.value = 0;

    await fetch(`/api/brightness/set?value=0`);

    updateBrightness();
    popup.style.display = "none";
};

/* DAILY GOLD VALUE */
async function fetchGold() {
    try {
        const res = await fetch('/api/gold');
        const data = await res.json();

        let rows = "";
        const tableObj = JSON.parse(data.tableValue);
        for (const [date, price] of Object.entries(tableObj)) {

            rows += `
                                <tr>
                                <td>₹${price}</td>
                                  <td>${date}</td>
                                </tr>
                                `;
        }

        // Reverse order: latest first (optional)
        rows = rows.split("\n").reverse().join("\n");

        //document.getElementById("goldBox").innerHTML = `
        //              <table class="unstyledTable">

        //                     ${rows}
        //              </tbody>
        //              </tr>
        //              </table>
        //            `;


        //document.getElementById("goldBox").innerHTML =
        //  `<img src="data:image/png;base64,${data.graph}" style="width:100%;" />`;

        document.getElementById("goldBox2").innerHTML =
            `<img src="data:image/png;base64,${data.graph}" style="width:100%;" />`;

    } catch (e) {
        console.error("Error fetching gold data:", e);
    }
}

document.getElementById("btnReload").onclick = async () => {
    const overlay = document.getElementById("fullRefreshOverlay");

    overlay.style.display = "block";    // screen turns fully black
    setTimeout(() => {
        overlay.style.display = "none"; // return to normal
    }, 800);  // 300 ms is ideal for Kindle refresh
};

document.getElementById("btnPrinter").onclick = async () => {
    const btnPrinter = document.getElementById("btnPrinter");
    btnPrinter.classList.toggle("active");
};

async function fetchStatusDump() {
    try {
        const res = await fetch('/api/getStatusDump');
        const data = await res.json();

        const statusObj = JSON.parse(data.status);

        document.getElementById("wifiSsid").textContent = statusObj.ssid || "Not Connected";
        document.getElementById("ip").textContent = statusObj.ip_address || "—";
        document.getElementById("batt").textContent = statusObj.battery + "%" || "—";
        document.getElementById("screensaver").textContent = statusObj.screensaver == 0 ? "ON" : "OFF" || "—";

        brightness = statusObj.brightness || 0;
        updateBrightness();

        const btnPrinter = document.getElementById("btnPrinter");
        if (data.printerOnline) {
            btnPrinter.classList.add("active");
        } else {
            btnPrinter.classList.remove("active");
        }

    } catch (e) {
        //document.getElementById("goldBox").textContent = e;
    }
};

fetchStatusDump();
setInterval(fetchStatusDump, 10 * 1000);   // 10 sec
fetchGold();
