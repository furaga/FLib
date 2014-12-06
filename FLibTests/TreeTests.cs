using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FLib.Tests
{
    [TestClass()]
    public class TreeTests
    {
        [TestMethod()]
        public void TreeTest()
        {
            Tree<string> tree = new Tree<string>("0");
            tree.Add(new Tree<string>("1a"));
            tree.Add(new Tree<string>("1b"));
            tree.Add(new Tree<string>("1c"));

            var children = tree.CopyChildren();
            Assert.AreEqual(tree.Value, "0");
            Assert.AreEqual(children[0].Value, "1a");
            Assert.AreEqual(children[1].Value, "1b");
            Assert.AreEqual(children[2].Value, "1c");

            tree.Apply((val, parent) => parent.Value + val, val => val);

            children = tree.CopyChildren();
            Assert.AreEqual(tree.Value, "0");
            Assert.AreEqual(children[0].Value, "01a");
            Assert.AreEqual(children[1].Value, "01b");
            Assert.AreEqual(children[2].Value, "01c");

        }
    }
}
