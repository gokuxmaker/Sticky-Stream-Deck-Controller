// =============================================================================
// Seeed Studio reTerminal Sticky - Stream Deck Controller
//
// Hardware Specs:
// - 3.97" 800x480 SSD1677 E-Paper Display (SPI)
// - Goodix GT911 Capacitive Touchscreen (I2C)
// - ESP32-S3 Native USB-C (USB CDC Serial at 115200 baud)
// - Piezo Buzzer on GPIO 48
// - Physical Buttons: AI/OK (GPIO 4), UP (GPIO 5), DOWN (GPIO 6)
//
// Power Management:
// - Long-press GPIO 4 (>= 2 sec): Powers down rails and enters Deep Sleep
// - Press GPIO 4 in Deep Sleep: Wakes up system and boots
// =============================================================================

#include <Arduino.h>
#include <Wire.h>
#include "USB.h"

#include "Sticky_Pins.h"
#include "Sticky_EPD.h"
#include "Sticky_Touch.h"
#include "Sticky_Buzzer.h"
#include "StreamDeck_Bitmap.h"

// Onboard Buzzer
StickyBuzzer Buzzer;
bool buzzerEnabled = true;

// Stream Deck Button Definition
struct StreamDeckButton {
    int16_t x;
    int16_t y;
    int16_t w;
    int16_t h;
    const char* name;      // Application / Function Name
};

// 15 Stream Deck Rectangles as designated
const StreamDeckButton STREAM_BUTTONS[15] = {
    // --- Row 1 ---
    {  12,  14, 147, 141, "Discord" },
    { 167,  11, 147, 141, "WhatsApp" },
    { 326,  12, 147, 141, "Autodesk / CAD" },
    { 480,  12, 147, 141, "Google Chrome" },
    { 637,  13, 147, 141, "Brave Browser" },

    // --- Row 2 ---
    {  12, 170, 147, 141, "Inkscape" },
    { 167, 167, 147, 141, "VS Code" },
    { 326, 168, 147, 141, "DaVinci Resolve" },
    { 480, 168, 147, 141, "KiCad" },
    { 637, 168, 147, 141, "WeChat" },

    // --- Row 3 ---
    {  13, 326, 147, 141, "Previous Track" },
    { 168, 323, 147, 141, "Play / Pause" },
    { 327, 324, 147, 141, "Next Track" },
    { 481, 324, 147, 141, "Volume Down" },
    { 638, 325, 147, 141, "Volume Up" }
};

// Touch state tracking & debounce
bool touchIsPressed = false;
int lastPressedIndex = -1;
uint32_t lastPressTime = 0;
const uint32_t DEBOUNCE_MS = 250;

// Physical Buttons Debounce & Long-Press
uint32_t lastBtnCheck = 0;
bool lastAiState = HIGH;
bool lastUpState = HIGH;
bool lastDownState = HIGH;

uint32_t aiPressStartTime = 0;
bool aiLongPressTriggered = false;

// Forward declarations
void latchPower();
void drawStreamDeckUI(bool fast = false);
void sendStreamDeckKey(int buttonIndex);
void handleSerialCommands();
void enterDeepSleep();

// Latch power rails for reTerminal Sticky
void latchPower() {
    pinMode(PIN_POWER_HOLD, OUTPUT);
    pinMode(PIN_POWER_LOCK, OUTPUT);
    digitalWrite(PIN_POWER_HOLD, HIGH);
    digitalWrite(PIN_POWER_LOCK, LOW);
    delayMicroseconds(10);
    digitalWrite(PIN_POWER_LOCK, HIGH);
    delayMicroseconds(10);
    digitalWrite(PIN_POWER_LOCK, LOW);
}

void setup() {
    // 1. Latch Board Power Controller
    latchPower();

    // 2. Initialize Serial Monitor (CDC)
    Serial.begin(115200);
    USB.begin();

    Serial.println("\n=============================================");
    Serial.println(" Seeed Studio reTerminal Sticky - Stream Deck ");
    Serial.println(" 15-Button Serial Controller                  ");
    Serial.println("=============================================");

    // 3. Initialize Buzzer & Play Welcome Tone
    Buzzer.begin();
    Buzzer.bootJingle();

    // 4. Initialize Physical Navigation Buttons
    pinMode(PIN_BTN_AI, INPUT_PULLUP);
    pinMode(PIN_BTN_UP, INPUT_PULLUP);
    pinMode(PIN_BTN_DOWN, INPUT_PULLUP);

    // 5. Initialize Capacitive Touch Screen (Goodix GT911)
    Serial.println("[Init] Capacitive Touchscreen (GT911)...");
    if (Touch.begin(Wire)) {
        Serial.println("[Touch] GT911 ready.");
    } else {
        Serial.println("[Touch] GT911 init failed! Retrying...");
    }

    // 6. Initialize E-Paper Display (SSD1677 800x480)
    Serial.println("[Init] E-Paper Display (SSD1677 800x480)...");
    if (Display.begin()) {
        Serial.println("[EPD] Framebuffer allocated. Rendering Stream Deck UI...");
        drawStreamDeckUI(false);
    } else {
        Serial.println("[EPD] Framebuffer allocation failed!");
    }

    Buzzer.successSound();
    Serial.println("[StreamDeck] System Ready. Touch any button to trigger action.");
}

