#!/bin/bash

# Script to extract changelog entry for a specific version
# Usage: ./scripts/get-changelog-entry.sh <version>

set -e

if [ -z "$1" ]; then
    echo "Error: Version argument required"
    echo "Usage: $0 <version>"
    exit 1
fi

VERSION=$1

# Extract the changelog entry for the specified version
awk -v version="$VERSION" '
BEGIN { found=0; printing=0 }
/^## \[/ {
    if (printing) {
        exit
    }
    if ($0 ~ "^## \\[" version "\\]") {
        found=1
        printing=1
        next
    }
}
printing { print }
END {
    if (!found) {
        print "Error: Version " version " not found in CHANGELOG.md" > "/dev/stderr"
        exit 1
    }
}
' CHANGELOG.md
