using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using ObjCRuntime;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ExtraLib.FFmpeg;
using ExtraLib;
using System.Threading;
using CoreGraphics;

namespace MPVDemo
{
    public partial class ViewController : NSViewController
    {

        private mpv mpvPlayer;
        private string _mediaFilePath;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            //var lib = Dlfcn.dlopen("/Users/zhoukaiwen/Projects/MPVDemo/MPVDemo/bin/Debug/MPVDemo.app/Contents/MonoBundle/libavformat.57.dylib", 0);
            //if (lib == IntPtr.Zero) {
            //    Debug.WriteLine("Faild load");
            //} else {
            //    Debug.WriteLine("Finished load");
            //}
            ffmpeg.RootPath = NSBundle.MainBundle.BundlePath + "/Contents/MonoBundle/";
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
            ffmpeg.avformat_network_init();

            base.ViewDidLoad();

            mpvPlayer = new mpv(VideoView.Handle);
        }

        partial void ChooseFile(NSObject sender)
        {

            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;
            dlg.AllowedFileTypes = new string[] { "mp4", "mov", "mkv" };
            
            if (dlg.RunModal() == 1)
            {
                // Nab the first file
                var url = dlg.Urls[0];
                var filename = url.LastPathComponent;
                if (url != null)
                {
                    var path = url.Path;
                    _mediaFilePath = path;
                    Debug.WriteLine("We have url: {0}", path, null);
                    Thread thread = new Thread(() => {
                        ProcessWithFFmpeg(path);
                    });

                    thread.Start();
                }
            }
        }

