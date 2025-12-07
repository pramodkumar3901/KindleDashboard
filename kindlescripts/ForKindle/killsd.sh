#!/bin/sh

# Script to stop statusdumper.sh

# Try to gracefully stop the process
pkill -f statusdumper.sh

# Optional: check if it’s still running and force kill
if pgrep -f statusdumper.sh > /dev/null; then
    echo "Process still running, forcing kill..."
    pkill -9 -f statusdumper.sh
else
    echo "statusdumper.sh stopped successfully."
fi
