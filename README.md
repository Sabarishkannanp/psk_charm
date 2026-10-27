# 🌟 PSK Desktop Charms
> **Created by PSK Developer**  
> *A native, hardware-accelerated physics-simulated hanging charm overlay for all Windows versions.*

---

## 📖 Overview

**PSK Charms** is a premium desktop companion application created by **PSK Developer**. It hangs interactive, physics-simulated charms from the top of your screen or window bar, bringing motion and personality to your desktop.

Unlike other apps that crash on missing runtimes or incompatible frameworks, **PSK Charms** is built with native **Windows WPF (.NET Framework 4.8)**, guaranteeing 100% compatibility across **all Windows versions** (Windows 11, Windows 10, Windows 8, and Windows 7) out of the box with **zero external dependencies**.

---

## ✨ Features & Highlights

- **🎯 Works on All Windows Versions**: Native Windows executable (`PSK.exe`) that starts in under 0.3 seconds and uses minimal memory (~40MB).
- **🪢 Realistic Verlet Rope Physics**:
  - Multi-particle rope simulation with natural tension, bending, and inertia.
  - Interactive mouse drag: grab the charm, pull it across your screen, and release or fling it with momentum!
  - Natural ambient breeze: subtle idle swaying that breathes life into your desktop.
- **🎨 71+ Built-in High-Resolution Charms across 10 Themes**:
  - **Avengers** (Spider-Man, Iron Man, Captain America, Hulk, Thor, Spider-Man Swinging)
  - **DC** (Bat-Man, Super-Man, Flash, Green Lantern, Wonder Women)
  - **Breaking Bad** (Heisenberg, Jesse Pinkman, Gus Fring, Mike Ehrmantraut, Saul Goodman, Tuco Salamanca, RV)
  - **Football** (Messi 10, Ronaldo 7, Neymar 10, FC Barcelona, Real Madrid)
  - **Friends** (Chandler Bing, Joey Tribbiani, Monica Geller, Phoebe Buffay, Rachel Green, Ross Geller)
  - **Music** (Michael Jackson, Billie Eilish, Juice WRLD, Taylor Swift, The Weeknd, Travis Scott)
  - **Stranger Things** (Eleven, Dustin Henderson, Mike Wheeler, Lucas Sinclair, Will Byers, Demogorgon)
  - **BTS** (RM, Jin, Suga, j-hope, Jimin, V, Jungkook)
  - **Tamil Gods** (Vinayaga, Muruga Vel, OM, Karuppu, Mani)
  - **Lucky & Cultural Amulets** (Dream Catcher, Maneki-neko, Nazar Boncugu, Nimbu-mirchi, Daruma, Horseshoe, Bead, Camera, Heart, Diamond, Star, Ghanta, Hamsa, Himmeli, Scarab)
- **➕ Custom Charm Importer**:
  - Click `➕ Add Custom Charm...` in the Control Center to add any personal image (`.png`, `.jpg`, `.jpeg`, `.svg`, `.gif`)!
- **🪡 Cord Customization**:
  - **5 Cord Styles**: Classic Rope, Golden Bead Chain (shiny golden pearls), Golden Thread (metallic sheen), Neon Glow (cyberpunk beam), or Invisible (floating charm).
  - **Cord Color Palette**: Rich Gold, Ruby Red, Neon Cyan, Emerald Green, Silver, Purple, Midnight Black, or any Custom Hex color.
  - **Sliders**: Cord Length (100px – 450px), Cord Thickness (1.5px – 7px), Charm Scale (50% – 150%).
- **⚙️ Physics Controls**:
  - Tweak gravity, ambient breeze strength, and swing damping/air resistance.
- **🖥️ Desktop Overlay & Position Controls**:
  - Drag the metallic anchor clip at the top of the screen to move the charm horizontally.
  - **Always On Top**: Floats above all open windows.
  - **Click-Through Mode**: Toggle to let clicks pass through during gaming or typing.
  - **Start with Windows**: Toggle to automatically launch PSK Charms on Windows login.
- **🔔 Windows System Tray Integration**:
  - Sleek notification tray icon with quick actions:
    - 🚀 Open PSK Control Center
    - 🔀 Next Charm
    - 📍 Reset Position to Center
    - 🌬️ Toggle Idle Breeze
    - 📌 Toggle Always On Top
    - ℹ️ About PSK...
    - ❌ Exit PSK

