#include "Sticky_EPD.h"

StickyEPD Display;

// Compact 5x7 ASCII Font table (chars 32 to 126)
static const uint8_t font5x7[] PROGMEM = {
    0x00, 0x00, 0x00, 0x00, 0x00, // Space (32)
    0x00, 0x00, 0x5F, 0x00, 0x00, // !
    0x00, 0x07, 0x00, 0x07, 0x00, // "
    0x14, 0x7F, 0x14, 0x7F, 0x14, // #
    0x24, 0x2A, 0x7F, 0x2A, 0x12, // $
    0x23, 0x13, 0x08, 0x64, 0x62, // %
    0x36, 0x49, 0x55, 0x22, 0x50, // &
    0x00, 0x05, 0x03, 0x00, 0x00, // '
    0x00, 0x1C, 0x22, 0x41, 0x00, // (
    0x00, 0x41, 0x22, 0x1C, 0x00, // )
    0x08, 0x2A, 0x1C, 0x2A, 0x08, // *
    0x08, 0x08, 0x3E, 0x08, 0x08, // +
    0x00, 0x50, 0x30, 0x00, 0x00, // ,
    0x08, 0x08, 0x08, 0x08, 0x08, // -
    0x00, 0x60, 0x60, 0x00, 0x00, // .
    0x20, 0x10, 0x08, 0x04, 0x02, // /
    0x3E, 0x51, 0x49, 0x45, 0x3E, // 0
    0x00, 0x42, 0x7F, 0x40, 0x00, // 1
    0x42, 0x61, 0x51, 0x49, 0x46, // 2
    0x21, 0x41, 0x45, 0x4B, 0x31, // 3
    0x18, 0x14, 0x12, 0x7F, 0x10, // 4
    0x27, 0x45, 0x45, 0x45, 0x39, // 5
    0x3C, 0x4A, 0x49, 0x49, 0x30, // 6
    0x01, 0x71, 0x09, 0x05, 0x03, // 7
    0x36, 0x49, 0x49, 0x49, 0x36, // 8
    0x06, 0x49, 0x49, 0x29, 0x1E, // 9
    0x00, 0x36, 0x36, 0x00, 0x00, // :
    0x00, 0x56, 0x36, 0x00, 0x00, // ;
    0x00, 0x08, 0x14, 0x22, 0x41, // <
    0x14, 0x14, 0x14, 0x14, 0x14, // =
    0x41, 0x22, 0x14, 0x08, 0x00, // >
    0x02, 0x01, 0x51, 0x09, 0x06, // ?
    0x32, 0x49, 0x79, 0x41, 0x3E, // @
    0x7E, 0x11, 0x11, 0x11, 0x7E, // A
    0x7F, 0x49, 0x49, 0x49, 0x36, // B
    0x3E, 0x41, 0x41, 0x41, 0x22, // C
    0x7F, 0x41, 0x41, 0x22, 0x1C, // D
    0x7F, 0x49, 0x49, 0x49, 0x41, // E
    0x7F, 0x09, 0x09, 0x01, 0x01, // F
    0x3E, 0x41, 0x41, 0x51, 0x32, // G
    0x7F, 0x08, 0x08, 0x08, 0x7F, // H
    0x00, 0x41, 0x7F, 0x41, 0x00, // I
    0x20, 0x40, 0x41, 0x3F, 0x01, // J
    0x7F, 0x08, 0x14, 0x22, 0x41, // K
    0x7F, 0x40, 0x40, 0x40, 0x40, // L
    0x7F, 0x02, 0x04, 0x02, 0x7F, // M
    0x7F, 0x04, 0x08, 0x10, 0x7F, // N
    0x3E, 0x41, 0x41, 0x41, 0x3E, // O
    0x7F, 0x09, 0x09, 0x09, 0x06, // P
    0x3E, 0x41, 0x51, 0x21, 0x5E, // Q
    0x7F, 0x09, 0x19, 0x29, 0x46, // R
    0x46, 0x49, 0x49, 0x49, 0x31, // S
    0x01, 0x01, 0x7F, 0x01, 0x01, // T
    0x3F, 0x40, 0x40, 0x40, 0x3F, // U
    0x1F, 0x20, 0x40, 0x20, 0x1F, // V
    0x7F, 0x20, 0x18, 0x20, 0x7F, // W
    0x63, 0x14, 0x08, 0x14, 0x63, // X
    0x03, 0x04, 0x78, 0x04, 0x03, // Y
    0x61, 0x51, 0x49, 0x45, 0x43, // Z
    0x00, 0x7F, 0x41, 0x41, 0x00, // [
    0x02, 0x04, 0x08, 0x10, 0x20, // "\"
    0x00, 0x41, 0x41, 0x7F, 0x00, // ]
    0x04, 0x02, 0x01, 0x02, 0x04, // ^
    0x40, 0x40, 0x40, 0x40, 0x40, // _
    0x00, 0x01, 0x02, 0x04, 0x00, // `
    0x20, 0x54, 0x54, 0x54, 0x78, // a
    0x7F, 0x48, 0x44, 0x44, 0x38, // b
    0x38, 0x44, 0x44, 0x44, 0x20, // c
    0x38, 0x44, 0x44, 0x48, 0x7F, // d
    0x38, 0x54, 0x54, 0x54, 0x18, // e
    0x08, 0x7E, 0x09, 0x01, 0x02, // f
    0x08, 0x14, 0x54, 0x54, 0x3C, // g
    0x7F, 0x08, 0x04, 0x04, 0x78, // h
    0x00, 0x44, 0x7D, 0x40, 0x00, // i
    0x20, 0x40, 0x44, 0x3D, 0x00, // j
    0x7F, 0x10, 0x28, 0x44, 0x00, // k
    0x00, 0x41, 0x7F, 0x40, 0x00, // l
    0x7C, 0x04, 0x18, 0x04, 0x78, // m
    0x7C, 0x08, 0x04, 0x04, 0x78, // n
    0x38, 0x44, 0x44, 0x44, 0x38, // o
    0x7C, 0x14, 0x14, 0x14, 0x08, // p
    0x08, 0x14, 0x14, 0x18, 0x7C, // q
    0x7C, 0x08, 0x04, 0x04, 0x08, // r
    0x48, 0x54, 0x54, 0x54, 0x20, // s
    0x04, 0x3F, 0x44, 0x40, 0x20, // t
    0x3C, 0x40, 0x40, 0x20, 0x7C, // u
    0x1C, 0x20, 0x40, 0x20, 0x1C, // v
    0x3C, 0x40, 0x30, 0x40, 0x3C, // w
    0x44, 0x28, 0x10, 0x28, 0x44, // x
    0x0C, 0x50, 0x50, 0x50, 0x3C, // y
    0x44, 0x64, 0x54, 0x4C, 0x44, // z
    0x00, 0x08, 0x36, 0x41, 0x00, // {
    0x00, 0x00, 0x7F, 0x00, 0x00, // |
    0x00, 0x41, 0x36, 0x08, 0x00, // }
    0x08, 0x08, 0x2A, 0x1C, 0x08  // ~
};

