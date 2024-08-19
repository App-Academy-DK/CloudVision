using Google.Cloud.Vision.V1;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

String dir = "c:\\images";

foreach (string file in Directory.EnumerateFiles(dir, "*.jpg"))
{
    await CropImageAsync(file, 1);
}

async Task CropImageAsync(string filename, float aspectRatio)
{
    var image = await readImageAsync(filename);
    ImageAnnotatorClient client = ImageAnnotatorClient.Create();

    CropHintsParams cropHintsParams = new();
    cropHintsParams.AspectRatios.Add(aspectRatio);

    ImageContext context = new();
    context.CropHintsParams = cropHintsParams;

    CropHintsAnnotation cropHints = client.DetectCropHints(image, context);

    foreach (CropHint hint in cropHints.CropHints)
    {
        Console.WriteLine(filename);
        Console.WriteLine("Crop hint:");
        string poly = string.Join(" - ", hint.BoundingPoly.Vertices.Select(v => $"({v.X}, {v.Y})"));
        Console.WriteLine($"  Poly: {poly}");
        Console.WriteLine($"  Confidence: {hint.Confidence}");
        Console.WriteLine($"  Importance fraction: {hint.ImportanceFraction}");
    }

    CropHint firstHint = cropHints.CropHints[0];
    int height = firstHint.BoundingPoly.Vertices[2].Y - firstHint.BoundingPoly.Vertices[0].Y;
    int width = firstHint.BoundingPoly.Vertices[2].X - firstHint.BoundingPoly.Vertices[0].X;
    int startX = firstHint.BoundingPoly.Vertices[0].X;
    int startY = firstHint.BoundingPoly.Vertices[0].Y;

    var systemImage = System.Drawing.Image.FromFile(filename);
    using (var croppedImage = CropImage(systemImage, height, width, startX, startY))
    {
        string croppedFilename = Path.Combine(dir, Path.GetFileNameWithoutExtension(filename) + "_cropped.jpg");
        croppedImage.Save(croppedFilename, ImageFormat.Jpeg);
    }
    
}

async Task<Google.Cloud.Vision.V1.Image> readImageAsync(string filename)
{
    using var stream = File.OpenRead(filename);
    using var memoryStream = new MemoryStream();
    await stream.CopyToAsync(memoryStream);
    var imageBytes = memoryStream.ToArray();
    var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
    return image;
}


// Kilde: https://jasonjano.wordpress.com/2010/02/13/image-resizing-and-cropping-in-c/
System.Drawing.Image CropImage(System.Drawing.Image Image, int Height, int Width, int StartAtX, int StartAtY)
{
    System.Drawing.Image outimage;
    MemoryStream mm = null;
    try
    {
        //check the image height against our desired image height
        if (Image.Height < Height)
        {
            Height = Image.Height;
        }

        if (Image.Width < Width)
        {
            Width = Image.Width;
        }

        //create a bitmap window for cropping
        Bitmap bmPhoto = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
        bmPhoto.SetResolution(72, 72);

        //create a new graphics object from our image and set properties
        Graphics grPhoto = Graphics.FromImage(bmPhoto);
        grPhoto.SmoothingMode = SmoothingMode.AntiAlias;
        grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
        grPhoto.PixelOffsetMode = PixelOffsetMode.HighQuality;

        //now do the crop
        grPhoto.DrawImage(Image, new Rectangle(0, 0, Width, Height), StartAtX, StartAtY, Width, Height, GraphicsUnit.Pixel);

        // Save out to memory and get an image from it to send back out the method.
        mm = new MemoryStream();
        bmPhoto.Save(mm, System.Drawing.Imaging.ImageFormat.Jpeg);
        Image.Dispose();
        bmPhoto.Dispose();
        grPhoto.Dispose();
        outimage = System.Drawing.Image.FromStream(mm);

        return outimage;
    }
    catch (Exception ex)
    {
        throw new Exception("Error cropping image, the error was: " + ex.Message);
    }
}