void loop() {
    uint32_t now = millis();

    // -------------------------------------------------------------------------
    // 1. Capacitive Touch Polling & Hit-Testing
    // -------------------------------------------------------------------------
    if (Touch.readTouch()) {
        const TouchPoint& pt = Touch.getPoint(0);

        // Find which rectangle was touched
        int hitIndex = -1;
        for (int i = 0; i < 15; i++) {
            if (pt.x >= STREAM_BUTTONS[i].x && pt.x < (STREAM_BUTTONS[i].x + STREAM_BUTTONS[i].w) &&
                pt.y >= STREAM_BUTTONS[i].y && pt.y < (STREAM_BUTTONS[i].y + STREAM_BUTTONS[i].h)) {
                hitIndex = i;
                break;
            }
        }

        if (hitIndex >= 0) {
            if (!touchIsPressed || hitIndex != lastPressedIndex || (now - lastPressTime > DEBOUNCE_MS)) {
                touchIsPressed = true;
                lastPressedIndex = hitIndex;
                lastPressTime = now;

                sendStreamDeckKey(hitIndex);
            }
        }
    } else {
        touchIsPressed = false;
        lastPressedIndex = -1;
    }

    // -------------------------------------------------------------------------
    // 2. Physical Buttons Polling (Debounced at 50ms)
    // -------------------------------------------------------------------------
    if (now - lastBtnCheck > 50) {
        lastBtnCheck = now;

        bool aiState = digitalRead(PIN_BTN_AI);
        bool upState = digitalRead(PIN_BTN_UP);
        bool downState = digitalRead(PIN_BTN_DOWN);

        // AI / OK Button (GPIO 4) -> Short Press = EPD Refresh, Long Press (>= 2s) = Deep Sleep
        if (aiState == LOW) {
            if (lastAiState == HIGH) {
                // Just pressed down
                aiPressStartTime = now;
                aiLongPressTriggered = false;
            } else if (!aiLongPressTriggered && (now - aiPressStartTime >= 2000)) {
                // Long press threshold reached!
                aiLongPressTriggered = true;
                enterDeepSleep();
            }
        } else {
            // Button released
            if (lastAiState == LOW) {
                if (!aiLongPressTriggered) {
                    // Short press (< 2000ms): Force full screen refresh
                    Serial.println("[Button] AI Short Press -> Redraw & Refresh E-Paper");
                    if (buzzerEnabled) Buzzer.beep(3000, 50);
                    drawStreamDeckUI(false);
                }
            }
        }

        // UP Button (GPIO 5) -> Toggle Buzzer Audio Feedback
        if (lastUpState == HIGH && upState == LOW) {
            buzzerEnabled = !buzzerEnabled;
            Serial.printf("[Button] UP Pressed -> Audio Feedback: %s\n", buzzerEnabled ? "ON" : "OFF");
            if (buzzerEnabled) {
                Buzzer.beep(2500, 60);
            }
        }

        // DOWN Button (GPIO 6) -> Fast partial refresh
        if (lastDownState == HIGH && downState == LOW) {
            Serial.println("[Button] DOWN Pressed -> Fast Partial Screen Refresh");
            if (buzzerEnabled) Buzzer.beep(2000, 50);
            drawStreamDeckUI(true);
        }

        lastAiState = aiState;
        lastUpState = upState;
        lastDownState = downState;
    }

    // -------------------------------------------------------------------------
    // 3. Serial Command Interface (for host-side script testing/control)
    // -------------------------------------------------------------------------
    if (Serial.available()) {
        handleSerialCommands();
    }

    delay(10);
}

// -----------------------------------------------------------------------------
// Power down peripherals and enter Deep Sleep (GPIO 4 wakes up)
// -----------------------------------------------------------------------------
void enterDeepSleep() {
    Serial.println("\n[Power] Long-press GPIO 4 detected -> Entering Deep Sleep...");

    // Power-down chime
    if (buzzerEnabled) {
        Buzzer.beep(1800, 80);
        delay(100);
        Buzzer.beep(1200, 80);
        delay(100);
        Buzzer.beep(600, 200);
        delay(220);
    }

    // Power off peripheral rails (E-Paper display retains image with 0 power)
    digitalWrite(PIN_TOUCH_PWR_EN, LOW);
    digitalWrite(PIN_EPD_PWR_EN, LOW);

    Serial.println("[Power] Peripherals powered down. Press GPIO 4 (AI button) to wake up.");
    Serial.flush();

    // Wait until button is released before entering sleep to avoid immediate re-wake
    while (digitalRead(PIN_BTN_AI) == LOW) {
        delay(20);
    }
    delay(150);

    // Configure GPIO 4 (PIN_BTN_AI) active-LOW wakeup
    esp_sleep_enable_ext0_wakeup((gpio_num_t)PIN_BTN_AI, 0);
    esp_deep_sleep_start();
}

