#pragma once

#include "logger.h"

// Backward compatibility macros - use logger instead of direct macros
#define DEBUG_PRINT(...) logger.debug(__VA_ARGS__)
#define TRACE_PRINT(...) logger.trace(__VA_ARGS__)
