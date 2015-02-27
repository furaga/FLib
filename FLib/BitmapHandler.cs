using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Windows.Forms;
using System.Drawing.Imaging;
//using System.Windows.Media.Imaging;
using System.IO;

namespace FLib
{
    /// <summary>
    /// BitmapIteratorとは別。注意
    /// </summary>
    static public class BitmapHandler
    {
        static public BitmapIterator GetBitmapIterator(Bitmap bmp, ImageLockMode lockMode, PixelFormat pixelFormat)
        {
            return new BitmapIterator(bmp, lockMode, pixelFormat);
        }

        static public Bitmap CreateThumbnail(Bitmap bmp, int w, int h)
        {
            return CreateThumbnail(bmp, w, h, Color.White);
        }

        static public Bitmap CreateThumbnail(Bitmap bmp, int w, int h, Color bgColor)
        {
            Bitmap thumbnail = new Bitmap(w, h, bmp.PixelFormat);
            using (Graphics g = Graphics.FromImage(thumbnail))
            {
                g.Clear(bgColor);
                SizeF size = GetFittingSize(bmp, w, h);
                g.DrawImage(bmp, new Rectangle(0, 0, (int)(size.Width), (int)(size.Height)));
            }
            return thumbnail;
        }

        static public SizeF GetFittingSize(Bitmap bmp, int w, int h)
        {
            float ratio = Math.Min((float)w / bmp.Width, (float)h / bmp.Height);
            float ww = bmp.Width * ratio;
            float hh = bmp.Height * ratio;
            return new SizeF(ww, hh);
        }

        static public Bitmap FromSketch(List<List<Point>> sketch, int w, int h, Pen pen, Color clearColor)
        {
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(clearColor);
                foreach (var stroke in sketch)
                {
                    if (stroke.Count >= 2)
                    {
                        g.DrawLines(pen, stroke.ToArray());
                    }
                }
            }
            return bmp;
        }

        static public Bitmap FromSketchFile(string filePath, int w, int h, Pen pen, Color clearColor)
        {
            if (!System.IO.File.Exists(filePath)) return null;

            string[] lines = System.IO.File.ReadAllLines(filePath);
            List<List<Point>> sketch = new List<List<Point>>();
            foreach (var line in lines)
            {
                List<Point> stroke = new List<Point>();
                string[] pts = line.Split(' ').Where(str => string.IsNullOrWhiteSpace(str) == false).ToArray();
                foreach (var ptText in pts)
                {
                    string[] tokens = ptText.Split(',');
                    System.Diagnostics.Debug.Assert(tokens.Length == 2);
                    int x, y;
                    if (int.TryParse(tokens[0], out x) && int.TryParse(tokens[1], out y))
                    {
                        stroke.Add(new Point(x, y));
                    }
                }
                sketch.Add(stroke);
            }

            return FromSketch(sketch, w, h, pen, clearColor);
        }

        /// <summary>
        /// 自作VS拡張機能Inspector2Dの内部で使用。基本的には使用禁止
        /// </summary>
        /// <param name="path"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="objs"></param>
        /// <param name="types"></param>
        public static void DrawAndSave(string path, int w, int h, object obj, string type, int param, int argb)
        {
            Color color = Color.FromArgb(argb);
            Bitmap bmp;
            if (type == "As Bitmap")
            {
                bmp = obj as Bitmap;
            }
            else
            {
                bmp = new Bitmap(w, h);
            }
            try
            {
                if (type == "As Bitmap")
                {
                }
                else
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        switch (type)
                        {
                            case "As Lines":
                                dynamic lines = obj;
                                g.DrawLines(new Pen(color, param), lines.ToArray());
                                break;
                            case "As Points":
                                try
                                {
                                    // Point型の場合
                                    dynamic pt = obj;
                                    float x = pt.X;
                                    float y = pt.Y;
                                    float size = param;
                                    g.FillRectangle(new SolidBrush(color), x - size / 2, y - size / 2, size, size);
                                }
                                catch
                                {
                                    // IEnumerable<Point>の場合
                                    dynamic pts = obj;
                                    for (int j = 0; j < pts.ToArray().Length; j++)
                                    {
                                        float x = pts[j].X;
                                        float y = pts[j].Y;
                                        float size = param;
                                        g.FillRectangle(new SolidBrush(color), x - size / 2, y - size / 2, size, size);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                bmp.Save(path);
            }
        }
    }
}