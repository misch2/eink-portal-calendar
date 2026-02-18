#ifndef HTTP_CLIENT_MANAGER_H
#define HTTP_CLIENT_MANAGER_H

#include <Arduino.h>
#include <HTTPClient.h>
#include <WiFi.h>

#include "board_config.h"

#define SLEEP_TIME_DEFAULT (SECONDS_PER_MINUTE * 5)
#define SLEEP_TIME_TEMPORARY_ERROR (SECONDS_PER_MINUTE * 5)
#define SLEEP_TIME_PERMANENT_ERROR (SECONDS_PER_HOUR * 1)

// Forward declarations
class Logger;
class WDTManager;
class OTAManager;
class VoltageReader;
class SystemInfo;
class DisplayManager;

class HTTPClientManager {
 private:
  Logger& logger;
  WDTManager& wdtManager;
  OTAManager& otaManager;
  VoltageReader& voltageReader;
  SystemInfo& systemInfo;
  DisplayManager& displayManager;

  int& sleepTime;
  char* lastChecksum;
  const char* defined_color_type;
  const String serverUrl = String("http://") + CALENDAR_URL_HOST + ":" + String(CALENDAR_URL_PORT);

  String statusCodeAsString(int statusCode);
  int readLineFromStream(WiFiClient* stream, String& result);

 public:
  HTTPClientManager(Logger& logger, WDTManager& wdtManager, OTAManager& otaManager, VoltageReader& voltageReader, SystemInfo& systemInfo,
                    DisplayManager& displayManager, int& sleepTime, char* lastChecksum, const char* defined_color_type);

  void loadConfigFromWeb(uint32_t& configLoadTime, bool& otaMode);
  void showRawBitmapFrom_HTTP(const char* path, int16_t x, int16_t y, int16_t w, int16_t h);
};

#endif  // HTTP_CLIENT_MANAGER_H
