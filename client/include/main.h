void stopWiFi();
bool startWiFi();
void readVoltage();

void basicInit();
void wakeupAndConnect();
void logResetReason();
void logRuntimeStats();
void checkVoltage();
void loadConfigFromWeb();
void fetchAndDrawImageIfNeeded();
void disconnectAndHibernate();
void stopDisplay();
void initDisplay();
void espDeepSleep(uint64_t seconds);

String httpGETRequestAsString(const char* url);
void showRawBitmapFrom_HTTP(const char* host, int port, const char* path, int16_t x, int16_t y, int16_t w, int16_t h);
void displayText(String message);
void error(String message);
void errorNoWifi();
void testDisplayMessage();
void initOTA();

uint32_t read8n(WiFiClient& client, uint8_t* buffer, int32_t bytes);

#define uS_PER_S 1000000
#define SECONDS_PER_HOUR 3600

// fonts
#include "Open_Sans_Regular_16.h"
#include "Open_Sans_Regular_24.h"

// helper for stringifying defines
#define xstr(a) str(a)
#define str(a) #a