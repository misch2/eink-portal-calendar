// fonts
#include "DejaVu_Sans_Mono_16.h"
#include "Open_Sans_Regular_16.h"
#include "Open_Sans_Regular_24.h"

void basicInit();
void boardSpecificInit();
void boardSpecificDone();
void wakeupAndConnect();

bool startWiFi();
void stopWiFi();
void initDisplay();
void stopDisplay();
void initOTA();
void disconnectAndHibernate();

void displayText(String message, const GFXfont* font);
void error(String message);

void readVoltage();
void logResetReason();
void logRuntimeStats();
void loadConfigFromWeb();

void showRawBitmapFrom_HTTP(const char* path, int16_t x, int16_t y, int16_t w, int16_t h);
void espDeepSleep(uint64_t seconds);

#define uS_PER_S 1000000
#define SECONDS_PER_HOUR 3600
#define SECONDS_PER_MINUTE 60
