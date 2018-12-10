/*
  This file is part of the Structure SDK.
  Copyright © 2015 Occipital, Inc. All rights reserved.
  http://structure.io
*/

#import "StructureAR.h"
#import <OpenGLES/ES2/glext.h>
#import <Structure/StructureSLAM.h>
#import <mach/mach_time.h>
#import <vector>

#import "UnityAppController.h"
#import "StructureUnityAppController.h"
#import "UnityAppController+Rendering.h"

extern "C"
{
    typedef void (*Callback_void)();
    typedef void (*Callback_bool)(bool);
    typedef void (*Callback_IntPtr)(void*);
    typedef void (*Callback_IntPtr_int_int)(void*, int, int);
    typedef void (*Callback_float_float_float)(float, float, float);
    typedef void (*Callback_float_float_float_float)(float, float, float, float);
    typedef void (*Callback_CharPtr)(char*);
}

struct UnityCallbacks
{
    void notifySensorConnected ()
    {
        assert(0 != functions.notifySensorConnected);
        (*functions.notifySensorConnected)();
    }

    void notifySensorDisconnected ()
    {
        assert(0 != functions.notifySensorDisconnected);
        (*functions.notifySensorDisconnected)();
    }

    void notifyCameraAccessRequired ()
    {
        assert(0 != functions.notifyCameraAccessRequired);
        (*functions.notifyCameraAccessRequired)();
    }

    void notifySensorNeedsCharging ()
    {
        assert(0 != functions.notifySensorNeedsCharging);
        (*functions.notifySensorNeedsCharging)();
    }

    void setCameraPosition (float x, float y, float z)
    {
        assert(0 != functions.setCameraPosition);
        (*functions.setCameraPosition)(x, y, z);
    }

    void setCameraRotation (float q0, float q1, float q2, float q3)
    {
        assert(0 != functions.setCameraRotation);
        (*functions.setCameraRotation)(q0, q1, q2, q3);
    }

    void setCameraScale (float x, float y, float z)
    {
        assert(0 != functions.setCameraScale);
        (*functions.setCameraScale)(x, y, z);
    }

    void setCameraProjectionMatrix (float* projMatrix)
    {
        assert(0 != functions.setCameraProjectionMatrix);
        (*functions.setCameraProjectionMatrix)(projMatrix);
    }

    void notifyTrackingStatus (bool ok)
    {
        assert(0 != functions.notifyTrackingStatus);
        (*functions.notifyTrackingStatus)(ok);
    }

    void notifyMeshReady ()
    {
        assert(0 != functions.notifyMeshReady);
        (*functions.notifyMeshReady)();
    }

    void setDepthBuffer (float* buf, int w, int h)
    {
        assert(0 != functions.setDepthBuffer);
        (*functions.setDepthBuffer)(buf, w, h);
    }
    
    void sensorLog(char* str)
    {
        assert(0 != functions.sensorLog);
        (*functions.sensorLog)(str);
    }
    
    UnityCallbacks ()
    {
        std::memset(&functions, 0, sizeof(Functions));
    }

	struct Functions
	{
		Callback_void                    notifySensorConnected;
		Callback_void                    notifySensorDisconnected;
		Callback_void                    notifyCameraAccessRequired;
		Callback_void                    notifySensorNeedsCharging;
		Callback_bool                    notifyTrackingStatus;
		Callback_IntPtr_int_int          setDepthBuffer;
		Callback_float_float_float       setCameraPosition;
		Callback_float_float_float_float setCameraRotation;
		Callback_float_float_float       setCameraScale;
		Callback_IntPtr                  setCameraProjectionMatrix;
		Callback_void                    notifyMeshReady;
        Callback_CharPtr                 sensorLog;
	};
    Functions functions;
};

static UnityCallbacks ucb;

extern "C" {

	void _setCallbacks (
		Callback_void                    notifySensorConnected,
		Callback_void                    notifySensorDisconnected,
		Callback_void                    notifyCameraAccessRequired,
		Callback_void                    notifySensorNeedsCharging,
		Callback_bool                    notifyTrackingStatus,
		Callback_IntPtr_int_int          setDepthBuffer,
		Callback_float_float_float       setCameraPosition,
		Callback_float_float_float_float setCameraRotation,
		Callback_float_float_float       setCameraScale,
		Callback_IntPtr                  setCameraProjectionMatrix,
		Callback_void                    notifyMeshReady,
        Callback_CharPtr                 sensorLog
	)
    {
		ucb.functions.notifySensorConnected      = notifySensorConnected;
		ucb.functions.notifySensorDisconnected   = notifySensorDisconnected;
		ucb.functions.notifyCameraAccessRequired = notifyCameraAccessRequired;
		ucb.functions.notifySensorNeedsCharging  = notifySensorNeedsCharging;
		ucb.functions.notifyTrackingStatus       = notifyTrackingStatus;
		ucb.functions.setDepthBuffer             = setDepthBuffer;
		ucb.functions.setCameraPosition          = setCameraPosition;
		ucb.functions.setCameraRotation          = setCameraRotation;
		ucb.functions.setCameraScale             = setCameraScale;
		ucb.functions.setCameraProjectionMatrix  = setCameraProjectionMatrix;
		ucb.functions.notifyMeshReady            = notifyMeshReady;
        ucb.functions.sensorLog                  = sensorLog;
    }
}

namespace // anonymous namespace for local functions
{
    NSString* computeTrackerMessage (STTrackerHints hints)
    {
        if (hints.trackerIsLost)
            return @"Tracking Lost! Please Realign or Press Reset.";
        
        if (hints.modelOutOfView)
            return @"Please put the model back in view.";
        
        if (hints.sceneIsTooClose)
            return @"Too close to the scene! Please step back.";

        return nil;
    }
}

struct Options
{
    const GLKVector3 initialVolumeSize = GLKVector3Make (2.f, 1.f, 2.f);
    
    // We will use a color overlay, so we want registered depth.
    const STStreamConfig structureStreamConfig = STStreamConfigDepth320x240;
    
    const float lensPosition = 0.75f;
    
    const bool useColorTracking = true;
};

enum ScannerState
{
    ScannerStateUnknown = -1,
    
    // Defining the volume to scan
    ScannerStateCubePlacement = 0,
    
    // Scanning
    ScannerStateScanning,
    
    // Playing the Game
    ScannerStatePlaying,
    
    NumStates
};

// Utility struct to manage a gesture-based scale.
struct PinchScaleState
{
    PinchScaleState ()
    : currentScale (1.f)
    , initialPinchScale (1.f)
    {}
    
