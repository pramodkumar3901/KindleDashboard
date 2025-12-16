function updateClock() {
    const now = new Date();

    // Analog
    const seconds = now.getSeconds();
    const minutes = now.getMinutes();
    const hours = now.getHours();

    const minuteDeg = ((minutes / 60) * 360);
    const hourDeg = ((hours / 12) * 360) + ((minutes / 60) * 30);

    setRotation('minute', minuteDeg);
    setRotation('hour', hourDeg);

    // Digital
    const hStr = String(hours % 12 || 12).padStart(2, '0');
    const mStr = String(minutes).padStart(2, '0');
    const ampm = hours >= 12 ? 'PM' : 'AM';

    document.getElementById('digital').textContent = `${hStr}:${mStr} ${ampm}`;

    // Date
    const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    document.getElementById('date').textContent = now.toLocaleDateString(undefined, options);
}

function setRotation(id, deg) {
    document.getElementById(id).style.transform = `rotate(${deg}deg)`;
}

setInterval(updateClock, 1000);
updateClock();
