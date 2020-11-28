using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImageDataExtractor {
    internal class Program {
        public static void Main(string[] args) {
            foreach (var file in Directory.GetFiles(@"D:\LearningDataCropped\", "*.png", SearchOption.AllDirectories)) {
                using (var orig = new Bitmap(file)) {
                    var nstep = 6;
                    var xstep = orig.Width / nstep;
                    var ystep = orig.Height / nstep;
                    var epsilon = 1;
                    var blends = new List<uint>();
                    for (var y = ystep; y < orig.Height - nstep; y += ystep)
                    for (var x = xstep; x < orig.Width - nstep; x += xstep) {
                        uint rb = 0, gb = 0, bb = 0;
                        for (var xx = x - epsilon; xx <= x + epsilon; xx++)
                        for (var yy = y - epsilon; yy <= y + epsilon; yy++) {
                            var px = orig.GetPixel(xx, yy);
                            rb += px.R;
                            gb += px.G;
                            bb += px.B;
                        }

                        var nPoints = (2 * epsilon + 1) * (2 * epsilon + 1);
                        blends.Add(((uint) (((rb / nPoints) & 0xffu) << 16)) | ((uint) (((gb / nPoints) & 0xffu) << 8)) | ((uint) (((bb / nPoints) & 0xffu))));
                    }

                    Console.Write(Path.GetFileName(Path.GetDirectoryName(file)));
                    /*Console.Write("\t");
                    Console.Write(blends.Count());*/
                    foreach (var b in blends) {
                        Console.Write("\t0x10");
                        Console.Write(b.ToString("x6"));
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}
