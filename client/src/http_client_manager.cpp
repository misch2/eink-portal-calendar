#include "http_client_manager.h"

#include <ArduinoJson.h>
#include <HTTPClient.h>

#include "display_manager.h"
#include "hw_config.h"
#include "logger.h"
#include "main.h"
#include "ota_manager.h"
#include "system_info.h"
#include "version.h"
#include "voltage.h"
#include "wdt_manager.h"

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

    wdtManager.ping();
    otaManager.loop();
  }

  return bytesRead;
}

bool HTTPClientManager::loadConfigFromWeb(uint32_t& configLoadTime, bool& otaMode) {
  logger.debug("loadConfigFromWeb()");
  configLoadTime = millis();

  HTTPClient http;
  String url = serverUrl + "/api/device/config?mac=" + WiFi.macAddress() +  //
               "&adc=" + String(voltageReader.getAdcRaw()) +                //
               "&v=" + String(voltageReader.getVoltageReal()) +             //
               "&vmin=" + String(VOLTAGE_MIN) +                             //
               "&vmax=" + String(VOLTAGE_MAX) +                             //
               "&vlmin=" + String(VOLTAGE_LINEAR_MIN) +                     //
               "&vlmax=" + String(VOLTAGE_LINEAR_MAX) +                     //
               "&w=" + String(DISPLAY_WIDTH) +                              //
               "&h=" + String(DISPLAY_HEIGHT) +                             //
               "&c=" + String(defined_color_type) +                         //
               "&fw=" + String(FIRMWARE_VERSION) +                          //
               "&rot=" + String(DISPLAY_ROTATION) +                         // new in 2.1.1, not used for anything yet
               "&reset=" + systemInfo.resetReasonAsString() +               //
               "&wakeup=" + systemInfo.wakeupReasonAsString();

  logger.trace("URL: %s", url.c_str());
  http.begin(url);
  http.setTimeout(10000);

  int httpCode = http.GET();
  logger.debug("HTTP response code: %d (%s)", httpCode, statusCodeAsString(httpCode).c_str());

  if (httpCode != 200) {
    sleepTime = SLEEP_TIME_TEMPORARY_ERROR;
    logger.debug("Failed to load config, HTTP code: %d", httpCode);
    http.end();
    lastErrorMessage = "Failed to load config, HTTP code: " + String(httpCode) + " (" + statusCodeAsString(httpCode) + ")";
    return false;
  }

  String jsonText = http.getString();
  http.end();

  DynamicJsonDocument response(1000);
  DeserializationError errorStr = deserializeJson(response, jsonText);

  if (errorStr) {
    logger.debug("JSON parse error: %s", errorStr.c_str());
    sleepTime = SLEEP_TIME_TEMPORARY_ERROR;
    lastErrorMessage = "Can't parse JSON response: " + String(errorStr.c_str()) + "\nResponse was: " + jsonText;
    return false;
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

  wdtManager.ping();
  return true;
}

bool HTTPClientManager::showRawBitmapFromWeb() {
  String newChecksum = "?";
  displayManager.beginBitmapDraw();

  do {
    if (!_displayPartialPageFromWeb(newChecksum)) {
      return false;
    }
  } while (displayManager.nextPageBitmapDraw());

  displayManager.endBitmapDraw();

  // Update checksum in semi-permanent storage for next time
  strncpy(lastChecksum, newChecksum.c_str(), 64);
  lastChecksum[64] = '\0';

  return true;
}

bool HTTPClientManager::_displayPartialPageFromWeb(String& newChecksum) {
  static unsigned char row_buffer[DISPLAY_WIDTH];  // 1 byte per pixel as a theoretical worst case, actual may be less depending on display type

  uint32_t startTime = millis();

  String path = "/api/device/bitmap/epaper";
  String url = serverUrl + path + "?" +      //
               "mac=" + WiFi.macAddress() +  //
               "&fmt=2"                      // format 2 = optimized for simple pixel drawing, no HW-specific code on server side
      ;
  logger.debug("Loading bitmap from: %s", url.c_str());

  int rowBytes = displayManager.bytesPerRow();
  bool ok = false;

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
      continue;  // next attempt
    }

    WiFiClient* stream = http.getStreamPtr();
    int contentLength = http.getSize();
    logger.debug("Content length: %d", contentLength);

    // Read magic header "MM"
    wdtManager.ping();
    String line;
    int bytesRead = readLineFromStream(stream, line);

    logger.debug("Magic: %s", line.c_str());
    if (line != "MM") {
      sleepTime = SLEEP_TIME_PERMANENT_ERROR;
      http.end();
      lastErrorMessage = "Invalid magic header: " + line;
      return false;
    }

    // Read checksum
    wdtManager.ping();
    bytesRead += readLineFromStream(stream, line);
    newChecksum = line;

    logger.debug("Last checksum: %s", lastChecksum);
    logger.debug("New checksum: %s", newChecksum.c_str());

    if (newChecksum == String(lastChecksum)) {
      logger.debug("Checksum unchanged, skipping");
      http.end();
      return true;
    }

    logger.debug("Reading bitmap data");
    uint32_t totalBytesRead = bytesRead;
    bool readError = false;

    for (uint16_t row = 0; row < displayManager.displayHeight(); row++) {
      wdtManager.ping();
      otaManager.loop();

      int timeout = 100;  // 1 second timeout per row
      while (stream->available() < rowBytes && timeout > 0) {
        delay(10);
        timeout--;
      }

      if (stream->available() >= rowBytes) {
        size_t read = stream->readBytes(row_buffer, rowBytes);
        if (read == (size_t)rowBytes) {
          displayManager.drawBitmapRow(row_buffer, row);
          totalBytesRead += read;
        } else {
          logger.debug("WARNING: Read %d bytes, expected %d on row %d", read, rowBytes, row);
          readError = true;
          break;
        }
      } else {
        logger.debug("WARNING: Timeout waiting for data on row %d", row);
        readError = true;
        break;
      }
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
    lastErrorMessage = "Failed to download image after all attempts";
    return false;
  }

  wdtManager.ping();

  return true;
}
