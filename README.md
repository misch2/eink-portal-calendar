# "Portal" calendar for e-ink display

<img src="https://github.com/misch2/eink-portal-calendar/assets/16558674/b2b185de-a960-480c-99a1-aa7d521ed9d6" width="250">
<img src="https://github.com/misch2/eink-portal-calendar/assets/16558674/66098158-f8c2-456c-95e3-673dab4ea655" width="250">

## Examples (screenshots)

[<img height="100" src="screenshots/googlefit_weight.png"> <img height="100" src="screenshots/weather_raining.png"> <img height="100" src="screenshots/weather_mixed.png"> <img height="100" src="screenshots/compare_source_and_output2.png"> <img height="100" src="screenshots/config_epaper.png"> <img height="100" src="screenshots/displays_overview.png"> <img height="100" src="screenshots/compare_source_and_output.png">](screenshots/README.md)

## ⚠️ Important notice!

If you are interested in building this, please **consider using the [ZivyObraz](https://zivyobraz.eu/) service instead**. 
It's very reasonably priced (and first year free, IIRC), comes with a precompiled firmware for LaskaKit board and it is incredibly easy to use and set up.

Getting a copy of my project to work requires some basic software knowledge and I would only recommend it to people who like experimenting or are willing to take this project as an inspiration for their own work. 

## Summary

Inspired by https://github.com/wuspy/portal_calendar. Hardware is mostly the same but the software is different.

The software is divided into two parts:
 1. Simple ESP32 web client which only handles the e-Paper display
 2. PC/Raspberry webserver + backend process which produces the images and takes care of everything else, e.g.:
    - integration with web calendars
    - integration with weather provider
    - integration with HomeAssistant (battery & status monitor), 
    - configuration UI
 
I've chosen this approach because it's easier and more fun for me to implement the server part in my favourite environments (C#/.NET and HTML+CSS) than to try to do this directly on ESP32.

Everything is designed for a specific e-Paper size of 480x800 pixels, but I'm trying to use relative units in CSS so it should be possible to use different size just by changing the screen and font size.

Images are served as raw bitmaps and the task for ESP is only to fetch this image and display it.

All the rendering is performed on the server, using standard HTML + CSS. This allows me to provide content without constantly re-flashing the ESP32. It's also much easier for me to debug CSS and try to pixel-perfect position everything or to integrate for example ICS calendar etc.

I also added a voltage monitoring because with ePaper it's not easily detectable when the battery goes low -- the old image just keeps being on the display. Also the server tries to keep track of when each of the ePaper display should contact it and if it doesn't happen for a while, it will display a warning in the configuration UI (TODO: send this warning via email or Telegram message).

## Bill of materials

* Display:
  + [Waveshare 7.5" 800x480 ePaper B/W display](https://www.laskakit.cz/waveshare-7-5--800x480-epaper-raw-displej-bw/)
  + or [WFT0583CZ61 7.5" 800x480 ePaper B/W/R display](https://www.aliexpress.com/item/1005005121813674.html)
  + or [GDEM075F52 7.5" 4C 800x480 ePaper B/W/R/Y display](https://www.aliexpress.com/item/1005010179550278.html)
* ESP32 board: [LaskaKit low power ePaper ESP32 board with USB-C and LiPol charging circuit](https://www.laskakit.cz/laskakit-espink-esp32-e-paper-pcb-antenna/)
* Power source: [LiPol battery](https://www.laskakit.cz/geb-lipol-baterie-805060-3000mah-3-7v-jst-ph-2-0/)
* ePaper frame: [3D printed frame by @MultiTricker](https://www.printables.com/model/541552-ramecek-pro-epaper-75-waveshare-i-good-display-v1/related)
* And optionally: [FFC cable](https://www.laskakit.cz/ffc-fpc-nestineny-flexibilni-kabel-awm-20624-80c-60v-0-5mm-24pin--10cm/) + [FFC FPC connector](https://www.laskakit.cz/laskakit-e-paper-ffc-fpc-24pin-atapter/) for easier connection between the display and the ESP32 board

## Installation

### 1. Server

1. Download the [latest release](https://github.com/misch2/eink-portal-calendar/releases) Windows binary (`portal-calendar-server-windows-x64-*.zip`)
1. Unzip it
1. Run the `PortalCalendarServer.exe`
1. In your browser go to http://localhost:5000/ and log in as `admin` with password `changeme`
1. You should see an empty list of displays there with only the "Default settings" being listed there.

### 2. Client (ESP32)

**Prerequisites:** [PlatformIO](https://platformio.org/) (VS Code extension or [PlatformIO Core](https://platformio.org/install/cli) CLI)

Download the [latest release](https://github.com/misch2/eink-portal-calendar/releases) source code ( `eink-portal-calendar-*.zip` ) and unzip it.

**Step 1 — Choose a board config.**  
Copy the example that best matches your hardware from `client/include/boards/` (see the table of verified displays below). The available environments defined in `platformio.ini` are:

| Environment | Board | Display |
|---|---|---|
| `example1` | LaskaKit ESPink 2.5 | black and white GDEW075T7 |
| `example2` | LaskaKit ESPink 3.5 | 3 color GDEW075Z08 |
| `example3` | LaskaKit ESPink 2.5 | 4 color GDEM075F52 |

**Step 2 — Flash the firmware.**

```
pio run -e example -t upload --upload-port COM5
```

Replace `example` with your chosen environment and `COM5` with the correct serial port.

**Step 3 — Configure WiFi.**  
On first boot the ESP32 starts a WiFi access point `ESP32-xxyyzz` (where xxyyzz is a part of the MAC address). Connect to it with your phone, enter your WiFi credentials in the captive portal, and save. The device will reboot and connect to your network.

**Step 4 — Wait for the first image.**  
After connecting to WiFi the ESP32 will contact the server and display the first generated image.

**Troubleshooting:** Connect to the serial port at 115200 baud to see detailed log output from the ESP32.

## Verified ePaper displays and controllers

See the `client/include/driver/` and `client/include/epaper/` folders for a list of verified configurations.

## Sources:

 - The portal sign icons were downloaded from https://decalrobot.com/. 
   - Icons in `server/PortalCalendarServer/wwwroot/images/portal_icons` were extracted manually from the source image
 - Fonts in `server/PortalCalendarServer/wwwroot/fonts` were downloaded from:
   - `d-din/` — https://www.fontsquirrel.com/fonts/d-din (SIL Open Font License 1.1)
   - `gidole/` — https://github.com/larsenwork/Gidole (OFL + MIT)
   - `clear-sans/` — https://www.fontsquirrel.com/fonts/clear-sans (Apache License 2.0)
 - Weather Icons font — https://github.com/erikflowers/weather-icons (font: SIL OFL 1.1, CSS: MIT)
 - Multi-display support (and other functionalities too) inspired by https://zivyobraz.eu/

## License

The source code of this project is licensed under the **[MIT License](LICENSE)**.

The compiled firmware binary incorporates [GxEPD2](https://github.com/ZinggJM/GxEPD2) (GPLv3) and is therefore distributed as a combined work under the **[GNU General Public License v3](https://www.gnu.org/licenses/gpl-3.0.html)**.

Third-party assets bundled in this repository have their own licenses:

| Asset | License |
|---|---|
| D-DIN fonts ( `server/.../fonts/d-din/` ) | SIL Open Font License 1.1 |
| Gidole font ( `server/.../fonts/gidole/` ) | OFL + MIT |
| Clear Sans font ( `server/.../fonts/clear-sans/` ) | Apache 2.0 |
| Weather Icons ( `server/.../wwwroot/font/` ) | Font: SIL OFL 1.1, CSS: MIT |
| Open Sans, DejaVu Sans Mono (client header fonts) | Apache 2.0 / Bitstream Vera |
| Portal sign icons ( `server/.../images/portal_icons/` ) | © Valve Corporation (fan use) |
| GxEPD2 library | GNU General Public License v3 |