    float currentScale;
    float initialPinchScale;
};

// SLAM-related members.
struct SlamData
{
    SlamData ()
    : initialized (false)
    , scannerState (ScannerStateUnknown)
    {}
    
    bool initialized;
    
    STScene* scene;
    STTracker* tracker;
    STMapper* mapper;
    STCameraPoseInitializer* cameraPoseInitializer;
    ScannerState scannerState;

    GLKVector3 volumeSizeInMeters = GLKVector3Make(NAN, NAN, NAN);
};

// Display related members.
struct DisplayData
{
    DisplayData ()
    : framebufferReady (false)
    , viewAlreadyAppeared (false)
    {
    }
    ~DisplayData ()
    {
        if (lumaTexture)
        {
            CFRelease (lumaTexture);
            lumaTexture = NULL;
        }
        
        if (chromaTexture)
        {
            CFRelease (chromaTexture);
            lumaTexture = NULL;
        }
        
        if (videoTextureCache)
        {
            CFRelease(videoTextureCache);
            videoTextureCache = NULL;
        }
    }
    
    // OpenGL context.
    EAGLContext* context;
    
    GLuint unityCameraTexture;
    GLuint unityCameraTextureFramebuffer;
    
    // OpenGL Texture reference for y images.
    CVOpenGLESTextureRef lumaTexture;
    
    // OpenGL Texture reference for color images.
    CVOpenGLESTextureRef chromaTexture;
    
    // OpenGL Texture cache for the color camera.
    CVOpenGLESTextureCacheRef videoTextureCache;
    
    // Shader to render a GL texture as a simple quad.
    STGLTextureShaderYCbCr *yCbCrTextureShader;
    
    // Renders the volume boundaries as a cube.
    STCubeRenderer* cubeRenderer;
    
    // OpenGL viewport.
    GLfloat viewport[4];
    
    // Is the frame buffer initialized and ready to draw?
    bool framebufferReady;
    
    // Whether viewDidAppear was already triggered in the past.
    bool viewAlreadyAppeared;
    
    // Mesh rendering alpha
    float meshRenderingAlpha = 0.8;
};

struct MeshObj
{
    GLKVector3* vertices;
    GLKVector3* normals;
    unsigned short* faces;
    int numberOfMeshVertices;
    int numberOfMeshFaces;
    GLKVector3 meshOffset;
};

static StructureAR *sharedStructureAR = nil;
extern void UnityPause(bool pause);
extern void UnitySendMessage(const char *, const char *, const char *);

@interface StructureAR ()
{
    Options _options;
    
    DisplayData _display;
    SlamData _slamState;
    
    // Most recent gravity vector from IMU.
    GLKVector3 _lastGravity;
    
    // Scale of the scanning volume.
    PinchScaleState _volumeScale;
    
    // Structure Sensor controller.
    STSensorController* _stSensorController;
    
    // The decimated mesh for interaction
    STMesh* _decimatedMesh;
    
    // Color camera capture.
    AVCaptureSession* _avCaptureSession;
    AVCaptureDevice* _videoDevice;
    
    // IMU handling.
    CMMotionManager* _motionManager;
    NSOperationQueue* _imuQueue;
    
    GLKMatrix4 cameraGLProjection;
}

@end

@implementation StructureAR

+(StructureAR*)sharedStructureAR
{
    if(sharedStructureAR==nil)
    {
        sharedStructureAR = [[StructureAR alloc] init];
    }
    return sharedStructureAR;
}

+(void)setupWirelessDebugging
{
    // STWirelessLog is very helpful for debugging while your Structure Sensor is plugged in.
    // See SDK documentation for how to start a listener on your computer.
//    NSError* error = nil;
//    NSString *remoteLogHost = @"192.168.1.1";
//    [STWirelessLog broadcastLogsToWirelessConsoleAtAddress:remoteLogHost usingPort:4999 error:&error];
//    if (error)
//        NSLog(@"Oh no! Can't start wireless log: %@", [error localizedDescription]);
}

-(void) initStructureAR
{
    assert([GetAppController() renderingAPI] == UnityRenderingAPI::apiOpenGLES2 && 
        "This version of the Structure Unity Plugin requires using the Open GL ES 2.0 Graphics API");

    [StructureAR setupWirelessDebugging];

    [self setupGL];
    
    [self setupIMU];

    [self setupStructureSensor];
    
    [self setupColorCamera];
    
    _display.framebufferReady = true;
    
    // Everything was initialized previously, so we can skip further setup.
    if (_display.viewAlreadyAppeared)
        return;
    
    _display.viewAlreadyAppeared = true;
    
    [self setupGLViewport];
    
    // Try to connect to the sensor and start streaming depth data.
    [self connectToStructureSensorAndStartStreaming];
    
    // Make sure we get notified when the app becomes active to restart the sensor.
    [[NSNotificationCenter defaultCenter] addObserver:self
												selector:@selector(appDidBecomeActive)
												name:UIApplicationDidBecomeActiveNotification
												object:nil];
}

- (void)appDidBecomeActive
{
    // The sensor will stop on appWillResignActive, so we need to restart it.
    [self connectToStructureSensorAndStartStreaming];
    
    // Resume from the beginning.
    [self resetSLAM];
}

- (ScannerState) getScannerState
{
    return _slamState.scannerState;
}

#pragma mark -  Structure Sensor Management

-(void) setupStructureSensor
{
    // Get the sensor controller singleton and set ourself as the delegate.
    _stSensorController = [STSensorController sharedController];
    _stSensorController.delegate = self;
}

-(void) sensorDidDisconnect
{
    NSLog(@"Structure SDK: Structure did disconnect");

    ((StructureUnityAppController*)GetAppController()).trackerWillCallRepaint = NO;

    ucb.notifySensorDisconnected();
}

-(void) sensorDidConnect
{
    NSLog(@"Structure SDK: Structure did connect!");

    [self connectToStructureSensorAndStartStreaming];
}

- (void) sensorBatteryNeedsCharging
{
    NSLog(@"Structure SDK: Structure needs charging");

    ucb.notifySensorNeedsCharging();
}

-(void) sensorDidLeaveLowPowerMode
{
    ucb.notifySensorDisconnected();
}