StickyEPD::StickyEPD() : _buffer(nullptr), _bufferSize((EPD_WIDTH / 8) * EPD_HEIGHT), _spi(&SPI) {}

StickyEPD::~StickyEPD() {
    if (_buffer) {
        free(_buffer);
        _buffer = nullptr;
    }
}

static inline uint8_t reverseByteBits(uint8_t b) {
    b = (b & 0xF0) >> 4 | (b & 0x0F) << 4;
    b = (b & 0xCC) >> 2 | (b & 0x33) << 2;
    b = (b & 0xAA) >> 1 | (b & 0x55) << 1;
    return b;
}

void StickyEPD::sendCommand(uint8_t cmd) {
    _spi->beginTransaction(SPISettings(10000000, MSBFIRST, SPI_MODE0));
    digitalWrite(PIN_EPD_DC, LOW);
    digitalWrite(PIN_EPD_CS, LOW);
    _spi->transfer(cmd);
    digitalWrite(PIN_EPD_CS, HIGH);
    _spi->endTransaction();
}

void StickyEPD::sendData(uint8_t data) {
    _spi->beginTransaction(SPISettings(10000000, MSBFIRST, SPI_MODE0));
    digitalWrite(PIN_EPD_DC, HIGH);
    digitalWrite(PIN_EPD_CS, LOW);
    _spi->transfer(data);
    digitalWrite(PIN_EPD_CS, HIGH);
    _spi->endTransaction();
}

