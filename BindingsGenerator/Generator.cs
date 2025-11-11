using CppAst;
using CppAst.CodeGen.Common;
using CppAst.CodeGen.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zio.FileSystems;

namespace BindingsGenerator
{
    public static class Generator
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Generating bindings...");

            try
            {
                AcceptHeaderDirectory("Headers\\Dawn", "WebGPU", "WebGPU.NET.Dawn", "WebGPU.WGPU", "webgpu_native_dll_name", "WebGPU.NET.Dawn");
                AcceptHeaderDirectory("Headers\\wgpu_native", "WebGPU", "WebGPU.NET.Wgpu", "WebGPU.WGPU", "webgpu_native_dll_name", "WebGPU.NET.Wgpu");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to generate bindings:");
                Console.WriteLine(e);
                Console.ReadKey();
            }

            Console.WriteLine("Done. Press any key to exit");
            Console.ReadKey();
        }

        public static void AcceptHeaderDirectory(string sourceHeaderDirectory, string outputClass, string outputNamespace, string outputFileName, string dllImportName, string outputTargetProjectName)
        {
            //获取当前程序的运行路径
            var exeFolder = Path.GetDirectoryName(typeof(BindingsGenerator.Generator).Assembly.Location);

            //如过c语言头文件目录不存在，则返回
            if (!Directory.Exists(Path.Combine(exeFolder, sourceHeaderDirectory)))
                return;

            //查找从cppsharp的nuget包附带安装的clang头文件输出位置
            string findIncludeFolder(string rootFolder)
            {
                foreach (var folder in Directory.EnumerateDirectories(rootFolder))
                {
                    if (folder.Contains("include"))
                    {
                        return folder;
                    }
                    else
                    {
                        var f = findIncludeFolder(folder);
                        if (f == string.Empty)
                            continue;
                        else
                            return f;
                    }
                }

                return string.Empty;
            }

            Console.WriteLine($"Searching .h in: {sourceHeaderDirectory}");

            CSharpConverterOptions options = new CSharpConverterOptions()
            {
                DefaultNamespace = outputNamespace,
                DefaultClassLib = outputClass,
                DefaultOutputFilePath = outputFileName + ".cs",
                GenerateEnumItemAsFields = false,
                TypedefCodeGenKind = CppTypedefCodeGenKind.NoWrap,
                DefaultDllImportNameAndArguments = dllImportName,
                ParseMacros = true,//企图用它能分析typedef WGPUFlags WGPUBufferUsage;定义的枚举，但没用
            };

            options.MappingRules.Add(
                    r => r.MapAll<CppEnumItem>().CSharpAction((converter, element) =>
                    {
                        if (element is not CSharpEnumItem item)
                            return;

                        // replace literals like unchecked((int)0) with just 0
                        if (item.Value.StartsWith("unchecked((int)"))
                            item.Value = item.Value[15..^1];

                        CSharpEnum parent = item.Parent as CSharpEnum;

                        // remove redundancy in enum members names
                        string enumName = parent.Name;

                        if (item.Name.StartsWith(enumName))
                            item.Name = item.Name.Substring(enumName.Length);//为枚举Item删去带有枚举名的前缀

                        // this line is only really needed specifically for the WGPUInstanceBackend enum；即删去枚举值前缀
                        if (item.Value.Contains("WGPU" + parent.Name + "_"))
                            item.Value = item.Value.Replace("WGPU" + parent.Name + "_", "");
                        if (item.Value.Contains(parent.Name + "_"))
                            item.Value = item.Value.Replace(parent.Name + "_", "");

                        // at this point a few enums will be named 1D, 2D or 3D which is invalid, so we fix it
                        if (item.Name.StartsWith("1D"))
                            item.Name = "OneDimension";
                        else if (item.Name.StartsWith("2DArray"))
                            item.Name = "TwoDimensionalArray";
                        else if (item.Name.StartsWith("2D"))
                            item.Name = "TwoDimensions";
                        else if (item.Name.StartsWith("3D"))
                            item.Name = "ThreeDimensions";
                    })
                    );
            options.MappingRules.Add(
                    //对C#的名称移除_
                    r => r.MapAll<CppElement>().CppAction((converter, element) =>
                     {
                         if (element is not ICppMember member)
                             return;

                         //if (member.Name.StartsWith("WGPU"))
                         //member.Name = member.Name[4..];

                         member.Name = member.Name.Replace("_", "");
                     }));

            options.MappingRules.Add(

                    //避免结构中的指针类型被转换
                    r => r.MapAll<CppElement>().CSharpAction((converter, element) =>
                     {
                         if (element is not CSharpField member)
                             return;

                         if ((member.Parent as CSharpStruct)?.Name == "WGPUChainedStructOut")
                         {
                         }

                         CSharpType csharpType;
                         if (member.FieldType is CSharpTypeWithAttributes)// 如char *转换时会带有属性
                         {
                             csharpType = (member.FieldType as CSharpTypeWithAttributes).ElementType;
                         }
                         else
                         {
                             csharpType = member.FieldType;
                         }

                         if (csharpType is CSharpPrimitiveType)
                         {
                             var cppElement = member.CppElement;

                             if (cppElement is CppField)
                             {
                                 var cppField = cppElement as CppField;
                                 if (cppField.Type is CppPointerType)
                                 {
                                     var cppPointerType = cppField.Type as CppPointerType;
                                     if (cppPointerType.ElementType is CppQualifiedType)//const *
                                     {
                                         var cppQualifiedType = cppPointerType.ElementType as CppQualifiedType;
                                         if (cppQualifiedType.ElementType is CppClass)// WGPUChainedStruct const *
                                         {
                                             var cppClass = cppQualifiedType.ElementType as CppClass;
                                             var fieldTypeName = cppClass.Name;
                                             member.FieldType = new CSharpFreeType("unsafe " + fieldTypeName + "*");
                                         }
                                         else if (cppQualifiedType.ElementType is CppPrimitiveType)// char const *
                                         {
                                             var cppPrimitiveType = cppQualifiedType.ElementType as CppPrimitiveType;
                                             if (cppPrimitiveType.Kind == CppPrimitiveKind.Char)
                                             {
                                                 member.FieldType = new CSharpFreeType("unsafe " + "byte" + "*");
                                             }
                                         }
                                         else if (cppQualifiedType.ElementType is CppEnum)
                                         {
                                             var cppEnum = cppQualifiedType.ElementType as CppEnum;
                                             var fieldTypeName = cppEnum.Name;
                                             member.FieldType = new CSharpFreeType("unsafe " + fieldTypeName + "*");
                                         }
                                     }
                                     else if (cppPointerType.ElementType is CppClass)// WGPUChainedStructOut *
                                     {
                                         var cppClass = cppPointerType.ElementType as CppClass;
                                         var fieldTypeName = cppClass.Name;
                                         member.FieldType = new CSharpFreeType("unsafe " + fieldTypeName + "*");
                                     }
                                 }
                             }
                         }
                     }));
            options.MappingRules.Add(

                    //设置方法入口点和方法名删除wgpu前缀
                    r => r.MapAll<CppFunction>().CSharpAction((converter, element) =>
                     {
                         if (element is not CSharpMethod method)
                             return;

                         if (method.Name.StartsWith("wgpu"))
                         {
                             (method.Attributes[0] as CSharpLibraryImportAttribute)?.EntryPoint = $"\"{method.Name}\""; //入口点;
                                                                                                                        //method.Name = method.Name[4..]; //删除wgpu前缀
                         }
                     }));
            options.MappingRules.Add(

                    //函数中的指针依旧使用指针传递
                    r => r.MapAll<CppFunction>().CSharpAction((converter, element) =>
                     {
                         if (element is not CSharpMethod method)
                             return;

                         //if (method.Name == "InstanceEnumerateAdapters")
                         {
                             bool hasPointerTypeInParameter = false;
                             for (var index = 0; index < method.Parameters.Count; index++)
                             {
                                 var parameter = method.Parameters[index];
                                 if (parameter.CppElement is CppAst.CppParameter cppParameter)
                                 {
                                     //如果参数是指针类型
                                     if (cppParameter.Type is CppAst.CppPointerType cppPointerType)
                                     {
                                         if (parameter.ParameterType is CSharpRefType cSharpRefType)
                                         {
                                             if (cSharpRefType.ElementType is CSharpStruct csharpStruct)
                                             {
                                                 var typeName = csharpStruct.Name;
                                                 parameter.ParameterType = new CSharpFreeType(typeName + "*");
                                                 hasPointerTypeInParameter = true;
                                             }
                                         }
                                     }
                                 }
                             }
                             if (hasPointerTypeInParameter) method.Modifiers = method.Modifiers | CSharpModifiers.Unsafe;
                         }
                     }));

            var PreProcessing_typedef_WGPUFlags_result = PreProcessing_typedef_WGPUFlags(exeFolder, sourceHeaderDirectory);

            options.MappingRules.Add(
                   // 使用typedef WGPUFlags定义的枚举是无符号64位整数
                   r => r.MapAll<CppEnum>().CppAction((converter, element) =>
                   {
                       if (element is not CppEnum item)
                           return;
                       if (PreProcessing_typedef_WGPUFlags_result.typedef_WGPUFlags.Contains(item.Name))
                       {
                           item.IntegerType = CppPrimitiveType.UnsignedLongLong;
                       }
                   }));
            options.MappingRules.Add(
                   // 使用size_t定义的使用nint
                   r => r.MapAll<CppElement>().CSharpAction((converter, element) =>
                   {
                       if (element is CSharpField cSharpField)
                       {
                           if (cSharpField.FieldType.CppElement is CppTypedef cppTypedef)
                           {
                               if (cppTypedef.FullName == "size_t" || cppTypedef.FullName == "sizet")
                               {
                                   cSharpField.FieldType = CustomType.@nuint;
                               }
                           }
                       }else if(element is CSharpParameter cSharpParameter)
                       {
                           if (cSharpParameter.ParameterType.CppElement is CppTypedef cppTypedef)
                           {
                               if (cppTypedef.FullName == "size_t" || cppTypedef.FullName == "sizet")
                               {
                                   cSharpParameter.ParameterType = CustomType.@nuint;
                               }
                           }
                       }
                   }));
            options.MappingRules.Add(
                    //让生成的包含Impl结尾结构名去除Impl
                    r => r.MapAll<CppElement>().CSharpAction((converter, element) =>
                    {
                        if (element is CSharpStruct member)
                        {
                            if (member.Name.EndsWith("Impl"))
                            {
                                member.Name = member.Name.Substring(0, member.Name.Length - "Impl".Length);
                            }
                        }
                    }));

            //要编译的头文件所依赖的头文件
            options.IncludeFolders.AddRange(sourceHeaderDirectory);
            var FakeCStdHeaders = Path.Combine(exeFolder, "FakeCStdHeaders");
            options.AdditionalArguments.Add("-I" + FakeCStdHeaders);

            //要编译的头文件
            CSharpCompilation compilation = CSharpConverter.Convert(PreProcessing_typedef_WGPUFlags_result.targetFiles, options);

            //打印转换的错误
            if (compilation.HasErrors)
            {
                Console.WriteLine("Failed to generate bindings due to:");

                foreach (CppDiagnosticMessage message in compilation.Diagnostics.Messages)
                    if (message.Type == CppLogMessageType.Error)
                        Console.WriteLine(message);

                Console.ReadKey();
                return;
            }

            //输出转换结果到文件
            using PhysicalFileSystem fileSystem = new PhysicalFileSystem();
            using SubFileSystem subFileSystem = new SubFileSystem(fileSystem, fileSystem.ConvertPathFromInternal(Path.Combine(FindFolderUpwards(FindFolderUpwards(exeFolder, "BindingsGenerator")), outputTargetProjectName, "Generated")));
            CodeWriter writer = new CodeWriter(new CodeWriterOptions(subFileSystem));

            compilation.DumpTo(writer);
        }

        /// <summary>
        /// 参考<see cref="CSharpPrimitiveType"/>实现
        /// </summary>
        class CustomType : CSharpType
        {
            public static CustomType @nuint = new CustomType("nuint");

            public CustomType(string typeName)
            {
                TypeName = typeName;
            }

            string TypeName;

            public override void DumpTo(CodeWriter writer)
            {
                writer.Write(TypeName);
            }

            public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
        }

        /// <summary>
        /// 预处理使用typedef_WGPUFlags定义的枚举，方法返回处理后的文件列表和处理的所有枚举名
        /// </summary>
        /// <param name="exeFolder"></param>
        /// <param name="sourceHeaderDirectory"></param>
        /// <returns></returns>
        private static (List<string> targetFiles, List<string> typedef_WGPUFlags) PreProcessing_typedef_WGPUFlags(string exeFolder, string sourceHeaderDirectory)
        {
            var typedefWGPUFlagsList = new List<string>();
            var preProcessResultFileList = new List<string>();
            /*
             * 把类似typedef WGPUFlags WGPUMapMode;定义的东西转为c++枚举
             */
            var headerFiles = Directory.GetFiles(Path.Combine(exeFolder, sourceHeaderDirectory));
            for (var index = 0; index < headerFiles.Length; index++)
            {
                var filePath = headerFiles[index];

                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    StringBuilder result = new StringBuilder();

                    bool startNewEnum = false;
                    string enumName = "";
                    foreach (var line in lines)
                    {
                        string newLine = line;

                        //上一个枚举结尾
                        if (startNewEnum == true && line.Contains("typedef"))
                        {
                            startNewEnum = false;
                            result.AppendLine($"}} {enumName} WGPU_ENUM_ATTRIBUTE;");
                            enumName = "";
                        }

                        if (line.Contains("typedef WGPUFlags"))
                        {
                            startNewEnum = true;
                            enumName = line.Split("WGPUFlags")[1].Split(";")[0].Trim();
                            newLine = $"typedef enum {enumName} {{";
                            typedefWGPUFlagsList.Add(enumName);
                        }
                        else
                        {
                            if (startNewEnum)
                            {
                                if (line.Contains("static const"))
                                {
                                    newLine = line.Split(enumName + " ")[1].Split(";")[0] + ",";
                                }
                            }
                        }

                        result.AppendLine(newLine);
                    }

                    // 写到临时文件
                    var preProcessFileResultPath = Path.Combine(exeFolder, "Headers-temp", sourceHeaderDirectory, Path.GetFileName(filePath));
                    if (!File.Exists(preProcessFileResultPath))
                    {
                        // 1. 获取文件所在的目录
                        string directoryPath = Path.GetDirectoryName(preProcessFileResultPath);

                        // 2. 如果目录不存在，创建目录
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        File.Create(preProcessFileResultPath).Close();
                    }
                    File.WriteAllText(preProcessFileResultPath, result.ToString());
                    preProcessResultFileList.Add(preProcessFileResultPath);
                }
            }

            return (preProcessResultFileList, typedefWGPUFlagsList);
        }

        /// <summary>
        /// 向上找到目标文件夹，或者向上一层
        /// </summary>
        /// <param name="startDir"></param>
        /// <param name="targetFolderName"></param>
        /// <returns></returns>
        private static string FindFolderUpwards(string startDir, string targetFolderName = null)
        {
            var dir = new DirectoryInfo(startDir);

            if (targetFolderName != null)
            {
                while (dir != null)
                {
                    if (dir.Name.Equals(targetFolderName, StringComparison.OrdinalIgnoreCase))
                        return dir.FullName;

                    dir = dir.Parent;
                }
            }
            else
            {
                return dir.Parent.FullName;
            }

            return null; // 没找到
        }
    }
}