using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using FrooxEngine.LogiX;
using BaseX;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Web;

namespace LogixCompExport
{
    public class LogixCompExport : NeosMod
    {
        public override string Name => "LogixCompExport";
        public override string Author => "kka429";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("dev.kokoa.logixcompexport");
            harmony.PatchAll();
            Msg("Loading");
        }

        // TODO JSON serialise
        [HarmonyPatch(typeof(World))]
        [HarmonyPatch("RunUserSpawn")]
        class Patch
        {
            static bool Prefix(User user)
            {
                File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\LogiX.json", "[\n");
                File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\Component.json", "[\n");
                Msg("UserSpawn");
                // getNodes("LogiX", user);
                getComponents("", user);
                File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\LogiX.json", "\n]");
                File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\Component.json", "\n]");
                var process = Process.GetCurrentProcess();
                process.Kill();
                return true;
            }
        }

        public static void getComponents(string path, User user)
        {
            CategoryNode<System.Type> categoryNode;
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                categoryNode = WorkerInitializer.ComponentLibrary;
            }
            else
            {
                categoryNode = WorkerInitializer.ComponentLibrary.GetSubcategory(path);
                if (categoryNode == null)
                {
                    categoryNode = WorkerInitializer.ComponentLibrary;
                    path = "";
                }
            }
            if (categoryNode != WorkerInitializer.ComponentLibrary)
            {
                LocaleString localeString = (LocaleString)"< (back)";
                string path1 = categoryNode.Parent.GetPath();
            }
            foreach (CategoryNode<System.Type> subcategory in categoryNode.Subcategories)
            {
                LocaleString localeString = (LocaleString)(subcategory.Name + " >");
                ref LocaleString local11 = ref localeString;
                string str = path + "/" + subcategory.Name;
                // Msg(str);
                getComponents(str, user);
            }
            LocaleString niceName;