-(void) setupGL
{
    // Create an EAGLContext for our EAGLView.
    _display.context = [EAGLContext currentContext];
    if (!_display.context) { NSLog(@"Failed to create ES context"); }
    
    _display.yCbCrTextureShader = [[STGLTextureShaderYCbCr alloc] init];
    
    // Set up texture and textureCache for images output by the color camera.
    CVReturn texError = CVOpenGLESTextureCacheCreate(kCFAllocatorDefault, NULL, _display.context, NULL, &_display.videoTextureCache);
    if (texError) { NSLog(@"Error at CVOpenGLESTextureCacheCreate %d", texError); }
}

-(void) setupGLViewport
{
    //we also adjust the viewport per-frame for passthrough rendering
    
    int framebufferWidth, framebufferHeight;
    glGetRenderbufferParameteriv(GL_RENDERBUFFER, GL_RENDERBUFFER_WIDTH, &framebufferWidth);
    glGetRenderbufferParameteriv(GL_RENDERBUFFER, GL_RENDERBUFFER_HEIGHT, &framebufferHeight);

    _display.viewport[0] = 0;
    _display.viewport[1] = 0;
    _display.viewport[2] = framebufferWidth;
    _display.viewport[3] = framebufferHeight;
}

-(void) uploadGLColorTexture:(CMSampleBufferRef)sampleBuffer
{
    if (!_display.videoTextureCache)
    {
        NSLog(@"Cannot upload color texture: No texture cache is present.");
        return;
    }
    
    // Clear the previous color texture.
    if (_display.lumaTexture)
    {
        CFRelease (_display.lumaTexture);
        _display.lumaTexture = NULL;
    }
    
    // Clear the previous color texture
    if (_display.chromaTexture)
    {
        CFRelease (_display.chromaTexture);
        _display.chromaTexture = NULL;
    }
    
    CVReturn err;
    
    // Allow the texture cache to do internal cleanup.
    CVOpenGLESTextureCacheFlush(_display.videoTextureCache, 0);
    
    CVImageBufferRef pixelBuffer = CMSampleBufferGetImageBuffer(sampleBuffer);
    size_t width = CVPixelBufferGetWidth(pixelBuffer);
    size_t height = CVPixelBufferGetHeight(pixelBuffer);
    
    OSType pixelFormat = CVPixelBufferGetPixelFormatType (pixelBuffer);
    NSAssert(pixelFormat == kCVPixelFormatType_420YpCbCr8BiPlanarFullRange, @"YCbCr is expected!");
    
    // Activate the default texture unit.
    glActiveTexture (GL_TEXTURE0);
    
    // Create an new RGBA texture from the video texture cache.
    err = CVOpenGLESTextureCacheCreateTextureFromImage(kCFAllocatorDefault, _display.videoTextureCache,
                                                       pixelBuffer,
                                                       NULL,
                                                       GL_TEXTURE_2D,
                                                       GL_RED_EXT,
                                                       (int)width,
                                                       (int)height,
                                                       GL_RED_EXT,
                                                       GL_UNSIGNED_BYTE,
                                                       0,
                                                       &_display.lumaTexture);
    if (err)
    {
        NSLog(@"Error with CVOpenGLESTextureCacheCreateTextureFromImage: %d", err);
        return;
    }
    
    // Set good rendering properties for the new texture.
    glBindTexture(CVOpenGLESTextureGetTarget(_display.lumaTexture), CVOpenGLESTextureGetName(_display.lumaTexture));
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    
    
    // Activate the default texture unit.
    glActiveTexture (GL_TEXTURE1);
    // Create an new CbCr texture from the video texture cache.
    err = CVOpenGLESTextureCacheCreateTextureFromImage(kCFAllocatorDefault,
                                                       _display.videoTextureCache,
                                                       pixelBuffer,
                                                       NULL,
                                                       GL_TEXTURE_2D,
                                                       GL_RG_EXT,
                                                       (int)width/2,
                                                       (int)height/2,
                                                       GL_RG_EXT,
                                                       GL_UNSIGNED_BYTE,
                                                       1,
                                                       &_display.chromaTexture);
    
    if (err)
    {
        NSLog(@"Error with CVOpenGLESTextureCacheCreateTextureFromImage: %d", err);
        return;
    }
    
    // Set good rendering properties for the new texture.
    glBindTexture(CVOpenGLESTextureGetTarget(_display.chromaTexture), CVOpenGLESTextureGetName(_display.chromaTexture));
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
}

-(STSensorControllerInitStatus) connectToStructureSensorAndStartStreaming
{
    // Try connecting to a Structure Sensor.
    STSensorControllerInitStatus result = [_stSensorController initializeSensorConnection];
    
    if (result == STSensorControllerInitStatusSuccess || result == STSensorControllerInitStatusAlreadyInitialized)
    {
        // Start streaming depth data.
        [self startStructureStreaming];
    }
    else
    {
        switch (result)
        {
            case STSensorControllerInitStatusSensorNotFound:
                NSLog(@"[Structure] no sensor found"); break;
            case STSensorControllerInitStatusOpenFailed:
                NSLog(@"[Structure] error: open failed."); break;
            case STSensorControllerInitStatusSensorIsWakingUp:
                NSLog(@"[Structure] error: sensor still waking up."); break;
            case STSensorControllerInitStatusAlreadyInitialized:
                NSLog(@"[Structure] error: already initialized."); break;
            default: {}
        }
    }
    
    return result;
}

-(void) startStructureStreaming
{
    if (![self isStructureConnectedAndCharged])
        return;
    
    // Tell the sensor to start streaming.
    
    // We are also using the color camera, so make sure the depth gets synchronized with it using STFrameSyncDepthAndRgb.
    NSError* error;
    BOOL optionsAreValid = [_stSensorController
                            startStreamingWithOptions:@{kSTStreamConfigKey: @(_options.structureStreamConfig),
                                                        kSTFrameSyncConfigKey: @(STFrameSyncDepthAndRgb),
                                                        kSTColorCameraFixedLensPositionKey: @(_options.lensPosition)}
                            error:&error];
    if (!optionsAreValid)
    {
        NSLog(@"Streaming options are not valid: %@", [error localizedDescription]);
        return;
    }
    
    NSLog(@"[Structure] streaming started");
    
    // Notify and initialize streaming dependent objects.
    [self onStructureSensorStartedStreaming];
}

-(BOOL) isStructureConnectedAndCharged
{
    return [_stSensorController isConnected] && ![_stSensorController isLowPower];
}

-(void) onStructureSensorStartedStreaming
{
    // We cannot initialize SLAM objects before a sensor is ready to be used, so is a good time
    // to do it.
    
    // Retrieve some sensor info for our current streaming configuration.
    [self clearSLAM];
    
    if (!_slamState.initialized)
        [self setupSLAM];
    
    ucb.notifySensorConnected();
}

