#include "driver/laskakit_espink_v3_5.h"
#include "epaper/GDEM075F52_4C_BWRY.h"

// DIP switches:
//    1 = on
//    2 = off

#define DEBUG

// #define MODE_EPAPER_LOCAL_TEST_ONLY

#define USE_WIFI_MANAGER
#define HOSTNAME "esp69-epaper9"
#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define SYSLOG_SERVER "logserver.localnet"
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "epaper"

#define DISPLAY_ROTATION 2  // see hw_config.h for details
