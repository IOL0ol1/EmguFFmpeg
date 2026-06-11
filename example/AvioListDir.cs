using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: avio_list_dir.c
    /// List directory entries of a given URL using the AVIODirContext API.
    /// Works with local paths or network protocols that support directory listing (e.g. FTP, SMB).
    /// Implemented directly on the raw avio_*_dir API: C# offers <see cref="System.IO.Directory"/>
    /// for local paths, so this niche protocol-level API is not wrapped by the library.
    /// </summary>
    public unsafe class AvioListDir : ExampleBase
    {
        public AvioListDir() { Index = 16; Enable = false; }

        public override void Execute()
        {
            var inputDir = args.Length > 0 ? args[0] : ".";

            FFmpegUtil.NetworkInit();

            AVIODirContext* ctx = null;
            int ret = ffmpeg.avio_open_dir(&ctx, inputDir, null);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Cannot open directory '{inputDir}': {FFmpegException.GetErrorString(ret)}");
                FFmpegUtil.NetworkDeinit();
                return;
            }

            int cnt = 0;
            AVIODirEntry* entry = null;
            while (true)
            {
                ret = ffmpeg.avio_read_dir(ctx, &entry);
                if (ret < 0)
                {
                    Console.Error.WriteLine($"Cannot list directory: {FFmpegException.GetErrorString(ret)}");
                    break;
                }
                if (entry == null) break;

                if (cnt == 0)
                    Console.WriteLine($"{"TYPE",-9} {"SIZE",12} {"NAME",30} {"UID(GID)",10} {"UGO",3} {"MODIFIED",16} {"ACCESSED",16} {"STATUS",16}");

                string filemode = entry->filemode == -1 ? "???" : Convert.ToString(entry->filemode, 8).PadLeft(3);
                string uidGid   = $"{entry->user_id}({entry->group_id})";
                string name     = ((IntPtr)entry->name).PtrToStringUTF8() ?? "";
                Console.WriteLine(
                    $"{TypeString((AVIODirEntryType)entry->type),-9} {entry->size,12} {name,30} {uidGid,10} {filemode,3} " +
                    $"{entry->modification_timestamp,16} {entry->access_timestamp,16} {entry->status_change_timestamp,16}");

                ffmpeg.avio_free_directory_entry(&entry);
                cnt++;
            }

            ffmpeg.avio_close_dir(&ctx);
            FFmpegUtil.NetworkDeinit();
        }

        private static string TypeString(AVIODirEntryType type) => type switch
        {
            AVIODirEntryType.AVIO_ENTRY_DIRECTORY        => "<DIR>",
            AVIODirEntryType.AVIO_ENTRY_FILE             => "<FILE>",
            AVIODirEntryType.AVIO_ENTRY_BLOCK_DEVICE     => "<BLOCK DEVICE>",
            AVIODirEntryType.AVIO_ENTRY_CHARACTER_DEVICE => "<CHARACTER DEVICE>",
            AVIODirEntryType.AVIO_ENTRY_NAMED_PIPE       => "<PIPE>",
            AVIODirEntryType.AVIO_ENTRY_SYMBOLIC_LINK    => "<LINK>",
            AVIODirEntryType.AVIO_ENTRY_SOCKET           => "<SOCKET>",
            AVIODirEntryType.AVIO_ENTRY_SERVER           => "<SERVER>",
            AVIODirEntryType.AVIO_ENTRY_SHARE            => "<SHARE>",
            AVIODirEntryType.AVIO_ENTRY_WORKGROUP        => "<WORKGROUP>",
            _                                            => "<UNKNOWN>",
        };
    }
}
