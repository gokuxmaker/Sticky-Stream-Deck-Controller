#include "Sticky_Touch.h"
#include "Sticky_Pins.h"
#include <string.h>

StickyTouch Touch;

// Global IRQ flag for ISR
static volatile bool gt911IRQ = false;

static void IRAM_ATTR _gt911_irq_handler() {
    gt911IRQ = true;
}

StickyTouch::StickyTouch() 
    : _wire(&Wire), 
      _intPin(PIN_TOUCH_INT), 
      _rstPin(PIN_TOUCH_RST), 
      _pwrPin(PIN_TOUCH_PWR_EN),
      _addr(GT911_I2C_ADDR_5D), 
      _contacts(0),
      _rotation(Rotate::_0) {
    memset(&_info, 0, sizeof(_info));
    memset(_gtPoints, 0, sizeof(_gtPoints));
    memset(_points, 0, sizeof(_points));
    _emptyPoint = {0, 0, 0, 0};
}

void StickyTouch::reset(uint8_t targetAddr) {
    _addr = targetAddr;

    delay(10);
    pinMode(_intPin, OUTPUT);
    pinMode(_rstPin, OUTPUT);

    digitalWrite(_intPin, LOW);
    digitalWrite(_rstPin, LOW);

    delay(15);

    // Target address selection:
    // INT=1 -> 0x14, INT=0 -> 0x5D
    digitalWrite(_intPin, (_addr == GT911_I2C_ADDR_14) ? HIGH : LOW);

    delayMicroseconds(120);

    // Release reset line: change pin mode to INPUT to allow external pullup to pull HIGH
    // (exact waveform per alex-code/GT911)
    pinMode(_rstPin, INPUT);

    delay(10);

    digitalWrite(_intPin, LOW);
    delay(55);

    // Set INT pin to INPUT for interrupt / sensing
    pinMode(_intPin, INPUT);
    delay(50);
}

bool StickyTouch::write(uint16_t reg, uint8_t data) {
    if (!_wire) return false;
    _wire->beginTransmission(_addr);
    _wire->write((uint8_t)(reg >> 8));
    _wire->write((uint8_t)(reg & 0xFF));
    _wire->write(data);
    return (_wire->endTransmission() == 0);
}

uint8_t StickyTouch::read(uint16_t reg) {
    if (!_wire) return 0;
    _wire->beginTransmission(_addr);
    _wire->write((uint8_t)(reg >> 8));
    _wire->write((uint8_t)(reg & 0xFF));
    if (_wire->endTransmission() != 0) return 0;

    if (_wire->requestFrom(_addr, (uint8_t)1) == 1) {
        return _wire->read();
    }
    return 0;
}

bool StickyTouch::writeBytes(uint16_t reg, const uint8_t *data, uint16_t size) {
    if (!_wire) return false;
    _wire->beginTransmission(_addr);
    _wire->write((uint8_t)(reg >> 8));
    _wire->write((uint8_t)(reg & 0xFF));
    for (uint16_t i = 0; i < size; i++) {
        _wire->write(data[i]);
    }
    return (_wire->endTransmission() == 0);
}

bool StickyTouch::readBytes(uint16_t reg, uint8_t *data, uint16_t size) {
    if (!_wire) return false;
    _wire->beginTransmission(_addr);
    _wire->write((uint8_t)(reg >> 8));
    _wire->write((uint8_t)(reg & 0xFF));
    if (_wire->endTransmission() != 0) return false;

    uint8_t readCount = _wire->requestFrom(_addr, (uint8_t)size);
    if (readCount != size) return false;

    for (uint16_t i = 0; i < size; i++) {
        data[i] = _wire->read();
    }
    return true;
}

bool StickyTouch::readInfo() {
    if (readBytes(GT911_REG_DATA, (uint8_t*)&_info, sizeof(_info))) {
        return true;
    }
    return false;
}

int8_t StickyTouch::readTouches() {
    uint32_t timeout = millis() + 20;
    do {
        uint8_t flag = read(GT911_REG_COORD_ADDR);
        if ((flag & 0x80) && ((flag & 0x0F) <= GT911_MAX_CONTACTS)) {
            write(GT911_REG_COORD_ADDR, 0); // Acknowledge buffer
            return (flag & 0x0F);
        }
        delay(1);
    } while (millis() < timeout);

    return 0;
}

