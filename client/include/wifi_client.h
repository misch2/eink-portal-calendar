#ifndef WIFI_CLIENT_H
#define WIFI_CLIENT_H

#include <WiFi.h>

// Forward declarations
class Logger;
class WDTManager;
class OTAManager;

class WiFiClientWithBlockingReads : public WiFiClient {
 protected:
  uint32_t blockingReadTimeout = 2000;
  OTAManager* otaManager = nullptr;
  int blocking_read(uint8_t* buffer, size_t bytes);

 public:
  void setOTAManager(OTAManager* manager);
  void setBlockingReadTimeout(uint32_t timeout);
  int read() override;
  int read(uint8_t* buf, size_t size) override;
};

class WiFiConnectionManager {
 private:
  Logger& logger;
  WDTManager& wdtManager;

 public:
  WiFiConnectionManager(Logger& logger, WDTManager& wdtManager);
  
  bool init();
  void stop();
};

#endif  // WIFI_CLIENT_H
