#include <Arduino.h>

// #define ESP8266
// #define ENABLE_GxEPD2_GFX 0
#define DEBUG
// #define DEBUG_ESP_HTTP_CLIENT
// #define DEBUG_ESP_PORT Serial

// core libraries from ESP8266 Arduino
#include <ESP8266WiFi.h>
// #include <ESP8266HTTPClient.h>
// #include <DNSServer.h>
// #include <ESP8266mDNS.h>
// #include <ESP8266WebServer.h>

// other libraries
#include <WiFiManager.h>
#include <WiFiClient.h>
#include <GFX.h>
#include <GxEPD2_BW.h>

// SS is usually used for CS. define here for easy change
#ifndef EPD_CS
#define EPD_CS SS
#endif

// somehow there should be an easier way to do this
#define GxEPD2_BW_IS_GxEPD2_BW true
#define GxEPD2_3C_IS_GxEPD2_3C true
#define GxEPD2_7C_IS_GxEPD2_7C true
#define GxEPD2_1248_IS_GxEPD2_1248 true
#define GxEPD2_1248c_IS_GxEPD2_1248c true
#define IS_GxEPD(c, x) (c##x)
#define IS_GxEPD2_BW(x) IS_GxEPD(GxEPD2_BW_IS_, x)
#define IS_GxEPD2_3C(x) IS_GxEPD(GxEPD2_3C_IS_, x)
#define IS_GxEPD2_7C(x) IS_GxEPD(GxEPD2_7C_IS_, x)
#define IS_GxEPD2_1248(x) IS_GxEPD(GxEPD2_1248_IS_, x)
#define IS_GxEPD2_1248c(x) IS_GxEPD(GxEPD2_1248c_IS_, x)

#if defined(ESP8266)
#define MAX_DISPLAY_BUFFER_SIZE (81920ul - 34000ul - 36000ul) // ~34000 base use, WiFiClientSecure seems to need about 36k more to work (with certificates)
#if IS_GxEPD2_BW(GxEPD2_DISPLAY_CLASS)
#define MAX_HEIGHT(EPD) (EPD::HEIGHT <= MAX_DISPLAY_BUFFER_SIZE / (EPD::WIDTH / 8) ? EPD::HEIGHT : MAX_DISPLAY_BUFFER_SIZE / (EPD::WIDTH / 8))
#elif IS_GxEPD2_3C(GxEPD2_DISPLAY_CLASS)
#define MAX_HEIGHT(EPD) (EPD::HEIGHT <= (MAX_DISPLAY_BUFFER_SIZE / 2) / (EPD::WIDTH / 8) ? EPD::HEIGHT : (MAX_DISPLAY_BUFFER_SIZE / 2) / (EPD::WIDTH / 8))
#elif IS_GxEPD2_7C(GxEPD2_DISPLAY_CLASS)
#define MAX_HEIGHT(EPD) (EPD::HEIGHT <= (MAX_DISPLAY_BUFFER_SIZE) / (EPD::WIDTH / 2) ? EPD::HEIGHT : (MAX_DISPLAY_BUFFER_SIZE) / (EPD::WIDTH / 2))
#endif
#endif

// michals: 800x480
GxEPD2_BW<GxEPD2_750_T7, GxEPD2_750_T7::HEIGHT / 2> display(GxEPD2_750_T7(/*CS=D8=GPIO15*/ SS, /*DC=D2=GPIO4*/ 4, /*RST=D4=GPIO2*/ 2, /*BUSY=D1=GPIO5*/ 5)); // GDEW075T7 800x480, EK79655 (GD7965)

WiFiManager wifiManager;
WiFiClient wifiClient; // for HTTP requests

const int httpPort = 80;

// note that BMP bitmaps are drawn at physical position in physical orientation of the screen
void showBitmapFrom_HTTP(const char *host, int port, const char *path, const char *filename, int16_t x, int16_t y, bool with_color);
void drawBitmaps_other();
uint32_t read8n(WiFiClient &client, uint8_t *buffer, int32_t bytes);
uint32_t skip(WiFiClient &client, int32_t bytes);
uint32_t read32(WiFiClient &client);
uint16_t read16(WiFiClient &client);

