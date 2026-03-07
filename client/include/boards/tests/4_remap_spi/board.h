#include "driver/ezsbc_esp32_plus_waveshare_epaper_hat.h"
#include "epaper/GDEW075T7_BW.h"

#define DEBUG

#define USE_WIFI_MANAGER
#define HOSTNAME "epaper" /* host name for mDNS (in the .local domain) */

#define CALENDAR_URL_HOST "192.168.0.100"
#define CALENDAR_URL_PORT 5000

#define REMAP_SPI
#define SPI_BUS HSPI
#define PIN_SPI_CLK 18   // CLK
#define PIN_SPI_MISO -1  // unused
#define PIN_SPI_MOSI 23  // DIN
#define PIN_SPI_SS -1    // unused (FIXME or the same as CS_PIN)
