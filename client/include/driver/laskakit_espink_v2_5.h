#include <Arduino.h>

// ESP controller board: ESPink v2.5 from LaskaKit.cz
// https://github.com/LaskaKit/ESPink/blob/main/HW/old/ESPink_v2_5.pdf

#define CS_PIN 5    // SS
#define DC_PIN 17   // D/C
#define RST_PIN 16  // RES
#define BUSY_PIN 4  // PIN_BUSY

#define SPLIT_DISPLAY_INTO_N_PAGES 2  // memory issues with full page buffer

#define VOLTAGE_ADC_PIN 34
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.769388
#define VOLTAGE_MIN 3.0
#define VOLTAGE_MAX 4.2
#define VOLTAGE_LINEAR_MIN 3.4
#define VOLTAGE_LINEAR_MAX 3.8

inline void boardSpecificInit() {
  // power on the ePaper and I2C
  pinMode(2, OUTPUT);
  digitalWrite(2, HIGH);
  delay(50);
}

inline void boardSpecificDone() {
  // power off the ePaper and I2C
  digitalWrite(2, LOW);
}