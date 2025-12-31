using System;

namespace Photon.Voice
{
    public static class Platform
    {
        static public IDeviceEnumerator CreateAudioInEnumerator(ILogger logger)
        {

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
            return new UWP.AudioInEnumerator(logger);
#elif PHOTON_VOICE_WINDOWS || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return new Windows.AudioInEnumerator(logger);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return new MacOS.AudioInEnumerator(logger);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebAudioInEnumerator(logger);
#elif UNITY_WEBGL
            return new DeviceEnumeratorSingleDevice(logger, "Default");
#else
            return new AudioInEnumeratorNotSupported(logger);
#endif
        }

        static public IAudioInChangeNotifier CreateAudioInChangeNotifier(Action callback, ILogger logger)
        {
#if ((UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR)
            return new IOS.AudioInChangeNotifier(callback, logger);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return new MacOS.AudioInChangeNotifier(callback, logger);
#elif UNITY_SWITCH && !UNITY_EDITOR
            return new Switch.AudioInChangeNotifier(callback, logger);
#else
            return new AudioInChangeNotifierNotSupported(callback, logger);
#endif
        }

        static public IEncoder CreateDefaultAudioEncoder<T>(ILogger logger, VoiceInfo info)
        {
            switch (info.Codec)
            {
                case Codec.AudioOpus:
                    return OpusCodec.Factory.CreateEncoder<T[]>(info, logger);
                case Codec.Raw: // Debug only. Assumes that original data is short[].
                    return new RawCodec.Encoder<T>();
                default:
                    throw new UnsupportedCodecException("Platform.CreateDefaultAudioEncoder", info.Codec);
            }
        }

        static public IAudioDesc CreateDefaultAudioSource(ILogger logger, DeviceInfo dev, int samplingRate, int channels, object otherParams = null)
        {
#if PHOTON_VOICE_WINDOWS || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return new Windows.WindowsAudioInPusher(dev.IsDefault ? -1 : dev.IDInt, logger);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebAudioMicIn(dev.IDString, samplingRate, channels, logger);
#elif (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
            if (otherParams == null)
            {
                return new IOS.AudioInPusher(IOS.AudioSessionParametersPresets.VoIP, logger);
            }
            else
            {
                return new IOS.AudioInPusher((IOS.AudioSessionParameters)otherParams, logger);
            }
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return new MacOS.AudioInPusher(dev.IsDefault ? -1 : dev.IDInt, logger);
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (otherParams == null)
            {
                return new Unity.AndroidAudioInAEC(logger, true, true, true);
            }
            else
            {
                var p = (Unity.AndroidAudioInParameters)otherParams;
                return new Unity.AndroidAudioInAEC(logger, p.EnableAEC, p.EnableAGC, p.EnableNS);
            }
#elif UNITY_WSA && !UNITY_EDITOR
            return new UWP.AudioInPusher(logger, samplingRate, channels, dev.IsDefault ? "" : dev.IDString);
#elif UNITY_SWITCH && !UNITY_EDITOR
            return new Switch.AudioInPusher(logger);
#elif UNITY_5_3_OR_NEWER && !UNITY_WEBGL // #if UNITY except WebGL which does not support microphone
            return new Unity.MicWrapper(dev.IDString, samplingRate, logger);
#else
            throw new UnsupportedPlatformException("Platform.CreateDefaultAudioSource");
#endif
        }

#if PHOTON_VOICE_VIDEO_ENABLE
        static public IDeviceEnumerator CreateVideoInEnumerator(ILogger logger)
        {
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
            return new UWP.VideoInEnumerator(logger);
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            return new Unity.VideoInEnumerator(logger);
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new Unity.AndroidVideoInEnumerator(logger);
#elif ((UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR)
            return new IOS.VideoInEnumerator(logger);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebVideoInEnumerator(logger);
#elif UNITY_WEBGL
            return new DeviceEnumeratorSingleDevice(logger, "Default");
#else
            return new VideoInEnumeratorNotSupported(logger);
#endif
        }

