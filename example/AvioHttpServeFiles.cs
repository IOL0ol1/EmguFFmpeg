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

            FFmpegUtil.NetworkInit();

            MediaIOContext server;
            using (var options = new MediaDictionary { ["listen"] = "2" })
            {
                try
                {
                    server = MediaIOContext.Open(outUri, ffmpeg.AVIO_FLAG_WRITE, options);
                }
                catch (FFmpegException e)
                {
                    Console.Error.WriteLine($"Failed to open server: {FFmpegException.GetErrorString(e.ErrorCode)}");
                    FFmpegUtil.NetworkDeinit();
                    return;
                }
            }

            Console.Error.WriteLine("Entering main loop.");
            int ret;
            for (;;)
            {
                ret = server.Accept(out MediaIOContext client);
                if (ret < 0) break;

                Console.Error.WriteLine("Accepted client, starting thread.");
                // Hand the accepted client context straight to its worker thread.
                var inUriLocal = inUri;
                var t = new Thread(() => ProcessClient(client, inUriLocal));
                t.IsBackground = true;
                t.Start();
            }

            server.Dispose();
            FFmpegUtil.NetworkDeinit();
            if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
                Console.Error.WriteLine($"Some errors occurred: {FFmpegException.GetErrorString(ret)}");
        }

        private static void ProcessClient(MediaIOContext client, string inUri)
        {
            MediaIOContext input    = null;
            string         resource = null;
            int            ret;
            int            replyCode;

            try
            {
                // HTTP handshake loop – retrieve the requested resource path.
                while ((ret = client.Handshake()) > 0)
                {
                    resource = MediaOptions.Get(client, "resource", ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    // The "resource" option may be empty; only break when we have content.
                    if (!string.IsNullOrEmpty(resource))
                        break;
                }
                if (ret < 0) return;

                // Decide HTTP status code: 200 OK or 404 Not Found.
                if (resource != null && resource.StartsWith("/", StringComparison.Ordinal))
                {
                    string reqPath = resource.Substring(1);
                    replyCode = string.Equals(reqPath, inUri, StringComparison.Ordinal)
                                ? 200
                                : ffmpeg.AVERROR_HTTP_NOT_FOUND;
                }
                else
                {
                    replyCode = ffmpeg.AVERROR_HTTP_NOT_FOUND;
                }

                ret = MediaOptions.SetInt(client, "reply_code", replyCode, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                if (ret < 0)
                {
                    Console.Error.WriteLine($"Failed to set reply_code: {FFmpegException.GetErrorString(ret)}");
                    return;
                }

                // Complete the handshake (sends the HTTP response header).
                while ((ret = client.Handshake()) > 0) ;
                if (ret < 0) return;

                Console.Error.WriteLine("Handshake performed.");
                if (replyCode != 200) return;

                Console.Error.WriteLine("Opening input file.");
                try
                {
                    input = MediaIOContext.Open(inUri, ffmpeg.AVIO_FLAG_READ);
                }
                catch (FFmpegException e)
                {
                    Console.Error.WriteLine($"Failed to open input '{inUri}': {FFmpegException.GetErrorString(e.ErrorCode)}");
                    return;
                }

                var buf = new byte[1024];
                for (;;)
                {
                    int n;
                    try
                    {
                        n = input.Read(buf, 0, buf.Length); // returns 0 at EOF
                    }
                    catch (FFmpegException e)
                    {
                        Console.Error.WriteLine($"Error reading from input: {FFmpegException.GetErrorString(e.ErrorCode)}");
                        break;
                    }
                    if (n <= 0) break;
                    client.Write(buf, 0, n);
                    client.Flush();
                }
            }
            finally
            {
                Console.Error.WriteLine("Flushing client");
                client.Flush();
                Console.Error.WriteLine("Closing client");
                client.Dispose();
                Console.Error.WriteLine("Closing input");
                input?.Dispose();
            }
        }
    }
}
