using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using UnityExport;

namespace 清單下載器
{
    static class Static_Function
    {
        public static void WriteBundleFile(BundleFile BF, string savePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath))) Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            if (!File.Exists(savePath))
            {
                using (FileStream file = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    BF.MemoryAssetsFileList[0].memStream.WriteTo(file);
                    BF.MemoryAssetsFileList[0].memStream.Close();
                }
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap bitmap = new Bitmap(width, height);
            bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (ImageAttributes imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
                }
            }
            return bitmap;
        }
    }
}