void setup()
{
  Serial.begin(115200);
  Serial.println();
  Serial.println("GxEPD2_WiFi_Example");

  display.init(115200);

  wifiManager.autoConnect();
  Serial.println("WiFi connected");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  drawBitmaps_other();

  Serial.println("GxEPD2_WiFi_Example done");
}

void loop(void)
{
}

void drawBitmaps_other()
{
  showBitmapFrom_HTTP("10.52.6.20", 8084, "/images/", "test2.bmp", 0, 0, false); // 8-bit grayscale
  delay(5000);

  showBitmapFrom_HTTP("10.52.6.20", 8084, "/images/", "test3.bmp", 0, 0, false); // 1-bit bitmap
  delay(5000);
}

static const uint16_t input_buffer_pixels = 800; // may affect performance

static const uint16_t max_row_width = 1872;     // for up to 7.8" display 1872x1404
static const uint16_t max_palette_pixels = 256; // for depth <= 8

uint8_t input_buffer[3 * input_buffer_pixels];        // up to depth 24
uint8_t output_row_mono_buffer[max_row_width / 8];    // buffer for at least one row of b/w bits
uint8_t output_row_color_buffer[max_row_width / 8];   // buffer for at least one row of color bits
uint8_t mono_palette_buffer[max_palette_pixels / 8];  // palette buffer for depth <= 8 b/w
uint8_t color_palette_buffer[max_palette_pixels / 8]; // palette buffer for depth <= 8 c/w
uint16_t rgb_palette_buffer[max_palette_pixels];      // palette buffer for depth <= 8 for buffered graphics, needed for 7-color display

