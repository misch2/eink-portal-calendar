#pragma once

#include <Arduino.h>

// #define DEBUG

#define USE_WIFI_MANAGER
#define HOSTNAME "epaper" /* host name for mDNS (in the .local domain) */

#define CALENDAR_URL_HOST "192.168.0.100"
#define CALENDAR_URL_PORT 5000

// #define SYSLOG_SERVER "192.168.0.101"
// #define SYSLOG_PORT 514
// #define SYSLOG_MYAPPNAME "portal-calendar-client"

// ePaper board: GDEM075F52 7.5" 4C 800x480
// https://www.aliexpress.com/item/1005010179550278.html

// physical resolution, independent on display orientation
#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480

#define DISPLAY_TYPE_4C     // 4 colors - black, white, red, and yellow
#define BITMAP_BPP 2        // 2 bit per pixel for DISPLAY_TYPE_4C
#define DISPLAY_ROTATION 1  // vertical with connector on the left side

#define DISPLAY_CLASS_TYPE GxEPD2_4C<GxEPD2_750c_GDEM075F52, GxEPD2_750c_GDEM075F52::HEIGHT / 2>
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750c_GDEM075F52(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

// ESP controller board: ESPink v2.5 from LaskaKit.cz
// https://github.com/LaskaKit/ESPink/blob/main/HW/old/ESPink_v2_5.pdf
#define CS_PIN 5    // SS
#define DC_PIN 17   // D/C
#define RST_PIN 16  // RES
#define BUSY_PIN 4  // PIN_BUSY

// Voltage reader settings
#define VOLTAGE_ADC_PIN 34
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.7513
#define VOLTAGE_MIN 3.0
#define VOLTAGE_MAX 4.2
#define VOLTAGE_LINEAR_MIN 3.4
#define VOLTAGE_LINEAR_MAX 3.8

inline void boardSpecificInit() {
  // power on the ePaper and I2C bus
  pinMode(2, OUTPUT);
  digitalWrite(2, HIGH);
  delay(50);
}

inline void boardSpecificDone() {
  // power off the ePaper and I2C bus
  digitalWrite(2, LOW);
}