void StickyEPD::sendData(const uint8_t *data, uint32_t len) {
    _spi->beginTransaction(SPISettings(10000000, MSBFIRST, SPI_MODE0));
    digitalWrite(PIN_EPD_DC, HIGH);
    digitalWrite(PIN_EPD_CS, LOW);
    _spi->transferBytes(data, NULL, len);
    digitalWrite(PIN_EPD_CS, HIGH);
    _spi->endTransaction();
}

void StickyEPD::sendFrameMirrored(const uint8_t *src) {
    uint8_t rowBuf[100];
    const size_t rowBytes = EPD_WIDTH / 8; // 100 bytes per line

    _spi->beginTransaction(SPISettings(10000000, MSBFIRST, SPI_MODE0));
    digitalWrite(PIN_EPD_DC, HIGH);
    digitalWrite(PIN_EPD_CS, LOW);
    for (int y = 0; y < EPD_HEIGHT; y++) {
        const uint8_t *line = src + (y * rowBytes);
        for (int i = 0; i < (int)rowBytes; i++) {
            rowBuf[i] = reverseByteBits(line[(rowBytes - 1) - i]);
        }
        _spi->transferBytes(rowBuf, NULL, rowBytes);
    }
    digitalWrite(PIN_EPD_CS, HIGH);
    _spi->endTransaction();
}

void StickyEPD::waitBusy(uint32_t timeoutMs) {
    uint32_t start = millis();
    delay(10);
    // On SSD1677: BUSY is HIGH (1) while busy, goes LOW (0) when IDLE/READY
    while (digitalRead(PIN_EPD_BUSY) == HIGH) {
        delay(5);
        if (millis() - start > timeoutMs) {
            Serial.println("[EPD] Wait busy timeout!");
            break;
        }
    }
}

void StickyEPD::hardwareReset() {
    digitalWrite(PIN_EPD_RST, LOW);
    delay(10);
    digitalWrite(PIN_EPD_RST, HIGH);
    delay(10);
    waitBusy(5000);
}

bool StickyEPD::begin() {
    // 1. Allocate Framebuffer (48 KB) - prefer PSRAM if available
#if defined(BOARD_HAS_PSRAM) || defined(CONFIG_SPIRAM)
    if (psramFound()) {
        _buffer = (uint8_t*)ps_malloc(_bufferSize);
    }
#endif
    if (!_buffer) {
        _buffer = (uint8_t*)malloc(_bufferSize);
    }

    if (!_buffer) {
        Serial.println("[EPD] Failed to allocate framebuffer memory!");
        return false;
    }

    clear(EPD_COLOR_WHITE);

    // 2. Configure Power Rail and SD Chip Select to avoid SPI contention
    pinMode(PIN_EPD_PWR_EN, OUTPUT);
    digitalWrite(PIN_EPD_PWR_EN, HIGH); // Power rail enable

    pinMode(PIN_SD_CS, OUTPUT);
    digitalWrite(PIN_SD_CS, HIGH); // Deselect SD card

    pinMode(PIN_EPD_CS, OUTPUT);
    pinMode(PIN_EPD_DC, OUTPUT);
    pinMode(PIN_EPD_RST, OUTPUT);
    pinMode(PIN_EPD_BUSY, INPUT);

    digitalWrite(PIN_EPD_CS, HIGH);
    digitalWrite(PIN_EPD_DC, HIGH);

    // 3. Initialize SPI bus (SCK = 13, MOSI = 14)
    _spi->begin(PIN_EPD_SCK, -1, PIN_EPD_MOSI, PIN_EPD_CS);
    _spi->setFrequency(10000000); // 10 MHz

    Serial.println("[EPD] Hardware initialized (SSD1677 800x480).");
    return true;
}

