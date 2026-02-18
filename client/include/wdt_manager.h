#ifndef WDT_MANAGER_H
#define WDT_MANAGER_H

// Forward declarations
class Logger;

class WDTManager {
 private:
  Logger& logger;
  bool enabled;

 public:
  WDTManager(Logger& logger);
  
  void init();
  void refresh();
  void stop();
};

#endif  // WDT_MANAGER_H
