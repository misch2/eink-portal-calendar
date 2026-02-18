#include "http_client_manager.h"

#include <ArduinoJson.h>
#include <HTTPClient.h>

#include "board_config.h"
#include "display_manager.h"
#include "logger.h"
#include "main.h"
#include "ota_manager.h"
#include "system_info.h"
#include "version.h"
#include "voltage.h"
#include "wdt_manager.h"

extern DISPLAY_CLASS_TYPE display;

HTTPClientManager::HTTPClientManager(Logger& logger, WDTManager& wdtManager, OTAManager& otaManager, VoltageReader& voltageReader, SystemInfo& systemInfo,
                                     DisplayManager& displayManager, int& sleepTime, char* lastChecksum, const char* defined_color_type)
    : logger(logger),
      wdtManager(wdtManager),
      otaManager(otaManager),
      voltageReader(voltageReader),
      displayManager(displayManager),
      systemInfo(systemInfo),
      sleepTime(sleepTime),
      lastChecksum(lastChecksum),
      defined_color_type(defined_color_type) {}

String HTTPClientManager::statusCodeAsString(int statusCode) {
  switch (statusCode) {
    case 200:
      return "OK";
    case 304:
      return "Not Modified";
    case 400:
      return "Bad Request";
    case 404:
      return "Not Found";
    case 500:
      return "Internal Server Error";
    case 502:
      return "Bad Gateway";
    case 503:
      return "Service Unavailable";
    default:
      return "Code " + String(statusCode);
  }
}

int HTTPClientManager::readLineFromStream(WiFiClient* stream, String& result) {
  result = "";
  int bytesRead = 0;

  while (stream->available()) {
    char c = stream->read();
    bytesRead++;

    if (c == '\n') {
      return bytesRead;
    }
    if (c != '\r') {
      result += c;
    }

    wdtManager.refresh();
    otaManager.loop();
  }

  return bytesRead;
}

void HTTPClientManager::loadConfigFromWeb(uint32_t& configLoadTime, bool& otaMode) {
  logger.debug("loadConfigFromWeb()");
  configLoadTime = millis();

  HTTPClient http;
  String url = serverUrl + "/config?mac=" + WiFi.macAddress() + "&adc=" + String(voltageReader.getAdcRaw()) + "&v=" + String(voltageReader.getVoltageReal()) +
               "&vmin=" + String(VOLTAGE_MIN) + "&vmax=" + String(VOLTAGE_MAX) + "&vlmin=" + String(VOLTAGE_LINEAR_MIN) +
               "&vlmax=" + String(VOLTAGE_LINEAR_MAX) + "&w=" + String(DISPLAY_WIDTH) + "&h=" + String(DISPLAY_HEIGHT) + "&c=" + String(defined_color_type) +
               "&fw=" + String(FIRMWARE_VERSION) + "&reset=" + systemInfo.resetReasonAsString() + "&wakeup=" + systemInfo.wakeupReasonAsString();

  logger.trace("URL: %s", url.c_str());
  http.begin(url);
  http.setTimeout(10000);

  int httpCode = http.GET();
  logger.debug("HTTP response code: %d (%s)", httpCode, statusCodeAsString(httpCode).c_str());

  if (httpCode != 200) {
    sleepTime = SLEEP_TIME_TEMPORARY_ERROR;
    logger.debug("Failed to load config, HTTP code: %d", httpCode);
    http.end();
    extern void showErrorOnDisplay(String message);  // FIXME not nice
    showErrorOnDisplay(String("Failed to load config\nHTTP: ") + statusCodeAsString(httpCode));
    return;
  }

  String jsonText = http.getString();
  http.end();

  DynamicJsonDocument response(1000);
  DeserializationError errorStr = deserializeJson(response, jsonText);

  if (errorStr) {
    logger.debug("JSON parse error: %s", errorStr.c_str());
    sleepTime = SLEEP_TIME_TEMPORARY_ERROR;
    extern void showErrorOnDisplay(String message);  // FIXME not nice
    showErrorOnDisplay("Can't parse response");
    return;
  }

  int tmpi = response["sleep"];
  logger.trace("sleepTime from JSON: %d", tmpi);
  if (tmpi != 0) {
    sleepTime = tmpi;
  }

  bool tmpb = response["ota_mode"];
  logger.trace("otaMode from JSON: %d", tmpb);
  otaMode = tmpb;
  if (otaMode) {
    logger.debug("Permanent OTA mode enabled in remote config");
    if (esp_reset_reason() == ESP_RST_SW || esp_reset_reason() == ESP_RST_DEEPSLEEP) {
      logger.debug("^ but last reset was a software one => not running OTA loop.");
      logger.debug("To force OTA mode again, reset the device manually.");
      otaMode = false;
    }
  }

  wdtManager.refresh();
}

