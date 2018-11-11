/*
 This file is part of the Structure SDK.
 Copyright © 2015 Occipital, Inc. All rights reserved.
 http://structure.io
*/

#import "StructureUnityAppController.h"
#import "UnityAppController+Rendering.h"

#import <Structure/Structure.h>

extern bool	_didResignActive;


@implementation StructureUnityAppController

- (void)shouldAttachRenderDelegate
{
    // [StructureUnityAppController setupWirelessDebugging];
    
    //happens before createDisplayLink
    ::printf("StructureUnityAppController.shouldAttachRenderDelegate");
    
    self.trackerWillCallRepaint = NO;
}

//we override the createDisplayLink from the UnityAppController.

- (void)createDisplayLink
{
    // do we want to ensure that this refresh rate is set to 30 fps?
    int animationFrameInterval = (int)(60.0f / (float)UnityGetTargetFPS());
    assert(animationFrameInterval >= 1);
    
    self.trackerWillCallRepaint = NO;
    
    // our own repaintDisplayLink method should be called here.
    // after this we decide how often we need to call the repaintDisplayLink....
    _displayLink = [CADisplayLink displayLinkWithTarget:self selector:@selector(repaintDisplayLink)];
    [_displayLink setFrameInterval:animationFrameInterval];
    [_displayLink addToRunLoop:[NSRunLoop currentRunLoop] forMode:NSRunLoopCommonModes];

}

// we decide when to repaint unity, usually we do not
// need to block
- (void)repaintDisplayLink
{
    if (self.trackerWillCallRepaint)
        return;
    
    // we can make the decision of when to call repaint
    // once we have started StructureSensor we call repaint
    // with TriggerUnityRendering()
    
    [self repaintSTTrackerLink];
}

// actually repaint on demand determined by the plugin
- (void) repaintSTTrackerLink
{
    if(!_didResignActive)
    {
        [self repaint];
    }
}

- (void)applicationDidEnterBackground:(UIApplication*)application
{
    [super applicationDidEnterBackground:application];
}

- (void)applicationDidBecomeActive:(UIApplication*)application
{
    [super applicationDidBecomeActive:application];
}

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    /*  iOS 9.2+ introduced unexpected behavior: every time a Structure Sensor is
     plugged in to iOS, iOS will launch all Structure SDK apps in the background.
     The apps will not be visible to the user.  This can cause problems since
     Structure SDK apps typically ask the user for permission to use the camera
     when launched.  This leads to the user's first experience with a Structure
     SDK app being:
     1.  Download Structure SDK apps from App Store
     2.  Plug in Structure Sensor to iPad
     3.  Get bombarded with "X app wants to use the Camera" notifications from
         every installed Structure SDK app
     
     Each app has to deal with this problem in its own way.  In the Structure SDK,
     sample apps peacefully exit without causing a crash report.  This also
     has other benefits, such as not using memory.  Note that Structure SDK does
     not support connecting to Structure Sensor if the app is in the background. */
    if ([UIApplication sharedApplication].applicationState == UIApplicationStateBackground) {
        NSLog(@"iOS launched %@ in the background.  This app is not designed to be launched in the background so it will exit peacefully.",
              [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleDisplayName"]);
        exit(0);
    }
    return [super application:application didFinishLaunchingWithOptions:launchOptions];
}

@end
