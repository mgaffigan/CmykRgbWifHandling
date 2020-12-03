# CmykRgbWifHandling
Example for https://stackoverflow.com/questions/65131631/concatenate-a-bitmap-rgb-with-a-tiff-cmyk-without-converting-cmyk-to-rgb/

I’m not aware of any capacity in the TIFF format to have two different color spaces at the same time. Since you are dealing in CMYK, I assume that is the one you want to preserve. 

If so, the steps to do so would be:

1. Load CMYK image A (using BitmapDecoder)
2. Load RGB image B (using BitmapDecoder)
3. Convert image B to CMYK with the desired color profile (using FormatConvertedBitmap)
4. If required, ensure the pixel format for image B matches A (using FormatConvertedBitmap)
5. Composite the two in memory as a byte array (using Render, then memory manipulation, then new bitmap from the memory)
6. Save the composite to a new CMYK TIFF file (using TiffBitmapEncoder)

That should be possible with WIF (System.Media).

An example doing so could be written as:

    BitmapFrame LoadTiff(string filename)
    {
        using (var rs = File.OpenRead(filename))
        {
            return BitmapDecoder.Create(rs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        }
    }

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

Which maintains color accuracy of the CMYK image, and converts the RGB using the system color profile.  This can be verified in Photoshop which shows that the each letter, and rich black, have maintained their original values. (note that imgur does convert to png with dubious color handling - check github for originals.)

Image A (CMYK): 
[![Image 1 - CMYK, white, Rich Black][1]][1]

Image B (RGB):
[![Image B - RGB, white, black][2]][2]

Result (CMYK):
[![Image Result - RGB with CMYK maintained exactly, RGB altered.][3]][3]


  [1]: https://i.stack.imgur.com/ja5of.png
  [2]: https://i.stack.imgur.com/erWtZ.png
  [3]: https://i.stack.imgur.com/5GFIs.png
