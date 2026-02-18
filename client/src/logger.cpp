#include <Arduino.h>
#include "board_config.h"
#include "logger.h"

#ifdef SYSLOG_SERVER
Logger::Logger(WiFiUDP& udpClient, Syslog& syslog) 
  : udpClient(udpClient), syslog(syslog), syslogEnabled(false), debugEnabled(false) {
  #ifdef DEBUG
    debugEnabled = true;
    syslogEnabled = true;
  #endif
}
#else
Logger::Logger() : debugEnabled(false) {
  #ifdef DEBUG
    debugEnabled = true;
  #endif
}
#endif

void Logger::debug(const char* format, ...) {
  if (!debugEnabled) {
    return;
  }

  // Format the string first
  va_list args;
  va_start(args, format);
  char buffer[256];
  vsnprintf(buffer, sizeof(buffer), format, args);
  va_end(args);

  // Print to Serial
  Serial.print(buffer);
  Serial.print('\n');
  Serial.flush();

#ifdef SYSLOG_SERVER
  // Send to syslog if enabled and WiFi connected
  if (syslogEnabled && WiFi.status() == WL_CONNECTED) {
    for (int i = 0; i < 3; i++) {
      if (syslog.log(LOG_INFO, buffer)) {
        break;
      } else {
        delay(100);
      }
    }
  } else if (syslogEnabled) {
    Serial.println(" (no syslog, WiFi not connected)");
  }
#endif
}

void Logger::trace(const char* format, ...) {
#ifdef DEBUG
  // Trace is like debug but can be enabled/disabled separately if needed
  if (!debugEnabled) {
    return;
  }

  va_list args;
  va_start(args, format);
  char buffer[256];
  vsnprintf(buffer, sizeof(buffer), format, args);
  va_end(args);

  Serial.print(buffer);
  Serial.print('\n');
  Serial.flush();
#endif
}

void Logger::setEnabled(bool enable) {
  debugEnabled = enable;
}

bool Logger::isEnabled() const {
  return debugEnabled;
}
