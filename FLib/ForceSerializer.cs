using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;

namespace FLib
{
    /// <summary>
    /// リフレクションを使ってシリアライズできないクラスオブジェクトを無理やり（擬似的に）シリアライズする
    /// 参照関係などは当然崩れる。
    /// 正確さはいらないからさっと実行中のデータを保存して、テストとかに使いたい場合に役立つ
    /// </summary>
    public class ForceSerializer
    {
        const System.Reflection.BindingFlags reflectionFlg =
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static;

        [Serializable]
        class SerializeTreeNode
        {
            public string varName;
            public Type varType;
            public Object varValue;
            public List<SerializeTreeNode> nodes = new List<SerializeTreeNode>();

            // 他のTreeの参照を使う
            public void SetReference(SerializeTreeNode refTree)
            {
                this.isRefer = true;
                this.refTree = refTree;
            }
            public void ResetReference()
            {
                this.isRefer = false;
                this.refTree = null;
            }
            public bool isRefer { get; private set; }
            public SerializeTreeNode refTree { get; private set; }
            public string fullPath = "";
        }

        //--------------------------------------------------------------------------------------
        //
        // 保存
        //
        //--------------------------------------------------------------------------------------

        public static void Serialize(string savePath, Object obj, string filename)
        {
            var obj2tree = new Dictionary<object, SerializeTreeNode>();
            var tree = MakeSerializable(obj, filename);

            // ディレクトリを作る
            if (Directory.Exists(savePath))
                ;
            else
                Directory.CreateDirectory(savePath);

            // 各ノード値と変数名をファイルに書き出す
            using (Stream xmlstream = File.Open(Path.Combine(savePath, filename + ".xml"), FileMode.Create))
            using (Stream varNameStream = File.Open(Path.Combine(savePath, filename + "_varnames.txt"), FileMode.Create))
            using (var varNameWriter = new StreamWriter(varNameStream))
            using (Stream varTypeStream = File.Open(Path.Combine(savePath, filename + "_vartypes.txt"), FileMode.Create))
            using (var varTypeWriter = new StreamWriter(varTypeStream))
            {
                string xmlDir = Path.GetFullPath(Path.Combine(savePath, filename));
                Save(xmlstream, varNameWriter, varTypeWriter, "", tree);
            }
        }

        static SerializeTreeNode MakeSerializable(Object obj, string name)
        {
            var obj2tree = new Dictionary<Object, SerializeTreeNode>();
            var data = ConvertToSerializeTreeNode(obj, name, 0, obj2tree);
            return data;
        }

        static List<SerializeTreeNode> BreakupObject(object obj, int depth, Dictionary<Object, SerializeTreeNode> obj2tree)
        {
            var data = new List<SerializeTreeNode>();
            Type type = obj.GetType();
            System.Reflection.FieldInfo[] fields = type.GetFields(reflectionFlg);
            foreach (var f in fields)
                data.Add(ConvertToSerializeTreeNode(f.GetValue(obj), f.Name, depth, obj2tree));
            return data;
        }

        static SerializeTreeNode ConvertToSerializeTreeNode(object obj, string name, int depth, Dictionary<Object, SerializeTreeNode> obj2tree)
        {
            SerializeTreeNode subtree = new SerializeTreeNode();

            if (obj == null)
                return subtree;

            subtree.varType = obj.GetType();
            subtree.varName = name;
            subtree.varValue = obj;

            // 既存のオブジェクトを参照している場合。その旨を記録して戻る
            if (subtree.varType.FullName != typeof(string).FullName && subtree.varType.IsClass && obj2tree.ContainsKey(subtree.varValue))
            {
                subtree.SetReference(obj2tree[subtree.varValue]);
                return subtree;
            }
            else
            {
                obj2tree[subtree.varValue] = subtree;
            }

            // 列挙型か
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (IsIEnumerableType(obj.GetType()))
            {
                dynamic list = obj;
                int idx = 0;
                foreach (var e in list)
                {
                    subtree.nodes.Add(ConvertToSerializeTreeNode(e, GetIndexKey(name, idx, 0), depth, obj2tree));
                    idx++;
                }
                return subtree;
            }
            // シリアライズできるか
            else if (obj.GetType().IsSerializable)
            {
                if (obj.GetType().IsGenericType && obj.GetType().GetGenericArguments().Any(t => !t.IsSerializable))
                {
                    // 型引数のいずれかがserialize可能でなければxmlserializerでバグる
                }
                else
                {
                    //                    subtree.varValue = obj;
                    System.Diagnostics.Debug.Assert(subtree.varValue != null, "subtree.value is serializable but assigned null");
                    return subtree;
                }
            }


            // シリアライズできない場合、ある程度の深さまで探索して打ち切る
            if (depth < 5)
            {
                foreach (var e in BreakupObject(obj, depth + 1, obj2tree))
                    subtree.nodes.Add(e);
                return subtree;
            }

            return subtree;
        }

