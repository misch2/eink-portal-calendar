#include "driver/ezsbc_esp32_plus_waveshare_epaper_hat.h"
#include "epaper/GDEW075T7_BW.h"

#define DEBUG
#define USE_WIFI_MANAGER

#define HOSTNAME "esp15-portal" /* a board number, not a chip ID */

#define CALENDAR_URL_HOST "portal-calendar.localnet" /* .NET server on Proxmox LXC */
#define CALENDAR_URL_PORT 8084                       /* nginx port to log requests, real server runs on 5000 */

#define SYSLOG_SERVER "logserver.localnet" /* rpi1 */
#define SYSLOG_PORT 514
#define SYSLOG_MYAPPNAME "portal-calendar1"

#define DISPLAY_ROTATION 3  // vertical with connector on the right side

#define VOLTAGE_MULTIPLICATION_COEFFICIENT 2.371
