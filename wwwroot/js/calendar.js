let currentDate = new Date();
let eventsToRender = [];
let isListView = false;

// 1. Fetch Events
async function fetchEvents() {
    try {
        const res = await fetch('/api/calendar/events');
        if (res.ok) {
            eventsToRender = await res.json();
            console.log("Events loaded:", eventsToRender);
            renderCalendar();
            renderListView();
        }
    } catch (e) {
        console.error("Failed to load events", e);
    }
}

// 2. Render Calendar
function renderCalendar() {
    const y = currentDate.getFullYear();
    const m = currentDate.getMonth();
    const firstDayIndex = new Date(y, m, 1).getDay(); // 0 = Sunday
    const daysInMonth = new Date(y, m + 1, 0).getDate();

    // Update Header
    const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    document.getElementById("monthYear").textContent = `${monthNames[m]} ${y}`;

    // Clear Grid (keep headers)
    const grid = document.getElementById("calendarGrid");
    // Remove all children that are NOT .day-name
    const existingDays = grid.querySelectorAll('.day-cell');
    existingDays.forEach(cell => cell.remove());

    const now = new Date();
    const isTodayMonth = now.getFullYear() === y && now.getMonth() === m;
    const todayDate = now.getDate();

    // Empty cells
    for (let i = 0; i < firstDayIndex; i++) {
        const div = document.createElement("div");
        div.classList.add("day-cell", "empty");
        if (i === 0) div.classList.add("weekend");
        grid.appendChild(div);
    }

    // Days
    for (let d = 1; d <= daysInMonth; d++) {
        const div = document.createElement("div");
        div.classList.add("day-cell");
        div.textContent = d;

        // Current day of week for this date in loop
        const currentDayObj = new Date(y, m, d);
        const dayOfWeek = currentDayObj.getDay();
        if (dayOfWeek === 0 || dayOfWeek === 6) {
            div.classList.add("weekend");
        }

        if (isTodayMonth && d === todayDate) {
            div.classList.add("today");
        }

        // --- Event Logic ---
        // Format date as YYYY-MM-DD to match CSV
        // Note: Months in JS are 0-indexed, so +1. Padding needed.
        const dateStr = `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;

        // Find events for this day
        // Could be multiple, but for now we take the first or specific logic
        const dayEvents = eventsToRender.filter(e => e.date === dateStr);

        if (dayEvents.length > 0) {
            const evt = dayEvents[0]; // Take first
            // Add style class based on type
            // Lowercase type for class matching: Holiday -> evt-holiday
            const typeClass = "evt-" + evt.type.toLowerCase();
            div.classList.add(typeClass);

            // Add Click Handler
            div.onclick = () => {
                showEventDetails(evt);
            };
        }

        grid.appendChild(div);
    }
}

// 3. Render List View
function renderListView() {
    const listContainer = document.getElementById("eventListContent");
    listContainer.innerHTML = "";

    // Sort events by date?
    // Assuming CSV might be unsorted.
    const sortedEvents = [...eventsToRender].sort((a, b) => new Date(a.date) - new Date(b.date));

    // Filter to show only future events? Or all? Let's show all for now.
    // Or maybe current month? User said "Show the whole list".

    sortedEvents.forEach(evt => {
        const row = document.createElement("div");
        row.className = "list-item";
        row.innerHTML = `
            <span class="list-date">${evt.date}</span>
            <span class="list-name">${evt.name}</span>
            <span class="list-type">${evt.type}</span>
        `;
        listContainer.appendChild(row);
    });
}

// 4. toggle View
// 4. toggle View
document.getElementById("listBtn").onclick = () => {
    isListView = !isListView;
    const grid = document.getElementById("calendarGrid");
    const listWrapper = document.getElementById("listViewWrapper");

    if (isListView) {
        grid.style.display = "none";
        listWrapper.style.display = "flex";
    } else {
        grid.style.display = "grid";
        listWrapper.style.display = "none";
    }
};

// 5. Scroll Logic
document.getElementById("scrollUpBtn").onclick = () => {
    const list = document.getElementById("eventListContainer");
    list.scrollBy({ top: -200, behavior: 'smooth' }); // Scroll up by 200px
};

document.getElementById("scrollDownBtn").onclick = () => {
    const list = document.getElementById("eventListContainer");
    list.scrollBy({ top: 200, behavior: 'smooth' }); // Scroll down by 200px
};

// 6. Popup Logic
function showEventDetails(evt) {
    document.getElementById("detailTitle").textContent = evt.name;
    document.getElementById("detailDate").textContent = evt.date;
    document.getElementById("detailType").textContent = evt.type;

    document.getElementById("eventDetailPopup").style.display = "block";
}

document.getElementById("closeDetailBtn").onclick = () => {
    document.getElementById("eventDetailPopup").style.display = "none";
};


// Navigation
document.getElementById("prevBtn").onclick = () => {
    currentDate.setMonth(currentDate.getMonth() - 1);
    renderCalendar();
};

document.getElementById("nextBtn").onclick = () => {
    currentDate.setMonth(currentDate.getMonth() + 1);
    renderCalendar();
};

// Init
fetchEvents();
// Date Logic handles initial render inside fetchEvents or separately?
// Ideally render immediately empty, then update.
renderCalendar();