        static string GetIndexKey(string name, int idx, int typeArgIdx)
        {
            return string.Format("{0}[{1}]'{2}", name, idx, typeArgIdx);
        }

        // treeのとおりの階層構造をもつディレクトリとして各変数を保存する
        static void Save(Stream xmlStream, StreamWriter varNameWriter, StreamWriter varTypeWriter, string varPath, SerializeTreeNode tree)
        {
            if (tree.varName == null)
                return;

            string newPath = varPath + "." + tree.varName;
            tree.fullPath = newPath;

            bool isLeaf = tree.nodes == null || tree.nodes.Count <= 0;

            string refPrefix = tree.isRefer ? "R[" + tree.refTree.fullPath + "]:" : "";
            if (refPrefix.Length >= 1)
                isLeaf = false; // 他のオブジェクトを参照するなら葉として扱わない




            // 葉っぱならバイナリデータとして保存する
            if (isLeaf)
            {
                if (tree.varValue == null)
                    return;
                try
                {
                    // DataContact
                    new System.Runtime.Serialization.DataContractSerializer(tree.varType).WriteObject(xmlStream, tree.varValue);
                    xmlStream.WriteByte(10);

                    // TODO: reftable
                    varNameWriter.WriteLine(refPrefix + "L:" + newPath);
                    varTypeWriter.WriteLine(refPrefix + "L:" + tree.varType.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return;
            }
            else
            {
                try
                {
                    // TODO: reftable
                    if (tree.varValue != null)
                    {
                        varNameWriter.WriteLine(refPrefix + newPath);
                        varTypeWriter.WriteLine(refPrefix + tree.varType.ToString());
                    }

                    foreach (var n in tree.nodes)
                        Save(xmlStream, varNameWriter, varTypeWriter, newPath, n);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return;
            }
        }


        //--------------------------------------------------------------------------------------
        //
        // 復元
        //
        //--------------------------------------------------------------------------------------

        // TODO: この中間データ型を使わない実装にできるのでする
        private class Tree
        {
            public string rawpath = "";
            public Dictionary<string, Tree> nodes = new Dictionary<string, Tree>();
            public SerializeTreeNode tree;

            public Tree()
            {
                tree = new SerializeTreeNode();
            }

            public bool Add(string rawpath, SerializeTreeNode tree)
            {
                // 子供か
                if (rawpath.StartsWith(this.rawpath))
                {
                    string subpath = rawpath.Substring(this.rawpath.Length + 1);
                    string[] subtokens = subpath.Split('.');

                    if (subtokens.Length == 0)
                        return false;
                    else if (subtokens.Length == 1)
                    {
                        nodes[subtokens[0]] = new Tree()
                        {
                            rawpath = rawpath,
                            tree = tree,
                        };
                        return true;
                    }
                    else if (subtokens.Length >= 2)
                    {
                        if (nodes.ContainsKey(subtokens[0]))
                            return nodes[subtokens[0]].Add(rawpath, tree);
                    }

                }
                return false;
            }

            public void SetSerializeTreeNodeChildren()
            {
                if (nodes.Count <= 0)
                    return;
                foreach (var kv in nodes)
                {
                    kv.Value.SetSerializeTreeNodeChildren();
                    tree.nodes.Add(kv.Value.tree);
                }
            }

        }

        public static T Deserialize<T>(string dir, string filename)
            where T : class
        {
            return Deserialize(dir, filename, typeof(T)) as T;
        }

        public static Object /*T*/ Deserialize(string dir, string filename, Type type)
        {
            string valuePath = Path.Combine(dir, filename + ".xml");
            string namePath = Path.Combine(dir, filename + "_varnames.txt");
            string typePath = Path.Combine(dir, filename + "_vartypes.txt");

            using (var valueStream = File.OpenRead(valuePath))
            using (var nameStream = File.OpenRead(namePath))
            using (var nameReader = new StreamReader(nameStream))
            using (var typeStream = File.OpenRead(typePath))
            using (var typeReader = new StreamReader(typeStream))
            {
                var name2tree = new Dictionary<string, SerializeTreeNode>();
                SerializeTreeNode tree = BuildSerializeTree(Path.Combine(dir, filename), valueStream, nameReader, typeReader, name2tree);
                Object obj = null;
                if (tree.nodes.Count >= 1)
                {
                    if (tree.nodes[0].varType.FullName == type.FullName)
                        obj = CreateInstance(tree.nodes[0]);
                }
                return obj;
            }
        }

        static SerializeTreeNode BuildSerializeTree(string binDir, Stream valueStream, StreamReader nameReader, StreamReader typeReader, Dictionary<string, SerializeTreeNode> name2tree)
        {
            Dictionary<string, Type> name2type = new Dictionary<string, Type>();
            List<Type> genericTypes = new List<Type>();
            GetCurrentDomainAssembliesType(name2type, genericTypes);

            Tree nodeTreeInfo = new Tree();

            while (true)
            {
                string rawname = nameReader.ReadLine();
                string rawtypeName = typeReader.ReadLine();
                if (rawname == null || rawtypeName == null)
                    break;

                bool isLeaf = rawname.Contains("L:");
                string name = rawname.Split(':').Last();
                string typeName = rawtypeName.Split(':').Last();

                Type type = GetTypeFromName(typeName, name2type, genericTypes);

                var tree = new SerializeTreeNode();
                tree.varName = name.Split('.').Last();
                tree.varType = type;

                // 参照先のオブジェクト（を含むtreenode）を登録
                if (rawname.StartsWith("R["))
                {
                    string refName = rawname.Split(':').First().Substring(2).Trim().Trim('[', ']').Trim();
                    if (name2tree.ContainsKey(refName))
                        tree.SetReference(name2tree[refName]);
                }

                name2tree[name] = tree;

                // 葉ならXMLファイルを読んで値を復元
                if (isLeaf)
                {
                    if (type == null)
                        type = typeof(KeyValuePair<string, System.Drawing.PointF>);
                    tree.varValue = ReadDataContactObject(valueStream, type);
                }

                nodeTreeInfo.Add(name, tree);
            }

            nodeTreeInfo.SetSerializeTreeNodeChildren();

            return nodeTreeInfo.tree;
        }

        static Object ReadDataContactObject(Stream valueStream, Type type)
        {
            Stream stream = new MemoryStream();
            StreamWriter sw = new StreamWriter(stream);

            while (true)
            {
                // 一文字ずつ読んでいって、"<?"かEOFが来たら今まで読まれた文字列をXMLデシリアライズする
                int b = valueStream.ReadByte();
                if (b != -1)
                    stream.WriteByte((byte)b);
                if (b == 10) // '\n'
                {
                    // XMLデシリアライズ
                    //for debug
                    stream.Seek(0, SeekOrigin.Begin);
                    var content = new StreamReader(stream).ReadToEnd();

                    stream.Seek(0, SeekOrigin.Begin);
                    var obj = new System.Runtime.Serialization.DataContractSerializer(type).ReadObject(stream);
                    return obj;
                }
                if (b == -1) // EOF
                    return null;
            }
        }

        //--------------------------------------------------------------------
        //
        // インスタンスの生成
        //
        //--------------------------------------------------------------------

        static Object CreateInstance<T>(Type type, IEnumerable<T> content)
        {
            Object obj = new Object();

            if (content != null)
            {
                obj = Activator.CreateInstance(type, content);
            }

            return obj;
        }

        static Object CreateInstance(Type type)
        {
            Object obj = new Object();

            if (type.IsArray)
            {
                var elemType = type.GetElementType();
                var rank = type.GetArrayRank();
                if (rank <= 1)
                {
                    obj = Array.CreateInstance(elemType, 0);
                }
                else
                {
                    obj = Array.CreateInstance(elemType, new int[rank]);
                }
            }
            else if (type.IsValueType)
            {
                try
                {
                    obj = FormatterServices.GetUninitializedObject(type);
                }
                catch
                {
                    if (type.FullName == typeof(string).FullName)
                        obj = "";
                }
            }
            else
            {
                var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
                // 引数のないconstuctorのときはuninitializeを使う
                if (defaultConstructor == null)
                {
                    try
                    {
                        if (type.FullName == typeof(string).FullName)
                            obj = "";
                        else
                            obj = FormatterServices.GetUninitializedObject(type);

                    }
                    catch
                    {
                    }
                }
                else
                {

                    try
                    {
                        obj = Activator.CreateInstance(type);
                    }
                    catch
                    {
                        try
                        {
                            obj = Activator.CreateInstance(type, null);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return obj;
        }

        private static Object CreateInstance(SerializeTreeNode tree)
        {
            if (tree == null || tree.varType == null || tree.varName == null)
                return null;

            bool isLeaf = tree.nodes == null || tree.nodes.Count <= 0;



            if (tree.isRefer)
            {
                var refObj = tree.refTree.varValue;
                tree.varValue = refObj;
                return refObj;
            }




            if (isLeaf)
                return tree.varValue;

            // 葉以外ならインスタンスを作ってリフレクションで各メンバに変数を代入
            var obj = CreateInstance(tree.varType);
            if (obj == null)
                return null;

            if (IsIEnumerableType(tree.varType))
            {
                dynamic list = obj;

                // 配列型の場合要素の追加が難しいので
                // いったんListに全部の値を入れて、最後にList.ToArray()で配列を作る
                Type t_type = tree.varType.IsArray ? typeof(List<>).MakeGenericType(tree.varType.GetElementType()) : null;
                dynamic t_list_a = t_type == null ? null : CreateInstance(t_type);

                // 列挙型に値を追加していく
                foreach (var n in tree.nodes)
                {
                    try
                    {

                        dynamic val = CreateInstance(n);
                        if (list is string)
                            list += val;
                        else if (tree.varType.IsArray)
                        {
                            if (t_list_a != null)
                                t_list_a.Add(val);
                        }
                        else if (IsIList(tree.varType) || IsISet(tree.varType))
                            list.Add(val);
                        else if (IsIDictionary(tree.varType))
                            list[val.Key] = val.Value;
                        else
                            list = list.Concat(new[] { val });
                    }
                    catch
                    {
                        Console.WriteLine("Failed to add an element to IEnumerable object");
                    }
                }

                if (tree.varType.IsArray)
                {
                    list = t_list_a.ToArray();
                }

                obj = list;
            }
            else
            {
                // 列挙型でないならリフレクションで各メンバ変数に値を入れる
                System.Reflection.FieldInfo[] fields = tree.varType.GetFields(reflectionFlg);

                Dictionary<string, System.Reflection.FieldInfo> varName2f = fields.ToDictionary(f => f.Name, f => f);

                foreach (var n in tree.nodes)
                {
                    if (!varName2f.ContainsKey(n.varName))
                        continue;
                    var val = CreateInstance(n);
                    varName2f[n.varName].SetValue(obj, val);
                }
            }


            // objの値をtreenodeに保存。あとで参照される場合に使う。
            // treeが葉かはtree.nodesの要素数で判定するのでvarValueがnullになってなくても問題ない

            if (tree.varType.FullName.Contains("Joint"))
            {
                var xxx = 0;
            }

            tree.varValue = obj;


            return obj;
        }

        static IEnumerable<T> Append<T>(IEnumerable<T> list, T elem)
        {
            return list.Concat(new[] { elem });
        }

        static IEnumerable<KeyValuePair<T1, T2>> Append<T1, T2>(IEnumerable<KeyValuePair<T1, T2>> list, T1 key, T2 value)
        {
            return list.Concat(new[] { new KeyValuePair<T1, T2>(key, value) });
        }

        //--------------------------------------------------------------------------------------
        //
        // typeの判定
        //
        //--------------------------------------------------------------------------------------

        static bool IsIEnumerableType(Type type)
        {
            if (type == null)
                return false;
            return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
        static bool IsIList(Type type)
        {
            if (type == null)
                return false;
            return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));
        }
        static bool IsISet(Type type)
        {
            if (type == null)
                return false;
            return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISet<>));
        }
        static bool IsIDictionary(Type type)
        {
            if (type == null)
                return false;
            return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        //--------------------------------------------------------------------
        //
        // type name -> type
        //
        //--------------------------------------------------------------------

        static readonly System.Text.RegularExpressions.Regex arraySuffix = new System.Text.RegularExpressions.Regex(@"\[,*\]$", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static Type GetTypeFromName(String name, Dictionary<string, Type> name2type, List<Type> genericTypes)
        {
            // 通常の型の場合
            if (name2type.ContainsKey(name))
                return name2type[name];

            bool isGeneric = false;
            List<Type> argtypes = new List<Type>();

            // 配列の場合"System.Single[]"      
            if (name.EndsWith("]"))
            {
                var matches = arraySuffix.Matches(name);
                if (matches.Count >= 2)
                    return null;
                else if (matches.Count == 1)
                {
                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        string prefix = name.Substring(0, m.Index);
                        string suffix = name.Substring(m.Index);

                        Type prefixType = GetTypeFromName(prefix, name2type, genericTypes);
                        int rank = suffix.Count(c => c == ',') + 1;
                        Type arrayType = rank == 1 ? prefixType.MakeArrayType() : prefixType.MakeArrayType(rank);

                        return arrayType;
                    }
                }
            }

            // ジェネリック型のとき。"T`2[T1,T2,T3,...]"
            foreach (Type type in genericTypes)
            {
                // []の中身を個別にTypeになおしてジェネリック型を作る
                if (type.IsGenericType && name.StartsWith(type.FullName + "["))
                {
                    string subtypename = name.Substring(type.FullName.Length + 1);

                    int nest = 0;
                    List<string> argtypenames = new List<string>();
                    int pos = 0;
                    int startPos = 0;
                    while (true)
                    {
                        bool finish = false;
                        switch (subtypename[pos])
                        {
                            case '[':
                                nest++;
                                break;
                            case ']':
                                nest--;
                                if (nest < 0)
                                {
                                    finish = true;
                                    argtypenames.Add(subtypename.Substring(startPos, pos - startPos));
                                    startPos = pos + 1;
                                }
                                break;
                            case ',':
                                argtypenames.Add(subtypename.Substring(startPos, pos - startPos));
                                startPos = pos + 1;
                                break;
                        }

                        if (finish)
                            break;

                        pos++;
                    }

                    var genType = type.MakeGenericType(argtypenames.Select(typename => GetTypeFromName(typename, name2type, genericTypes)).ToArray());
                    Console.WriteLine(genType.ToString());
                    return genType;
                }

                //                if (String.Equals(type.FullName, name) || String.Equals(type.AssemblyQualifiedName, name))
                //                  return type;
            }

            return null;
        }

        public static void GetCurrentDomainAssembliesType(Dictionary<string, Type> name2type, List<Type> genericTypes)
        {
            name2type.Clear();
            genericTypes.Clear();

            System.Reflection.Assembly[] assemblies;
            assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    name2type[t.FullName] = t;
                    if (t.IsGenericType)
                        genericTypes.Add(t);
                }
            }
        }
    }
}
