#ifndef LOGGER_H
#define LOGGER_H

#include <Arduino.h>
#include <WiFi.h>

#ifdef SYSLOG_SERVER
#include <Syslog.h>
#include <WiFiUdp.h>
#endif

class Logger {
 private:
#ifdef SYSLOG_SERVER
  WiFiUDP& udpClient;
  Syslog& syslog;
  bool syslogEnabled;
#endif
  bool debugEnabled;

 public:
#ifdef SYSLOG_SERVER
  Logger(WiFiUDP& udpClient, Syslog& syslog);
#else
  Logger();
#endif

  void debug(const char* format, ...);
  void trace(const char* format, ...);

  void setEnabled(bool enable);
  bool isEnabled() const;
};

// Global logger instance (defined in main.cpp)
extern Logger logger;

#endif  // LOGGER_H
