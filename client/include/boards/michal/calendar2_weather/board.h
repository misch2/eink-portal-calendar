#include "driver/laskakit_espink_v2_5.h"
#include "epaper/GDEW075Z08_3C_BWR.h"

#define DEBUG
#define USE_WIFI_MANAGER
#define USE_WDT
#define WDT_TIMEOUT 120  // seconds

#define HOSTNAME "esp36-weather" /* a board number, not a chip ID */

#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define SYSLOG_SERVER "logserver.localnet" /* rpi1 */
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "weather-display"

#define DISPLAY_ROTATION 3  // vertical with connector on the right side

#define VOLTAGE_MULTIPLICATION_COEFFICIENT 1.7540
