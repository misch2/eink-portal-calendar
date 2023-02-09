#ifdef DEBUG
#ifdef SYSLOG_SERVER
// A UDP instance to let us send and receive packets over UDP
WiFiUDP udpClient;
// Create a new syslog instance with LOG_KERN facility
Syslog syslog(udpClient,
              SYSLOG_SERVER,
              SYSLOG_PORT,
              SYSLOG_MYHOSTNAME,
              SYSLOG_MYAPPNAME,
              LOG_KERN);
#define DEBUG_PRINT(...)                                \
  Serial.printf(__VA_ARGS__);                           \
  Serial.print('\n');                                   \
  if (WiFi.status() == WL_CONNECTED) {                  \
    syslog.logf(LOG_INFO, __VA_ARGS__);                 \
  } else {                                              \
    Serial.println(" (no syslog, WiFi not connected)"); \
  }
#else
#define DEBUG_PRINT(...)      \
  Serial.printf(__VA_ARGS__); \
  Serial.print('\n')
#endif
#else
#define DEBUG_PRINT(...)
#endif
