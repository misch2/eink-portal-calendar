#include <Adafruit_GFX.h>

// fonts
#include "fonts/DejaVu_Sans_Mono_16.h"
#include "fonts/Open_Sans_Regular_16.h"
#include "fonts/Open_Sans_Regular_24.h"

void minimalHardwareInit();
void wakeupDisplayAndConnectWiFi();

void disconnectWiFiAndHibernateAll();
void showErrorOnDisplay(String message);

void logRuntimeStats();

void espDeepSleep(uint64_t seconds);

#define uS_PER_S 1000000
#define SECONDS_PER_HOUR 3600
#define SECONDS_PER_MINUTE 60

#define VOLTAGE_AVERAGING_COUNT 5
