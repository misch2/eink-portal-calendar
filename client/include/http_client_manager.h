#ifndef HTTP_CLIENT_MANAGER_H
#define HTTP_CLIENT_MANAGER_H

#include <Arduino.h>
#include <ArduinoHttpClient.h>
#include <WiFi.h>

// Forward declarations
class Logger;
class WDTManager;
class VoltageReader;
class SystemInfo;

class HTTPClientManager {
 private:
  Logger& logger;
  WDTManager& wdtManager;
  VoltageReader& voltageReader;
  SystemInfo& systemInfo;
  HttpClient& httpClient;
  
  int& sleepTime;
  char* lastChecksum;
  const char* defined_color_type;

  String textStatusCode(int statusCode);
  void startHttpGetRequest(String path);
  int httpReadStringUntil(char terminator, String &result);

 public:
  HTTPClientManager(Logger& logger, WDTManager& wdtManager, VoltageReader& voltageReader, 
                   SystemInfo& systemInfo, HttpClient& httpClient, int& sleepTime,
                   char* lastChecksum, const char* defined_color_type);
  
  void loadConfigFromWeb(uint32_t& configLoadTime, bool& otaMode);
  void showRawBitmapFrom_HTTP(const char *path, int16_t x, int16_t y, int16_t w, int16_t h);
};

#endif  // HTTP_CLIENT_MANAGER_H
