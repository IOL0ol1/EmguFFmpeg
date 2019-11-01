using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using EmguFFmpeg;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.EmguCV
{
    public static class EmguFFmpegEx
    {
        public static Mat ToMat(this VideoFrame frame)
        {
            switch ((AVPixelFormat)frame.Format)
            {
                case AVPixelFormat.AV_PIX_FMT_NONE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUYV422:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB24:
                    Image<Rgb, byte> imageRGB24 = new Image<Rgb, byte>(frame.Width, frame.Height);
                    Marshal.Copy(frame.GetData()[0], 0, imageRGB24.Ptr, imageRGB24.Bytes.Length);
                    return imageRGB24.Mat;

                case AVPixelFormat.AV_PIX_FMT_BGR24:
                    Image<Bgr, byte> imageBGR24 = new Image<Bgr, byte>(frame.Width, frame.Height);
                    Marshal.Copy(frame.GetData()[0], 0, imageBGR24.Ptr, imageBGR24.Bytes.Length);
                    return imageBGR24.Mat;

                case AVPixelFormat.AV_PIX_FMT_YUV422P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV410P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV411P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY8:
                    Image<Gray, byte> imageGray = new Image<Gray, byte>(frame.Width, frame.Height);
                    Marshal.Copy(frame.GetData()[0], 0, imageGray.Ptr, imageGray.Bytes.Length);
                    return imageGray.Mat;

                case AVPixelFormat.AV_PIX_FMT_MONOWHITE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_MONOBLACK:
                    break;

                case AVPixelFormat.AV_PIX_FMT_PAL8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVJ420P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVJ422P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVJ444P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_UYVY422:
                    break;

                case AVPixelFormat.AV_PIX_FMT_UYYVYY411:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR4:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR4_BYTE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB4:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB4_BYTE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV12:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV21:
                    break;

                case AVPixelFormat.AV_PIX_FMT_ARGB:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGBA:
                    Image<Rgba, byte> imageRGBA = new Image<Rgba, byte>(frame.Width, frame.Height);
                    Marshal.Copy(frame.GetData()[0], 0, imageRGBA.Ptr, imageRGBA.Bytes.Length);
                    return imageRGBA.Mat;

                case AVPixelFormat.AV_PIX_FMT_ABGR:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGRA:
                    Image<Bgra, byte> imageBGRA = new Image<Bgra, byte>(frame.Width, frame.Height);
                    Marshal.Copy(frame.GetData()[0], 0, imageBGRA.Ptr, imageBGRA.Bytes.Length);
                    return imageBGRA.Mat;

                case AVPixelFormat.AV_PIX_FMT_GRAY16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV440P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVJ440P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB48BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB48LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB565BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB565LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB555BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB555LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR565BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR565LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR555BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR555LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_VAAPI_MOCO:
                    break;

                case AVPixelFormat.AV_PIX_FMT_VAAPI_IDCT:
                    break;

                case AVPixelFormat.AV_PIX_FMT_VAAPI_VLD:
                    break;
                //case AVPixelFormat.AV_PIX_FMT_VAAPI:
                //    break;
                case AVPixelFormat.AV_PIX_FMT_YUV420P16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_DXVA2_VLD:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB444LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB444BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR444LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR444BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YA8:
                    break;
                //case AVPixelFormat.AV_PIX_FMT_Y400A:
                //    break;
                //case AVPixelFormat.AV_PIX_FMT_GRAY8A:
                //    break;
                case AVPixelFormat.AV_PIX_FMT_BGR48BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR48LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP:
                    break;
                //case AVPixelFormat.AV_PIX_FMT_GBR24P:
                //    break;
                case AVPixelFormat.AV_PIX_FMT_GBRP9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA420P16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_VDPAU:
                    break;

                case AVPixelFormat.AV_PIX_FMT_XYZ12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_XYZ12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV16:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV20LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV20BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGBA64BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGBA64LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGRA64BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGRA64LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YVYU422:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YA16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YA16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_QSV:
                    break;

                case AVPixelFormat.AV_PIX_FMT_MMAL:
                    break;

                case AVPixelFormat.AV_PIX_FMT_D3D11VA_VLD:
                    break;

                case AVPixelFormat.AV_PIX_FMT_CUDA:
                    break;

                case AVPixelFormat.AV_PIX_FMT_0RGB:
                    break;

                case AVPixelFormat.AV_PIX_FMT_RGB0:
                    break;

                case AVPixelFormat.AV_PIX_FMT_0BGR:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BGR0:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P14BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV420P14LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P14BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV422P14LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P14BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV444P14LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP14BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRP14LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVJ411P:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_BGGR8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_RGGB8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_GBRG8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_GRBG8:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_BGGR16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_BGGR16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_RGGB16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_RGGB16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_GBRG16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_GBRG16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_GRBG16LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_BAYER_GRBG16BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_XVMC:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV440P10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV440P10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV440P12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUV440P12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_AYUV64LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_AYUV64BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX:
                    break;

                case AVPixelFormat.AV_PIX_FMT_P010LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_P010BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAP10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_MEDIACODEC:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY10BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY10LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_P016LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_P016BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_D3D11:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY9BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY9LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRPF32BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRPF32LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAPF32BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GBRAPF32LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_DRM_PRIME:
                    break;

                case AVPixelFormat.AV_PIX_FMT_OPENCL:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY14BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAY14LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAYF32BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_GRAYF32LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA422P12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P12BE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_YUVA444P12LE:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV24:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NV42:
                    break;

                case AVPixelFormat.AV_PIX_FMT_NB:
                    break;

                default:
                    break;
            }
            return null;
        }
    }
}