#pragma once

#include "board.h"

// Default for an optional display rotation (1 = vertical with connector on the left side).
// This is used only for error messages. Every other usage is configurable on the server side.

// 0 = Landscape, FPC connector on the bottom
// 1 = Portrait, FPC connector on the left side
// 2 = Landscape, FPC connector on the top
// 3 = Portrait, FPC connector on the right side
#ifndef DISPLAY_ROTATION
#define DISPLAY_ROTATION 1
#endif

// Include the correct GxEPD2 header file based on the display type defined in board.h and set the corresponding color type and bits per pixel (BPP) for bitmap
// handling.
#ifdef DISPLAY_TYPE_BW
#define DISPLAY_COLOR_TYPE_AS_STRING "BW"
#include <GxEPD2_BW.h>
#endif

#ifdef DISPLAY_TYPE_3C
#define DISPLAY_COLOR_TYPE_AS_STRING "3C"
#include <GxEPD2_3C.h>
#endif

#ifdef DISPLAY_TYPE_4C
#define DISPLAY_COLOR_TYPE_AS_STRING "4C"
#include <GxEPD2_4C.h>
#endif

// TODO
// #ifdef DISPLAY_TYPE_GRAYSCALE
// #define DISPLAY_COLOR_TYPE_AS_STRING "4G"
// #ifdef USE_GRAYSCALE_BW_DISPLAY
// #include <GxEPD2_4G_BW.h>
// #else
// #include <GxEPD2_4G_4G.h>
// #endif
// #endif

#define DISPLAY_BUFFER_SIZE (DISPLAY_WIDTH * BITMAP_BPP / 8)
