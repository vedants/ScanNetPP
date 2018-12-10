/*
 This file is part of the Structure SDK.
 Copyright © 2015 Occipital, Inc. All rights reserved.
 http://structure.io
*/

#pragma once

#include "UnityAppController.h"

@interface StructureUnityAppController : UnityAppController

@property (atomic) BOOL trackerWillCallRepaint;

- (void)shouldAttachRenderDelegate;

- (void)applicationDidEnterBackground:(UIApplication*)application;
- (void)applicationDidBecomeActive:(UIApplication*)application;

@end

// This macro will make it so that a StructureUnityAppController instance is setup as the unity app controller at startup.
IMPL_APP_CONTROLLER_SUBCLASS(StructureUnityAppController)