-(void) sensorDidStopStreaming:(STSensorControllerDidStopStreamingReason)reason
{
    ((StructureUnityAppController*)GetAppController()).trackerWillCallRepaint = NO;
}

-(void) sensorDidOutputSynchronizedDepthFrame:(STDepthFrame*)depthFrame colorFrame:(STColorFrame *)colorFrame
{
    if (!_display.framebufferReady)
        return;
    
    if (_slamState.initialized)
        [self processDepthFrame:depthFrame colorFrame:colorFrame];
    
    TriggerUnityRendering();
}

-(void) updateMeshAlphaForPoseAccuracy:(STTrackerPoseAccuracy)poseAccuracy
{
    switch (poseAccuracy)
    {
        case STTrackerPoseAccuracyHigh:
        case STTrackerPoseAccuracyApproximate:
            _display.meshRenderingAlpha = 0.8;
            break;
            
        case STTrackerPoseAccuracyLow:
        {
            _display.meshRenderingAlpha = 0.4;
            break;
        }
            
        case STTrackerPoseAccuracyVeryLow:
        case STTrackerPoseAccuracyNotAvailable:
        {
            _display.meshRenderingAlpha = 0.1;
            break;
        }
            
        default:
            NSLog(@"STTracker unknown pose accuracy.");
    };
}

-(void) processDepthFrame:(STDepthFrame*)depthFrame colorFrame:(STColorFrame*)colorFrame
{
    // Upload the new color image for next rendering.
    if (colorFrame != nil)
        [self uploadGLColorTexture:colorFrame.sampleBuffer];
    
    // Compute a processed depth frame from the raw data.
    // Both shift and float values in meters will then be available.
    {
        float* depthFloat = (float*)depthFrame.depthInMillimeters;
        int width = depthFrame.width;
        int height = depthFrame.height;

        ucb.setDepthBuffer(depthFloat, width, height);
    }
    
    cameraGLProjection = colorFrame.glProjectionMatrix;
    
    // place camera in position suitable for cubeRenderer calls (below)
    [self updateCameraPoseUnity:cameraGLProjection];
    
    bool isInvertible = true;
    GLKMatrix4 coordConversion = GLKMatrix4Invert([self getConversionBetweenStructureSDKAndUnityCoordinateSystems], &isInvertible);
    
    if (!isInvertible) {
        NSLog(@"Coordinate transform isn't invertible!");
        exit(1);
    }
    
    switch (_slamState.scannerState)
    {
        case ScannerStateCubePlacement:
        {
            // Provide the new depth frame to the cube renderer for ROI highlighting.
            [_display.cubeRenderer setDepthFrame:[depthFrame registeredToColorFrame:colorFrame]];
            
            // Estimate the new scanning volume position.
            NSError* error = nil;
            [_slamState.cameraPoseInitializer updateCameraPoseWithGravity:_lastGravity depthFrame:depthFrame error:&error];
            if (error)
                NSLog(@"Unable to update camera pose: %@", [error localizedDescription]);
            
            
            GLKMatrix4 structureSDKCameraPose = [_slamState.cameraPoseInitializer cameraPose];
            
            GLKMatrix4 unityCameraPose = GLKMatrix4Multiply(coordConversion, structureSDKCameraPose);

            [self updateCameraPoseUnity:unityCameraPose];
            
            // Tell the cube renderer whether there is a support plane or not.
            [_display.cubeRenderer setCubeHasSupportPlane:_slamState.cameraPoseInitializer.hasSupportPlane];
            
            break;
        }
            
        case ScannerStateScanning:
        case ScannerStatePlaying:
        {
            NSError* trackingError = nil;
            
            // First try to estimate the 3D pose of the new frame.
            [_slamState.tracker updateCameraPoseWithDepthFrame:depthFrame colorFrame:colorFrame error:&trackingError];
            
            bool okTrackingStatus = false;
            
            // Integrate it into the current mesh estimate if tracking was very successful.
            if (trackingError)
            {
                NSLog(@"[Structure] STTracker Error: %@.", [trackingError localizedDescription]);
            }
            else //tracking okay
            {
                // Update the tracking message.
                NSString* trackingMessage = computeTrackerMessage(_slamState.tracker.trackerHints);
                if (trackingMessage) NSLog(@"[Structure] Tracking Message: %@", trackingMessage);
            
                // Set the mesh transparency depending on the current accuracy.
                [self updateMeshAlphaForPoseAccuracy:_slamState.tracker.poseAccuracy];
                
                // not integrating into the mesh during the game.
                if(_slamState.scannerState == ScannerStateScanning &&
                    _slamState.tracker.poseAccuracy >= STTrackerPoseAccuracyLow)
                    [_slamState.mapper integrateDepthFrame:depthFrame cameraPose:[_slamState.tracker lastFrameCameraPose]];
                
                okTrackingStatus = _slamState.tracker.poseAccuracy >= STTrackerPoseAccuracyLow &&
                    !_slamState.tracker.trackerHints.modelOutOfView &&
                    !_slamState.tracker.trackerHints.trackerIsLost;
                
                // we don't do anything with color keyframes in this sample, otherwise we'd add them to a keyframe manager here.
                
                GLKMatrix4 structureSDKCameraPose = [_slamState.tracker lastFrameCameraPose];
                
                GLKMatrix4 unityCameraPose = GLKMatrix4Multiply(coordConversion, structureSDKCameraPose);
                
                [self updateCameraPoseUnity:unityCameraPose];
            }
            
            ucb.notifyTrackingStatus(okTrackingStatus);
            
            break;
        }
            
        default:
        { } //do nothing
    }
    

}

-(void) renderCameraImageToTexture
{
    if (!_display.lumaTexture || !_display.chromaTexture || !_display.unityCameraTextureFramebuffer)
        return;
    //  Draw the color image at the beginning of the draw call
    
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(CVOpenGLESTextureGetTarget(_display.lumaTexture),
                  CVOpenGLESTextureGetName(_display.lumaTexture));
    
    glActiveTexture(GL_TEXTURE1);
    glBindTexture(CVOpenGLESTextureGetTarget(_display.chromaTexture),
                  CVOpenGLESTextureGetName(_display.chromaTexture));
    
    // IMPORTANT !!!
    // Unity still keeps the buffer around, we need to unbind it before custom render, without it Unity will crash
    // because glVertexAttribPointer will change the offset in GLBuffer
    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
    
    GLint oldFBO;
    glGetIntegerv (GL_FRAMEBUFFER_BINDING, &oldFBO);
    glBindFramebuffer(GL_FRAMEBUFFER, _display.unityCameraTextureFramebuffer);
    
    int width = 640;
    int height = 480;
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone)
    {
        float fraction = 0.75f;
        glViewport (width*(1-fraction)/2.0f,0,width*fraction,height);
    }
    else
    {
        glViewport (0,0,width,height);
    }
    
    glClearColor(0.0, 0.0, 0.0, 1.0);
    glClear(GL_COLOR_BUFFER_BIT);
    
    glDisable(GL_BLEND);
    [_display.yCbCrTextureShader useShaderProgram];
    [_display.yCbCrTextureShader renderWithLumaTexture:GL_TEXTURE0 chromaTexture:GL_TEXTURE1];
    
    glBindFramebuffer(GL_FRAMEBUFFER, oldFBO);
    glViewport (_display.viewport[0], _display.viewport[1], _display.viewport[2], _display.viewport[3]);
}

