// fonts
#include "Open_Sans_Regular_16.h"
#include "Open_Sans_Regular_24.h"
#include "DejaVu_Sans_Mono_16.h"

void basicInit();
void boardSpecificInit();
void wakeupAndConnect();

bool startWiFi();
void stopWiFi();
void initDisplay();
void stopDisplay();
void initOTA();
void disconnectAndHibernate();

void displayText(String message, const GFXfont *font);
void error(String message);
void errorNoWifi();

void readVoltage();
void logResetReason();
void logRuntimeStats();
void checkVoltage();
void loadConfigFromWeb();

void testDisplayMessage();
void fetchAndDrawImageIfNeeded();

String httpGETRequestAsString(const char* url);
void showRawBitmapFrom_HTTP(const char* host, int port, const char* path, int16_t x, int16_t y, int16_t w, int16_t h);
void espDeepSleep(uint64_t seconds);
uint32_t read8n(WiFiClient& client, uint8_t* buffer, int32_t bytes);

#define uS_PER_S 1000000
#define SECONDS_PER_HOUR 3600
