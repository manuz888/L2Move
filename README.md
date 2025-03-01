# L2Move
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)[![Platform](https://img.shields.io/badge/platform-macOS-lightgrey)](#)[![Built with Avalonia](https://img.shields.io/badge/Framework-Avalonia-blue)](https://avaloniaui.net/)

#### 🎵 Convert Ableton Live Drum Racks to Move-Compatible `.adg` Files & Export Preset Bundles 
**L2Move** allows you to convert Ableton Live `.adg` drum racks into Move-compatible `.adg` files while keeping them editable in Live. You can also generate and export preset bundles directly, streamlining the workflow for Move users.

## ⚠️ Disclaimer
This software is provided "as is" without any guarantees or warranties.  
The authors **are not responsible** for any damage, data loss, or other issues that may affect your devices or data. Use it at your own risk.  
L2Move **is not affiliated with, authorized by, or endorsed by Ableton**.  
**Ableton Live**, `.adg` file format, **Ableton Move**, **Ableton Move Drum Rack template**, and all related trademarks or formats are the **property of their respective owners**.

## ✨ Features
- **Convert and edit**: Convert Ableton Live `.adg` drum racks for Move compatibility while keeping them editable in Live to modify and add effects before exporting as a preset bundles.
- **Create preset bundles directly**.
- **Batch conversion**: Process single files or entire directories.
- **Drag & Drop + Copy & Paste** support.
- **Supports multi-sample drum racks**: A new `.adg` file and/or preset bundle will be created for each kit inside.

## 🚀 Installation

### macOS
A binary is available in the [releases](https://github.com/manuz888/L2Move/releases/), but since it is **not signed or notarized**, you will need to bypass Gatekeeper to open it. **I'm truly sorry for the inconvenience!**  

1. Download the `.zip` file from the [releases page](https://github.com/manuz888/L2Move/releases/) and extract it.
2. Open **L2Move** by right-clicking on it, choose Open, then confirm when prompted (only required the first time).
3. If macOS prevents **L2Move** from opening, follow these steps:
   - Open **Terminal** and run (replace `<path-to-L2Move>` with the actual location of the app):
     ```sh
     sudo xattr -rd com.apple.quarantine "<path-to-L2Move>/L2Move.app"
     ```
   - Then try opening **L2Move** again.


### Windows
The Windows build is **not yet available**, but it will be released in the future. Stay tuned!

## 🛠️ Usage
1. **Open L2Move**.
2. **Load the** `.adg` **file** from the **Drum Rack** or a folder containing multiple `.adg` files for batch conversion.
   - You can do this via **copy & paste** or **drag & drop**.
3. **(Optional)** Check the box to choose whether to create the **preset bundle** for Move. Once the preset bundle is created, you can use **Move Manager** to transfer it to your device.
4. **Start the process**.
5. **For batch conversion**, a `report.txt` file will be generated containing details about the processing results.

⚠️ **If no samples are found**:
Try to open the **Drum Rack** in **Live**, save it as a preset, and use that file as source.

## 💡 Credits  
- **[Val Kalinic](https://exfont.com/vp-pixel-standard-2.font)** for designing the font used in the app and related logo.
- **[Charles Vestal](https://charles.pizza)** (and the rest of the community) for his contributions to extending Move.
- **[Avalonia](https://avaloniaui.net/)** for providing a great cross-platform UI framework.

## 📬 Contact
For questions, issues, or suggestions, open an issue on GitHub.
