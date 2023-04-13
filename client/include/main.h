void stopWiFi();
bool startWiFi();
float getVoltage();

void wakeupAndConnect();
void checkResetReason();
void checkVoltage();
void loadConfigFromWeb();
void fetchAndDrawImageIfNeeded();
void disconnectAndHibernate();

String httpGETRequestAsString(const char* url);
void showRawBitmapFrom_HTTP(const char* host,
                            int port,
                            const char* path,
                            int16_t x,
                            int16_t y,
                            int16_t w,
                            int16_t h,
                            int16_t bytes_per_row,
                            int16_t rows_at_once);
void displayText(String message);
void error(String message);
void errorNoWifi();
void testDisplayMessage();
void runOTALoopInsteadOfUsualFunctionality();

uint32_t read8n(WiFiClient& client, uint8_t* buffer, int32_t bytes);

void espDeepSleep(uint64_t seconds);

#define uS_PER_S 1000000
#define SECONDS_PER_HOUR 3600

// fonts
#include "Open_Sans_Regular_16.h"
#include "Open_Sans_Regular_24.h"

