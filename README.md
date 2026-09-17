# Seeed Studio reTerminal Sticky - Arduino Firmware & Drivers

[![MCU: ESP32-S3](https://img.shields.io/badge/MCU-ESP32--S3%20(16MB%20Flash%20%2F%208MB%20OPI%20PSRAM)-blue?logo=espressif)](https://www.seeedstudio.com/)
[![Display: 3.97" E-Paper](https://img.shields.io/badge/Display-3.97%22%20800x480%20SSD1677-orange)](#hardware-pinout-reference)
[![Touch: Capacitive GT911](https://img.shields.io/badge/Touch-Goodix%20GT911%20I2C-green)](#hardware-pinout-reference)
[![Arduino: ESP32 Core](https://img.shields.io/badge/Arduino%20IDE-ESP32%20Core%20v2%20%2F%20v3-00979D?logo=arduino)](reterminal_sticky_arduino.ino)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen)](../README.md)

This folder contains the complete, standalone Arduino firmware and driver suite for the **Seeed Studio reTerminal Sticky** (ESP32-S3), designed specifically for compilation and flashing with the **Arduino IDE**.

It includes full drivers and operational logic for:
- **3.97" 800x480 SSD1677 E-Paper Display (SPI)** with power rail control (`GPIO 47`)
- **Goodix GT911 Capacitive Touch Screen (I2C)** with power rail control (`GPIO 42`)
- **Piezo Buzzer (PWM)** with click and musical feedback (`GPIO 48`)
- **3 Physical Buttons** (`GPIO 4` AI/Power/Wakeup, `GPIO 5` Up, `GPIO 6` Down)
- **Deep Sleep Power Management** (long-press GPIO 4 to shut down, tap to wake)
- **USB CDC Serial Communication (115200 baud)** for real-time Stream Deck triggers and live 48KB bitmap streaming

---

## Hardware Pinout Reference

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

## Arduino IDE Setup & Uploading Instructions

### 1. Add ESP32 Boards URL
In Arduino IDE, open **File > Preferences** and add to **Additional boards manager URLs**:
```text
https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
```

### 2. Install ESP32 Board Core
Open **Tools > Board > Boards Manager...**, search for `esp32`, and install version **2.0.14+** or **3.0.x**.

### 3. Open Sketch & Configure Settings
Open `reterminal_sticky_arduino.ino` and set the following in the **Tools** menu:
- **Board**: `"ESP32S3 Dev Module"`
- **USB CDC On Boot**: `"Enabled"` *(Required for Serial & Host App)*
- **Upload Mode**: `"UART0 / Hardware CDC"`
- **Flash Size**: `"16MB (128Mb)"`
- **Flash Mode**: `"QIO 80MHz"`
- **Partition Scheme**: `"16MB Flash (3MB APP/9.9MB FATFS)"` or `"Default 4MB with spiffs"`
- **PSRAM**: `"OPI PSRAM"` *(Required for 800x480 framebuffers)*
- **Port**: Select your device COM port

### 4. Upload
Click the **Upload (➔)** button or press `Ctrl + U`.

---

## Serial Command Protocol (115200 baud)

- `REFRESH`: Full E-Paper screen refresh.
- `FAST_REFRESH`: Partial fast refresh.
- `BEEP`: Play buzzer confirmation tone.
- `PRESS <1-15>`: Simulate button tap.
- `UPLOAD_BITMAP`: Stream 48,000 raw bytes for instant display update over USB CDC.
- `HELP`: Display command overview.
