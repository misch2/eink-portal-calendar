# "Portal" calendar for e-ink display

<img src="https://github.com/misch2/eink-portal-calendar/assets/16558674/b2b185de-a960-480c-99a1-aa7d521ed9d6" width="250">
<img src="https://github.com/misch2/eink-portal-calendar/assets/16558674/66098158-f8c2-456c-95e3-673dab4ea655" width="250">


## Examples (screenshots)

[<img height="100" src="screenshots/googlefit_weight.png"> <img height="100" src="screenshots/weather_raining.png"> <img height="100" src="screenshots/weather_mixed.png"> <img height="100" src="screenshots/compare_source_and_output2.png"> <img height="100" src="screenshots/config_epaper.png"> <img height="100" src="screenshots/displays_overview.png"> <img height="100" src="screenshots/compare_source_and_output.png">](screenshots/README.md)

## ⚠️ Important notice!

If you are interested in building this, please **consider using the [ZivyObraz](https://zivyobraz.eu/) service instead**. 
It's very reasonably priced (and first year free, IIRC), comes with a precompiled firmware for LaskaKit board and it is incredibly easy to use and set up.

Getting a copy of my project to work requires some non-trivial knowledge and I would only recommend it to people who like experimenting and who are willing to take this project as an inspiration for their own work. 
I *can't guarantee* that any of READMEs is up to date and that there are not hidden obstacles in getting the client or server part running. This repository contains almost all the source files for my calendars, but there are configuration files specific for my environment (e.g. WiFi password, server IP addresses etc.) and while I tried to put an equivalent of these files to the `client/include/boards/example/` and `client/include/boards/example_4color_GDEM075F52_and_laskakit_ESPink_v2.5/` folders, I can't guarantee it's directly usable without any modifications.

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

All the rendering is performed on the server, using standard HTML + CSS. This allows me to use provide content without constantly re-flashing the ESP32. It's also much easier for me to debug CSS and try to pixel-perfect position everything or to integrace for example ICS calendar etc.

I also added a voltage monitorig because with ePaper it's not easily detactable when the battery goes low -- the old image just keeps being on the display. Also the server tries to keep track of when each of the ePaper display should contact it and if it doesn't happen for a while, it will display a warning in the configuration UI (TODO: send this warning via email or Telegram message).

## Bill of materials

* Display:
  * [Waveshare 7.5" 800x480 ePaper B/W display](https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/)
  * or [WFT0583CZ61 7.5" 800x480 ePaper B/W/R display](https://www.aliexpress.com/item/1005005121813674.html?spm=a2g0o.order_list.order_list_main.5.1a521802F7URVo)
* ESP32 board: [LaskaKit low power ePaper ESP32 board with USB-C and LiPol charging circuit](https://www.laskakit.cz/laskakit-espink-esp32-e-paper-pcb-antenna/)
* Power source: [LiPol battery](https://www.laskakit.cz/geb-lipol-baterie-805060-3000mah-3-7v-jst-ph-2-0/)
* ePaper frame: [3D printed frame by @MultiTricker](https://www.printables.com/model/541552-ramecek-pro-epaper-75-waveshare-i-good-display-v1/related)
* And optionally: [FFC cable](https://www.laskakit.cz/ffc-fpc-nestineny-flexibilni-kabel-awm-20624-80c-60v-0-5mm-24pin--10cm/) + [FFC FPC connector](https://www.laskakit.cz/laskakit-e-paper-ffc-fpc-24pin-atapter/) for easier connection between the display and the ESP32 board

## Sources:
 - The portal sign icons were downloaded from https://decalrobot.com/. 
   - Icons in `server/PortalCalendarServer/wwwroot/images/portal_icons` were extracted manually from the source image
 - Fonts in `server/PortalCalendarServer/wwwroot/fonts` were downloaded from:
   - D-DIN-BOLD.otf, D-DIN.otf, D-DINCondensed.otf
     https://www.fontsquirrel.com/fonts/d-din (ASCII and basic accents only)
   - 651-font.otf
     https://cs.fontsisland.com/font/din-pro (full Czech set of characters)
 - Files in `client/wuspy_portal_calendar` are git-cloned from https://github.com/wuspy/portal_calendar.git (see `.gitmodules` file in the root folder)
 - "Broken display" overlay was downloaded from https://www.wallpaperflare.com/technology-cracked-screen-broken-screen-no-people-animal-wildlife-wallpaper-jpnv
 - Multi-display support (and other functionalities too) inspired by https://zivyobraz.eu/
 
## Verified ePaper displays and controllers

All configurations use 7.5" 800x480 ePaper displays. Board configuration files are in `client/include/boards/`.

| ePaper panel | Colors | GxEPD2 driver class | Controller | Example board config |
|---|---|---|---|---|
| GoodDisplay GDEW075T7 | B/W (2 colors) | `GxEPD2_750_T7` | GD7965 (EK79655) | `example/` |
| GoodDisplay GDEW075Z08 (WFT0583CZ61 compatible) | B/W/R (3 colors) | `GxEPD2_750c_Z08` | GD7965 | `calendar2_weather/` |
| GoodDisplay GDEM075F52 | B/W/R/Y (4 colors) | `GxEPD2_750c_GDEM075F52` | — | `example_4color_GDEM075F52_and_laskakit_ESPink_v2.5/` |

Verified ESP32 controller boards:

| Board | Notes | Example board config |
|---|---|---|
| EzSBC ESP32 breakout board | Generic ESP32, custom SPI pin mapping | `calendar1_portal/` |
| LaskaKit ESPink v2.5 | Low power ePaper ESP32 board with USB-C, LiPol charging, ePaper power control via GPIO 2 | `example_4color_GDEM075F52_and_laskakit_ESPink_v2.5/` |

Color type support in the firmware: B/W (`DISPLAY_TYPE_BW`), 3-color (`DISPLAY_TYPE_3C`), and 4-color (`DISPLAY_TYPE_4C`).
