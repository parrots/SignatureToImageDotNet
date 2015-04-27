/*
 * Author:      Curtis Herbert (me@forgottenexpanse.com)
 * License:     BSD License
 * Version:     2.0 (2012-02-11)
 * Contributor: Justin Stolle (justin@justinstolle.com)
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace ConsumedByCode.SignatureToImage
{
    /// <summary>
    /// A supplemental class for Signature Pad (https://github.com/thomasjbradley/signature-pad)
    /// that generates an image of the signature's JSON output server-side using C#. Alternately,
    /// you can provide a name and generate an image that resembles a signature from a font.
    /// Similar to Signature to Image PHP (https://github.com/thomasjbradley/signature-to-image)
    /// </summary>
    public class SignatureToImage
    {
        public Color BackgroundColor { get; set; }
        public Color PenColor { get; set; }
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }
        public float PenWidth { get; set; }
        public float FontSize { get; set; }
        public string FontName { get; set; }

        /// <summary>
        /// Gets a new signature generator with the default options.
        /// </summary>
        public SignatureToImage()
        {
            // Default values
            BackgroundColor = Color.White;
            PenColor = Color.FromArgb(20, 83, 148);
            CanvasWidth = 198;
            CanvasHeight = 45;
            PenWidth = 2;
            FontSize = 24;
            FontName = "Journal";
        }

        /// <summary>
        /// Draws a signature based on the JSON provided by Signature Pad.
        /// </summary>
        /// <param name="json">JSON string of line drawing commands.</param>
        /// <returns>Bitmap image containing the signature.</returns>
        public Bitmap SigJsonToImage(string json)
	{
	    return SigJsonToImage(json, new Size(CanvasWidth, CanvasHeight));
	}
	        
	        
        public Bitmap SigJsonToImage(string json, Size size)
        {
            var signatureImage = GetBlankCanvas();
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (var signatureGraphic = Graphics.FromImage(signatureImage))
                {
                    signatureGraphic.SmoothingMode = SmoothingMode.AntiAlias;
                    var pen = new Pen(PenColor, PenWidth);
                    var serializer = new JavaScriptSerializer();
                    // Next line may throw System.ArgumentException if the string
                    // is an invalid json primitive for the SignatureLine structure
                    var lines = serializer.Deserialize<List<SignatureLine>>(json);
                    foreach (var line in lines)
                    {
                        signatureGraphic.DrawLine(pen, line.lx, line.ly, line.mx, line.my);
                    }
                }
            }
            return (Bitmap)((size.Width == CanvasWidth && size.Height == CanvasHeight) ? signatureImage : ResizeImage(signatureImage, size));
        }

        /// <summary>
        /// Draws an approximation of a signature using a font.
        /// </summary>
        /// <param name="name">The string that will be drawn.</param>
        /// <param name="fontPath">Full path of font file to be used if default font is not installed on the system.</param>
        /// <returns>Bitmap image containing the user's signature.</returns>
        public Bitmap SigNameToImage(string name, string fontPath = null)
        {
            var signatureImage = GetBlankCanvas();
            if (!string.IsNullOrWhiteSpace(name))
            {
                Font font;
                // Need a reference to the font, be it the .ttf in the project or the system-installed font
                if (string.IsNullOrWhiteSpace(fontPath))
                {
                    // Path parameter not provided, try to use system-installed font
                    var installedFontCollection = new InstalledFontCollection();
                    if (installedFontCollection.Families.Any(f => f.Name == FontName))
                    {
                        font = new Font(FontName, FontSize);
                    }
                    else
                    {
                        throw new ArgumentException("The full path of the font file must be provided when the specified font is not installed on the system.", "fontPath");
                    }
                }
                else if (File.Exists(fontPath))
                {
                    try
                    {
                        // Temporarily install font while not affecting the system-installed collection
                        var collection = new PrivateFontCollection();
                        collection.AddFontFile(fontPath);
                        font = new Font(collection.Families.First(), FontSize);
                    }
                    catch (FileNotFoundException)
                    {
                        // Since the existence of the file has already been tested, this exception
                        // means the file is invalid or not supported when trying to load
                        throw new Exception("The specified font file \"" + fontPath + "\" is either invalid or not supported.");
                    }
                }
                else
                {
                    throw new FileNotFoundException("The specified font file \"" + fontPath + "\" does not exist or permission was denied.", fontPath);
                }
                using (var signatureGraphic = Graphics.FromImage(signatureImage))
                {
                    signatureGraphic.TextRenderingHint = TextRenderingHint.AntiAlias;
                    signatureGraphic.DrawString(name, font, new SolidBrush(PenColor), 0, 0);
                }
            }
            return signatureImage;
        }

        /// <summary>
        /// Get a blank bitmap using instance properties for dimensions and background color.
        /// </summary>
        /// <returns>Blank bitmap image.</returns>
        private Bitmap GetBlankCanvas()
        {
            var blankImage = new Bitmap(CanvasWidth, CanvasHeight);
            blankImage.MakeTransparent();
            using (var signatureGraphic = Graphics.FromImage(blankImage))
            {
                signatureGraphic.Clear(BackgroundColor);
            }
            return blankImage;
        }
        
        /// <summary>
	/// Resizes the image to fit the canvas in the event that the signature was drawn larger than it will be redisplayed.
	/// </summary>
        /// <returns>Resized image.</returns>
        private Image ResizeImage(Image img, Size size)
	{
	    int srcWidth = img.Width;
	    int srcHeight = img.Height;

	    float percent = 0;
	    float percWidth = 0;
	    float percHeight = 0;

	    percWidth = ((float)size.Width / (float)srcWidth);
	    percHeight = ((float)size.Height / (float)srcHeight);
	    percent = (percHeight < percWidth) ? percHeight : percWidth;

	    int destWidth = (int)(srcWidth * percent);
	    int destHeight = (int)(srcHeight * percent);

	    Bitmap bmp = new Bitmap(destWidth, destHeight);

	    Graphics graphic = Graphics.FromImage((Image)bmp);
	    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
	    graphic.DrawImage(img, 0, 0, destWidth, destHeight);
	    graphic.Dispose();

	    return (Image)bmp;
        }

        /// <summary>
        /// Line drawing commands as generated by the Signature Pad JSON export option.
        /// </summary>
        private class SignatureLine
        {
            public int lx { get; set; }
            public int ly { get; set; }
            public int mx { get; set; }
            public int my { get; set; }
        }
    }
}
