#define DEBUG
#define SYSLOG_SERVER "logserver.localnet"
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "epaper"

#include "driver/laskakit_esp32-lpkit_v2_4_plus_despic73.h"
#include "epaper/GDEP073E01_6C.h"

#define USE_WIFI_MANAGER
// #define USE_WDT
// #define WDT_TIMEOUT 120  // seconds

#define HOSTNAME "esp68-epaper8"                     /* a board number, not a chip ID */
#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define DISPLAY_ROTATION 0  // see hw_config.h for details