-(void) preRenderScene
{
    [self renderCameraImageToTexture];
}

// OnPostRenderFromUnity
-(void) renderScene
{
    //  Depth stream is needed for the StructureAR render call here
    if(![self isStructureConnectedAndCharged])
        return;
    
    // IMPORTANT !!!
    // Unity still keeps the buffer around, we need to unbind it before custom render, without it Unity will crash
    // becasue glVertexAttribPointer will change the offset in GLBuffer
    
    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
    
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone)
    {
        float fraction = 0.75f;
        glViewport (_display.viewport[2]*(1-fraction)/2.0f,
                    0,
                    _display.viewport[2]*fraction,
                    _display.viewport[3] );
    }
    else
    {
        glViewport (0, 0, _display.viewport[2], _display.viewport[3]);
    }
    
    glColorMask(true, true, true, true);
    
    switch (_slamState.scannerState)
    {
        case ScannerStateCubePlacement:
        {
            // This section renders the red-yellow depth pixels over the scene.
            
            // Highlighted depth values inside the current volume area.
            [_display.cubeRenderer renderHighlightedDepthWithCameraPose:_slamState.cameraPoseInitializer.cameraPose alpha:0.5];
            
            // Render the wireframe cube corresponding to the current scanning volume.
            [_display.cubeRenderer renderCubeOutlineWithCameraPose:_slamState.cameraPoseInitializer.cameraPose
                                                  depthTestEnabled:false
                                              occlusionTestEnabled:true];
            break;
        }
            
        case ScannerStateScanning:
        {
            // This section renders the grey polygons from the scanning of the mesh.
            
            // Forcing the GL state to be favorable for mesh rendering. Particle effects
            // Were previously randomly altering these states, so this is defensive.
            glDepthMask(GL_TRUE);
            
            // Make sure we restore the default depth function.
            glDepthFunc(GL_LESS);
            
            // Render the current mesh reconstruction using the last estimated camera pose.
            GLKMatrix4 cameraPose = [_slamState.tracker lastFrameCameraPose];
            
            // we should not be doing this here since Unity is having issues drawing the mesh
            // along with it's own objects in the scene.
            [_slamState.scene renderMeshFromViewpoint:cameraPose
                                   cameraGLProjection:cameraGLProjection
                                                alpha:_display.meshRenderingAlpha
                             highlightOutOfRangeDepth:false
                                            wireframe:false];
            
            // Render the wireframe cube corresponding to the scanning volume.
            // Here we don't enable occlusions to avoid performance hit.
            [_display.cubeRenderer renderCubeOutlineWithCameraPose:cameraPose
                                                  depthTestEnabled:false
                                              occlusionTestEnabled:false];
            break;
        }
            
        case ScannerStatePlaying:
        default: {}
    };
    
    glViewport (_display.viewport[0], _display.viewport[1], _display.viewport[2], _display.viewport[3]);
}

- (void) handlePinchScale:(float)scale
{
    _volumeScale.currentScale = scale;
    [self adjustVolumeSize:GLKVector3Make(_volumeScale.currentScale * _options.initialVolumeSize.v[0],
                                          _volumeScale.currentScale * _options.initialVolumeSize.v[1],
                                          _volumeScale.currentScale * _options.initialVolumeSize.v[2])];
}


- (CGSize)getFramebufferSize
{
    int framebufferWidth, framebufferHeight;
    glGetRenderbufferParameteriv(GL_RENDERBUFFER, GL_RENDERBUFFER_WIDTH, &framebufferWidth);
    glGetRenderbufferParameteriv(GL_RENDERBUFFER, GL_RENDERBUFFER_HEIGHT, &framebufferHeight);
    
    NSLog(@"framebuffer size %d %d", framebufferWidth, framebufferHeight);
    return CGSizeMake(framebufferWidth, framebufferHeight);
}


#pragma mark - SLAM

- (void)clearSLAM
{
    _slamState.initialized = false;
    _slamState.scene = nil;
    _slamState.tracker = nil;
    _slamState.mapper = nil;

    ((StructureUnityAppController*)GetAppController()).trackerWillCallRepaint = NO;
}

// Setup SLAM related objects.
- (void)setupSLAM
{
    if (_slamState.initialized)
        return;
        
    // Initialize the scene.
    // This creates the mesh rendering scene that is used to draw
    // the mesh on top of the unity scene.
    _slamState.scene = [[STScene alloc] initWithContext:_display.context];
    
    // Initialize the camera pose tracker.
    NSDictionary* trackerOptions = @{
                                     kSTTrackerTypeKey: @(STTrackerDepthAndColorBased),
                                     kSTTrackerTrackAgainstModelKey: @TRUE, // tracking against the model is much better for close range scanning.
                                     kSTTrackerQualityKey: @(STTrackerQualityAccurate),
                                     kSTTrackerBackgroundProcessingEnabledKey: @YES,
                                     };
    
    // Initialize the camera pose tracker.
    _slamState.tracker = [[STTracker alloc] initWithScene:_slamState.scene options:trackerOptions];

    // Default volume size set in options struct
    if( isnan(_slamState.volumeSizeInMeters.v[0]) )
        _slamState.volumeSizeInMeters = _options.initialVolumeSize;
    
    // The mapper will be initialized when we start scanning.
    
    // Setup the cube placement initializer.
    _slamState.cameraPoseInitializer = [[STCameraPoseInitializer alloc]
                                        initWithVolumeSizeInMeters:_slamState.volumeSizeInMeters
                                        options:@{kSTCameraPoseInitializerStrategyKey: @(STCameraPoseInitializerStrategyTableTopCube)}];
    
    
    // Setup the cube renderer with the current volume size.
    _display.cubeRenderer = [[STCubeRenderer alloc] initWithContext:_display.context];
    
    // Setup the initial volume size.
    [self adjustVolumeSize:_slamState.volumeSizeInMeters];
    
    // Start with cube placement mode
    [self enterCubePlacementState];
    
    _slamState.initialized = true;
}

