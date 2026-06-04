using System;
using System.Threading;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: avio_http_serve_files.c
    /// Serve a file without decoding or demuxing it over the HTTP protocol.
    /// Multiple clients can connect and will receive the same file.
    /// Usage: args[0] = input_file, args[1] = http://hostname[:port]
    /// </summary>
    public unsafe class AvioHttpServeFiles : ExampleBase
    {
        public AvioHttpServeFiles() { Index = 20; Enable = false; }

        public override void Execute()
        {
            var inUri  = args.Length > 0 ? args[0] : "input.mp4";
            var outUri = args.Length > 1 ? args[1] : "http://127.0.0.1:8080";

            ffmpeg.avformat_network_init();

            AVIOContext* server = null;
            AVDictionary* options = null;
            ffmpeg.av_dict_set(&options, "listen", "2", 0);

            int ret = ffmpeg.avio_open2(&server, outUri, ffmpeg.AVIO_FLAG_WRITE, null, &options);
            ffmpeg.av_dict_free(&options);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Failed to open server: {FFmpegException.GetErrorString(ret)}");
                return;
            }

            Console.Error.WriteLine("Entering main loop.");
            for (;;)
            {
                AVIOContext* client = null;
                ret = ffmpeg.avio_accept(server, &client);
                if (ret < 0) break;

                Console.Error.WriteLine("Accepted client, starting thread.");
                // Capture the client pointer across the thread boundary via IntPtr.
                var clientPtr  = (IntPtr)client;
                var inUriLocal = inUri;
                var t = new Thread(() => ProcessClient((AVIOContext*)clientPtr, inUriLocal));
                t.IsBackground = true;
                t.Start();
            }

            ffmpeg.avio_close(server);
            if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
                Console.Error.WriteLine($"Some errors occurred: {FFmpegException.GetErrorString(ret)}");
        }

        private static unsafe void ProcessClient(AVIOContext* client, string inUri)
        {
            AVIOContext* input    = null;
            byte*        resource = null;
            int          ret;
            int          replyCode;

            // HTTP handshake loop – retrieve the requested resource path.
            while ((ret = ffmpeg.avio_handshake(client)) > 0)
            {
                ffmpeg.av_opt_get(client, "resource", ffmpeg.AV_OPT_SEARCH_CHILDREN, &resource);
                // av_opt_get may return an empty string; only break when we have content.
                if (resource != null && resource[0] != 0)
                    break;
                ffmpeg.av_freep(&resource);
            }
            if (ret < 0) goto end;

            // Decide HTTP status code: 200 OK or 404 Not Found.
            if (resource != null && resource[0] == '/')
            {
                string reqPath = ((IntPtr)(resource + 1)).PtrToStringUTF8() ?? "";
                replyCode = string.Equals(reqPath, inUri, StringComparison.Ordinal)
                            ? 200
                            : ffmpeg.AVERROR_HTTP_NOT_FOUND;
            }
            else
            {
                replyCode = ffmpeg.AVERROR_HTTP_NOT_FOUND;
            }

            ret = (int)ffmpeg.av_opt_set_int(client, "reply_code", replyCode,
                                             ffmpeg.AV_OPT_SEARCH_CHILDREN);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Failed to set reply_code: {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            // Complete the handshake (sends the HTTP response header).
            while ((ret = ffmpeg.avio_handshake(client)) > 0) ;
            if (ret < 0) goto end;

            Console.Error.WriteLine("Handshake performed.");
            if (replyCode != 200) goto end;

            Console.Error.WriteLine("Opening input file.");
            ret = ffmpeg.avio_open2(&input, inUri, ffmpeg.AVIO_FLAG_READ, null, null);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Failed to open input '{inUri}': {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            {
                var buf = new byte[1024];
                fixed (byte* pBuf = buf)
                {
                    for (;;)
                    {
                        int n = ffmpeg.avio_read(input, pBuf, buf.Length);
                        if (n < 0)
                        {
                            if (n != ffmpeg.AVERROR_EOF)
                                Console.Error.WriteLine($"Error reading from input: {FFmpegException.GetErrorString(n)}");
                            break;
                        }
                        ffmpeg.avio_write(client, pBuf, n);
                        ffmpeg.avio_flush(client);
                    }
                }
            }

        end:
            Console.Error.WriteLine("Flushing client");
            ffmpeg.avio_flush(client);
            Console.Error.WriteLine("Closing client");
            ffmpeg.avio_close(client);
            Console.Error.WriteLine("Closing input");
            if (input != null) ffmpeg.avio_close(input);
            ffmpeg.av_freep(&resource);
        }
    }
}