---

## 🚀 Quick Start Guide

### 1. Launching the App
Double-click either:
- **`Run-PSK.bat`** (Quick Launcher), or
- **`PSK.exe`**

### 2. Interacting with Your Charm
- **Swing it**: Click and drag the hanging charm with your left mouse button, then release to swing or throw it with velocity!
- **Move it**: Hover over the small metallic clip at the very top of your screen until the cursor changes to `↔`, then drag horizontally to position it anywhere along your monitor.
- **Quick Menu**: Right-click directly on the charm for quick options (Switch Charm, Settings, Always On Top, etc.).
- **Control Center**: Double-click or right-click the **PSK icon** in your Windows System Tray (near the clock) to browse the catalog, customize cords, and change settings.

---

## 📂 Project Structure

```text
d:\charm\
├── PSK.exe                      # Standalone native executable
├── Run-PSK.bat                  # Quick launch batch script
├── charms.json                  # Charms catalog metadata
├── settings.json                # User preferences & configuration
├── Assets\
│   ├── psk.ico                  # Application & Tray icon
│   ├── psk.png                  # High-res emblem logo
│   └── Charms\                  # 71+ charm PNG assets by category
└── PSK_Charms\
    ├── PSK.exe                  # Standalone executable
    ├── Run-PSK.bat              # Launcher
    ├── build.bat                # 1-click build script (uses native csc.exe)
    ├── src\
    │   ├── AssemblyInfo.cs      # Metadata (Created by PSK Developer)
    │   ├── AppSettings.cs       # Settings persistence (JSON)
    │   ├── CharmModel.cs        # Catalog loader & custom importer
    │   ├── PhysicsSimulator.cs  # Verlet multi-link rope dynamics
    │   ├── OverlayWindow.cs     # Transparent hardware-accelerated overlay
    │   ├── ControlCenterWindow.cs # Modern dark UI Control Center
    │   ├── TrayManager.cs       # Windows System Tray controller
    │   └── Win32Helper.cs       # Native Windows API interop
    └── Assets\                  # Core project assets
```

---

---

## 📦 How to Share with Friends

You have **two super easy ways** to share with friends:

### Option 1: Send the Single EXE File (Easiest & Recommended)
> **File to Send**: [`d:\charm\PSK-Charms-Setup.exe`](file:///d:/charm/PSK-Charms-Setup.exe) (or [`d:\charm\PSK_Charms_Installer.exe`](file:///d:/charm/PSK_Charms_Installer.exe))  
> **Size**: ~17.7 MB

- Just send this **ONE `.exe` file** directly to your friend (via WhatsApp Web, Telegram, Google Drive, or Pen Drive).
- **Your friend simply double-clicks `PSK-Charms-Setup.exe`**.
- It shows the **PSK Developer** splash screen, sets up all 72 charms, adds a **"PSK Charms"** shortcut to their Desktop & Start Menu, and immediately launches the charm on their laptop screen!
- **Zero setup, zero unzipping!**

### Option 2: Send the Portable ZIP Archive
> **File to Send**: [`d:\charm\PSK_Charms_v1.0.zip`](file:///d:/charm/PSK_Charms_v1.0.zip)  
> **Size**: 17.3 MB

1. Send `PSK_Charms_v1.0.zip` to your friend.
2. Friend right-clicks the zip and chooses **Extract All...**.
3. Inside the extracted folder, double-click **`Run-PSK.bat`** (or **`PSK.exe`**).

To re-package or rebuild the installer anytime, simply run `package.bat`.

---

## 🛠️ Rebuilding from Source

The project requires **no external SDKs or NuGet packages**. It compiles directly with the Windows built-in C# compiler:

```cmd
cd d:\charm\PSK_Charms
build.bat
```

---

## 👤 Developer Information

- **Application Name**: PSK Charms
- **Author**: **PSK Developer**
- **Featured Charm**: 3D Cartoon Avatar of PSK Developer (with trimmed beard & sleek dark suit)
- **Version**: 1.0.0 Stable
- **Distribution Package**: `PSK_Charms_v1.0.zip` (17.3 MB)
- **Platform**: Windows (All versions: 7, 8, 10, 11)
