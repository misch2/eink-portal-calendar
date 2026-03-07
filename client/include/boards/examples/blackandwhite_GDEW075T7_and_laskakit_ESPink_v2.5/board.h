#include "driver/laskakit_espink_v2_5.h"
#include "epaper/GDEW075T7_BW.h"

#define DEBUG

#define USE_WIFI_MANAGER
#define HOSTNAME "epaper" /* host name for mDNS (in the .local domain) */

#define CALENDAR_URL_HOST "192.168.0.100"
#define CALENDAR_URL_PORT 5000
