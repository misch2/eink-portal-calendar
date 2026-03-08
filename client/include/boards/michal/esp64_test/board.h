#include "driver/laskakit_espink_v3_5.h"
#include "epaper/GDEY075Z08_3C_BWR_inverted_red.h"

#define DEBUG

// #define MODE_EPAPER_LOCAL_TEST_ONLY

#define USE_WIFI_MANAGER
#define HOSTNAME "esp64-test"
#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define DISPLAY_ROTATION 2  // see hw_config.h for details

