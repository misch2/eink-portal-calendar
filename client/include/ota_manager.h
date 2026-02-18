#ifndef OTA_MANAGER_H
#define OTA_MANAGER_H

// Forward declarations
class Logger;
class WDTManager;

class OTAManager {
 private:
  Logger& logger;
  WDTManager& wdtManager;

 public:
  OTAManager(Logger& logger, WDTManager& wdtManager);
  
  void init();
  void loop();
};

#endif  // OTA_MANAGER_H
