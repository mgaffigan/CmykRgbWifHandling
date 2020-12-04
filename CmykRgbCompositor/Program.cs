using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CmykRgbCompositor
{
    class Program
    {
        static void Main(string[] args)
        {
            LayoutLeftAndRight();
            Overlay();
        }

        private static void Overlay()
        {
            // Load, validate A
            var imageA = LoadTiff("CMYK.tif");
            if (imageA.Format != PixelFormats.Cmyk32)
            {
                throw new InvalidOperationException("imageA is not CMYK");
            }

            // Load, validate, convert B
            var imageB = LoadTiff("RGBOverlay.tif");
            if (imageB.PixelHeight != imageA.PixelHeight
                || imageB.PixelWidth != imageA.PixelWidth)
            {
                throw new InvalidOperationException("Image B is not the same size as image A");
            }
            var imageBBGRA = new FormatConvertedBitmap(imageB, PixelFormats.Bgra32, null, 0d);
            var imageBCmyk = new FormatConvertedBitmap(imageB, imageA.Format, null, 0d);

            // Merge
            int width = imageA.PixelWidth, height = imageA.PixelHeight;
            var stride = width * (imageA.Format.BitsPerPixel / 8);
            var bufferA = new uint[width * height];
            var bufferB = new uint[width * height];
            var maskBuffer = new uint[width * height];
            imageA.CopyPixels(bufferA, stride, 0);
            imageBBGRA.CopyPixels(maskBuffer, stride, 0);
            imageBCmyk.CopyPixels(bufferB, stride, 0);

            for (int i = 0; i < bufferA.Length; i++)
            {
                // set pixel in bufferA to the value from bufferB if mask is not white
                if (maskBuffer[i] != 0xffffffff)
                {
                    bufferA[i] = bufferB[i];
                }
            }

            var result = BitmapSource.Create(width, height, imageA.DpiX, imageA.DpiY, imageA.Format, null, bufferA, stride);

            // save to new file
            using (var ws = File.Create("out_overlay.tif"))
            {
                var tiffEncoder = new TiffBitmapEncoder();
                tiffEncoder.Frames.Add(BitmapFrame.Create(result));
                tiffEncoder.Save(ws);
            }
        }

        private static void LayoutLeftAndRight()
        {
            // Load, validate A
            var imageA = LoadTiff("CMYK.tif");
            if (imageA.Format != PixelFormats.Cmyk32)
            {
                throw new InvalidOperationException("imageA is not CMYK");
            }

            // Load, validate, convert B
            var imageB = LoadTiff("RGB.tif");
            if (imageB.PixelHeight != imageA.PixelHeight)
            {
                throw new InvalidOperationException("Image B is not the same height as image A");
            }
            var imageBCmyk = new FormatConvertedBitmap(imageB, imageA.Format, null, 0d);

            // Merge
            int width = imageA.PixelWidth + imageB.PixelWidth,
                height = imageA.PixelHeight,
                bytesPerPixel = imageA.Format.BitsPerPixel / 8,
                stride = width * bytesPerPixel;
            var buffer = new byte[stride * height];
            imageA.CopyPixels(buffer, stride, 0);
            imageBCmyk.CopyPixels(buffer, stride, imageA.PixelWidth * bytesPerPixel);
            var result = BitmapSource.Create(width, height, imageA.DpiX, imageA.DpiY, imageA.Format, null, buffer, stride);

            // save to new file
            using (var ws = File.Create("out.tif"))
            {
                var tiffEncoder = new TiffBitmapEncoder();
                tiffEncoder.Frames.Add(BitmapFrame.Create(result));
                tiffEncoder.Save(ws);
            }
        }

        private static BitmapFrame LoadTiff(string filename)
        {
            using (var rs = File.OpenRead(filename))
            {
                return BitmapDecoder.Create(rs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
            }
        }
    }
}
