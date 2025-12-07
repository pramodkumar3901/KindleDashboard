#!/bin/sh

OUTFILE="$(dirname "$0")/status.json"
retryTimer=0
retryCount=0

while true; do
    # WiFi State
    WIFI_STATE=$(lipc-get-prop com.lab126.wifid cmState 2>/dev/null || echo "Unknown")

    if [ "$WIFI_STATE" = "CONNECTED" ]; then
        retryCount=0
    elif [ "$retryTimer" -eq 0 ]; then
        lipc-set-prop com.lab126.cmd wirelessEnable 0
        sleep 2
        lipc-set-prop com.lab126.cmd wirelessEnable 1
        retryCount=$((retryCount + 1))
        case $retryCount in
            1) retryTimer=3 ;;    # 30s
            2) retryTimer=6 ;;    # 60s
            3) retryTimer=12 ;;   # 2min
            4) retryTimer=30 ;;   # 5min
            5) retryTimer=60 ;;   # 10min
            *) retryTimer=90 ;;   # 15min
        esac
    fi

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

    retryTimer=$((retryTimer - 1))
    sleep 10
done