void showBitmapFrom_HTTP(const char *host, int port, const char *path, const char *filename, int16_t x, int16_t y, bool with_color)
{
  WiFiClient client;
  bool connection_ok = false;
  bool valid = false; // valid format to be handled
  bool flip = true;   // bitmap is stored bottom-to-top
  uint32_t startTime = millis();
  if ((x >= display.epd2.WIDTH) || (y >= display.epd2.HEIGHT))
    return;
  Serial.println();
  Serial.print("downloading file \"");
  Serial.print(filename);
  Serial.println("\"");
  Serial.print("connecting to ");
  Serial.println(host);
  if (!client.connect(host, port))
  {
    Serial.println("HTTP connection failed");
    return;
  }
  Serial.print("requesting URL: ");
  Serial.println(String("http://") + host + path + filename);
  client.print(String("GET ") + path + filename + " HTTP/1.1\r\n" +
               "Host: " + host + "\r\n" +
               "User-Agent: GxEPD2_WiFi_Example\r\n" +
               "Connection: close\r\n\r\n");
  Serial.println("request sent");
  while (client.connected())
  {
    String line = client.readStringUntil('\n');
    if (!connection_ok)
    {
      connection_ok = line.startsWith("HTTP/1.1 200 OK");
      if (connection_ok)
        Serial.println(line);
      // if (!connection_ok) Serial.println(line);
    }
    if (!connection_ok)
      Serial.println(line);
    // Serial.println(line);
    if (line == "\r")
    {
      Serial.println("headers received");
      break;
    }
  }
  if (!connection_ok)
    return;
  // Parse BMP header

  Serial.println("Parsing BMP header");
  if (read16(client) == 0x4D42) // BMP signature
  {
    uint32_t fileSize = read32(client);
    uint32_t creatorBytes = read32(client);
    (void)creatorBytes;                    // unused
    uint32_t imageOffset = read32(client); // Start of image data
    uint32_t headerSize = read32(client);
    uint32_t width = read32(client);
    int32_t height = (int32_t)read32(client);
    uint16_t planes = read16(client);
    uint16_t depth = read16(client); // bits per pixel
    uint32_t format = read32(client);
    uint32_t bytes_read = 7 * 4 + 3 * 2;                   // read so far
    if ((planes == 1) && ((format == 0) || (format == 3))) // uncompressed is handled, 565 also
    {
      Serial.print("File size: ");
      Serial.println(fileSize);
      Serial.print("Image Offset: ");
      Serial.println(imageOffset);
      Serial.print("Header size: ");
      Serial.println(headerSize);
      Serial.print("Bit Depth: ");
      Serial.println(depth);
      Serial.print("Image size: "); // OK = 99x100
      Serial.print(width);
      Serial.print('x');
      Serial.println(height);
      // BMP rows are padded (if needed) to 4-byte boundary
      uint32_t rowSize = (width * depth / 8 + 3) & ~3;
      if (depth < 8)
        rowSize = ((width * depth + 8 - depth) / 8 + 3) & ~3;
      if (height < 0)
      {
        height = -height;
        flip = false;
      }
      uint16_t w = width;
      uint16_t h = height;
      if ((x + w - 1) >= display.epd2.WIDTH)
        w = display.epd2.WIDTH - x;
      if ((y + h - 1) >= display.epd2.HEIGHT)
        h = display.epd2.HEIGHT - y;
      Serial.println(String("") + "w=" + w + ", h=" + h);            // OK=[99, 100]
      Serial.println(String("") + "max_row_width=" + max_row_width); // OK=1872
      Serial.println(String("") + "rowSize=" + rowSize);             // OK=200
      if (w <= max_row_width)                                        // handle with direct drawing
      {
        valid = true;
        uint8_t bitmask = 0xFF;
        uint8_t bitshift = 8 - depth;
        uint16_t red, green, blue;
        bool whitish = false;
        bool colored = false;
        if (depth == 1)
          with_color = false;

        Serial.println(String("") + "depth=" + depth); // OK=24
        if (depth <= 8)
        {
          if (depth < 8)
            bitmask >>= depth;
          // bytes_read += skip(client, 54 - bytes_read); //palette is always @ 54
          bytes_read += skip(client, imageOffset - (4 << depth) - bytes_read); // 54 for regular, diff for colorsimportant
          for (uint16_t pn = 0; pn < (1 << depth); pn++)
          {
            blue = client.read();
            green = client.read();
            red = client.read();
            client.read();
            bytes_read += 4;
            whitish = with_color ? ((red > 0x80) && (green > 0x80) && (blue > 0x80)) : ((red + green + blue) > 3 * 0x80); // whitish
            colored = (red > 0xF0) || ((green > 0xF0) && (blue > 0xF0));                                                  // reddish or yellowish?
            if (0 == pn % 8)
              mono_palette_buffer[pn / 8] = 0;
            mono_palette_buffer[pn / 8] |= whitish << pn % 8;
            if (0 == pn % 8)
              color_palette_buffer[pn / 8] = 0;
            color_palette_buffer[pn / 8] |= colored << pn % 8;
          }
        }
        display.clearScreen();
        uint32_t rowPosition = flip ? imageOffset + (height - h) * rowSize : imageOffset;
        // Serial.print("skip "); Serial.println(rowPosition - bytes_read);
        bytes_read += skip(client, rowPosition - bytes_read);
        for (uint16_t row = 0; row < h; row++, rowPosition += rowSize) // for each line
        {
          if (!connection_ok || !(client.connected() || client.available()))
            break;
          delay(1); // yield() to avoid WDT
          uint32_t in_remain = rowSize;
          uint32_t in_idx = 0;
          uint32_t in_bytes = 0;
          uint8_t in_byte = 0;           // for depth <= 8
          uint8_t in_bits = 0;           // for depth <= 8
          uint8_t out_byte = 0xFF;       // white (for w%8!=0 border)
          uint8_t out_color_byte = 0xFF; // white (for w%8!=0 border)
          uint32_t out_idx = 0;
          for (uint16_t col = 0; col < w; col++) // for each pixel
          {
            yield();
            if (!connection_ok || !(client.connected() || client.available()))
              break;
            // Time to read more pixel data?
            if (in_idx >= in_bytes) // ok, exact match for 24bit also (size IS multiple of 3)
            {
              uint32_t get = in_remain > sizeof(input_buffer) ? sizeof(input_buffer) : in_remain;
              uint32_t got = read8n(client, input_buffer, get);
              while ((got < get) && connection_ok)
              {
                // Serial.print("got "); Serial.print(got); Serial.print(" < "); Serial.print(get); Serial.print(" @ "); Serial.println(bytes_read);
                uint32_t gotmore = read8n(client, input_buffer + got, get - got);
                got += gotmore;
                connection_ok = gotmore > 0;
              }
              in_bytes = got;
              in_remain -= got;
              bytes_read += got;
            }
            if (!connection_ok)
            {
              Serial.print("Error: got no more after ");
              Serial.print(bytes_read);
              Serial.println(" bytes read!");
              break;
            }
            switch (depth)
            {
            case 1:
            case 4:
            case 8:
            {
              if (0 == in_bits)
              {
                in_byte = input_buffer[in_idx++];
                in_bits = 8;
              }
              uint16_t pn = (in_byte >> bitshift) & bitmask;
              whitish = mono_palette_buffer[pn / 8] & (0x1 << pn % 8);
              colored = color_palette_buffer[pn / 8] & (0x1 << pn % 8);
              in_byte <<= depth;
              in_bits -= depth;
            }
            break;
            }
            if (whitish)
            {
              // keep white
            }
            else if (colored && with_color)
            {
              out_color_byte &= ~(0x80 >> col % 8); // colored
            }
            else
            {
              out_byte &= ~(0x80 >> col % 8); // black
            }
            if ((7 == col % 8) || (col == w - 1)) // write that last byte! (for w%8!=0 border)
            {
              output_row_color_buffer[out_idx] = out_color_byte;
              output_row_mono_buffer[out_idx++] = out_byte;
              out_byte = 0xFF;       // white (for w%8!=0 border)
              out_color_byte = 0xFF; // white (for w%8!=0 border)
            }
          } // end pixel
          int16_t yrow = y + (flip ? h - row - 1 : row);
          display.writeImage(output_row_mono_buffer, output_row_color_buffer, x, yrow, w, 1);
        } // end line
        Serial.print("downloaded and displayed in ");
        Serial.print(millis() - startTime);
        Serial.println(" ms");
        display.refresh();
      }
      Serial.print("bytes read ");
      Serial.println(bytes_read);
    }
  }
  client.stop();
  if (!valid)
  {
    Serial.println("bitmap format not handled.");
  }
}

