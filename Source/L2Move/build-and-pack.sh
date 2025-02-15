#!/bin/bash

# Build for both x64
dotnet publish -r osx-x64 -c Release

# Create dir and structure
mkdir -p bin/Release/publish/L2Move.app/Contents/MacOS bin/Release/publish/L2Move.app/Contents/Resources

# Copy binary into .app
cp -R bin/Release/net8.0/osx-x64/publish/* bin/Release/publish/L2Move.app/Contents/MacOS/

# Execute permission
chmod +x bin/Release/publish/L2Move.app/Contents/MacOS/L2Move

# Create Info.plist
cat > bin/Release/publish/L2Move.app/Contents/Info.plist <<EOL
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>L2Move</string>
    <key>CFBundleDisplayName</key>
    <string>L2Move</string>
    <key>CFBundleIdentifier</key>
    <string>com.manuz.l2move</string>
    <key>CFBundleVersion</key>
    <string>0.1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>L2Move</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon.icns</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOL

# Copy icon 
cp Assets/AppIcon.icns bin/Release/publish/L2Move.app/Contents/Resources/

echo "L2Move.app created successfully"