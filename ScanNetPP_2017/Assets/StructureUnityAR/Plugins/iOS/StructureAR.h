/*
 This file is part of the Structure SDK.
 Copyright © 2015 Occipital, Inc. All rights reserved.
 http://structure.io
*/

#import <Structure/Structure.h>

// We have disabled the Unity rendering loop by default.
// This is called by the native plugin to tell Unity to render
void TriggerUnityRendering();

@interface StructureAR : NSObject  <AVCaptureVideoDataOutputSampleBufferDelegate, STSensorControllerDelegate>
{
}

+(StructureAR*)sharedStructureAR;
@end
