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

// physical resolution, independent on display orientation
// GDEW075T7 800x480, EK79655 (GD7965)
#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480

#define DISPLAY_TYPE_BW  // black and white
#define BITMAP_BPP 1     // 1 bit per pixel for DISPLAY_TYPE_BW

#define DISPLAY_ROTATION 3  // vertical with connector on the right side

#define DISPLAY_CLASS_TYPE GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2>
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

// ePaper display driver

// #define REMAP_SPI
// #define SPI_BUS HSPI
// #define PIN_SPI_CLK 18   // CLK
// #define PIN_SPI_MISO -1  // unused
// #define PIN_SPI_MOSI 23  // DIN
// #define PIN_SPI_SS -1    // unused (FIXME or the same as CS_PIN)

#define CS_PIN 15
#define DC_PIN 23
#define RST_PIN 33
#define BUSY_PIN 27

// Voltage reader settings
#define VOLTAGE_ADC_PIN 32
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 2.371
#define VOLTAGE_MIN 4.4
#define VOLTAGE_MAX 6.0
#define VOLTAGE_LINEAR_MIN VOLTAGE_MIN
#define VOLTAGE_LINEAR_MAX VOLTAGE_MAX

inline void boardSpecificInit() {
  // power on the ePaper and I2C
  //   pinMode(2, OUTPUT);
  //   digitalWrite(2, HIGH);
  //   delay(50);
}

inline void boardSpecificDone() {
  // power off the ePaper and I2C
  //   digitalWrite(2, LOW);
}