let currentDate = new Date();

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
    const dayCells = grid.querySelectorAll('.day-cell');
    dayCells.forEach(cell => cell.remove());

    const now = new Date();
    const isTodayMonth = now.getFullYear() === y && now.getMonth() === m;
    const todayDate = now.getDate();

    // Empty cells before first day
    for (let i = 0; i < firstDayIndex; i++) {
        const div = document.createElement("div");
        div.classList.add("day-cell", "empty");
        // Check if this empty slot falls on a weekend column?
        // i=0 is Sun, i=6 is Sat.
        // But headers are separate.
        // We need to calculate column index carefully?
        // Grid auto-placement handles rows, but we need to track column for shading.
        // Actually, cleaner to just use modulo on total cells inserted?
        // Wait, empty cells also occupy columns.
        if (i === 0) div.classList.add("weekend"); // Sunday
        grid.appendChild(div);
    }

    // Days
    for (let d = 1; d <= daysInMonth; d++) {
        const div = document.createElement("div");
        div.classList.add("day-cell");
        div.textContent = d;

        // Current day of week for this date
        const dayOfWeek = new Date(y, m, d).getDay();
        if (dayOfWeek === 0 || dayOfWeek === 6) {
            div.classList.add("weekend");
        }

        if (isTodayMonth && d === todayDate) {
            div.classList.add("today");
        }

        grid.appendChild(div);
    }
}

document.getElementById("prevBtn").onclick = () => {
    currentDate.setMonth(currentDate.getMonth() - 1);
    renderCalendar();
};

document.getElementById("nextBtn").onclick = () => {
    currentDate.setMonth(currentDate.getMonth() + 1);
    renderCalendar();
};

renderCalendar();
