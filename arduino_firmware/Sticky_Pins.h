#pragma once

#include <Arduino.h>

// =============================================================================
// Seeed Studio reTerminal Sticky - Complete Pin Definitions & Hardware Mapping
// Reference: https://www.seeedstudio.com/sticky/docs/en/device-guide/hardware-overview/
// =============================================================================

// --- Power Latching & Management ---
#define PIN_POWER_HOLD      45   // Power Hold Rail Enable (Active HIGH)
#define PIN_POWER_LOCK      46   // Power Lock Pulse line

// --- E-Paper Display (3.97" 800x480 Black & White E-Ink, SSD1677) ---
#define PIN_EPD_SCK         13   // SPI Clock (Shared with MicroSD)
#define PIN_EPD_MOSI        14   // SPI MOSI / SDI (Shared with MicroSD)
#define PIN_EPD_CS          15   // EPD Chip Select (Active LOW)
#define PIN_EPD_DC          16   // EPD Data / Command (HIGH = Data, LOW = Command)
#define PIN_EPD_RST         17   // EPD Reset (Active LOW)
#define PIN_EPD_BUSY        18   // EPD Busy signal (Active HIGH during refresh/busy, LOW when idle)
#define PIN_EPD_PWR_EN      47   // EPD Power Rail Enable (Active HIGH)

#define EPD_WIDTH           800
#define EPD_HEIGHT          480

// --- Capacitive Touch Panel (Goodix GT911) ---
#define PIN_TOUCH_SCL        2   // Touch I2C SCL
#define PIN_TOUCH_SDA        3   // Touch I2C SDA
#define PIN_TOUCH_INT       21   // Touch Interrupt line
#define PIN_TOUCH_RST       41   // Touch Reset line
#define PIN_TOUCH_PWR_EN    42   // Touch Power Rail Enable (Active HIGH)
#define TOUCH_I2C_ADDR_5D   0x5D // Default GT911 I2C address
#define TOUCH_I2C_ADDR_14   0x14 // Alternative GT911 I2C address

// --- Physical Buttons (Active LOW with internal pull-up) ---
#define PIN_BTN_AI           4   // AI / Power / OK button
#define PIN_BTN_UP           5   // Up navigation button
#define PIN_BTN_DOWN         6   // Down navigation button

// --- Audio Prompt (Buzzer) ---
#define PIN_BUZZER          48   // PWM Buzzer output

// --- Onboard Sensors & Peripherals (I2C Bus 1) ---
#define PIN_SENSOR_SCL       0   // Shared Sensor I2C SCL
#define PIN_SENSOR_SDA       1   // Shared Sensor I2C SDA
#define I2C_ADDR_SHT40      0x44 // SHT40 Temperature & Humidity
#define I2C_ADDR_LSM6DS3TR  0x6A // 6-Axis IMU (or 0x6B)
#define I2C_ADDR_PCF8563    0x51 // PCF8563 Real-Time Clock
#define I2C_ADDR_BQ27220    0x55 // BQ27220 Fuel Gauge

// --- MicroSD Card (SPI Interface, shares SCK=13, MOSI=14) ---
#define PIN_SD_CS            8   // MicroSD Chip Select
#define PIN_SD_MISO         12   // MicroSD MISO

// --- Digital PDM Microphone ---
#define PIN_MIC_CLK         19   // PDM Clock
#define PIN_MIC_DATA        20   // PDM Data