- (void)setupMapper
{
    if (_slamState.mapper)
        _slamState.mapper = nil; // make sure we first remove a previous mapper.
    
    const float voxelSizeInMeters = 0.0125; // mm per voxel
    
    // Compute the volume bounds in cells, as a multiple of the volume resolution.
    GLKVector3 volumeBounds = {
        roundf(_slamState.volumeSizeInMeters.v[0] / voxelSizeInMeters),
        roundf(_slamState.volumeSizeInMeters.v[1] / voxelSizeInMeters),
        roundf(_slamState.volumeSizeInMeters.v[2] / voxelSizeInMeters)
    };
    
    NSLog(@"[Mapper] volumeBounds: %f %f %f (resolution=%f)", volumeBounds.v[0], volumeBounds.v[1], volumeBounds.v[2], voxelSizeInMeters );

    NSDictionary* mapperOptions =
    @{
      kSTMapperVolumeResolutionKey: @(voxelSizeInMeters),
      kSTMapperVolumeBoundsKey: @[@(volumeBounds.v[0]), @(volumeBounds.v[1]), @(volumeBounds.v[2])],
      kSTMapperVolumeHasSupportPlaneKey: @(_slamState.cameraPoseInitializer.hasSupportPlane),
      kSTMapperEnableLiveWireFrameKey: @YES,
    };
    
    _slamState.mapper = [[STMapper alloc] initWithScene:_slamState.scene options:mapperOptions];
}

inline float keepInRange (float value, float minValue, float maxValue)
{
    if (value > maxValue) return maxValue;
    if (value < minValue) return minValue;
    return value;
}

- (void)adjustVolumeSize:(GLKVector3)volumeSize
{
    // Make sure that each dimension of the volume size remains between a tenth and a five times its initial value.
    volumeSize.v[0] = keepInRange (volumeSize.v[0], 0.1 * _options.initialVolumeSize.v[0], 5.f * _options.initialVolumeSize.v[0]);
    volumeSize.v[1] = keepInRange (volumeSize.v[1], 0.1 * _options.initialVolumeSize.v[1], 5.f * _options.initialVolumeSize.v[1]);
    volumeSize.v[2] = keepInRange (volumeSize.v[2], 0.1 * _options.initialVolumeSize.v[2], 5.f * _options.initialVolumeSize.v[2]);
    
    _slamState.volumeSizeInMeters = volumeSize;
    
    _slamState.cameraPoseInitializer.volumeSizeInMeters = volumeSize;
    [_display.cubeRenderer adjustCubeSize:_slamState.volumeSizeInMeters];
}

- (void)resetSLAM
{
    [_slamState.mapper reset];
    [_slamState.tracker reset];
    [_slamState.scene clear];
    
    [self enterCubePlacementState];
}

- (void)enterCubePlacementState
{
    [self setColorCameraParametersForInit];
    _slamState.scannerState = ScannerStateCubePlacement;
    ((StructureUnityAppController*)GetAppController()).trackerWillCallRepaint = YES;
}

- (void)enterScanningState
{
    // Prepare the mapper for the new scan.
    [self setupMapper];

    [self setColorCameraParametersForScanning];
    
    _slamState.tracker.initialCameraPose = _slamState.cameraPoseInitializer.cameraPose;
    
    _slamState.scannerState = ScannerStateScanning;
}

- (GLKMatrix4)getConversionBetweenStructureSDKAndUnityCoordinateSystems {
    
    // The Structure SDK's coodinate system is at the front-upper-left corner of the TSDF volume.
    // This changes based on volume size and whether or not there is a supporting plane.
    
    // Unity's coordinate system is centered in the middle of the volume on the floor. It's an easy conversion
    // since there's no rotation.
    const GLKVector3 volumeSize = _slamState.cameraPoseInitializer.volumeSizeInMeters;
    return GLKMatrix4MakeTranslation(volumeSize.v[0]/2.0f, volumeSize.v[1], volumeSize.v[2]/2.0f);
}

- (void) setCameraTexture:(int)texID
{
    _display.unityCameraTexture = texID;
    if(_display.unityCameraTextureFramebuffer == 0)
        glGenFramebuffers(1, &_display.unityCameraTextureFramebuffer);

    GLint oldFBO;
    glGetIntegerv (GL_FRAMEBUFFER_BINDING, &oldFBO);
    
    glBindFramebuffer(GL_FRAMEBUFFER, _display.unityCameraTextureFramebuffer);
    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _display.unityCameraTexture, 0);
    
    glBindFramebuffer(GL_FRAMEBUFFER, oldFBO);
}

- (void) setMeshObj:(MeshObj*)mesh
{
    NSLog(@"setting mesh object");
    mesh->vertices = [_decimatedMesh meshVertices:0];
    mesh->normals = [_decimatedMesh meshPerVertexNormals:0];
    mesh->faces = [_decimatedMesh meshFaces:0];
    mesh->numberOfMeshVertices = [_decimatedMesh numberOfMeshVertices:0];
    mesh->numberOfMeshFaces = [_decimatedMesh numberOfMeshFaces:0];
    
    // The mesh will have an offset that will convert it to use the Unity coordinate system.
    GLKMatrix4 coordSystemConversion = [self getConversionBetweenStructureSDKAndUnityCoordinateSystems];
    
    // Extracting the translation from the conversion. GLKMatrix4 is column major which is why this indexing is the way it is.
    mesh->meshOffset = GLKVector3Make(coordSystemConversion.m[12], coordSystemConversion.m[13], coordSystemConversion.m[14]);
}

