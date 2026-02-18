#include "voltage.h"

#include <Arduino.h>

#include "board_config.h"
#include "logger.h"
#include "main.h"

VoltageReader::VoltageReader(Logger& logger) : logger(logger), voltage_adc_raw(-1), voltage_real(-1.0f) {}

void VoltageReader::read() {
#ifdef VOLTAGE_ADC_PIN
  adc.attach(VOLTAGE_ADC_PIN);

  float voltage = 0;
  for (int i = 0; i < VOLTAGE_AVERAGING_COUNT; i++) {
    delay(100);
    voltage += adc.readVoltage();
  }
  voltage /= VOLTAGE_AVERAGING_COUNT;
  logger.debug("raw voltage read (avg): %f V", voltage);

  voltage_real = voltage * VOLTAGE_MULTIPLICATION_COEFFICIENT;
  logger.debug("real voltage (corrected by %f): %f V", VOLTAGE_MULTIPLICATION_COEFFICIENT, voltage_real);

  voltage_adc_raw = adc.readRaw();
  logger.debug("RAW via adc: %d", voltage_adc_raw);
#else
  voltage_real = -1;
  voltage_adc_raw = -1;
  logger.debug("Voltage not measured, no pin defined");
#endif
}