        static public IEncoderDirectImage CreateDefaultVideoEncoder(ILogger logger, VoiceInfo info)
        {
            switch (info.Codec)
            {
                case Codec.VideoVP8:
                case Codec.VideoVP9:
                    //return new FFmpegCodec.Encoder(logger, info);
                    return new VPxCodec.Encoder(logger, info);
#if PHOTON_VOICE_WINDOWS || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                case Codec.VideoH264:
                    //return new FFmpegCodec.Encoder(logger, info);
                    return new Windows.MFTCodec.VideoEncoder(logger, info);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                case Codec.VideoH264:
                    //return new FFmpegCodec.Encoder(logger, info);
                    return new MacOS.VideoEncoder(logger, info);
#endif
                default:
                    throw new UnsupportedCodecException("Platform.CreateDefaultVideoEncoder", info.Codec);
            }
        }

        static public IDecoderDirect<ImageBufferNative> CreateDefaultVideoDecoder(ILogger logger, VoiceInfo info)
        {
            switch (info.Codec)
            {
                case Codec.VideoVP8:
                case Codec.VideoVP9:
                    //return new FFmpegCodec.Decoder(logger);
                    return new VPxCodec.Decoder(logger);
#if PHOTON_VOICE_WINDOWS || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                case Codec.VideoH264:
                    //return new FFmpegCodec.Decoder(logger);
                    return new Windows.MFTCodec.VideoDecoder(logger, info);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                case Codec.VideoH264:
                    //return new FFmpegCodec.Decoder(logger);
                    return new MacOS.VideoDecoder(logger, info);
                    break;
#endif
                default:
                    throw new UnsupportedCodecException("Platform.CreateDefaultVideoDecoder", info.Codec);
            }
        }