- (void)enterPlayingState
{
    NSDictionary* trackerOptions = @{kSTTrackerQualityKey: @(STTrackerQualityFast)};
    [_slamState.tracker setOptions:trackerOptions];
    _slamState.scannerState = ScannerStatePlaying;
    
     if(_decimatedMesh)
         _decimatedMesh = nil;
    
    // This locks the mesh being rendered ontop of the Unity scene and prepares
    // to copy it to the Unity scene once it's finished.
    STMesh* sceneMesh = [_slamState.scene lockAndGetSceneMesh];
    STMesh* sceneMeshCopy = [[STMesh alloc] initWithMesh:sceneMesh];
    [_slamState.scene unlockSceneMesh];

    STBackgroundTask* decimationTask = [STMesh newDecimateTaskWithMesh:sceneMeshCopy
                                                       numFaces:2000
                                              completionHandler:^(STMesh *result, NSError *error)
    {
        // This is the mesh to send to Unity
        _decimatedMesh = result;

        // Done decimation, notify the unity to grab the mesh
        // Once the mesh is finished decimation Unity is notified and
        // Unity begins to copy the mesh into the unity scene
        // we should try to do this as it's being scanned not
        // after it's been scanned.
        dispatch_async(dispatch_get_main_queue(), ^(void) {
            ucb.notifyMeshReady();
        });
    }];

    [decimationTask start];
}

- (void) updateCameraPoseUnity:(GLKMatrix4)m
{
    if(isnan(m.m[12]))
    {
        return;
    }

    // Converting from Structure SDK axis convention to Unity axis convention
    GLKMatrix4 flipY = GLKMatrix4MakeScale(1, -1, 1);
    m = GLKMatrix4Multiply(flipY, m);
    m = GLKMatrix4Multiply(m, flipY);
    
    // setCameraPositionCallback
    {
        ucb.setCameraPosition(m.m[12], m.m[13], m.m[14]);
    }
    
    // setCameraRotationCallback
    {
        GLKQuaternion q = GLKQuaternionMakeWithMatrix4(m);

        ucb.setCameraRotation(q.q[0], q.q[1], q.q[2], q.q[3]);
    }
    
    // setCameraScaleCallback
    {
        float x = sqrt(m.m[0] * m.m[0] + m.m[4] * m.m[4] + m.m[8] * m.m[8]);
        float y = sqrt(m.m[1] * m.m[1] + m.m[5] * m.m[5] + m.m[9] * m.m[9]);
        float z = sqrt(m.m[2] * m.m[2] + m.m[6] * m.m[6] + m.m[10] * m.m[10]);

        ucb.setCameraScale(x, y, z);
    }
}

- (void) updateProjectionMatrixCamera:(GLKMatrix4)projectionMatrix
{
    GLKMatrix4 flipYZ = GLKMatrix4MakeScale(1, -1, -1);
    projectionMatrix = GLKMatrix4Multiply(projectionMatrix, flipYZ);

    ucb.setCameraProjectionMatrix(projectionMatrix.m);
}

#pragma mark -  Color Camera

- (bool)queryCameraAuthorizationStatusAndNotifyUserIfNotGranted
{
    const NSUInteger numCameras = [[AVCaptureDevice devicesWithMediaType:AVMediaTypeVideo] count];
    
    if (0 == numCameras)
    {
        NSLog(@"Camera usage is restricted on this device! Change it in Settings -> General -> Restrictions.");
        dispatch_async(dispatch_get_main_queue(), ^(void) {
            ucb.notifyCameraAccessRequired();
            
        });
        return false; // This can happen even on devices that include a camera, when camera access is restricted globally.
    }

    // This API was introduced in iOS 7, but in iOS 8 it's actually enforced.
    if ([AVCaptureDevice respondsToSelector:@selector(authorizationStatusForMediaType:)])
    {
        AVAuthorizationStatus authStatus = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
        
        if (authStatus != AVAuthorizationStatusAuthorized)
        {
            NSLog(@"Not authorized to use the camera!");
            
            [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo
                                     completionHandler:^(BOOL granted)
             {
                 // This block fires on a separate thread, so we need to ensure any actions here
                 // are sent to the right place.
                 
                 // If the request is granted, let's try again to start an AVFoundation session. Otherwise, alert
                 // the user that things won't go well.
                 if (granted)
                 {
                     dispatch_async(dispatch_get_main_queue(), ^(void) {
                         [self setupColorCamera];
                     });
                 }
                 else
                 {
                     dispatch_async(dispatch_get_main_queue(), ^(void) {
                         ucb.notifyCameraAccessRequired();
                     });
                 }
             }];
            return false;
        }
        
    }
    return true;
}

- (void)setupColorCamera
{
    bool cameraAccessAuthorized = [self queryCameraAuthorizationStatusAndNotifyUserIfNotGranted];
    
    if (!cameraAccessAuthorized)
        return;
    
    // Use VGA color.
    NSString *sessionPreset = AVCaptureSessionPreset640x480;
    
    // Set up Capture Session.
    _avCaptureSession = [[AVCaptureSession alloc] init];
    [_avCaptureSession beginConfiguration];
    
    // Set preset session size.
    [_avCaptureSession setSessionPreset:sessionPreset];
    
    // Create a video device and input from that Device.  Add the input to the capture session.
    _videoDevice = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    if (_videoDevice == nil)
        assert(0);
    
    // Configure Focus, Exposure, and White Balance
    NSError *error;
    
    if([_videoDevice lockForConfiguration:&error])
    {
        // Allow exposure to initially change
        if ([_videoDevice isExposureModeSupported:AVCaptureExposureModeContinuousAutoExposure])
            [_videoDevice setExposureMode:AVCaptureExposureModeContinuousAutoExposure];
        
        // Allow white balance to initially change
        if ([_videoDevice isWhiteBalanceModeSupported:AVCaptureWhiteBalanceModeContinuousAutoWhiteBalance])
            [_videoDevice setWhiteBalanceMode:AVCaptureWhiteBalanceModeContinuousAutoWhiteBalance];
        
        [_videoDevice setFocusModeLockedWithLensPosition:_options.lensPosition completionHandler:nil];

        [_videoDevice unlockForConfiguration];
    }
    
    
    
    //  Add the device to the session.
    AVCaptureDeviceInput *input = [AVCaptureDeviceInput deviceInputWithDevice:_videoDevice error:&error];
    if (error)
    {
        NSLog(@"Cannot initialize AVCaptureDeviceInput");
        assert(0);
    }
    
    [_avCaptureSession addInput:input];
    // After this point, captureSession captureOptions are filled.
    
    // Create the output for the capture session.
    AVCaptureVideoDataOutput* dataOutput = [[AVCaptureVideoDataOutput alloc] init];
    
    // We don't want to process late frames.
    [dataOutput setAlwaysDiscardsLateVideoFrames:YES];
    
    // Use YCbCr pixel format.
    [dataOutput setVideoSettings:[NSDictionary
                                  dictionaryWithObject:[NSNumber numberWithInt:kCVPixelFormatType_420YpCbCr8BiPlanarFullRange]
                                  forKey:(id)kCVPixelBufferPixelFormatTypeKey]];
    
    // Set dispatch to be on the main thread so OpenGL can do things with the data
    [dataOutput setSampleBufferDelegate:self queue:dispatch_get_main_queue()];
    
    [_avCaptureSession addOutput:dataOutput];
    // Force the framerate to 30 FPS, to be in sync with Structure Sensor.
    
    if ([[[UIDevice currentDevice] systemVersion] compare:@"7.0" options:NSNumericSearch] != NSOrderedAscending)
    {
        // iOS 7
        if([_videoDevice lockForConfiguration:&error])
        {
            [_videoDevice setActiveVideoMaxFrameDuration:CMTimeMake(1, 30)];
            [_videoDevice setActiveVideoMinFrameDuration:CMTimeMake(1, 30)];
            [_videoDevice unlockForConfiguration];
        }
    }
    else
    {
        NSLog(@"iOS 7 is required.  Camera not properly configured.");
    }
    
    // Read in Apple Intrinsics, if required
    AVCaptureConnection *conn = [dataOutput connectionWithMediaType:AVMediaTypeVideo];
    conn.preferredVideoStabilizationMode = AVCaptureVideoStabilizationModeOff;
    #if __IPHONE_OS_VERSION_MAX_ALLOWED >= 110000
        if (@available(iOS 11_0, *))
        {
            if (conn.isCameraIntrinsicMatrixDeliverySupported)
                conn.cameraIntrinsicMatrixDeliveryEnabled = YES;
        }
    #endif

    
    [_avCaptureSession commitConfiguration];
    
    // Start streaming color images.
    [_avCaptureSession startRunning];
}

