#pragma once

#include "board_config.h"

#ifdef VOLTAGE_ADC_PIN
#include <ESP32AnalogRead.h>
#endif

// Forward declarations
class Logger;

class VoltageReader {
 private:
  Logger& logger;
  int voltage_adc_raw;
  float voltage_real;

 public:
  VoltageReader(Logger& logger);

  void read();
  int getAdcRaw() const { return voltage_adc_raw; }
  float getVoltageReal() const { return voltage_real; }

 private:
#ifdef VOLTAGE_ADC_PIN
  ESP32AnalogRead adc = ESP32AnalogRead();
#endif
};
