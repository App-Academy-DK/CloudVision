namespace CloudVision
{
    using Google.Cloud.Vision.V1;
    using Microsoft.Maui.Controls;
    using SkiaSharp.Views.Maui.Controls;
    using Microsoft.Maui.Storage;
    using SkiaSharp;
    using System;
    using System.IO;
    using SkiaSharp.Views.Maui;

    public partial class MainPage : ContentPage
    {
        private byte[] imageBytes;
        private readonly List<SKRect> regions = new();

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSelectImageButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Vælg et billede",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                    SelectedImage.Source = Microsoft.Maui.Controls.ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fejl", $"Kunne ikke vælge billede: {ex.Message}", "OK");
            }
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            if (imageBytes != null)
            {
                using var bitmap = SKBitmap.Decode(imageBytes);
                canvas.DrawBitmap(bitmap, new SKRect(0, 0, e.Info.Width, e.Info.Height));
               
            }

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Red,
                StrokeWidth = 5
            };

            foreach (var region in regions)
            {
                canvas.DrawRect(region, paint);
            }
        }
        
        private void OnAnalyzeButtonClicked(object sender, EventArgs e)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
            var labels = client.DetectLabels(image);

            string analysisResult = "<b>Labels (og sandsynlighed):</b><ul>";
            foreach (var label in labels)
            {
                analysisResult += $"<li>{label.Description} ({label.Score:P2})</li>";
            }
            analysisResult += "</ul>";
            AnalysisText.Text = analysisResult;
        }


        private void OnTextButtonClicked(object sender, EventArgs e)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
            var annotations = client.DetectText(image);

            regions.Clear();

            string analysisResult = "<b>Tekst</b><ul>";
            foreach (var annotation in annotations)
            {
                analysisResult += $"<li>{annotation.Description}</li>";
                Vertex[] vertices = annotation.BoundingPoly.Vertices.ToArray();
                regions.Add(new SKRect(vertices[0].X, vertices[0].Y, vertices[2].X, vertices[2].Y));
            }
            analysisResult += "</ul>";
            AnalysisText.Text = analysisResult;

            CanvasView.InvalidateSurface();

        }

        private void OnLandmarkButtonClicked(object sender, EventArgs e)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
            var landmarks = client.DetectLandmarks(image);

            string analysisResult = "<b>Vartegn</b><ul>";
            foreach (var mark in landmarks)
            {
                analysisResult += $"<li>{mark.Description}</li>";
            }
            analysisResult += "</ul>";
            AnalysisText.Text = analysisResult;

        }
    }
}
    