using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace FLib.Tests
{
    [TestClass()]
    public class FMathTests
    {
        [TestMethod()]
        public void CrossPointTest()
        {
            // 垂直
            var cross1 = FMath.CrossPoint(new PointF(0, 0), new PointF(100, 0), new PointF(30, -50), new PointF(30, 50));
            Assert.AreEqual(cross1.X, 30, 1e-4);
            Assert.AreEqual(cross1.Y, 0, 1e-4);

            // ななめ
            var cross2 = FMath.CrossPoint(new PointF(10, 10), new PointF(110, 110), new PointF(40, 60), new PointF(60, 40));
            Assert.AreEqual(cross2.X, 50, 1e-4);
            Assert.AreEqual(cross2.Y, 50, 1e-4);

            // 端点1
            var cross3 = FMath.CrossPoint(new PointF(10, 10), new PointF(110, 110), new PointF(0, 10), new PointF(20, 10));
            Assert.AreEqual(cross3.X, 10, 1e-4);
            Assert.AreEqual(cross3.Y, 10, 1e-4);

            // 端点2
            var cross4 = FMath.CrossPoint(new PointF(10, 10), new PointF(110, 110), new PointF(210, 1200), new PointF(210, 0));
            Assert.AreEqual(cross4.X, 210, 1e-4);
            Assert.AreEqual(cross4.Y, 210, 1e-4);

            // 平行
            var cross5 = FMath.CrossPoint(new PointF(10, 10), new PointF(110, 110), new PointF(-100, 10), new PointF(-10, 100));
            Assert.AreEqual(cross5.X, 0);
            Assert.AreEqual(cross5.Y, 0);
        }
    }
}
