# "Portal" calendar for e-ink display

Heavily inspired by https://github.com/wuspy/portal_calendar.

The main difference is that this calendar is split into two parts:
 1. Simple ESP32 or ESP8266 web client which handles the eInk display
 2. PC/Raspberry webserver which produces the images and (in the future) handles integration with web calendars etc.
 
I choose this approach because it's easier (and more fun) for me to implement the server part in my favourite environments (Perl, NodeJS, HTML+CSS) than to try to do this directly on ESP32.


## Principles

- Everything is designed for a specific eInk size of 480x800 pixels. 
- The display content is served as raw bitmap. ESP is expected to just fetch this image from a specific location and display it.
- ESP8266 (which my driver board uses) has very limited amount of RAM. It definitely can't read PNG into memory and then transform it and sent to display. The application therefore reads uncompressed bitmap from server and sends each line to the display as it reads the data stream from http. This means that only a small buffer for 1 image line is needed in RAM.


## Bill of materials
- [Waveshare 7.5" 800x480 ePaper B/W display](https://www.laskakit.cz/waveshare-7-5--640x384-epaper-raw-displej-bw/)
- [ESP8266 ePaper driver board](https://www.laskakit.cz/waveshare-esp8266-e-paper-raw-panel-driver-board/)
- [FFC FPC cable](https://www.laskakit.cz/ffc-fpc-nestineny-flexibilni-kabel-awm-20624-80c-60v-0-5mm-24pin--20cm/)
- [FFC FPC connector](https://www.laskakit.cz/laskakit-e-paper-ffc-fpc-24pin-atapter/)


## Installation 
Edit the `.env` file and set `PORT` for HTTP server.
```
$ sudo apt install perl libimlib2-dev libimlib2
$ make install_modules
$ make run
```

## Disclaimer

I don't expect anyone to use this project directly, mainly because it's written in Perl. But the HTML, CSS and ESP code might serve as an inspiration maybe.

The `custom-portal-sign-icons.png` and `custom-portal-sign-full.png` were downloaded from https://decalrobot.com/. 

The `D-DIN` font set was downloaded from https://www.fontsquirrel.com/fonts/d-din.


---

## Examples:

Half-finished:
![image](https://user-images.githubusercontent.com/16558674/214158618-31573f8c-0cd9-4471-a230-aabc3bd393cd.png)

How the grayscale rendered image (`/calendar/bitmap?rotate=0&flip=`) looks like:
![image](https://user-images.githubusercontent.com/16558674/214158921-2f32e729-0c1f-4a88-ba1f-8cc30d49ce02.png)

And how the B&W bitmap looks like with custom threshold (`/calendar/bitmap?rotate=0&flip=&threshold=165`):
![image](https://user-images.githubusercontent.com/16558674/214159074-c412f859-92c3-431f-9361-c70cbd15db3c.png)

