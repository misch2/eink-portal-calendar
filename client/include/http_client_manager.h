#ifndef HTTP_CLIENT_MANAGER_H
#define HTTP_CLIENT_MANAGER_H

#include <Arduino.h>
#include <HTTPClient.h>
#include <Preferences.h>
#include <WiFi.h>

#include "hw_config.h"

#define SLEEP_TIME_DEFAULT (SECONDS_PER_MINUTE * 5)
#define SLEEP_TIME_TEMPORARY_ERROR (SECONDS_PER_MINUTE * 5)
#define SLEEP_TIME_PERMANENT_ERROR (SECONDS_PER_HOUR * 1)

#define NVS_NAMESPACE "portal-cal"
#define NVS_KEY_API_KEY "api_key"

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
  String serverUrl = "";
  String apiKey = "";  // loaded from NVS; sent with every request once assigned

  String statusCodeAsString(int statusCode);
  int readLineFromStream(WiFiClient* stream, String& result);
  int _displayPartialPageFromWeb(String& newChecksum);
  bool _verifyConfig();
  void _loadApiKeyFromNvs();
  void _saveApiKeyToNvs(const String& key);

 public:
  HTTPClientManager(Logger& logger, WDTManager& wdtManager, OTAManager& otaManager, VoltageReader& voltageReader, SystemInfo& systemInfo,
                    DisplayManager& displayManager, int& sleepTime, char* lastChecksum, const char* defined_color_type);

  String lastErrorMessage = "";
  void init();
  bool loadConfigFromWeb(uint32_t& configLoadTime, bool& otaMode);
  bool showRawBitmapFromWeb();
};

#endif  // HTTP_CLIENT_MANAGER_H
