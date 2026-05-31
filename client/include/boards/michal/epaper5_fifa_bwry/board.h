#include "driver/laskakit_espink_v2_5.h"
#include "epaper/GDEM075F52_4C_BWRY.h"

// #define USE_WDT
// #define WDT_TIMEOUT 120  // seconds

#define USE_WIFI_MANAGER
#define HOSTNAME "esp35-epaper5" /* a board number, not a chip ID */
#define CALENDAR_URL_HOST "portal-calendar.pve.land.cz"
#define CALENDAR_URL_PORT 80

#define DISPLAY_ROTATION 0  // see hw_config.h for details
