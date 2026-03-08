#pragma once

#include "hw_config.h"

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
};