- (void)setColorCameraParametersForInit
{
    NSError *error;
    
    [_videoDevice lockForConfiguration:&error];
    
    // Auto-exposure
    if ([_videoDevice isExposureModeSupported:AVCaptureExposureModeContinuousAutoExposure])
        [_videoDevice setExposureMode:AVCaptureExposureModeContinuousAutoExposure];
    
    // Auto-white balance.
    if ([_videoDevice isWhiteBalanceModeSupported:AVCaptureWhiteBalanceModeContinuousAutoWhiteBalance])
        [_videoDevice setWhiteBalanceMode:AVCaptureWhiteBalanceModeContinuousAutoWhiteBalance];
    
    [_videoDevice unlockForConfiguration];
    
}

- (void)setColorCameraParametersForScanning
{
    // If using color tracking, we need to lock exposure. We can leave the auto-exposure mode on
    // if only using depth though.
    if (_options.useColorTracking)
    {
        NSError *error;
        
        [_videoDevice lockForConfiguration:&error];
        
        // Exposure locked to its current value.
        if ([_videoDevice isExposureModeSupported:AVCaptureExposureModeLocked])
            [_videoDevice setExposureMode:AVCaptureExposureModeLocked];
        
        // White balance locked to its current value.
        if ([_videoDevice isWhiteBalanceModeSupported:AVCaptureWhiteBalanceModeLocked])
            [_videoDevice setWhiteBalanceMode:AVCaptureWhiteBalanceModeLocked];
        
        [_videoDevice unlockForConfiguration];
    }
}

- (void)captureOutput:(AVCaptureOutput *)captureOutput
didOutputSampleBuffer:(CMSampleBufferRef)sampleBuffer
       fromConnection:(AVCaptureConnection *)connection
{
    if(![self isStructureConnectedAndCharged])
    {
        [self uploadGLColorTexture:sampleBuffer];
    }
    
    // Pass color buffers directly to the driver, which will then produce synchronized depth/color pairs.
    [_stSensorController frameSyncNewColorBuffer:sampleBuffer];
}

#pragma mark - IMU

- (void)setupIMU
{
    _lastGravity = GLKVector3Make (0,0,0);
    
    // 60 FPS is responsive enough for motion events.
    const float fps = 60.0;
    _motionManager = [[CMMotionManager alloc] init];
    _motionManager.accelerometerUpdateInterval = 1.0/fps;
    _motionManager.gyroUpdateInterval = 1.0/fps;
    
    // Limiting the concurrent ops to 1 is a simple way to force serial execution
    _imuQueue = [[NSOperationQueue alloc] init];
    [_imuQueue setMaxConcurrentOperationCount:1];
    
    CMDeviceMotionHandler dmHandler = ^(CMDeviceMotion *motion, NSError *error)
    {
        [self processDeviceMotion:motion withError:error];
    };
    
    [_motionManager startDeviceMotionUpdatesToQueue:_imuQueue withHandler:dmHandler];
}

- (void) processDeviceMotion:(CMDeviceMotion*)motion withError:(NSError*)error
{
    if (_slamState.scannerState == ScannerStateCubePlacement)
    {
        // Update our gravity vector, it will be used by the cube placement initializer.
        _lastGravity = GLKVector3Make (motion.gravity.x, motion.gravity.y, motion.gravity.z);
    }
    
    // The tracker is more robust to fast moves if we feed it with motion data.
    [_slamState.tracker updateCameraPoseWithMotion:motion];
}

extern "C" {

    void _initStructureAR()
    {
        [[StructureAR sharedStructureAR] initStructureAR];
    }
    
    bool _isStructureConnected()
    {
        return [[StructureAR sharedStructureAR] isStructureConnectedAndCharged];
    }
    
    ScannerState _getScannerState()
    {
        return [[StructureAR sharedStructureAR] getScannerState];
    }
    
    void _startScanning()
    {
        [[StructureAR sharedStructureAR] enterScanningState];
    }
    
    void _doneScanning()
    {
        [[StructureAR sharedStructureAR] enterPlayingState];
    }
    
    void _resetScanning()
    {
        [[StructureAR sharedStructureAR] resetSLAM];
    }
    
    void _handlePinchScale (float scale)
    {
        [[StructureAR sharedStructureAR] handlePinchScale:scale];
    }
    
    void _UnityPreRenderEvent ()
    {
        [[StructureAR sharedStructureAR] preRenderScene];
    }
    
    void _UnityPostRenderEvent ()
    {
        [[StructureAR sharedStructureAR] renderScene];
    }
    
    void _getMeshObj (MeshObj* mesh)
    {
        [[StructureAR sharedStructureAR] setMeshObj:mesh];
    }
    
    void _setCameraTexture (int texID)
    {
        [[StructureAR sharedStructureAR] setCameraTexture:texID];
    }
    
}

@end

// we actually render from our plugin using this.
void TriggerUnityRendering ()
{
    [GetAppController() performSelector:@selector(repaintSTTrackerLink)];
}
