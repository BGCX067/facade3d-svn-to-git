// Copyright (C) 2006-2011 NeoAxis Group Ltd.
#include "precompiled.h"
#include "MyNativeDLL.h"

EXPORT int Test( int parameter )
{
	if(sizeof(void*) == 8)
		return 64;
	else
		return 32;
}