void StickyEPD::powerOn(bool fast) {
    digitalWrite(PIN_EPD_PWR_EN, HIGH);
    delay(10);
    initDisplay(fast);
}

void StickyEPD::powerOff() {
    // Put SSD1677 to Deep Sleep Mode 1
    sendCommand(0x10);
    sendData(0x03);
    delay(100);
}

void StickyEPD::sleep() {
    powerOff();
    digitalWrite(PIN_EPD_PWR_EN, LOW); // Cut power rail
}

void StickyEPD::initDisplay(bool fast) {
    hardwareReset();

    if (!fast) {
        sendCommand(0x12); // Software Reset
        waitBusy(10000);
    }

    // SSD1677 initialization sequence for 800x480 panel
    sendCommand(0x18); // Temperature sensor selection: internal
    sendData(0x80);

    sendCommand(0x3C); // Border Waveform Control
    sendData(fast ? 0x80 : 0x01); // 0x01 for full, 0x80 for partial

    const uint8_t softstart[] = {0xAE, 0xC7, 0xC3, 0xC0, 0x80};
    sendCommand(0x0C); // Booster soft start control
    sendData(softstart, sizeof(softstart));

    const uint8_t driver_output[] = {
        (uint8_t)((EPD_HEIGHT - 1) & 0xFF),
        (uint8_t)((EPD_HEIGHT - 1) >> 8),
        0x02
    };
    sendCommand(0x01); // Driver output control
    sendData(driver_output, sizeof(driver_output));

    sendCommand(0x11); // Data entry mode: X increment, Y increment
    sendData(0x03);

    const uint8_t ram_x[] = {
        0x00,
        0x00,
        (uint8_t)((EPD_WIDTH - 1) & 0xFF),
        (uint8_t)((EPD_WIDTH - 1) >> 8)
    };
    sendCommand(0x44); // Set RAM X address start / end
    sendData(ram_x, sizeof(ram_x));

    const uint8_t ram_y[] = {
        0x00,
        0x00,
        (uint8_t)((EPD_HEIGHT - 1) & 0xFF),
        (uint8_t)((EPD_HEIGHT - 1) >> 8)
    };
    sendCommand(0x45); // Set RAM Y address start / end
    sendData(ram_y, sizeof(ram_y));

    const uint8_t zero2[] = {0x00, 0x00};
    sendCommand(0x4E); // Set RAM X address counter
    sendData(zero2, sizeof(zero2));

    sendCommand(0x4F); // Set RAM Y address counter
    sendData(zero2, sizeof(zero2));

    waitBusy(10000);
}

void StickyEPD::clear(uint8_t color) {
    if (!_buffer) return;
    memset(_buffer, (color == EPD_COLOR_WHITE) ? 0xFF : 0x00, _bufferSize);
}

void StickyEPD::drawPixel(int16_t x, int16_t y, uint8_t color) {
    if (x < 0 || x >= EPD_WIDTH || y < 0 || y >= EPD_HEIGHT || !_buffer) return;
    uint32_t byteIdx = (y * (EPD_WIDTH / 8)) + (x / 8);
    uint8_t bitMask = 0x80 >> (x % 8);

    if (color == EPD_COLOR_WHITE) {
        _buffer[byteIdx] |= bitMask;
    } else {
        _buffer[byteIdx] &= ~bitMask;
    }
}

void StickyEPD::drawLine(int16_t x0, int16_t y0, int16_t x1, int16_t y1, uint8_t color) {
    int16_t dx = abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
    int16_t dy = -abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
    int16_t err = dx + dy, e2;

    while (true) {
        drawPixel(x0, y0, color);
        if (x0 == x1 && y0 == y1) break;
        e2 = 2 * err;
        if (e2 >= dy) { err += dy; x0 += sx; }
        if (e2 <= dx) { err += dx; y0 += sy; }
    }
}

