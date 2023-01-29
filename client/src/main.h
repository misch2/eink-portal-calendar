void stopWiFi();
bool startWiFi();

void drawImageFromServer();
void showRawBitmapFrom_HTTP(const char* host,
                            int port,
                            const char* path,
                            int16_t x,
                            int16_t y,
                            int16_t w,
                            int16_t h,
                            int16_t bytes_per_row,
                            int16_t rows_at_once);
void display_text_fast(String message);
void error(String message);
void errorNoWifi();
void testDisplayMessage();

uint32_t read8n(WiFiClient& client, uint8_t* buffer, int32_t bytes);
uint32_t skip(WiFiClient& client, int32_t bytes);
uint32_t read32(WiFiClient& client);
uint16_t read16(WiFiClient& client);

void hibernateAll(uint64_t seconds);
void espDeepSleep(uint64_t seconds);


#define uS_PER_S 1000000
#define SECONDS_PER_HOUR 3600
