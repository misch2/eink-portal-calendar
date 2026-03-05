#pragma once

#include <Arduino.h>

#define DEBUG

#define USE_WIFI_MANAGER
#define USE_WDT
#define WDT_TIMEOUT 120  // seconds

#define HOSTNAME "esp35-epaper5" /* a board number, not a chip ID */

#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define SYSLOG_SERVER "logserver.localnet"
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "epaper"

#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480
#define DISPLAY_TYPE_4C  // 4 colors - black, white, red, and yellow
#define DISPLAY_ROTATION 1  // vertical with connector on the left side
#define BITMAP_BPP 1     // 1 bit per pixel (and two buffers, but that's up to the code)

#define CS_PIN 5    // SS
#define DC_PIN 17   // D/C
#define RST_PIN 16  // RES
#define BUSY_PIN 4  // PIN_BUSY

#define VOLTAGE_ADC_PIN 34
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.7513
#define VOLTAGE_MIN 3.0
#define VOLTAGE_MAX 4.2
#define VOLTAGE_LINEAR_MIN 3.4
#define VOLTAGE_LINEAR_MAX 3.8

// 7.5" 4C 800x480
#define DISPLAY_CLASS_TYPE GxEPD2_4C<GxEPD2_750c_GDEM075F52, GxEPD2_750c_GDEM075F52::HEIGHT / 2>
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750c_GDEM075F52(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

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