#pragma once

#include <Arduino.h>

#define DEBUG

#define USE_WIFI_MANAGER

#define HOSTNAME "esp15-portal" /* a board number, not a chip ID */

#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define SYSLOG_SERVER "logserver.localnet" /* rpi1 */
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "portal-calendar1"

// Display is 800x480 B/W
// https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/ aka
// WaveShare SKU 13187: https://www.waveshare.com/7.5inch-e-paper.htm aka
// GoodDisplay GDEW075T7 800x480 (EK79655 / GD7965)

// Driver board is
// https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/ aka
// WaveShare "ESP8266 e-Paper Raw Panel Driver Board" Driver board settings:
// switch set to "A" position (because the "B" position produced streaking and
// incompletely drawn content)

#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480
#define DISPLAY_TYPE_BW  // black and white
#define BITMAP_BPP 1

// EzSBC ESP32 breakout board
#define SPI_BUS HSPI
#define CS_PIN 15
#define DC_PIN 23
#define RST_PIN 33
#define BUSY_PIN 27

#define VOLTAGE_ADC_PIN 32
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 2.371
#define VOLTAGE_MIN 4.4
#define VOLTAGE_MAX 6.0
#define VOLTAGE_LINEAR_MIN VOLTAGE_MIN
#define VOLTAGE_LINEAR_MAX VOLTAGE_MAX

// #ifdef USE_GxEPD2_4G
// #ifdef USE_GRAYSCALE_DISPLAY
// GxEPD2_4G_4G<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
//     GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
// #else
// GxEPD2_4G_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(
//     GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
// #endif
// #else
// GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN));
// #endif
#define DISPLAY_CLASS_TYPE GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2>
#define DISPLAY_CLASS_ARGUMENTS (GxEPD2_750_T7(CS_PIN, DC_PIN, RST_PIN, BUSY_PIN))

inline void boardSpecificInit() {
  // First RGB LED in on pins 13, 14 and 19 and has a common anode.
  // Second one is on pins 16, 17 and 18 and also has a common anode.

  // Put all the outputs in a high impedance state, i.e. turn them off.
  pinMode(13, INPUT);
  pinMode(14, INPUT);
  pinMode(19, INPUT);
  pinMode(16, INPUT);
  pinMode(17, INPUT);
  pinMode(18, INPUT);
}

inline void boardSpecificDone() {}