// OK:
// Parsing BMP header
// File size: 30138
// Image Offset: 138
// Header size: 124
// Bit Depth: 24
// Image size: 99x100
// w=99, h=100
// max_row_width=1872
// rowSize=300
// depth=24

// bad:
// Parsing BMP header
// File size: 1536138
// Image Offset: 138
// Header size: 124
// Bit Depth: 32
// Image size: 800x480
// w=800, h=480
// max_row_width=1872
// rowSize=3200
// depth=32

// OK (saved from Photoshop)
// Parsing BMP header
// File size: 385080
// Image Offset: 1078
// Header size: 40
// Bit Depth: 8
// Image size: 800x480
// w=800, h=480
// max_row_width=1872
// rowSize=800
// depth=8

uint16_t read16(WiFiClient &client)
{
  // BMP data is stored little-endian, same as Arduino.
  uint16_t result;
  ((uint8_t *)&result)[0] = client.read(); // LSB
  ((uint8_t *)&result)[1] = client.read(); // MSB
  return result;
}

uint32_t read32(WiFiClient &client)
{
  // BMP data is stored little-endian, same as Arduino.
  uint32_t result;
  ((uint8_t *)&result)[0] = client.read(); // LSB
  ((uint8_t *)&result)[1] = client.read();
  ((uint8_t *)&result)[2] = client.read();
  ((uint8_t *)&result)[3] = client.read(); // MSB
  return result;
}

uint32_t skip(WiFiClient &client, int32_t bytes)
{
  int32_t remain = bytes;
  uint32_t start = millis();
  while ((client.connected() || client.available()) && (remain > 0))
  {
    if (client.available())
    {
      client.read();
      remain--;
    }
    else
      delay(1);
    if (millis() - start > 2000)
      break; // don't hang forever
  }
  return bytes - remain;
}

uint32_t read8n(WiFiClient &client, uint8_t *buffer, int32_t bytes)
{
  int32_t remain = bytes;
  uint32_t start = millis();
  while ((client.connected() || client.available()) && (remain > 0))
  {
    if (client.available())
    {
      int16_t v = client.read();
      *buffer++ = uint8_t(v);
      remain--;
    }
    else
      delay(1);
    if (millis() - start > 2000)
      break; // don't hang forever
  }
  return bytes - remain;
}
