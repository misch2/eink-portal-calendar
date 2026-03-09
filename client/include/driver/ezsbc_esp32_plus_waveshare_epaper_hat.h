#include <Arduino.h>

// EzSBC ESP32 breakout board
// https://ezsbc.shop/products/esp32-breakout-and-development-board
//
// and
//
// WaveShare "Universal e-Paper Raw Panel Drive HAT"
// https://rpishop.cz/e-paper-karty-hat/4577-waveshare-univerzalni-e-paper-raw-panel-drive-hat.html?gad_campaignid=20406600696

// #define REMAP_SPI
// #define SPI_BUS HSPI
// #define PIN_SPI_CLK 18   // CLK
// #define PIN_SPI_MISO -1  // unused
// #define PIN_SPI_MOSI 23  // DIN
// #define PIN_SPI_SS -1    // unused (FIXME or the same as CS_PIN)

#define SPI_BUS HSPI
#define CS_PIN 15
#define DC_PIN 23
#define RST_PIN 33
#define BUSY_PIN 27

#define SPLIT_DISPLAY_INTO_N_PAGES 2  // memory issues with full page buffer

// 4xAAA battery pack with a voltage divider to measure the voltage on an ADC pin.
#define VOLTAGE_ADC_PIN 32
#define VOLTAGE_MULTIPLICATION_COEFFICIENT 2.371
#define VOLTAGE_MIN 4.4
#define VOLTAGE_MAX 6.0
#define VOLTAGE_LINEAR_MIN VOLTAGE_MIN
#define VOLTAGE_LINEAR_MAX VOLTAGE_MAX

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
