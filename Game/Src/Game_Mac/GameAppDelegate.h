// Copyright (C) 2006-2011 NeoAxis Group Ltd.

#import <Cocoa/Cocoa.h>

//for 10.5
@interface GameAppDelegate : NSObject
//@interface GameAppDelegate : NSObject<NSApplicationDelegate>
{
    NSWindow *window;
}

@property (assign) IBOutlet NSWindow *window;

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication;

@end