void StickyEPD::drawRect(int16_t x, int16_t y, int16_t w, int16_t h, uint8_t color) {
    drawLine(x, y, x + w - 1, y, color);
    drawLine(x, y + h - 1, x + w - 1, y + h - 1, color);
    drawLine(x, y, x, y + h - 1, color);
    drawLine(x + w - 1, y, x + w - 1, y + h - 1, color);
}

void StickyEPD::fillRect(int16_t x, int16_t y, int16_t w, int16_t h, uint8_t color) {
    for (int16_t i = x; i < x + w; i++) {
        for (int16_t j = y; j < y + h; j++) {
            drawPixel(i, j, color);
        }
    }
}

void StickyEPD::drawCircle(int16_t x0, int16_t y0, int16_t r, uint8_t color) {
    int16_t f = 1 - r;
    int16_t ddF_x = 1;
    int16_t ddF_y = -2 * r;
    int16_t x = 0;
    int16_t y = r;

    drawPixel(x0, y0 + r, color);
    drawPixel(x0, y0 - r, color);
    drawPixel(x0 + r, y0, color);
    drawPixel(x0 - r, y0, color);

    while (x < y) {
        if (f >= 0) {
            y--;
            ddF_y += 2;
            f += ddF_y;
        }
        x++;
        ddF_x += 2;
        f += ddF_x;

        drawPixel(x0 + x, y0 + y, color);
        drawPixel(x0 - x, y0 + y, color);
        drawPixel(x0 + x, y0 - y, color);
        drawPixel(x0 - x, y0 - y, color);
        drawPixel(x0 + y, y0 + x, color);
        drawPixel(x0 - y, y0 + x, color);
        drawPixel(x0 + y, y0 - x, color);
        drawPixel(x0 - y, y0 - x, color);
    }
}

void StickyEPD::fillCircle(int16_t x0, int16_t y0, int16_t r, uint8_t color) {
    for (int16_t y = -r; y <= r; y++) {
        for (int16_t x = -r; x <= r; x++) {
            if (x * x + y * y <= r * r) {
                drawPixel(x0 + x, y0 + y, color);
            }
        }
    }
}

void StickyEPD::drawChar(int16_t x, int16_t y, char c, uint8_t color, uint8_t bg, uint8_t size) {
    if (c < 32 || c > 126) c = '?';
    uint16_t fontOffset = (c - 32) * 5;

    for (int8_t i = 0; i < 5; i++) {
        uint8_t line = pgm_read_byte(&font5x7[fontOffset + i]);
        for (int8_t j = 0; j < 8; j++) {
            uint8_t pixelColor = (line & (1 << j)) ? color : bg;
            if (size == 1) {
                drawPixel(x + i, y + j, pixelColor);
            } else {
                fillRect(x + (i * size), y + (j * size), size, size, pixelColor);
            }
        }
    }
    // 1-pixel gap after character
    if (bg != color) {
        if (size == 1) {
            for (int8_t j = 0; j < 8; j++) drawPixel(x + 5, y + j, bg);
        } else {
            fillRect(x + (5 * size), y, size, 8 * size, bg);
        }
    }
}

void StickyEPD::drawString(int16_t x, int16_t y, const char *str, uint8_t color, uint8_t bg, uint8_t size) {
    int16_t curX = x;
    int16_t curY = y;
    while (*str) {
        if (*str == '\n') {
            curX = x;
            curY += 10 * size;
        } else {
            drawChar(curX, curY, *str, color, bg, size);
            curX += 6 * size;
        }
        str++;
    }
}

void StickyEPD::refresh(bool fast) {
    if (!_buffer) return;
    powerOn(fast);

    // 1. Send previous image plane (0x26)
    sendCommand(0x26);
    sendFrameMirrored(_buffer);

    // 2. Send current image plane (0x24)
    sendCommand(0x24);
    sendFrameMirrored(_buffer);

    // 3. Display Update Control (0x22): 0xF7 for full, 0xFF for fast/partial
    sendCommand(0x22);
    sendData(fast ? 0xFF : 0xF7);

    // 4. Master Activation (0x20) - executes refresh
    sendCommand(0x20);
    waitBusy(15000);

    // 5. Power down display controller
    powerOff();
}
