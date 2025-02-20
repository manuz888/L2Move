#!/bin/bash

ICONSET_NAME=AppIcon.iconset
if [ ! -d "$ICONSET_NAME" ]; then
    mkdir $ICONSET_NAME
fi

# Create icons for various size
magick icon_1024.png -resize 16x16 $ICONSET_NAME/icon_16x16.png
magick icon_1024.png -resize 32x32 $ICONSET_NAME/icon_16x16@2x.png
magick icon_1024.png -resize 32x32 $ICONSET_NAME/icon_32x32.png
magick icon_1024.png -resize 64x64 $ICONSET_NAME/icon_32x32@2x.png
magick icon_1024.png -resize 128x128 $ICONSET_NAME/icon_128x128.png
magick icon_1024.png -resize 256x256 $ICONSET_NAME/icon_128x128@2x.png
magick icon_1024.png -resize 256x256 $ICONSET_NAME/icon_256x256.png
magick icon_1024.png -resize 512x512 $ICONSET_NAME/icon_256x256@2x.png
magick icon_1024.png -resize 512x512 $ICONSET_NAME/icon_512x512.png
magick icon_1024.png -resize 1024x1024 $ICONSET_NAME/icon_512x512@2x.png

# Create json content for iconset
cat > $ICONSET_NAME/Contents.json <<EOL
{
  "images": [
    {
      "size": "16x16",
      "idiom": "mac",
      "filename": "icon_16x16.png",
      "scale": "1x"
    },
    {
      "size": "16x16",
      "idiom": "mac",
      "filename": "icon_16x16@2x.png",
      "scale": "2x"
    },
    {
      "size": "32x32",
      "idiom": "mac",
      "filename": "icon_32x32.png",
      "scale": "1x"
    },
    {
      "size": "32x32",
      "idiom": "mac",
      "filename": "icon_32x32@2x.png",
      "scale": "2x"
    },
    {
      "size": "128x128",
      "idiom": "mac",
      "filename": "icon_128x128.png",
      "scale": "1x"
    },
    {
      "size": "128x128",
      "idiom": "mac",
      "filename": "icon_128x128@2x.png",
      "scale": "2x"
    },
    {
      "size": "256x256",
      "idiom": "mac",
      "filename": "icon_256x256.png",
      "scale": "1x"
    },
    {
      "size": "256x256",
      "idiom": "mac",
      "filename": "icon_256x256@2x.png",
      "scale": "2x"
    },
    {
      "size": "512x512",
      "idiom": "mac",
      "filename": "icon_512x512.png",
      "scale": "1x"
    },
    {
      "size": "512x512",
      "idiom": "mac",
      "filename": "icon_512x512@2x.png",
      "scale": "2x"
    }
  ],
  "info": {
    "version": 1,
    "author": "xcode"
  }
}
EOL

# Make icns file from iconset
iconutil -c icns $ICONSET_NAME
