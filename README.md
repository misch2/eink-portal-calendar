# Portal calendar for e-ink display

Heavily inspired by https://github.com/wuspy/portal_calendar.

:warning: FIXME not finished yet

The main difference is that this calendar is split into two parts:
 1. Simple ESP32 or ESP8266 web client which handles the eInk display
 2. PC/Raspberry webserver which produces the images and (in the future) handles integration with web calendars etc.
 

I choose this approach because it's easier (and more fun) for me to implement the server part in my favourite environments (Perl, NodeJS, HTML+CSS) than to try to do this directly on ESP32.


## Principles

- Everything is designed for a specific eInk size of 480x800 pixels. 
- The display content is served as PNG image. ESP32 is expected to just fetch this image from a specific location and display it.
- :warning:




## Prerequisities:

Installation: `make install_modules`

The `custom-portal-sign-icons.png` and `custom-portal-sign-full.png` were downloaded from https://decalrobot.com/. The `D-DIN` font set was downloaded from https://www.fontsquirrel.com/fonts/d-din

