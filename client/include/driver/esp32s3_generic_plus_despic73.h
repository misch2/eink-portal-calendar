#include <Arduino.h>
// #include "logger.h"

// DESPI-C73 board | wire color | ESP32 board
// ----------------|------------|-------------
// BUSY            | orange     | 5
// RES             | green      | 4
// D/C             | brown      | 3
// CS              | white      | 2
// SCK             | yellow     | 12
// SDI             | violet     | 11
// GND             | black      | GND
// 3V3             | red        | 3V3

#define CS_PIN 2    // SS
#define DC_PIN 6    // D/C
#define RST_PIN 4   // RES
#define BUSY_PIN 5  // PIN_BUSY

#define REMAP_SPI
#define SPI_BUS HSPI
#define PIN_SPI_CLK 12   // SCK on DESPI-C73
#define PIN_SPI_MISO -1  // unused (13)
#define PIN_SPI_MOSI 11  // SDI on DESPI-C73
#define PIN_SPI_SS -1    // unused (10) FIXME or the same as CS_PIN

// // No voltage measurement
// #define VOLTAGE_ADC_PIN 9
// #define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.769388
// #define VOLTAGE_MIN 3.0
// #define VOLTAGE_MAX 4.2
// #define VOLTAGE_LINEAR_MIN 3.4
// #define VOLTAGE_LINEAR_MAX 3.8

inline void boardSpecificInit() {
}

inline void boardSpecificDone() {
}