        static public IVideoRecorder CreateDefaultVideoRecorder(ILogger logger, VoiceInfo info, DeviceInfo camDevice, Action<IVideoRecorder> onReady)
        {
            // native platform-specific recorders
#if UNITY_ANDROID && !UNITY_EDITOR
#if PHOTON_VOICE_VIDEO_ANDROID_JAVA_API
            return new Unity.AndroidVideoRecorderSurfaceView(logger, info, camDevice.IDString, onReady);
#else
            return new Unity.AndroidNDKVideoRecorderSurfaceView(logger, info, camDevice.IDString, onReady);
#endif
#elif (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
            if (info.Codec == Codec.VideoH264)
            {
                return new IOS.VideoRecorderLayer(logger, info, camDevice.IDString, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoRecorder", info.Codec);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            if (info.Codec == Codec.VideoH264)
            {
                return new UWP.VideoRecorderMediaPlayerElement(logger, info, camDevice.IDString, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoRecorder", info.Codec);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebCodecsCameraRecorderUnityTexture(logger, info, camDevice.IDString, onReady);
#else // multi-platform VideoRecorderUnity
#if UNITY_5_3_OR_NEWER // #if UNITY
            var ve = CreateDefaultVideoEncoder(logger, info);
            return new Unity.VideoRecorderUnity(ve, null, camDevice.IDString, info.Width, info.Height, info.FPS, onReady);
#endif

            throw new NotImplementedException("Platform.CreateDefaultVideoRecorder: default Video Recorder for the platform is not implemented.");
#endif
        }

        static public IVideoPlayer CreateDefaultVideoPlayer(ILogger logger, VoiceInfo info, Action<IVideoPlayer> onReady)
        {
            // native platform-specific players
#if UNITY_ANDROID && !UNITY_EDITOR
            var vda = new Unity.AndroidVideoDecoderSurfaceView(logger, info);
            return new VideoPlayer(vda, vda.Preview, info.Width, info.Height, onReady);
#elif (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
            if (info.Codec == Codec.VideoH264)
            {
                var vd = new IOS.VideoDecoderLayer(logger);
                return new VideoPlayer(vd, vd.PreviewLayer, info.Width, info.Height, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoPlayer", info.Codec);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            if (info.Codec == Codec.VideoH264)
            {
                var vd = new UWP.VideoDecoderMediaPlayerElement(logger, info);
                return new VideoPlayer(vd, vd.PreviewMediaPlayerElement, info.Width, info.Height, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoPlayer", info.Codec);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebCodecsVideoPlayerUnityTexture(logger, info, onReady);
#endif
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            var vdu = CreateDefaultVideoDecoder(logger, info);
            var vp = new Unity.VideoPlayerUnity(vdu, onReady);
            // assign Draw method copying Image to Unity texture as software decoder Output
            vdu.Output = vp.Draw;
            return vp;
#else
            throw new NotImplementedException("Platform.CreateDefaultVideoPlayer: default Video Player for the platform is not implemented.");
#endif
        }

        public static IPreviewManager CreateDefaultPreviewManager(ILogger logger)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new Unity.AndroidPreviewManagerSurfaceView(logger);
#elif (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
            return new IOS.PreviewManagerLayer(logger);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            return new UWP.PreviewManagerMediaPlayerElement(logger);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.PreviewManagerScreenQuadTexture(logger); // uses custom shader
#elif UNITY_5_3_OR_NEWER // #if UNITY
            return new Unity.PreviewManagerScreenQuadTexture(logger); // uses custom shader
            // return new Unity.PreviewManagerUnityGUI(); // uses GUI.DrawTexture
#else
            return null;
#endif
        }

        // Unity Texture Previews
#if UNITY_5_3_OR_NEWER // #if UNITY
        static public IVideoRecorder CreateVideoRecorderUnityTexture(ILogger logger, VoiceInfo info, DeviceInfo camDevice, Action<IVideoRecorder> onReady)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
#if PHOTON_VOICE_VIDEO_ANDROID_JAVA_API
            return new Unity.AndroidVideoRecorderUnityTexture(logger, info, camDevice.IDString, onReady);
#else
            return new Unity.AndroidNDKVideoRecorderUnityTexture(logger, info, camDevice.IDString, onReady);
#endif
#elif (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
            if (info.Codec == Codec.VideoH264)
            {
                return new IOS.VideoRecorderUnityTexture(logger, info, camDevice.IDString, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateVideoRecorderUnityTexture", info.Codec);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            if (info.Codec == Codec.VideoH264)
            {
                return new UWP.VideoRecorderUnityTexture(logger, info, camDevice.IDString, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateVideoRecorderUnityTexture", info.Codec);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebCodecsCameraRecorderUnityTexture(logger, info, camDevice.IDString, onReady);
#else       // multi-platform VideoRecorderUnity
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            var ve = CreateDefaultVideoEncoder(logger, info);
            if (ve is IEncoderDirectImage)
            {
                return new Unity.VideoRecorderUnity(ve, null, camDevice.IDString, info.Width, info.Height, info.FPS, onReady);
            }
#endif

            throw new NotImplementedException("Platform.CreateDefaultVideoRecorder: default Video Recorder for the platform is not implemented.");
#endif
        }

        static public IVideoPlayer CreateVideoPlayerUnityTexture(ILogger logger, VoiceInfo info, Action<IVideoPlayer> onReady)
        {
            // native platform-specific players
#if UNITY_ANDROID && !UNITY_EDITOR
            return new Unity.AndroidVideoPlayerUnityTexture(logger, info, onReady);
#elif (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
            if (info.Codec == Codec.VideoH264)
            {
                return new IOS.VideoPlayerUnityTexture(logger, info, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateVideoPlayerUnityTexture", info.Codec);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            if (info.Codec == Codec.VideoH264)
            {
                return new UWP.VideoPlayerUnityTexture(logger, info, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateVideoPlayerUnityTexture", info.Codec);
#elif UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            return new Unity.WebCodecsVideoPlayerUnityTexture(logger, info, onReady);
#endif
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            var vd = CreateDefaultVideoDecoder(logger, info);
            var vp = new Unity.VideoPlayerUnity(vd, onReady);
            // assign Draw method copying Image to Unity texture as software decoder Output
            vd.Output = vp.Draw;
            return vp;
#else
            throw new NotImplementedException("Platform.CreateVideoPlayerUnityTexture: default Video Player for the platform is not implemented.");
#endif
        }

        static public IPreviewManager CreatePreviewManagerUnityTexture(ILogger logger)
        {
            return new Unity.PreviewManagerScreenQuadTexture(logger);
        }
#endif // UNITY_5_3_OR_NEWER
#endif // PHOTON_VOICE_VIDEO_ENABLE
        }
    }
