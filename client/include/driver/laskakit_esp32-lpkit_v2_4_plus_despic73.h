#include <Arduino.h>
// #include "logger.h"

// DESPI-C73 board | wire color | ESP32 board
// ----------------|------------|-------------
// BUSY            | orange     | 25
// RES             | green      | 26
// D/C             | brown      | 27
// CS              | white      | 5 (SPI_CS)
// SCK             | yellow     | 18 (SPI_CLK)
// SDI             | violet     | 23 (SPI_MOSI)
// GND             | black      | GND
// 3V3             | red        | 3V3
//                 |            | 34 = VOLTAGE_ADC_PIN

#define CS_PIN 5    // SS
#define DC_PIN 27   // D/C
#define RST_PIN 26  // RES
#define BUSY_PIN 25 // PIN_BUSY

// VSPI:
// CLK = 18, MISO = 19, MOSI = 23, SS = 5

// Voltage measurement
#define VOLTAGE_ADC_PIN 34
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.769388
#define VOLTAGE_MIN 3.0
#define VOLTAGE_MAX 4.2
#define VOLTAGE_LINEAR_MIN 3.4
#define VOLTAGE_LINEAR_MAX 3.8

inline void boardSpecificInit() {
}

inline void boardSpecificDone() {
} 