        unsafe void ProcessWithFFmpeg(string path) {
            // FFmpeg test
            Console.WriteLine($"FFmpeg version info: {ffmpeg.av_version_info()}");

            // setup logging
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
            av_log_set_callback_callback logCallback = (p0, level, format, vl) =>
            {
                if (level > ffmpeg.av_log_get_level()) return;

                var lineSize = 1024;
                var lineBuffer = stackalloc byte[lineSize];
                var printPrefix = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
                Console.Write(line);
            };
            ffmpeg.av_log_set_callback(logCallback);

            // decode N frames from url or path

            //string url = @"../../sample_mpeg4.mp4";
            var url = path;

            var pFormatContext = ffmpeg.avformat_alloc_context();

            int error;
            error = ffmpeg.avformat_open_input(&pFormatContext, url, null, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            error = ffmpeg.avformat_find_stream_info(pFormatContext, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            AVDictionaryEntry* tag = null;
            while ((tag = ffmpeg.av_dict_get(pFormatContext->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                Console.WriteLine($"{key} = {value}");
            }

            AVStream* pStream = null;
            for (var i = 0; i < pFormatContext->nb_streams; i++)
                if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    pStream = pFormatContext->streams[i];
                    break;
                }
            if (pStream == null) throw new ApplicationException(@"Could not found video stream.");

            var codecContext = *pStream->codec;

            Console.WriteLine($"codec name: {ffmpeg.avcodec_get_name(codecContext.codec_id)}");

            var width = codecContext.width;
            var height = codecContext.height;
            var sourcePixFmt = codecContext.pix_fmt;
            var codecId = codecContext.codec_id;
            var destinationPixFmt = AVPixelFormat.AV_PIX_FMT_RGBA;
            var pConvertContext = ffmpeg.sws_getContext(width, height, sourcePixFmt,
                width, height, destinationPixFmt,
                                                        ffmpeg.SWS_BILINEAR, null, null, null);
            if (pConvertContext == null) throw new ApplicationException(@"Could not initialize the conversion context.");

            var pConvertedFrame = ffmpeg.av_frame_alloc();
            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixFmt, width, height, 1);
            var convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
            var dstData = new byte_ptrArray4();
            var dstLinesize = new int_array4();
            ffmpeg.av_image_fill_arrays(ref dstData, ref dstLinesize, (byte*)convertedFrameBufferPtr, destinationPixFmt, width, height, 1);

            var pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (pCodec == null) throw new ApplicationException(@"Unsupported codec.");

            var pCodecContext = &codecContext;

            if ((pCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_TRUNCATED;

            error = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
            if (error < 0) throw new ApplicationException(GetErrorMessage(error));

            var pDecodedFrame = ffmpeg.av_frame_alloc();

            var packet = new AVPacket();
            var pPacket = &packet;
            ffmpeg.av_init_packet(pPacket);

            var frameNumber = 0;
            while (frameNumber < 10)
            {
                try
                {
                    do
                    {
                        error = ffmpeg.av_read_frame(pFormatContext, pPacket);
                        if (error == ffmpeg.AVERROR_EOF) break;
                        if (error < 0) throw new ApplicationException(GetErrorMessage(error));

                        if (pPacket->stream_index != pStream->index) continue;

                        error = ffmpeg.avcodec_send_packet(pCodecContext, pPacket);
                        if (error < 0) throw new ApplicationException(GetErrorMessage(error));

                        error = ffmpeg.avcodec_receive_frame(pCodecContext, pDecodedFrame);
                    } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));
                    if (error == ffmpeg.AVERROR_EOF) break;
                   

                    if (pPacket->stream_index != pStream->index) continue;

                    Console.WriteLine($@"frame: {frameNumber}");

                    ffmpeg.sws_scale(pConvertContext, pDecodedFrame->data, pDecodedFrame->linesize, 0, height, dstData, dstLinesize);
                    SaveToFile(dstData, width, height, $@"{frameNumber}.tiff");
                }
                finally
                {
                   
                    ffmpeg.av_packet_unref(pPacket);
                    ffmpeg.av_frame_unref(pDecodedFrame);
                }

             
                //using (var bitmap = new Bitmap(width, height, dstLinesize[0], PixelFormat.Format24bppRgb, convertedFrameBufferPtr))
                    //bitmap.Save(@"frame.buffer.jpg", ImageFormat.Jpeg);


                frameNumber++;

            }

            Marshal.FreeHGlobal(convertedFrameBufferPtr);
            ffmpeg.av_free(pConvertedFrame);
            ffmpeg.sws_freeContext(pConvertContext);

            ffmpeg.av_free(pDecodedFrame);
            ffmpeg.avcodec_close(pCodecContext);
            ffmpeg.avformat_close_input(&pFormatContext);

        }

        unsafe void SaveToFile(byte_ptrArray4 frame, int width, int height, string file) {
            var rgb = CGColorSpace.CreateDeviceRGB();
            var data = frame[0];

            //CGBitmapContext
            //bitsPerComponent: bytes length of each pixel
            //bytesPerRow: image width*channels (RGBA = 4)
            //bitmapInfo: bitmap type，the same as destColorFormat (A RGB = PremultipliedFirst, RGB A = PremultipliedLast)

         
            var cgContext = new CGBitmapContext((IntPtr)data, width, height, 8, width*4, rgb, CGImageAlphaInfo.PremultipliedLast);

            var image = cgContext.ToImage();
            var imageFinal = new NSImage(image, new CGSize(0, 0));
            var documentsDirectory = Environment.GetFolderPath
                        (Environment.SpecialFolder.Personal);
            string jpgFilename = System.IO.Path.Combine(documentsDirectory, file); // hardcoded filename, overwritten each time
            NSData imgData = imageFinal.AsTiff();
            NSError err = null;
            if (imgData.Save(jpgFilename, false, out err))
            {
                Console.WriteLine("saved as " + jpgFilename);
            }
            else
            {
                Console.WriteLine("NOT saved as " + jpgFilename + " because" + err.LocalizedDescription);
            }
        }

        private static unsafe string GetErrorMessage(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }

        partial void Play(NSObject sender)
        {
            mpvPlayer.LoadFile(_mediaFilePath);

        }

        partial void Pause(NSObject sender)
        {
            mpvPlayer.Pause();
        }


        partial void MakeScreenShot(NSObject sender)
        {
            mpvPlayer.MakeScreenShot();
            Debug.WriteLine("ScreenShot time position is {0}", mpvPlayer.TimePosition, null);
        }
    }
}
