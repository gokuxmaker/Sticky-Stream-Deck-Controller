#pragma once

#include <Arduino.h>
#include <SPI.h>
#include "Sticky_Pins.h"


#define EPD_COLOR_BLACK 0
#define EPD_COLOR_WHITE 1

class StickyEPD {
public:
    StickyEPD();
    ~StickyEPD();

    bool begin();
    void powerOn(bool fast = false);
    void powerOff();
    void sleep();

    // Framebuffer operations (in-memory)
    void clear(uint8_t color = EPD_COLOR_WHITE);
    void drawPixel(int16_t x, int16_t y, uint8_t color);
    void drawLine(int16_t x0, int16_t y0, int16_t x1, int16_t y1, uint8_t color);
    void drawRect(int16_t x, int16_t y, int16_t w, int16_t h, uint8_t color);
    void fillRect(int16_t x, int16_t y, int16_t w, int16_t h, uint8_t color);
    void drawCircle(int16_t x0, int16_t y0, int16_t r, uint8_t color);
    void fillCircle(int16_t x0, int16_t y0, int16_t r, uint8_t color);
    void drawChar(int16_t x, int16_t y, char c, uint8_t color, uint8_t bg, uint8_t size = 1);
    void drawString(int16_t x, int16_t y, const char *str, uint8_t color, uint8_t bg, uint8_t size = 1);

    // Hardware update
    void refresh(bool fast = false);

    uint8_t* getBuffer() { return _buffer; }
    uint32_t getBufferSize() const { return _bufferSize; }

private:
    uint8_t *_buffer;
    uint32_t _bufferSize;
    SPIClass *_spi;

    void sendCommand(uint8_t cmd);
    void sendData(uint8_t data);
    void sendData(const uint8_t *data, uint32_t len);
    void waitBusy(uint32_t timeoutMs = 15000);
    void hardwareReset();
    void initDisplay(bool fast = false);
    void sendFrameMirrored(const uint8_t *src);
};

extern StickyEPD Display;