bool StickyTouch::readTouchPoints() {
    // Points start at 0x814F (GT911_REG_COORD_ADDR + 1)
    bool result = readBytes(GT911_REG_COORD_ADDR + 1, (uint8_t*)_gtPoints, sizeof(GTPoint) * GT911_MAX_CONTACTS);

    if (result) {
        for (uint8_t i = 0; i < GT911_MAX_CONTACTS; i++) {
            uint16_t x = _gtPoints[i].x;
            uint16_t y = _gtPoints[i].y;

            // If touch digitizer orientation is portrait (e.g. 480x800) but display is landscape (800x480),
            // swap axes so coordinates match the landscape display
            if (_info.xResolution > 0 && _info.yResolution > 0 && _info.xResolution < _info.yResolution) {
                uint16_t temp = x;
                x = y;
                y = temp;
                if (_rotation == Rotate::_180) {
                    if (_info.yResolution > 0) x = _info.yResolution - x;
                    if (_info.xResolution > 0) y = _info.xResolution - y;
                }
                if (_info.yResolution != EPD_WIDTH) {
                    x = (uint32_t)x * EPD_WIDTH / _info.yResolution;
                }
                if (_info.xResolution != EPD_HEIGHT) {
                    y = (uint32_t)y * EPD_HEIGHT / _info.xResolution;
                }
            } else {
                if (_rotation == Rotate::_180) {
                    if (_info.xResolution > 0) x = _info.xResolution - x;
                    if (_info.yResolution > 0) y = _info.yResolution - y;
                }
                if (_info.xResolution > 0 && _info.xResolution != EPD_WIDTH) {
                    x = (uint32_t)x * EPD_WIDTH / _info.xResolution;
                }
                if (_info.yResolution > 0 && _info.yResolution != EPD_HEIGHT) {
                    y = (uint32_t)y * EPD_HEIGHT / _info.yResolution;
                }
            }

            if (x >= EPD_WIDTH) x = EPD_WIDTH - 1;
            if (y >= EPD_HEIGHT) y = EPD_HEIGHT - 1;

            _points[i].id = _gtPoints[i].trackId;
            _points[i].x = x;
            _points[i].y = y;
            _points[i].size = _gtPoints[i].area;
        }
    }
    return result;
}

bool StickyTouch::begin(TwoWire &wire) {
    _wire = &wire;

    // 1. Power on GT911 touch panel rail
    pinMode(_pwrPin, OUTPUT);
    digitalWrite(_pwrPin, HIGH);
    delay(300); // 300ms rail stabilization (matches alex-code/GT911 recommendation)

    // 2. Initialize Touch I2C Bus at 100 kHz
    _wire->begin(PIN_TOUCH_SDA, PIN_TOUCH_SCL, 100000);

    // 3. Reset and probe for address 0x5D
    bool detected = false;
    Serial.println("[Touch] Resetting GT911 for address 0x5D...");
    reset(GT911_I2C_ADDR_5D);
    delay(50);

    _wire->beginTransmission(GT911_I2C_ADDR_5D);
    if (_wire->endTransmission() == 0) {
        _addr = GT911_I2C_ADDR_5D;
        detected = true;
    } else {
        Serial.println("[Touch] 0x5D did not respond. Resetting for 0x14...");
        reset(GT911_I2C_ADDR_14);
        delay(50);

        _wire->beginTransmission(GT911_I2C_ADDR_14);
        if (_wire->endTransmission() == 0) {
            _addr = GT911_I2C_ADDR_14;
            detected = true;
        }
    }

    if (!detected) {
        // I2C bus scan fallback
        for (uint8_t a = 1; a < 127; a++) {
            _wire->beginTransmission(a);
            if (_wire->endTransmission() == 0) {
                if (a == GT911_I2C_ADDR_5D || a == GT911_I2C_ADDR_14) {
                    _addr = a;
                    detected = true;
                    break;
                }
            }
        }
    }

    if (!detected) {
        Serial.println("[Touch] ERROR: GT911 not responding on 0x5D or 0x14!");
        return false;
    }

    // 4. Attach interrupt to INT pin
    pinMode(_intPin, INPUT);
    attachInterrupt(_intPin, _gt911_irq_handler, CHANGE);

    // 5. Read info (product ID and resolution)
    if (readInfo()) {
        Serial.printf("[Touch] GT911 detected at 0x%02X | Product ID: %.4s | Resolution: %u x %u\n",
                      _addr, _info.productId, _info.xResolution, _info.yResolution);
    } else {
        Serial.printf("[Touch] GT911 detected at 0x%02X (readInfo failed)\n", _addr);
    }

    // 6. Clear coordinate buffer ready flag
    write(GT911_REG_COORD_ADDR, 0);

    return true;
}

uint8_t StickyTouch::touched(bool polling) {
    bool irq = false;
    if (!polling) {
        noInterrupts();
        irq = gt911IRQ;
        gt911IRQ = false;
        interrupts();
    } else {
        irq = true;
    }

    // If interrupt didn't fire, also check register flag directly
    if (!irq) {
        uint8_t flag = read(GT911_REG_COORD_ADDR);
        if ((flag & 0x80) && ((flag & 0x0F) <= GT911_MAX_CONTACTS)) {
            write(GT911_REG_COORD_ADDR, 0);
            _contacts = (flag & 0x0F);
            if (_contacts > 0) {
                readTouchPoints();
            }
            return _contacts;
        }
        return 0;
    }

    uint8_t contacts = readTouches();
    if (contacts > 0) {
        readTouchPoints();
    }
    _contacts = contacts;
    return contacts;
}

bool StickyTouch::readTouch() {
    return (touched(true) > 0); // Polling mode returns true when touch detected
}

const TouchPoint& StickyTouch::getPoint(uint8_t num) {
    return (num < GT911_MAX_CONTACTS) ? _points[num] : _emptyPoint;
}