void HTTPClientManager::showRawBitmapFrom_HTTP(const char* path, int16_t x, int16_t y, int16_t w, int16_t h) {
  logger.debug("showRawBitmapFrom_HTTP(%s)", path);

#ifdef DISPLAY_TYPE_BW
  static unsigned char input_row_mono_buffer[DISPLAY_BUFFER_SIZE];
#endif

#ifdef DISPLAY_TYPE_3C
  static unsigned char input_row_mono_buffer[DISPLAY_BUFFER_SIZE];
  static unsigned char input_row_color_buffer[DISPLAY_BUFFER_SIZE];
#endif

  uint32_t startTime = millis();
  if ((x >= displayManager.displayWidth()) || (y >= displayManager.displayHeight())) {
    logger.debug("Invalid coordinates: x=%d, y=%d", x, y);
    return;
  }

  String url = serverUrl + String(path) + "?mac=" + WiFi.macAddress();
  logger.trace("URL: %s", url.c_str());

  bool ok = false;
  String newChecksum = String(lastChecksum);

  for (int attempt = 1; attempt <= 5; attempt++) {
    if (attempt > 1) {
      logger.debug("Retrying download, attempt #%d", attempt);
      delay(1000);
    }

    HTTPClient http;
    http.begin(url);
    http.setTimeout(30000);  // 30 second timeout for bitmap download

    int httpCode = http.GET();
    logger.debug("HTTP response code: %d (%s)", httpCode, statusCodeAsString(httpCode).c_str());

    if (httpCode != 200) {
      http.end();
      if (httpCode == 304) {
        logger.debug("Image not modified (304), skipping");
        return;
      }
      continue;
    }

    WiFiClient* stream = http.getStreamPtr();
    int contentLength = http.getSize();
    logger.debug("Content length: %d", contentLength);

    // Read magic header "MM"
    wdtManager.refresh();
    String line;
    int bytesRead = readLineFromStream(stream, line);

    logger.debug("Magic: %s", line.c_str());
    if (line != "MM") {
      sleepTime = SLEEP_TIME_PERMANENT_ERROR;
      extern void showErrorOnDisplay(String message);  // FIXME not nice
      showErrorOnDisplay(String("Invalid bitmap: ") + line);
      http.end();
      return;
    }

    // Read checksum
    wdtManager.refresh();
    bytesRead += readLineFromStream(stream, line);
    newChecksum = line;

    logger.debug("Last checksum: %s", lastChecksum);
    logger.debug("New checksum: %s", newChecksum.c_str());

    if (newChecksum == String(lastChecksum)) {
      logger.debug("Checksum unchanged, skipping");
      http.end();
      return;
    }

    logger.debug("Reading bitmap data for %d rows", h);
    uint32_t totalBytesRead = bytesRead;
    bool readError = false;

    for (uint16_t row = 0; row < h; row++) {
      wdtManager.refresh();
      otaManager.loop();

#ifdef DISPLAY_TYPE_BW
      size_t bytesAvail = 0;
      int timeout = 100;  // 1 second timeout per row
      while (stream->available() < DISPLAY_BUFFER_SIZE && timeout > 0) {
        delay(10);
        timeout--;
      }

      if (stream->available() >= DISPLAY_BUFFER_SIZE) {
        size_t read = stream->readBytes(input_row_mono_buffer, DISPLAY_BUFFER_SIZE);
        if (read == DISPLAY_BUFFER_SIZE) {
          display.writeImage(input_row_mono_buffer, x, y + row, w, 1);
          totalBytesRead += read;
        } else {
          logger.debug("WARNING: Read %d bytes, expected %d on row %d", read, DISPLAY_BUFFER_SIZE, row);
          readError = true;
          break;
        }
      } else {
        logger.debug("WARNING: Timeout waiting for data on row %d", row);
        readError = true;
        break;
      }
#endif

#ifdef DISPLAY_TYPE_3C
      int timeout = 100;
      while (stream->available() < DISPLAY_BUFFER_SIZE * 2 && timeout > 0) {
        delay(10);
        timeout--;
      }

      if (stream->available() >= DISPLAY_BUFFER_SIZE * 2) {
        size_t read1 = stream->readBytes(input_row_mono_buffer, DISPLAY_BUFFER_SIZE);
        size_t read2 = stream->readBytes(input_row_color_buffer, DISPLAY_BUFFER_SIZE);

        if (read1 == DISPLAY_BUFFER_SIZE && read2 == DISPLAY_BUFFER_SIZE) {
          display.writeImage(input_row_mono_buffer, input_row_color_buffer, x, y + row, w, 1);
          totalBytesRead += read1 + read2;
        } else {
          logger.debug("WARNING: Read %d+%d bytes, expected %d+%d on row %d", read1, read2, DISPLAY_BUFFER_SIZE, DISPLAY_BUFFER_SIZE, row);
          readError = true;
          break;
        }
      } else {
        logger.debug("WARNING: Timeout waiting for data on row %d", row);
        readError = true;
        break;
      }
#endif
    }

    http.end();

    logger.debug("Total bytes read: %d, expected: %d", totalBytesRead, contentLength);

    if (!readError) {
      ok = true;
      break;
    }
  }

  logger.debug("Download time: %lu ms", millis() - startTime);

  if (!ok) {
    sleepTime = SLEEP_TIME_PERMANENT_ERROR;
    extern void showErrorOnDisplay(String message);  // FIXME not nice
    showErrorOnDisplay("Failed to download image after all attempts");
    return;
  }

  // Refresh display
  logger.debug("Refreshing display");
  wdtManager.refresh();
  startTime = millis();
  display.refresh();
  logger.debug("Display refresh time: %lu ms", millis() - startTime);

  // Update checksum
  strncpy(lastChecksum, newChecksum.c_str(), 64);
  lastChecksum[64] = '\0';

  wdtManager.refresh();
}
