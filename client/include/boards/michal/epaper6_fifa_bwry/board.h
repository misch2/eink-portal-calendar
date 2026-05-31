#include "driver/laskakit_espink_v3_5.h"
#include "epaper/GDEM075F52_4C_BWRY.h"

// DIP switches:
//    1 = on
//    2 = off

#define USE_WIFI_MANAGER
#define HOSTNAME "esp64-epaper6"
#define CALENDAR_URL_HOST "portal-calendar.pve.land.cz"
#define CALENDAR_URL_PORT 80

#define DISPLAY_ROTATION 1  // see hw_config.h for details
