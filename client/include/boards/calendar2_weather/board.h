#define DEBUG

#define USE_WIFI_MANAGER
#define USE_WDT
#define WDT_TIMEOUT 120  // seconds

#define HOSTNAME "esp36-weather" /* a board number, not a chip ID */

#define CALENDAR_URL_HOST "michal-apps.localnet" /* rpi1 */
#define CALENDAR_URL_PORT 8084                   /* nginx port to log requests, real server runs on 8085 */

#define SYSLOG_SERVER "logserver.localnet" /* rpi1 */
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "weather-display"

// Display is 800x480 B/W/R
// https://www.aliexpress.com/item/1005005121813674.html?spm=a2g0o.order_list.order_list_main.5.335f1802RZxENL

// Driver board is LaskaKit ESP32 e-Paper Driver Board v2.5 (16 MB Flash)

#define DISPLAY_WIDTH 800
#define DISPLAY_HEIGHT 480
#define DISPLAY_TYPE_3C  // 3 colors - black, white and red/yellow
#define BITMAP_BPP 1     // 1 bit per pixel (and two buffers, but that's up to the code)

// #define REMAP_SPI
// #define SPI_BUS HSPI
// #define PIN_SPI_CLK 18   // CLK
// #define PIN_SPI_MISO -1  // unused
// #define PIN_SPI_MOSI 23  // DIN
// #define PIN_SPI_SS -1    // unused (FIXME or the same as CS_PIN)

#define CS_PIN 5
#define DC_PIN 17
#define RST_PIN 16
#define BUSY_PIN 4

#define VOLTAGE_ADC_PIN 34

#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.7540
#define VOLTAGE_MIN 3.0
#define VOLTAGE_MAX 4.2
#define VOLTAGE_LINEAR_MIN 3.4
#define VOLTAGE_LINEAR_MAX 3.8

// 7.5" 3C 800x480
#define DISPLAY_INSTANCE \
  GxEPD2_3C<GxEPD2_750c_Z08, GxEPD2_750c_Z08::HEIGHT / 2> display(GxEPD2_750c_Z08(/*CS*/ CS_PIN, /*DC*/ DC_PIN, /*RST*/ RST_PIN, /*BUSY*/ BUSY_PIN));

void boardSpecificInit() {
  // power on the ePaper and I2C
  pinMode(2, OUTPUT);
  digitalWrite(2, HIGH);
  delay(50);
}

void boardSpecificDone() {
  // power off the ePaper and I2C
  digitalWrite(2, LOW);
}
