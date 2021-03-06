﻿// Copyright 2013-2020 Dirk Lemstra <https://github.com/dlemstra/Magick.NET/>
//
// Licensed under the ImageMagick License (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
//
//   https://www.imagemagick.org/script/license.php
//
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
// either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.IO;
using System.Linq;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if Q8
using QuantumType = System.Byte;
#elif Q16
using QuantumType = System.UInt16;
#elif Q16HDRI
using QuantumType = System.Single;
#else
#error Not implemented!
#endif

namespace Magick.NET.Tests
{
    [TestClass]
    public partial class MagickImageCollectionTests
    {
        [TestMethod]
        public void Test_CopyTo()
        {
            using (var collection = new MagickImageCollection())
            {
                collection.Add(new MagickImage(Files.SnakewarePNG));
                collection.Add(new MagickImage(Files.RoseSparkleGIF));

                MagickImage[] images = new MagickImage[collection.Count];
                collection.CopyTo(images, 0);

                Assert.AreEqual(collection[0], images[0]);
                Assert.AreNotEqual(collection[0], images[1]);

                collection.CopyTo(images, 1);
                Assert.AreEqual(collection[0], images[0]);
                Assert.AreEqual(collection[0], images[1]);

                images = new MagickImage[collection.Count + 1];
                collection.CopyTo(images, 0);

                images = new MagickImage[1];
                collection.CopyTo(images, 0);

                ExceptionAssert.Throws<ArgumentNullException>("array", () =>
                {
                    collection.CopyTo(null, -1);
                });

                ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    collection.CopyTo(images, -1);
                });
            }
        }

        [TestMethod]
        public void Test_Deconstruct()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Deconstruct();
                });

                collection.Add(new MagickImage(MagickColors.Red, 20, 20));

                using (var frames = new MagickImageCollection())
                {
                    frames.Add(new MagickImage(MagickColors.Red, 10, 20));
                    frames.Add(new MagickImage(MagickColors.Purple, 10, 20));

                    collection.Add(frames.AppendHorizontally());
                }

                Assert.AreEqual(20, collection[1].Width);
                Assert.AreEqual(20, collection[1].Height);
                Assert.AreEqual(new MagickGeometry(0, 0, 10, 20), collection[1].Page);
                ColorAssert.AreEqual(MagickColors.Red, collection[1], 3, 3);

                collection.Deconstruct();

                Assert.AreEqual(10, collection[1].Width);
                Assert.AreEqual(20, collection[1].Height);
                Assert.AreEqual(new MagickGeometry(10, 0, 10, 20), collection[1].Page);
                ColorAssert.AreEqual(MagickColors.Purple, collection[1], 3, 3);
            }
        }

        [TestMethod]
        public void Test_Evaluate()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Evaluate(EvaluateOperator.Exponential);
                });

                collection.Add(new MagickImage(MagickColors.Yellow, 40, 10));

                using (var frames = new MagickImageCollection())
                {
                    frames.Add(new MagickImage(MagickColors.Green, 10, 10));
                    frames.Add(new MagickImage(MagickColors.White, 10, 10));
                    frames.Add(new MagickImage(MagickColors.Black, 10, 10));
                    frames.Add(new MagickImage(MagickColors.Yellow, 10, 10));

                    collection.Add(frames.AppendHorizontally());
                }

                using (var image = collection.Evaluate(EvaluateOperator.Min))
                {
                    ColorAssert.AreEqual(MagickColors.Green, image, 0, 0);
                    ColorAssert.AreEqual(MagickColors.Yellow, image, 10, 0);
                    ColorAssert.AreEqual(MagickColors.Black, image, 20, 0);
                    ColorAssert.AreEqual(MagickColors.Yellow, image, 30, 0);
                }
            }
        }

        [TestMethod]
        public void Test_Flatten()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Flatten();
                });

                collection.Add(new MagickImage(MagickColors.Brown, 10, 10));
                var center = new MagickImage(MagickColors.Fuchsia, 4, 4);
                center.Page = new MagickGeometry(3, 3, 4, 4);
                collection.Add(center);

                using (var image = collection.Flatten())
                {
                    ColorAssert.AreEqual(MagickColors.Brown, image, 0, 0);
                    ColorAssert.AreEqual(MagickColors.Fuchsia, image, 5, 5);
                }
            }
        }

        [TestMethod]
        public void Test_Index()
        {
            using (var collection = new MagickImageCollection(Files.RoseSparkleGIF))
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    collection[i].Resize(35, 23);
                    Assert.AreEqual(35, collection[i].Width);

                    collection[i] = collection[i];
                    Assert.AreEqual(35, collection[i].Width);
                }
            }
        }

        [TestMethod]
        public void Test_Merge()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Merge();
                });

                collection.Read(Files.RoseSparkleGIF);

                using (var first = collection.Merge())
                {
                    Assert.AreEqual(collection[0].Width, first.Width);
                    Assert.AreEqual(collection[0].Height, first.Height);
                }
            }
        }

        [TestMethod]
        public void Test_Montage()
        {
            using (var collection = new MagickImageCollection())
            {
                var settings = new MontageSettings();
                settings.Geometry = new MagickGeometry(string.Format("{0}x{1}", 200, 200));
                settings.TileGeometry = new MagickGeometry(string.Format("{0}x", 2));

                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Montage(settings);
                });

                for (int i = 0; i < 9; i++)
                    collection.Add(Files.Builtin.Logo);

                using (var montageResult = collection.Montage(settings))
                {
                    Assert.IsNotNull(montageResult);
                    Assert.AreEqual(400, montageResult.Width);
                    Assert.AreEqual(1000, montageResult.Height);
                }
            }
        }

        [TestMethod]
        public void Test_Morph()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Morph(10);
                });

                collection.Add(Files.Builtin.Logo);

                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Morph(10);
                });

                collection.AddRange(Files.Builtin.Wizard);

                collection.Morph(4);
                Assert.AreEqual(6, collection.Count);
            }
        }

        [TestMethod]
        public void Test_Mosaic()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.Mosaic();
                });

                collection.Add(Files.SnakewarePNG);
                collection.Add(Files.ImageMagickJPG);

                using (var mosaic = collection.Mosaic())
                {
                    Assert.AreEqual(286, mosaic.Width);
                    Assert.AreEqual(118, mosaic.Height);
                }
            }
        }

        [TestMethod]
        public void Test_Smush()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.SmushHorizontal(5);
                });

                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.SmushVertical(6);
                });

                collection.AddRange(Files.RoseSparkleGIF);

                using (var image = collection.SmushHorizontal(20))
                {
                    Assert.AreEqual((70 * 3) + (20 * 2), image.Width);
                    Assert.AreEqual(46, image.Height);
                }

                using (var image = collection.SmushVertical(40))
                {
                    Assert.AreEqual(70, image.Width);
                    Assert.AreEqual((46 * 3) + (40 * 2), image.Height);
                }
            }
        }

        [TestMethod]
        public void Test_ReadSettings()
        {
            var settings = new MagickReadSettings();
            settings.FontFamily = "Courier New";
            settings.FillColor = MagickColors.Gold;
            settings.FontPointsize = 80;
            settings.Format = MagickFormat.Text;
            settings.TextGravity = Gravity.Center;

            using (var images = new MagickImageCollection(Files.ImageMagickTXT, settings))
            {
                Assert.AreEqual(2, images.Count);
                ColorAssert.AreEqual(MagickColors.Gold, images[0], 348, 648);
            }

            using (var images = new MagickImageCollection())
            {
                images.Ping(Files.ImageMagickTXT, settings);

                Assert.AreEqual(2, images.Count);

                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    ColorAssert.AreEqual(MagickColors.Gold, images[0], 348, 648);
                });
            }
        }

        [TestMethod]
        public void Test_Remove()
        {
            using (var collection = new MagickImageCollection(Files.RoseSparkleGIF))
            {
                var first = collection[0];
                collection.Remove(first);

                Assert.AreEqual(2, collection.Count);
                Assert.AreEqual(-1, collection.IndexOf(first));

                first = collection[0];
                collection.RemoveAt(0);

                EnumerableAssert.IsSingle(collection);
                Assert.AreEqual(-1, collection.IndexOf(first));
            }
        }

        [TestMethod]
        public void Test_RePage()
        {
            using (var collection = new MagickImageCollection(Files.RoseSparkleGIF))
            {
                collection[0].Page = new MagickGeometry("0x0+10+20");

                Assert.AreEqual(10, collection[0].Page.X);
                Assert.AreEqual(20, collection[0].Page.Y);

                collection[0].Settings.Page = new MagickGeometry("0x0+10+20");

                Assert.AreEqual(10, collection[0].Settings.Page.X);
                Assert.AreEqual(20, collection[0].Settings.Page.Y);

                collection.RePage();

                Assert.AreEqual(0, collection[0].Page.X);
                Assert.AreEqual(0, collection[0].Page.Y);

                Assert.AreEqual(10, collection[0].Settings.Page.X);
                Assert.AreEqual(20, collection[0].Settings.Page.Y);
            }
        }

        [TestMethod]
        public void Test_Reverse()
        {
            using (var collection = new MagickImageCollection(Files.RoseSparkleGIF))
            {
                var first = collection.First();
                collection.Reverse();

                var last = collection.Last();
                Assert.IsTrue(last == first);
            }
        }

        [TestMethod]
        public void Test_ToBase64()
        {
            using (var collection = new MagickImageCollection())
            {
                Assert.AreEqual(string.Empty, collection.ToBase64());

                collection.Read(Files.Builtin.Logo);
                Assert.AreEqual(1228800, collection.ToBase64(MagickFormat.Rgb).Length);
            }
        }

        [TestMethod]
        public void Test_TrimBounds()
        {
            using (var collection = new MagickImageCollection())
            {
                ExceptionAssert.Throws<InvalidOperationException>(() =>
                {
                    collection.TrimBounds();
                });

                collection.Add(Files.Builtin.Logo);
                collection.Add(Files.Builtin.Wizard);
                collection.TrimBounds();

                Assert.AreEqual(640, collection[0].Page.Width);
                Assert.AreEqual(640, collection[0].Page.Height);
                Assert.AreEqual(0, collection[0].Page.X);
                Assert.AreEqual(0, collection[0].Page.Y);

                Assert.AreEqual(640, collection[1].Page.Width);
                Assert.AreEqual(640, collection[1].Page.Height);
                Assert.AreEqual(0, collection[0].Page.X);
                Assert.AreEqual(0, collection[0].Page.Y);
            }
        }

        [TestMethod]
        public void Test_Warning()
        {
            var count = 0;
            EventHandler<WarningEventArgs> warningDelegate = (sender, arguments) =>
            {
                Assert.IsNotNull(sender);
                Assert.IsNotNull(arguments);
                Assert.IsNotNull(arguments.Message);
                Assert.IsNotNull(arguments.Exception);
                Assert.AreNotEqual(string.Empty, arguments.Message);

                count++;
            };

            using (var collection = new MagickImageCollection())
            {
                collection.Warning += warningDelegate;
                collection.Read(Files.EightBimTIF);

                Assert.AreNotEqual(0, count);

                int expectedCount = count;
                collection.Warning -= warningDelegate;
                collection.Read(Files.EightBimTIF);

                Assert.AreEqual(expectedCount, count);
            }
        }

        [TestMethod]
        public void Test_Write()
        {
            var fileSize = new FileInfo(Files.RoseSparkleGIF).Length;
            Assert.AreEqual(fileSize, 9891);

            using (var collection = new MagickImageCollection(Files.RoseSparkleGIF))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    collection.Write(memStream);

                    Assert.AreEqual(fileSize, memStream.Length);
                }
            }

            var tempFile = new FileInfo(Path.GetTempFileName() + ".gif");
            try
            {
                using (var collection = new MagickImageCollection(Files.RoseSparkleGIF))
                {
                    collection.Write(tempFile);

                    Assert.AreEqual(fileSize, tempFile.Length);
                }
            }
            finally
            {
                Cleanup.DeleteFile(tempFile);
            }
        }
    }
}
