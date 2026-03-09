#include <Arduino.h>
// #include "logger.h"

// LaskaKit ESPink v3.5 - enable on GPIO47, voltage divider on GPIO 9
// https://www.laskakit.cz/laskakit-espink-esp32-e-paper-pcb-antenna/?variantId=12419

#define CS_PIN 10    // SS
#define DC_PIN 48    // D/C
#define RST_PIN 45   // RES
#define BUSY_PIN 38  // PIN_BUSY

#define SPLIT_DISPLAY_INTO_N_PAGES 1  // fits in memory, no need to split into multiple pages

#define VOLTAGE_ADC_PIN 9
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.769388
#define VOLTAGE_MIN 3.0
#define VOLTAGE_MAX 4.2
#define VOLTAGE_LINEAR_MIN 3.4
#define VOLTAGE_LINEAR_MAX 3.8

inline void boardSpecificInit() {
  // power on the ePaper and I2C
  // logger.debug("Board specific init: powering on ePaper and I2C");
  // logger.debug(" - Setting GPIO 47 to HIGH");
  pinMode(47, OUTPUT);
  digitalWrite(47, HIGH);
  delay(50);
}

inline void boardSpecificDone() {
  // power off the ePaper and I2C
  // logger.debug("Board specific done: powering off ePaper and I2C");
  // logger.debug(" - Setting GPIO 47 to LOW");
  digitalWrite(47, LOW);
}