using System.Drawing;
using ZXing;

namespace test2.Helpers
{
    public class BitmapLuminanceSource : BaseLuminanceSource
    {
        public BitmapLuminanceSource(Bitmap bitmap) : base(bitmap.Width, bitmap.Height)
        {
            var data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb
            );

            try
            {
                var stride = data.Stride;
                var rgbValues = new byte[stride * bitmap.Height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, rgbValues.Length);

                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var offset = (y * stride) + (x * 4);
                        var r = rgbValues[offset + 2];
                        var g = rgbValues[offset + 1];
                        var b = rgbValues[offset];

                        SetLuminance(y * bitmap.Width + x, (byte)((r + g + b) / 3));
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
        {
            return new BitmapLuminanceSource(new Bitmap(width, height))
            {
                luminances = newLuminances
            };
        }

        private void SetLuminance(int index, byte value)
        {
            luminances[index] = value;
        }
    }
}
