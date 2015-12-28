// Copyright (C) 2006-2011 NeoAxis Group Ltd.
#pragma once

#ifdef PLATFORM_WINDOWS
	#define EXPORT extern "C" __declspec(dllexport)
#elif defined(PLATFORM_MACOS)
	#define EXPORT extern "C" __attribute__ ((visibility("default")))
#else
	#error Unknown platform
#endif
