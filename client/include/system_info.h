#ifndef SYSTEM_INFO_H
#define SYSTEM_INFO_H

#include <Arduino.h>

// Forward declarations
class Logger;

class SystemInfo {
 private:
  Logger& logger;
  int& wakeupCount;

 public:
  SystemInfo(Logger& logger, int& wakeupCount);
  
  String resetReasonAsString();
  String wakeupReasonAsString();
  void logResetReason(const char* lastChecksum);
};

#endif  // SYSTEM_INFO_H
