#pragma once

#include <Arduino.h>
#include <Wire.h>

#define GT911_I2C_ADDR_5D  0x5D
#define GT911_I2C_ADDR_14  0x14

#define GT911_MAX_CONTACTS 5

#define GT911_REG_CFG        0x8047
#define GT911_REG_CHECKSUM   0x80FF
#define GT911_REG_DATA       0x8140
#define GT911_REG_ID         0x8140
#define GT911_REG_COORD_ADDR 0x814E

struct __attribute__((packed)) GTInfo {
    char productId[4];      // 0x8140 - 0x8143
    uint16_t fwId;          // 0x8144 - 0x8145
    uint16_t xResolution;   // 0x8146 - 0x8147
    uint16_t yResolution;   // 0x8148 - 0x8149
    uint8_t vendorId;       // 0x814A
};

struct __attribute__((packed)) GTPoint {
    uint8_t trackId;        // 0x814F
    uint16_t x;             // 0x8150 - 0x8151
    uint16_t y;             // 0x8152 - 0x8153
    uint16_t area;          // 0x8154 - 0x8155
    uint8_t reserved;       // 0x8156
};

// TouchPoint for compatibility with sketch
struct TouchPoint {
    uint8_t id;
    uint16_t x;
    uint16_t y;
    uint16_t size;
};

class StickyTouch {
public:
    enum class Rotate {
        _0,
        _90,
        _180,
        _270,
    };

    StickyTouch();

    bool begin(TwoWire &wire = Wire);
    bool readTouch();

    uint8_t touched(bool polling = false);
    const TouchPoint& getPoint(uint8_t num = 0);
    uint8_t getPointCount() const { return _contacts; }
    bool isPressed() const { return _contacts > 0; }
    uint8_t getI2CAddress() const { return _addr; }
    const GTInfo& getInfo() const { return _info; }

    void setRotation(Rotate rotation) { _rotation = rotation; }

private:
    TwoWire *_wire;
    int8_t _intPin;
    int8_t _rstPin;
    int8_t _pwrPin;
    uint8_t _addr;
    uint8_t _contacts;

    GTInfo _info;
    GTPoint _gtPoints[GT911_MAX_CONTACTS];
    TouchPoint _points[GT911_MAX_CONTACTS];
    TouchPoint _emptyPoint;

    Rotate _rotation;

    void reset(uint8_t targetAddr);
    bool write(uint16_t reg, uint8_t data);
    uint8_t read(uint16_t reg);
    bool writeBytes(uint16_t reg, const uint8_t *data, uint16_t size);
    bool readBytes(uint16_t reg, uint8_t *data, uint16_t size);
    int8_t readTouches();
    bool readTouchPoints();
    bool readInfo();
};

extern StickyTouch Touch;
