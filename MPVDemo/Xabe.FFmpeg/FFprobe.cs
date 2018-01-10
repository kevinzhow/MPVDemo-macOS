﻿using System;
using System.Linq;
using Newtonsoft.Json;
using Xabe.FFmpeg.Model;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Get information about media file
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    internal sealed class FFprobe: FFbase
    {
        private ProbeModel.Stream[] GetStream(string videoPath)
        {
            string jsonStreams =
                RunProcess($"-v quiet -print_format json -show_streams \"{videoPath}\"");

            var probe =
                JsonConvert.DeserializeObject<ProbeModel>(jsonStreams, new JsonSerializerSettings());

            return new[] {probe.streams?.FirstOrDefault(x => x.codec_type == "video") ?? null, probe.streams?.FirstOrDefault(x => x.codec_type == "audio") ?? null};
        }

        private double GetVideoFramerate(ProbeModel.Stream vid)
        {
            string[] fr = vid.r_frame_rate.Split('/');
            return Math.Round(double.Parse(fr[0]) / double.Parse(fr[1]), 3);
        }

        private string GetVideoAspectRatio(int width, int heigght)
        {
            int cd = GetGcd(width, heigght);
            return width / cd + ":" + heigght / cd;
        }

        private TimeSpan GetVideoDuration(FormatModel.Format format, ProbeModel.Stream video)
        {
            video.duration = format.duration;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            video.bit_rate = format.bitRate == 0 ? video.bit_rate : format.bitRate;

            double duration = video.duration;
            TimeSpan videoDuration = TimeSpan.FromSeconds(duration);
            videoDuration = videoDuration.Subtract(TimeSpan.FromMilliseconds(videoDuration.Milliseconds));

            return videoDuration;
        }

        private FormatModel.Format GetFormat(string videoPath)
        {
            string jsonFormat =
                RunProcess($"-v quiet -print_format json -show_format \"{videoPath}\"");
            FormatModel.Format format = JsonConvert.DeserializeObject<FormatModel.Root>(jsonFormat)
                                                   .format;
            return format;
        }

        private TimeSpan GetAudioDuration(ProbeModel.Stream audio)
        {
            double duration = audio.duration;
            TimeSpan audioDuration = TimeSpan.FromSeconds(duration);
            audioDuration = audioDuration.Subtract(TimeSpan.FromMilliseconds(audioDuration.Milliseconds));

            return audioDuration;
        }

        private int GetGcd(int width, int height)
        {
            while(width != 0 &&
                  height != 0)
                if(width > height)
                    width -= height;
                else
                    height -= width;
            return width == 0 ? height : width;
        }


        private string RunProcess(string args)
        {
            RunProcess(args, FFprobePath, rStandardOutput: true);

            string output;

            try
            {
                output = Process.StandardOutput.ReadToEnd();
            }
            catch(Exception)
            {
                output = "";
            }
            finally
            {
                Process.WaitForExit();
                Process.Close();
            }

            return output;
        }

        public MediaProperties GetProperties(string videoPath)
        {
            var videoProperties = new MediaProperties();
            ProbeModel.Stream[] streams = GetStream(videoPath);
            ProbeModel.Stream videoStream = streams[0];
            ProbeModel.Stream audioStream = streams[1];
            if(videoStream == null &&
               audioStream == null)
                return null;

            FormatModel.Format format = GetFormat(videoPath);
            videoProperties.Size = long.Parse(format.size);

            if(videoStream != null)
            {
                videoProperties.VideoFormat = videoStream.codec_name;
                videoProperties.VideoDuration = GetVideoDuration(format, videoStream);
                videoProperties.Width = videoStream.width;
                videoProperties.Height = videoStream.height;
                videoProperties.FrameRate = GetVideoFramerate(videoStream);
                videoProperties.Ratio = GetVideoAspectRatio(videoProperties.Width, videoProperties.Height);
            }
            if(audioStream != null)
            {
                videoProperties.AudioFormat = audioStream.codec_name;
                videoProperties.AudioDuration = GetAudioDuration(audioStream);
            }

            videoProperties.Duration = TimeSpan.FromSeconds(Math.Max(videoProperties.VideoDuration.TotalSeconds, videoProperties.AudioDuration.TotalSeconds));
            return videoProperties;
        }
    }
}