// -----------------------------------------------------------------------------
// Send Stream Deck trigger notification over Serial CDC
// -----------------------------------------------------------------------------
void sendStreamDeckKey(int buttonIndex) {
    if (buttonIndex < 0 || buttonIndex >= 15) return;

    const StreamDeckButton& btn = STREAM_BUTTONS[buttonIndex];

    // Tactile audio feedback
    if (buzzerEnabled) {
        Buzzer.click();
    }

    // Serial CDC notification for Windows host app
    Serial.printf("[StreamDeck] Button %d (%s) pressed\n",
                  buttonIndex + 1, btn.name);
}


// -----------------------------------------------------------------------------
// Draw Stream Deck UI bitmap onto 800x480 E-Paper Display
// -----------------------------------------------------------------------------
void drawStreamDeckUI(bool fast) {
    uint8_t* buffer = Display.getBuffer();
    uint32_t bufSize = Display.getBufferSize();

    if (!buffer || bufSize < 48000) {
        Serial.println("[EPD] Invalid framebuffer size!");
        return;
    }

    Serial.printf("[EPD] Copying 48KB Stream Deck bitmap (fast=%s)...\n", fast ? "true" : "false");
    memcpy(buffer, image_g1650_bits, 48000);

    Display.refresh(fast);
    Serial.println("[EPD] Stream Deck UI refresh complete.");
}

// -----------------------------------------------------------------------------
// Process serial commands from PC
// -----------------------------------------------------------------------------
void handleSerialCommands() {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();

    if (cmd.equalsIgnoreCase("REFRESH")) {
        Serial.println("[CMD] Executing full EPD refresh...");
        drawStreamDeckUI(false);
    } else if (cmd.equalsIgnoreCase("FAST_REFRESH")) {
        Serial.println("[CMD] Executing fast EPD refresh...");
        drawStreamDeckUI(true);
    } else if (cmd.equalsIgnoreCase("BEEP")) {
        Buzzer.beep(2000, 100);
        Serial.println("[CMD] Beep played.");
    } else if (cmd.startsWith("PRESS ")) {
        int btnNum = cmd.substring(6).toInt();
        if (btnNum >= 1 && btnNum <= 15) {
            Serial.printf("[CMD] Simulating button %d press...\n", btnNum);
            sendStreamDeckKey(btnNum - 1);
        } else {
            Serial.println("[CMD] Invalid button index (must be 1..15)");
        }
    } else if (cmd.startsWith("UPLOAD_BITMAP")) {
        uint8_t* buffer = Display.getBuffer();
        uint32_t bufSize = Display.getBufferSize();
        if (!buffer || bufSize < 48000) {
            Serial.println("[CMD] ERROR: Framebuffer unavailable");
            return;
        }
        Serial.println("[CMD] READY_FOR_BITMAP");
        Serial.flush();

        size_t bytesRead = 0;
        uint32_t startMs = millis();
        while (bytesRead < 48000 && (millis() - startMs < 8000)) {
            size_t avail = Serial.available();
            if (avail > 0) {
                size_t toRead = min(avail, (size_t)(48000 - bytesRead));
                size_t r = Serial.readBytes((char*)(buffer + bytesRead), toRead);
                bytesRead += r;
                startMs = millis();
            } else {
                delay(2);
            }
        }

        if (bytesRead == 48000) {
            Serial.println("[CMD] BITMAP_UPLOAD_SUCCESS");
            Buzzer.beep(2500, 80);
            Display.refresh(false);
            Serial.println("[EPD] Custom UI refreshed successfully.");
        } else {
            Serial.printf("[CMD] ERROR: Timeout reading bitmap (got %u / 48000 bytes)\n", bytesRead);
        }
    } else if (cmd.equalsIgnoreCase("HELP")) {
        Serial.println("\n--- Stream Deck Serial Commands ---");
        Serial.println("REFRESH         - Full E-ink display refresh");
        Serial.println("FAST_REFRESH    - Fast E-ink partial refresh");
        Serial.println("BEEP            - Test buzzer beep");
        Serial.println("PRESS <1-15>    - Simulate button press (Alt+1..Alt+#)");
        Serial.println("UPLOAD_BITMAP   - Stream 48KB 1-bit monochrome UI bitmap");
        Serial.println("HELP            - Display this help message");
        Serial.println("-----------------------------------\n");
    }
}
