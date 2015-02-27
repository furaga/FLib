using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib
{
    public class FileManager
    {
        public static void OpenExplorer(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(System.IO.Path.GetFullPath(path));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + e.StackTrace);
            }
        }

        public static string RelativePath(string filePath, string basePath)
        {
            // http://dobon.net/vb/dotnet/file/getabsolutepath.html　を参考に実装

            filePath = System.IO.Path.GetFullPath(filePath);
            basePath = System.IO.Path.GetFullPath(basePath);

            //"%"を"%25"に変換しておく（デコード対策）
            basePath = basePath.Replace("%", "%25");
            filePath = filePath.Replace("%", "%25");

            //相対パスを取得する
            Uri u1 = new Uri(basePath);
            Uri u2 = new Uri(filePath);
            Uri relativeUri = u1.MakeRelativeUri(u2);
            string relativePath = relativeUri.ToString();

            //URLデコードする（エンコード対策）
            relativePath = Uri.UnescapeDataString(relativePath);

            //"%25"を"%"に戻す
            relativePath = relativePath.Replace("%25", "%");

            return relativePath;
        }
    }
}
