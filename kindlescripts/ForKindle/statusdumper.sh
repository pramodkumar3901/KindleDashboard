#!/bin/sh

OUTFILE="$(dirname "$0")/status.json"

while true; do
    # WiFi State
    WIFI_STATE=$(lipc-get-prop com.lab126.wifid cmState 2>/dev/null || echo "Unknown")

    # SSID (iwconfig is slow; extract with one awk)
    SSID=$(iwconfig wlan0 2>/dev/null | awk -F\" '/ESSID:/ {print $2}')
    [ -z "$SSID" ] && SSID="Unknown"

    # IP Address (single grep+awk)
    IPADDR=$(ifconfig wlan0 2>/dev/null | awk -F'[: ]+' '/inet addr/ {print $4}')
    [ -z "$IPADDR" ] && IPADDR="0.0.0.0"

    # Battery Level
    BATTERY=$(lipc-get-prop com.lab126.powerd battLevel 2>/dev/null || echo "Unknown")

    # Brightness
    BRIGHTNESS=$(cat /sys/class/backlight/bl/brightness 2>/dev/null || echo "Unknown")

    # ScreenSaver State
    SCREENSAVER=$(lipc-get-prop com.lab126.powerd preventScreenSaver 2>/dev/null || echo "Unknown")

    # Write JSON
    cat <<EOF > "$OUTFILE"
{
  "wifi_state": "$WIFI_STATE",
  "ssid": "$SSID",
  "ip_address": "$IPADDR",
  "battery": "$BATTERY",
  "brightness": "$BRIGHTNESS",
  "screensaver": "$SCREENSAVER"
}
EOF

    sleep 10
done
