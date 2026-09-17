#pragma once

#include <Arduino.h>
#include "Sticky_Pins.h"

class StickyBuzzer {
public:
    StickyBuzzer() {}

    void begin() {
        pinMode(PIN_BUZZER, OUTPUT);
        digitalWrite(PIN_BUZZER, LOW);
    }

    void beep(uint16_t freq = 2000, uint16_t durationMs = 80) {
        tone(PIN_BUZZER, freq, durationMs);
    }

    void click() {
        beep(2800, 25);
    }

    void successSound() {
        tone(PIN_BUZZER, 1046, 70); // C6
        delay(80);
        tone(PIN_BUZZER, 1318, 70); // E6
        delay(80);
        tone(PIN_BUZZER, 1568, 120); // G6
    }

    void alertSound() {
        tone(PIN_BUZZER, 800, 100);
        delay(120);
        tone(PIN_BUZZER, 600, 150);
    }

    void bootJingle() {
        tone(PIN_BUZZER, 523, 60);  // C5
        delay(70);
        tone(PIN_BUZZER, 659, 60);  // E5
        delay(70);
        tone(PIN_BUZZER, 784, 60);  // G5
        delay(70);
        tone(PIN_BUZZER, 1046, 120); // C6
    }

    void stop() {
        noTone(PIN_BUZZER);
        digitalWrite(PIN_BUZZER, LOW);
    }
};

extern StickyBuzzer Buzzer;
