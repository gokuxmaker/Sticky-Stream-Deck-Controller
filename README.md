# Seeed Studio reTerminal Sticky - Stream Deck Controller & Arduino BSP

[![MCU: ESP32-S3](https://img.shields.io/badge/MCU-ESP32--S3%20(16MB%20Flash%20%2F%208MB%20OPI%20PSRAM)-blue?logo=espressif)](https://www.seeedstudio.com/)
[![Display: 3.97" E-Paper](https://img.shields.io/badge/Display-3.97%22%20800x480%20SSD1677-orange)](#1-hardware-pinout-reference)
[![Touch: Capacitive GT911](https://img.shields.io/badge/Touch-Goodix%20GT911%20I2C-green)](#1-hardware-pinout-reference)
[![Arduino IDE](https://img.shields.io/badge/Arduino%20IDE-v2.x%20%2F%20v1.8-00979D?logo=arduino)](reterminal_sticky_arduino.ino)
[![Windows App: .NET 4.0](https://img.shields.io/badge/Host%20App-Windows%20(.NET%204.0%20%2F%20C%23)-0078D7?logo=windows)](windows_app/)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen)](LICENSE)

Transform your **Seeed Studio reTerminal Sticky** (ESP32-S3) into a standalone, customizable **15-button E-Paper Stream Deck & Macro Controller** for Windows.

This repository provides both the complete **ESP32-S3 Arduino firmware** (built for **Arduino IDE**) with all hardware drivers, and the standalone **Windows Companion Desktop App** (`ReTerminalStreamDeck.exe`) featuring official Seeed Studio styling, live USB bitmap uploads, and flexible macro/application automation.

---

## 📌 Project Overview & Use Cases

The **reTerminal Sticky Stream Deck** turns the 3.97-inch (800x480) capacitive touch E-Paper screen into an interactive desktop macro pad. Touching any of the 15 on-screen tiles instantly triggers your customized Windows action over USB CDC serial.

### What is this project used for?

1. **One-Touch App & Game Launcher**: Instantly launch or switch to apps like VS Code, Discord, DaVinci Resolve, Chrome, KiCad, or Spotify with a single tap.
2. **Media & Volume Controller**: Dedicated controls for Play/Pause, Next Track, Previous Track, Volume Up, Volume Down, and Mute.
3. **Web & Workflow Shortcuts**: Open frequently visited websites, dashboards, and internal URLs directly in your default browser.
4. **Developer & Automation Scripts**: Execute batch scripts, PowerShell commands, and build tools silently in the background.
5. **Zero-Power Persistent Display**: Thanks to E-Paper technology, your custom button layout, labels, and artwork remain permanently visible on screen even when unplugged or powered down—drawing zero static electricity.

---

## 📸 Showcase & Demos

| Windows Companion App (`ReTerminalStreamDeck.exe`) | 800x480 E-Paper Display UI Artwork |
| :---: | :---: |
| ![Windows Companion App](https://github.com/gokuxmaker/Sticky-Stream-Deck-Controller/blob/main/windows_app/screenshot_app.png) | ![reTerminal Sticky E-Paper UI](https://github.com/gokuxmaker/Sticky-Stream-Deck-Controller/blob/main/windows_app/custom_ui_800x480.png) |
| *Full visual 3x5 button configuration, live CDC serial monitoring & instant USB upload* | *High-contrast 15-button monochrome layout displayed permanently with zero static power* |

---

## ✨ Key Features

- ⚡ **15 Capacitive Touch Macro Buttons**: Responsive 3x5 matrix mapped across the 800x480 display with tactile audio clicks and software debouncing.
- 🖼️ **Live E-Paper Image Uploading via USB**: Stream custom 800x480 artwork directly to the display over USB CDC serial in ~2 seconds without reflashing firmware! Includes automatic Floyd-Steinberg error-diffusion dithering.
- 🔋 **Zero Static Power Consumption**: E-Paper technology retains your customized layout indefinitely even when powered off or disconnected.
- 💤 **Smart Deep Sleep & Instant Wake**:
  - **Long-press `AI / OK` button (≥ 2 sec)**: Powers down E-Paper and Touch power rails and enters ultra-low-power deep sleep.
  - **Short-press `AI / OK` button in sleep**: Wakes up immediately, boots peripherals, and plays a welcome chime.
  - **Short-press `AI / OK` button while running**: Performs a full E-Paper refresh to clear ghosting.
- 🎛️ **Zero-Dependency Windows Desktop App**:
  - Self-contained executable (`ReTerminalStreamDeck.exe`) running on standard Windows .NET 4.0+ (no Python or node runtime required).
  - Can be recompiled in ~1 second via [build.bat](build.bat) using Windows' native `csc.exe`.
  - Supports 4 action types: **Launch Executable / App / Script**, **Open Website / URL**, **Media Control (Play/Pause/Track/Volume)**, and **Run Silent Shell Commands**.
  - Background system tray integration with minimize-to-tray support.
- 🔊 **Tactile Buzzer Feedback**: Instant auditory click on touch events, test beep command, and boot/shutdown musical chimes.
- 🛠️ **Complete Hardware Driver Suite (BSP)**: Modular, clean C++ drivers for SSD1677 SPI E-Paper, Goodix GT911 capacitive touch, piezo PWM buzzer, power rails, and I2C peripherals.

---

## 1. Hardware Pinout Reference

The reTerminal Sticky features dedicated power latching and separate power rails for the E-Paper panel and Touch controller:

| Subsystem | Function | ESP32-S3 GPIO | Description / Notes |
|---|---|---|---|
| **Power Controller** | Power Hold Rail | `GPIO 45` | Latches system power rail HIGH |
| | Power Lock Pulse | `GPIO 46` | Power controller toggle pulse |
| **E-Paper Display** | SCK (SPI Clock) | `GPIO 13` | SPI2 Bus (Shared with MicroSD) |
| | MOSI / SDI | `GPIO 14` | SPI2 Bus (Shared with MicroSD) |
| | CS (Chip Select) | `GPIO 15` | Active LOW |
| | DC (Data/Command) | `GPIO 16` | HIGH = Data, LOW = Command |
| | RST (Reset) | `GPIO 17` | Active LOW |
| | BUSY | `GPIO 18` | HIGH = Busy refreshing, LOW = Idle |
| | **Power Rail Enable** | `GPIO 47` | **Must be driven HIGH** to power the EPD panel |
| **Touchscreen** | SCL | `GPIO 2` | Dedicated Touch I2C Bus (`TwoWire(0)`) |
| | SDA | `GPIO 3` | Dedicated Touch I2C Bus (`TwoWire(0)`) |
| | INT | `GPIO 21` | Goodix GT911 Touch Interrupt |
| | RST | `GPIO 41` | Goodix GT911 Hardware Reset |
| | **Power Rail Enable** | `GPIO 42` | **Must be driven HIGH** to power the GT911 |
| **Physical Buttons** | AI / Power / OK | `GPIO 4` | Active LOW (`INPUT_PULLUP`), Sleep Wakeup source |
| | UP | `GPIO 5` | Active LOW (`INPUT_PULLUP`), Buzzer Mute Toggle |
| | DOWN | `GPIO 6` | Active LOW (`INPUT_PULLUP`), Fast Partial Refresh |
| **Buzzer** | PWM Output | `GPIO 48` | Piezo transducer driven by `tone()` |
| **Sensor I2C Bus** | SCL | `GPIO 0` | Secondary I2C Bus (`TwoWire(1)`): SHT40, RTC, IMU |
| | SDA | `GPIO 1` | Secondary I2C Bus (`TwoWire(1)`): SHT40, RTC, IMU |
| **MicroSD Slot** | CS | `GPIO 8` | MicroSD SPI Chip Select |
| | MISO | `GPIO 12` | SPI2 Bus Data In |
| **Digital Mic** | CLK / DATA | `GPIO 19 / 20` | PDM Microphone Interface |

---

## 2. 15-Button Stream Deck Matrix

The 800x480 display surface is partitioned into 15 equal touch hitboxes arranged in a 3-row by 5-column grid:

```text
+-------------------+-------------------+-------------------+-------------------+-------------------+
|     [ Button 1 ]  |     [ Button 2 ]  |     [ Button 3 ]  |     [ Button 4 ]  |     [ Button 5 ]  |
|       Discord     |      WhatsApp     |   Autodesk / CAD  |   Google Chrome   |   Brave Browser   |
+-------------------+-------------------+-------------------+-------------------+-------------------+
|     [ Button 6 ]  |     [ Button 7 ]  |     [ Button 8 ]  |     [ Button 9 ]  |    [ Button 10 ]  |
|      Inkscape     |      VS Code      |  DaVinci Resolve  |       KiCad       |       WeChat      |
+-------------------+-------------------+-------------------+-------------------+-------------------+
|    [ Button 11 ]  |    [ Button 12 ]  |    [ Button 13 ]  |    [ Button 14 ]  |    [ Button 15 ]  |
|   Previous Track  |    Play / Pause   |     Next Track    |    Volume Down    |     Volume Up     |
+-------------------+-------------------+-------------------+-------------------+-------------------+
```

### Touch Geometry & Actions

| Button | Touch Coordinate `(X, Y, W, H)` | CDC Trigger String | Factory Default Action |
| :---: | :--- | :---: | :--- |
| **1** | `Rect(12, 14, 147, 141)` | `[StreamDeck] Button 1 (Discord) pressed` | Launch / Focus Discord |
| **2** | `Rect(167, 11, 147, 141)` | `[StreamDeck] Button 2 (WhatsApp) pressed` | Open WhatsApp Web |
| **3** | `Rect(326, 12, 147, 141)` | `[StreamDeck] Button 3 (Autodesk / CAD) pressed` | Launch 3D CAD Application |
| **4** | `Rect(480, 12, 147, 141)` | `[StreamDeck] Button 4 (Google Chrome) pressed` | Launch Google Chrome |
| **5** | `Rect(637, 13, 147, 141)` | `[StreamDeck] Button 5 (Brave Browser) pressed` | Launch Brave Browser |
| **6** | `Rect(12, 170, 147, 141)` | `[StreamDeck] Button 6 (Inkscape) pressed` | Launch Inkscape |
| **7** | `Rect(167, 167, 147, 141)` | `[StreamDeck] Button 7 (VS Code) pressed` | Launch Visual Studio Code |
| **8** | `Rect(326, 168, 147, 141)` | `[StreamDeck] Button 8 (DaVinci Resolve) pressed` | Launch DaVinci Resolve |
| **9** | `Rect(480, 168, 147, 141)` | `[StreamDeck] Button 9 (KiCad) pressed` | Launch KiCad EDA |
| **10** | `Rect(637, 168, 147, 141)` | `[StreamDeck] Button 10 (WeChat) pressed` | Launch WeChat |
| **11** | `Rect(13, 326, 147, 141)` | `[StreamDeck] Button 11 (Previous Track) pressed` | Media: Previous Track |
| **12** | `Rect(168, 323, 147, 141)` | `[StreamDeck] Button 12 (Play / Pause) pressed` | Media: Play / Pause Toggle |
| **13** | `Rect(327, 324, 147, 141)` | `[StreamDeck] Button 13 (Next Track) pressed` | Media: Next Track |
| **14** | `Rect(481, 324, 147, 141)` | `[StreamDeck] Button 14 (Volume Down) pressed` | Media: Volume Down |
| **15** | `Rect(638, 325, 147, 141)` | `[StreamDeck] Button 15 (Volume Up) pressed` | Media: Volume Up |

---

## 3. Quick Start Guide: Arduino Firmware Setup

### Step 1: Add ESP32 Board URL
1. Open **Arduino IDE** (version 2.x or 1.8.x).
2. Go to **File > Preferences** (or **Arduino IDE > Settings** on macOS).
3. In the **Additional boards manager URLs** field, paste:
   ```text
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
4. Click **OK**.

### Step 2: Install ESP32 Board Support
1. Open **Tools > Board > Boards Manager...** (or click the Boards icon in the sidebar).
2. Search for **esp32** (by *Espressif Systems*).
3. Install version **2.0.14+** or **3.0.x**.

### Step 3: Open the Project
1. In Arduino IDE, go to **File > Open...**
2. Select [reterminal_sticky_arduino.ino](reterminal_sticky_arduino.ino).

### Step 4: Configure Board Settings
Under the **Tools** menu, configure the exact settings below:

| Setting | Recommended Value | Reason / Description |
|---|---|---|
| **Board** | **`ESP32S3 Dev Module`** | Target MCU for reTerminal Sticky |
| **USB CDC On Boot** | **`Enabled`** | **Critical**: Enables Serial Communication & Host app over native USB-C |
| **Upload Mode** | **`UART0 / Hardware CDC`** | Standard upload mechanism |
| **CPU Frequency** | **`240MHz (WiFi)`** | Maximum performance |
| **Flash Mode** | **`QIO 80MHz`** | Quad SPI Flash speed |
| **Flash Size** | **`16MB (128Mb)`** | Total onboard Flash capacity |
| **Partition Scheme** | **`16MB Flash (3MB APP/9.9MB FATFS)`** | Ample partition space for sketches |
| **PSRAM** | **`OPI PSRAM`** | **Critical**: Enables the 8MB Octal PSRAM buffer for the 800x480 display |
| **Port** | Select your device COM port | (e.g. `COM14` on Windows) |

### Step 5: Compile and Upload
1. Connect your reTerminal Sticky to your computer via USB-C.
2. Click the **Upload** arrow button `➔` (or press `Ctrl + U`).
3. Arduino IDE will compile the sketch and flash the ESP32-S3.
4. Once flashed, the screen will power on, display the 15-button Stream Deck UI, and play a startup chime!

---

### 📦 Optional: Flash Pre-Compiled `.bin` Directly (No Arduino IDE Needed)

Pre-compiled binary packages are provided so you can flash the device in seconds:

- **Single Merged Binary (Recommended)**: [**`reterminal_sticky_merged_0x0.bin`**](reterminal_sticky_merged_0x0.bin) (or [`firmware_bin/reterminal_sticky_merged_0x0.bin`](firmware_bin/reterminal_sticky_merged_0x0.bin))
  - **Flash Offset**: **`0x0`**
  - **Size**: ~528 KB
  - Contains Bootloader + Partition Table + App Firmware merged into one file.
- **Standalone App Binary**: [`reterminal_sticky_firmware.bin`](reterminal_sticky_firmware.bin) (offset `0x10000`)
- **1-Click Flasher Script**: Run [`firmware_bin/flash.bat`](firmware_bin/flash.bat) to automatically detect `esptool.exe` and flash the merged binary.

#### Single-Command Flash via `esptool.py` (at `0x0`):
```bash
esptool.py --chip esp32s3 --port COMx --baud 921600 write_flash -z 0x0 reterminal_sticky_merged_0x0.bin
```

---

## 4. 🖥️ Windows Companion App Guide & Step-by-Step Key Setup

The Windows host software ([`ReTerminalStreamDeck.exe`](ReTerminalStreamDeck.exe)) is a portable, zero-dependency desktop utility built with C# and .NET 4.0+. It requires no complex installation, Python environments, or background drivers.

<!-- 📷 IMAGE PLACEHOLDER: Windows Companion App Main Interface Overview -->
<!-- Replace with: ![Windows App Overview](images/app_overview.png) -->
![Windows Companion App Overview](https://github.com/gokuxmaker/Sticky-Stream-Deck-Controller/blob/main/windows_app/screenshot_app.png)

---

### Step 1: Launch the Companion App

1. Locate [ReTerminalStreamDeck.exe](ReTerminalStreamDeck.exe) in the root directory (or in the `windows_app/` folder).
2. Double-click to run it. 
3. The app starts with a native dark theme styled with official Seeed Studio colors (Seeed Green `#8FC31F` and Deep Blue `#003A4A`).

> [!NOTE]
> All configurations are saved automatically to [`streamdeck_config.json`](streamdeck_config.json) in the same directory as the executable.

---

### Step 2: Connect to reTerminal Sticky via USB

<!-- 📷 IMAGE PLACEHOLDER: Top Toolbar & COM Port Connection Indicator -->
<!-- Replace with: ![COM Port Connection](images/com_connection.png) -->

1. Plug your reTerminal Sticky into your Windows PC using a USB-C data cable.
2. At the top-right toolbar:
   - **Automatic Mode**: Keep the port selector on **`AUTO`** and the application will scan and connect automatically.
   - **Manual Mode**: If multiple COM ports are present, select the specific port (e.g., `COM14`) from the dropdown and click **Connect**.
3. When connected, the status indicator will turn **green**:
   ```text
   ● Connected: COM14
   ```
4. If you disconnect the cable, the indicator will display `● Disconnected` and turn amber/red. Click **Connect** again once plugged back in.

---

### Step 3: Select a Key to Configure

The central workspace displays the **Hardware Keypad (15-Key 3x5 Grid)** mirroring the physical reTerminal Sticky screen.

<!-- 📷 IMAGE PLACEHOLDER: Selecting a Key on the 15-Button Grid -->
<!-- Replace with: ![Key Selection](images/key_grid_selection.png) -->

1. Click on any of the 15 squircle key cards (`#01` to `#15`).
2. The active key is highlighted with a vivid **Seeed Green border**.
3. The right-hand **Button Inspector** panel will immediately load that key's current configuration, name, action type, and target.

---

### Step 4: Configure Button Actions (Step-by-Step)

In the **Button Inspector** sidebar on the right, you can configure your selected key:

<!-- 📷 IMAGE PLACEHOLDER: Button Inspector Configuration Panel -->
<!-- Replace with: ![Button Inspector](images/button_inspector.png) -->

#### 1. Set the Button Name
- In the **Button Name** field, type the label you want (e.g., `Discord`, `VS Code`, `Mute Audio`, `Compile Project`).
- This label will appear on the key card and in the real-time activity log.

#### 2. Choose an Action Type
Open the **Action Type** dropdown and choose one of four available action types:

---

#### 🔹 Option A: Launch Application (`.exe`, `.lnk`, `.bat`, `.cmd`)
Use this to open any software, game, or script installed on your PC.

<!-- 📷 IMAGE PLACEHOLDER: Launch Application Action Setup -->
<!-- Replace with: ![Launch Application Setup](images/action_launch_app.png) -->

1. Set **Action Type** to `Launch Application (.exe / script)`.
2. Click the **Browse** button next to *Program Executable Path*.
3. Select your `.exe`, desktop shortcut (`.lnk`), or script (`.bat` / `.cmd`).
   - *Example Target:* `C:\Users\Username\AppData\Local\Programs\Microsoft VS Code\Code.exe`
4. *(Optional)* **Command-line Arguments**: Enter arguments to pass to the program (e.g., `--processStart Discord.exe` or `D:\project\my_code`).
5. *(Optional)* **Working Directory**: Click **Browse** to specify the startup directory if required by the application.

---

#### 🔹 Option B: Open Website URL
Use this to launch web apps, documentation, or links in your default browser.

<!-- 📷 IMAGE PLACEHOLDER: Open Website URL Action Setup -->
<!-- Replace with: ![Open Website URL](images/action_open_url.png) -->

1. Set **Action Type** to `Open Website URL`.
2. In the **Website URL** field, enter the full web address.
   - *Examples:*
     - `https://web.whatsapp.com`
     - `https://github.com`
     - `https://wiki.seeedstudio.com`

---

#### 🔹 Option C: Media Control Shortcut
Use this for instant system media controls and volume adjustments.

<!-- 📷 IMAGE PLACEHOLDER: Media Control Dropdown Selection -->
<!-- Replace with: ![Media Control Setup](images/action_media_control.png) -->

1. Set **Action Type** to `Media Control Shortcut`.
2. Select the desired media function from the **Media Shortcut Function** dropdown:
   - `PLAY_PAUSE`: Play / Pause playback toggle
   - `NEXT_TRACK`: Skip to next track
   - `PREV_TRACK`: Return to previous track
   - `VOL_UP`: Volume Up
   - `VOL_DOWN`: Volume Down
   - `MUTE`: Toggle Master Mute

---

#### 🔹 Option D: Run Shell Command
Use this to run background commands or automated developer tasks.

<!-- 📷 IMAGE PLACEHOLDER: Run Shell Command Action Setup -->
<!-- Replace with: ![Run Command Setup](images/action_run_command.png) -->

1. Set **Action Type** to `Run Shell Command`.
2. In the **Command or Script Path** field, enter your command (e.g., `git pull`, `shutdown -s -t 60`, or a `.ps1` / `.bat` file).
3. Specify optional arguments and working directory as needed.

---

### Step 5: Test & Save the Key

<!-- 📷 IMAGE PLACEHOLDER: Save Changes & Test Launch Buttons -->
<!-- Replace with: ![Save and Test](images/save_and_test.png) -->

1. **Test the Action**: Click **`▶ Test Launch`** to execute the action immediately on Windows. This verifies the path or URL works without having to tap the physical screen.
2. **Save the Configuration**: Click the green **`Save Changes`** button.
   - The key card in the grid updates its title and badge:
     - 🟦 **`APP`** (Sky Blue) for applications
     - 🟩 **`WEB`** (Seeed Green) for URLs
     - 🟪 **`MEDIA`** (Purple) for media shortcuts
     - 🟧 **`CMD`** (Amber) for shell commands
   - Settings are immediately written to [`streamdeck_config.json`](streamdeck_config.json).

---

### Step 6: Customizing the 800x480 E-Paper Display Artwork

You can change the entire 800x480 screen artwork on the device with zero firmware modifications:

<!-- 📷 IMAGE PLACEHOLDER: 800x480 E-Paper Image Customizer Dialog -->
<!-- Replace with: ![E-Paper Image Manager](images/image_customizer_dialog.png) -->

1. In the top toolbar, click **`🖼 800x480 E-Paper Image`**.
2. Click **`📂 Choose 800x480 Image...`** and select any image (`.png`, `.jpg`, `.bmp`).
3. The manager automatically resizes and applies **Floyd-Steinberg error-diffusion dithering** to produce crisp monochrome artwork.
4. Choose an action:
   - **⚡ Upload to reTerminal**: Streams all 48,000 bytes directly over USB serial in ~2 seconds. The reTerminal Sticky refreshes instantly with your new layout!
   - **💾 Save StreamDeck_Bitmap.h**: Generates and replaces the C byte-array header in your project directory so the artwork is permanently built into future firmware flashes.
   - **📁 Export PNG**: Saves the 1-bit processed bitmap for fine-tuning in Photoshop, GIMP, or Inkscape.
   - **↺ Reset Default**: Restores the factory 15-button Seeed Studio design.

---

### Step 7: Hardware Utility Controls

In the top-right corner of the app, three utility buttons let you test and maintain the physical reTerminal Sticky:

<!-- 📷 IMAGE PLACEHOLDER: Hardware Maintenance Toolbar Buttons -->
<!-- Replace with: ![Hardware Buttons](images/hardware_toolbar_buttons.png) -->

- **`↻ Refresh`**: Sends the `REFRESH` serial command to perform a full E-Paper clearing cycle to remove ghosting.
- **`⚡ Fast`**: Sends the `FAST_REFRESH` serial command for a quick partial refresh.
- **`🔔 Buzzer`**: Sends the `BEEP` command to emit a short test chime on the onboard piezo buzzer.

---

### Step 8: Background & System Tray Operation

<!-- 📷 IMAGE PLACEHOLDER: Activity Console & System Tray Settings -->
<!-- Replace with: ![System Tray & Activity Console](images/tray_and_console.png) -->

1. In the bottom **Activity Console** bar:
   - Check **`Minimize to tray on close`**: When you close or minimize the window, it stays running silently in the Windows taskbar notification area.
   - Check **`Desktop notifications`**: Displays balloon notifications whenever a key is pressed.
2. To restore the window, double-click the **reTerminal Sticky** icon in the Windows notification area or right-click and choose **Open Stream Deck**.
3. To exit the app completely, right-click the tray icon and select **Exit**.

---

## 5. Serial Communication Protocol

The firmware communicates over USB CDC serial at **115200 baud, 8N1**:

### Commands from ESP32 to PC Host
When a button is tapped on the touch surface:
```text
[StreamDeck] Button <N> (<Name>) pressed
```
*Example: `[StreamDeck] Button 1 (Discord) pressed`*

### Commands from PC Host to ESP32
The Windows app or any terminal/script can send these commands terminated by `\n`:

| Command | Action Description |
|---|---|
| `REFRESH` | Triggers a full high-fidelity E-Paper screen refresh. |
| `FAST_REFRESH` | Triggers a fast partial E-Paper refresh cycle. |
| `BEEP` | Emits a 2000Hz confirmation tone on the onboard piezo buzzer. |
| `PRESS <1-15>` | Simulates a physical touch button event for button number `1` through `15`. |
| `UPLOAD_BITMAP` | Handshakes the 48,000-byte 1-bit monochrome raw bitmap stream. Device replies with `[CMD] READY_FOR_BITMAP`, accepts 48KB, and refreshes. |
| `HELP` | Prints available command list in serial monitor. |

---

## 6. AutoHotkey & Python Scripts

For advanced scripting, alternative listeners are included:

- **[streamdeck.ahk](streamdeck.ahk)**: Standalone AutoHotkey v1/v2 script mapped to all 15 Stream Deck shortcuts.
- **[serial_worker.py](serial_worker.py)**: Lightweight Python serial reader utilizing `pyserial` for cross-platform integrations on Linux, macOS, or Windows.

---

## 7. Repository File Structure

```text
reterminal_sticky_arduino/
├── reterminal_sticky_arduino.ino   # Main sketch: setup, loop, serial parser, power management
├── Sticky_Pins.h                   # Master GPIO and I2C hardware pin mappings
├── Sticky_EPD.h / .cpp             # 3.97" 800x480 SSD1677 E-Paper display SPI driver & buffer
├── Sticky_Touch.h / .cpp           # Goodix GT911 capacitive touch driver & coordinate mapping
├── Sticky_Buzzer.h                 # Buzzer PWM audio controller with sound chimes
├── StreamDeck_Bitmap.h             # 48,000-byte 1-bit monochrome UI bitmap header
│
├── ReTerminalStreamDeck.exe        # Pre-compiled Windows Companion App (Seeed Studio edition)
├── StreamDeckManager.cs            # Complete Windows Host App C# source code
├── streamdeck_config.json          # User-defined macro actions and preferences
├── build.bat                       # 1-click Windows build script using native csc.exe
│
├── epd_ui_preview.png              # Factory 800x480 E-Paper UI artwork preview
├── custom_ui_800x480.png          # Active customized 800x480 E-Paper artwork
├── screenshot_app.png              # Screenshot of the Windows Companion Application
├── streamdeck.ahk                  # AutoHotkey listener script
├── serial_worker.py                # Python serial bridge script
│
├── arduino_firmware/               # Standalone Arduino IDE firmware package
├── windows_app/                    # Standalone Windows app package with source & config
└── final_reterminal_sticky_streamdeck.zip # Standalone complete project ZIP archive
```

---

## 8. Building from Source

Want to modify the Windows host app? Simply run [build.bat](build.bat) from Command Prompt or PowerShell:

```batch
build.bat
```

It uses the built-in Microsoft .NET Framework C# compiler (`csc.exe`) present on every Windows PC to compile `StreamDeckManager.cs` into `ReTerminalStreamDeck.exe` in under 2 seconds without requiring Visual Studio or external toolchains.

---

## 9. Troubleshooting & FAQ

<details>
<summary><b>1. The E-Paper screen stays blank on first boot</b></summary>

- The reTerminal Sticky requires driving `PIN_EPD_PWR_EN` (`GPIO 47`) **HIGH** to supply power to the SSD1677 display rail. The included firmware handles this automatically in `Sticky_EPD::begin()`.
- Also ensure that the power latch pins (`GPIO 45` and `GPIO 46`) are initialized on boot via `latchPower()`.
</details>

<details>
<summary><b>2. Touch screen does not respond to taps</b></summary>

- Make sure `PIN_TOUCH_PWR_EN` (`GPIO 42`) is driven **HIGH**. The Goodix GT911 will not initialize without its dedicated power rail active.
- Verify that touch I2C is running on `TwoWire(0)` using `PIN_TOUCH_SDA` (`GPIO 3`) and `PIN_TOUCH_SCL` (`GPIO 2`).
</details>

<details>
<summary><b>3. Windows app shows "Port: Disconnected"</b></summary>

- Verify you compiled in Arduino IDE with **USB CDC On Boot: Enabled**.
- Open Windows Device Manager and confirm that a "USB Serial Device (COMx)" appears under *Ports (COM & LPT)* when the device is plugged in.
- You can manually select the COM port in the app's top bar if auto-detection does not pick it up.
</details>

<details>
<summary><b>4. Arduino IDE reports out-of-memory or compile errors for PSRAM</b></summary>

- Set **PSRAM** to `"OPI PSRAM"` in the Arduino IDE Tools menu. The 800x480 1-bit display buffer and full image manipulation allocate from the onboard 8MB Octal PSRAM.
</details>

<details>
<summary><b>5. Device not detected for uploading in Arduino IDE</b></summary>

- Ensure you are using a data-capable USB-C cable.
- If Arduino IDE fails to connect during upload: hold the **AI / OK** button (`GPIO 4`), tap the reset button (or reconnect the cable), and release the button to force the ESP32-S3 into ROM bootloader mode.
</details>

---

## 📄 License

This project is licensed under the **MIT License** — feel free to use, modify, and distribute for personal or commercial projects.

Made with ❤️ for the [Seeed Studio](https://www.seeedstudio.com/) ecosystem.
