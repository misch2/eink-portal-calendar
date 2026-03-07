#include "driver/laskakit_espink_v2_5.h"
#include "epaper/GDEM075F52_4C_BWRY.h"

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

#define DISPLAY_ROTATION 1  // vertical with connector on the left side
