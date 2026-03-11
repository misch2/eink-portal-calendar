#define DEBUG
#define SYSLOG_SERVER "logserver.localnet"
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "epaper"

#include "driver/laskakit_espink_v3_5.h"
#include "epaper/GDEM075F52_4C_BWRY.h"

#define USE_WIFI_MANAGER
#define USE_WDT
#define WDT_TIMEOUT 120  // seconds

#define HOSTNAME "esp65-epaper7"                     /* a board number, not a chip ID */
#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define DISPLAY_ROTATION 2  // see hw_config.h for details