            string result = "";
            foreach (System.Type element in categoryNode.Elements)
            {
                // File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\Comp.json", element.FullName);

                result += "{\n";
                if (element.IsGenericTypeDefinition)
                {
                    niceName = (LocaleString)element.GetNiceName();
                    string str = Path.Combine(path, element.Name);
                    Msg(str);
                    Msg(element.FullName);

                    result += $"\"name\": \"{niceName.ToString().Replace("\\", "\\\\")}\",\n";
                    result += $"\"pathName\": \"{path}/{niceName.ToString().Replace("\\", "\\\\")}\",\n";
                    result += $"\"fullName\": \"{element.FullName.Replace("\\", "\\\\")}\",\n";


                    result += $"\"genericTypes\": [\n";
                    foreach (System.Type commonGenericType in WorkerInitializer.GetCommonGenericTypes(element))
                    {
                        try
                        {
                            if (!WorkerManager.IsValidGenericType(commonGenericType, true))
                                continue;
                        }
                        catch (Exception ex)
                        {
                            UniLog.Warning("Exception checking validity of a generic type: " + commonGenericType?.ToString() + "for " + element?.ToString() + "\n" + ex?.ToString());
                            continue;
                        }
                        Msg(commonGenericType.GetNiceName());
                        string str2 = TypeHelper.TryGetAlias(commonGenericType) ?? commonGenericType.FullName;
                        Msg(str2);
                        result += $"{{ \"type\": \"{str2}\"}},";
                    }
                    result = result.Substring(0, result.Length - 1);
                    result += $"]\n";
                }
                else
                {
                    niceName = (LocaleString)element.GetNiceName();
                    string fullName = element.FullName;
                    string str = Path.Combine(path, niceName.ToString());

                    result += $"\"name\": \"{niceName.ToString().Replace("\\", "\\\\")}\",\n";
                    result += $"\"pathName\": \"{path}/{niceName.ToString().Replace("\\", "\\\\")}\",\n";
                    result += $"\"fullName\": \"{element.FullName.Replace("\\", "\\\\")}\",\n";
                    // Msg("Name: " + str);
                    // Msg("Class: " + element.FullName);
                    var test = Activator.CreateInstance(element);
                    PropertyInfo mi = element.GetProperty("SyncMemberCount");
                    // MethodInfo getSyncMember = element.GetMethod("GetSyncMember");
                    int o = (int)mi.GetValue(test, null);

                    try
                    {
                        MethodInfo init = element.GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        // Msg(init == null ? "init: null" : "init: null janai");
                        if(init != null)
                        {
                            init.Invoke(test, new Object[2] { (Component)test, true }); 
                            // Msg("init : done");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Msg("init get failed \n" + ex);
                    }

                    MethodInfo initilizeSyncMembers = element.GetMethod("InitializeSyncMembers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    initilizeSyncMembers.Invoke(test, null);
                    // Msg("initilizeSyncMembers done");
                    MethodInfo onAwake = element.GetMethod("OnAwake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    //Msg(onAwake == null ? "null" : "null janai");
                    try
                    {
                        onAwake.Invoke(test, null);
                        //Msg("awake done");
                    }
                    catch (Exception ex)
                    {
                        //Msg("awake sippai\n" + ex.Message);
                    }

                    result += $"\"syncmembers\": [\n";
                    for (int i = 0; i < o; i++)
                    {
                        //// ISyncMember im = (ISyncMember)getSyncMember.Invoke(test,new Object[] { i });
                        try
                        {
                            // test.InitializeSyncMembers();
                            ISyncMember syncMember = ((Component)test).GetSyncMember(i);
                            if (syncMember != null)
                            {
                                string name = ((Component)test).GetSyncMemberName(i);
                                FieldInfo t = ((Component)test).GetSyncMemberFieldInfo(i);
                                FieldInfo nameField = element.GetField(name);
                                //Msg(name);
                                //Msg(t.FieldType);
                                //Msg("default: " + t.GetValue(test));
                                string d = t.GetValue(test).ToString();
                                string ty = t.FieldType.ToString();

                                switch (syncMember)
                                {
                                    case SyncObject syncObject:
                                        result += $"{{ \"name\": \"{name}\" , \"type\": \"{t.FieldType}\", \"default\": \"{ Regex.Escape(d).Replace("\\", "\\\\") }\"}},";
                                        break;
                                    case IField field:
                                        result += $"{{ \"name\": \"{name}\" , \"type\": \"{t.FieldType}\", \"default\": \"{ d }\"}},";
                                        break;
                                }

                            } else
                            {
                                //Msg("null");
                            }
                        }
                        catch
                        {
                            //Msg("Error");
                        }
                    }
                    result = result.Substring(0, result.Length - 1);
                    result += $"]\n";
                }

                result += $"}},\n";
            }
            File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\Component.json", result);
        }

        // TODO JSON Seriarize!
        public static void getNodes(string path, User user)
        {
            List<ElementListing> list = Pool.BorrowList<ElementListing>();
            CategoryNode<System.Type> subcategory1 = WorkerInitializer.ComponentLibrary.GetSubcategory(path);

            foreach (CategoryNode<System.Type> subcategory2 in subcategory1.Subcategories)
            {
                if ((subcategory2.Name == "Experimental")) continue;

                // フォルダ
                string str = path + "/" + (LocaleString)subcategory2.Name;
                getNodes(str, user);
                // Msg(str);
            }
            foreach (System.Type element in subcategory1.Elements)
            {
                if (!LogixHelper.IsHidden(element))
                {
                    string overloadName = LogixHelper.GetOverloadName(element);
                    System.Type type = element;
                    list.Add(new ElementListing(LogixHelper.GetNodeName(type), type));
                }
            }
            // list.Sort();

            foreach (ElementListing elementListing in list)
            {
                System.Type type = elementListing.type;
                // System.Type nodeVisualType = LogixHelper.GetNodeVisualType(type);

                string str = "";

                {
                    str += "{\n";
                    LocaleString name = (LocaleString)elementListing.name;
                    str += $"\"name\": \"{name.ToString().Replace("\\","\\\\")}\",\n";
                    str += $"\"pathName\": \"{path}/{name.ToString().Replace("\\", "\\\\")}\",\n";
                    string fullName = type.FullName;
                    str += $"\"fullName\": \"{fullName}\",\n";

                    var tt = LogixHelper.ExtractNodeTypes(elementListing.type);


                    str += $"\"inputs\": [\n";
                    foreach (KeyValuePair<string, Type> input in tt.inputs)
                    {
                        str += $"{{ \"name\": \"{input.Key}\" , \"value\": \"{input.Value}\"}},";
                    }
                    str = str.Substring(0, str.Length - 1);
                    str += $"],\n";

                    str += $"\"outputs\": [\n";
                    foreach (KeyValuePair<string, Type> output in tt.outputs)
                    {
                        str += $"{{ \"name\": \"{output.Key}\" , \"value\": \"{output.Value}\"}},";
                    }
                    str = str.Substring(0, str.Length - 1);
                    str += $"],\n";

                    var test = elementListing.type.GetMethods();
                    str += $"\"ImpulseInputs\": [\n";
                    foreach (var t in test)
                    {
                        var attrs = t.CustomAttributes;
                        if (attrs.Count() == 0) continue;
                        if (attrs.First().AttributeType.Equals(typeof(ImpulseTarget)))
                        {
                            str += $"{{ \"name\": \"{t.Name}\"}},";
                        }
                    }
                    str = str.Substring(0, str.Length - 1);
                    str += $"],\n";

                    str += $"\"ImpulseOutputs\": [\n";
                    var test2 = elementListing.type.GetFields();
                    foreach (var t in test2)
                    {
                        if (t.FieldType.Equals(typeof(Impulse)))
                        {
                            str += $"{{ \"name\": \"{t.Name}\"}},";
                        }
                    }
                    str = str.Substring(0, str.Length - 1);

                    if (type.IsGenericTypeDefinition && WorkerInitializer.GetCommonGenericTypes(type).Any<System.Type>())
                    {
                        str += $"],\n";
                        str += $"\"types\": [\n";
                        try
                        {
                            Msg(path);
                            Msg($"/{path}\\{name}");
                            // /LogiX/Variables/Storage\FrooxEngine.LogiX.Data.ValueRegister`1
                            var x = WorkerManager.GetType(PathUtility.GetFileName($"/{path}\\{fullName}"));
                            
                            foreach (System.Type commonGenericType in WorkerInitializer.GetCommonGenericTypes(x))
                            {
                                System.Type genericTypeArgument = commonGenericType.GenericTypeArguments[0];
                                // UIBuilder uiBuilder6 = uiBuilder1;
                                LocaleString niceName = (LocaleString)genericTypeArgument.GetNiceName();
                                // ref LocaleString local9 = ref niceName;
                                string str2 = TypeHelper.TryGetAlias(commonGenericType) ?? commonGenericType.FullName;
                                str += $"{{ \"name\": \"{niceName}\", \"fullName\":\"{str2}\"}},";
                            }
                            str = str.Substring(0,str.Length - 1);
                        }
                        catch (Exception ex)
                        {
                            Msg("ERR");
                            str += $"{{ \"name\": \"error\",\"fullName\": \"{ex}\":}},";
                        }
                        str += $"]\n";
                    } else
                    {
                        str += $"]\n";
                    }


                    str += $"}},\n";
                }

                File.AppendAllText(@"C:\Users\kokoa\Documents\LogiX\LogiX.json", str);
            }

            Pool.Return<ElementListing>(ref list);

            return;
        }

        private readonly struct ElementListing : IComparable<ElementListing>, IEquatable<ElementListing>
        {
            public readonly string name;
            public readonly System.Type type;

            public ElementListing(string name, System.Type type)
            {
                this.name = name;
                this.type = type;
            }

            public int CompareTo(ElementListing other) => this.name.CompareTo(other.name);

            public bool Equals(ElementListing other) => this.name == other.name && this.type == other.type;
        }

        private static int GetTypeRank(System.Type type)
        {
            System.Type type1 = type;
            if (typeof(IVector).IsAssignableFrom(type))
                type1 = type.GetVectorBaseType();
            if (type1 == typeof(dummy))
                return 0;
            if (type1 == typeof(float))
                return 1;
            return type1 == typeof(int) ? 2 : type.GetTypeRank() + 3;
        }

    }

}
