#include "voltage.h"

#include <Arduino.h>

#include "hw_config.h"
#include "logger.h"
#include "main.h"

VoltageReader::VoltageReader(Logger& logger) : logger(logger), voltage_adc_raw(-1), voltage_real(-1.0f) {}

void VoltageReader::read() {
#ifdef VOLTAGE_ADC_PIN
  analogSetAttenuation(ADC_11db);

  float voltage_mv = 0;
  for (int i = 0; i < VOLTAGE_AVERAGING_COUNT; i++) {
    delay(100);
    voltage_mv += analogReadMilliVolts(VOLTAGE_ADC_PIN);
  }
  voltage_mv /= VOLTAGE_AVERAGING_COUNT;
  float voltage = voltage_mv / 1000.0f;
  logger.debug("raw voltage read (avg): %f V", voltage);

  voltage_real = voltage * VOLTAGE_MULTIPLICATION_COEFFICIENT;
  logger.debug("real voltage (corrected by %f): %f V", VOLTAGE_MULTIPLICATION_COEFFICIENT, voltage_real);

  voltage_adc_raw = analogRead(VOLTAGE_ADC_PIN);
  logger.debug("RAW via adc: %d", voltage_adc_raw);
#else
  voltage_real = -1;
  voltage_adc_raw = -1;
  logger.debug("Voltage not measured, no pin defined");
#endif
}
