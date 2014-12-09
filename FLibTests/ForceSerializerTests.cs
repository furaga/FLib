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
    public class ForceSerializerTests
    {
        [TestMethod()]
        public void SerializeTest()
        {
            // PatchworkLib.PatchSkeletalMeshのシリアライズができるか確認する
            // オブジェクトをシリアライズした結果と、
            // それをデシリアライズしてもう一回シリアライズしたものが同じになるかをテスト

            // シリアライズ
            var dict = Magic2D.SegmentToPatch.LoadPatches(".", "../../../../../../Patchwork_resources/GJ_ED3_Lite/3_segmentation", null, 2);

            for (int i = 0; i < 4; i++)
            {
                //                if (i != 3)
                //                  continue;

                var patch = dict.ElementAt(i).Value;
                patch.mesh.BeginDeformation();
                ForceSerializer.Serialize(patch, "./patch", "_patch");

                // デシリアライズしてもういちどシリアライズ
                var obj = ForceSerializer.Deserialize("./patch", "_patch", typeof(PatchworkLib.PatchMesh.PatchSkeletalMesh));
                ForceSerializer.Serialize(obj, "./patch_deserialized", "_patch");

                // 結果が同じか判定
                var diffList_vartypes = Diff("./patch/_patch_vartypes.txt", "./patch_deserialized/_patch_vartypes.txt");
                Assert.AreEqual(diffList_vartypes.Count, 0);

                var diffList_varnames = Diff("./patch/_patch_varnames.txt", "./patch_deserialized/_patch_varnames.txt");
                Assert.AreEqual(diffList_varnames.Count, 0);

                var diffList_values = DiffWithDeserialization("./patch/_patch.xml", "./patch_deserialized/_patch.xml");
                Assert.AreEqual(diffList_values.Count, 0);
            }
        }

        Dictionary<int, Tuple<string, string>> Diff(string filepath1, string filepath2)
        {
            var diffList = new Dictionary<int, Tuple<string, string>>();

            using (var sr1 = new System.IO.StreamReader(filepath1))
            using (var sr2 = new System.IO.StreamReader(filepath2))
            {
                int idx = 0;
                while (true)
                {
                    string l1 = sr1.ReadLine();
                    string l2 = sr2.ReadLine();
                    if (l1 == null && l2 == null)
                        break;
                    if (l1 != l2)
                        diffList[idx + 1] = new Tuple<string, string>(l1, l2);
                    idx++;
                }
            }

            return diffList;
        }

        Dictionary<int, Tuple<string, string>> DiffWithDeserialization(string filepath1, string filepath2)
        {
            var diffList = new Dictionary<int, Tuple<string, string>>();

            using (var sr1 = new System.IO.StreamReader(filepath1))
            using (var sr2 = new System.IO.StreamReader(filepath2))
            {
                int idx = 0;
                while (true)
                {
                    string l1 = sr1.ReadLine();
                    string l2 = sr2.ReadLine();
                    if (l1 == null && l2 == null)
                        break;

                    // 同じ文字列だったらOK
                    if (l1 == l2)
                    {
                        idx++;
                        continue;
                    }

                    // 異なる文字列でも,小数点以下の違いならOK
                    int start = 0;
                    while (l1[start] == l2[start])
                        start++;

                    string diffStr1 = "";
                    int i = start;
                    while (true)
                    {
                        if (char.IsNumber(l1[i]) || l1[i] == '.' || l1[i] == '-')
                        {
                            diffStr1 += l1[i];
                            i++;
                            continue;
                        }
                        break;
                    }

                    string diffStr2 = "";
                    i = start;
                    while (true)
                    {
                        if (char.IsNumber(l2[i]) || l2[i] == '.' || l2[i] == '-')
                        {
                            diffStr2 += l2[i];
                            i++;
                            continue;
                        }
                        break;
                    }

                    float diffVal1F, diffVal2F;
                    if (float.TryParse(diffStr1, out diffVal1F) && float.TryParse(diffStr2, out diffVal2F))
                    {
                        // 差が1e-4以下ならOK
                        if (Math.Abs(diffVal1F - diffVal2F) <= 1e-4)
                        {
                            idx++;
                            continue;
                        }
                    }

                    diffList[idx + 1] = new Tuple<string, string>(l1, l2);

                    idx++;
                }
            }

            return diffList;
        }

    }
}