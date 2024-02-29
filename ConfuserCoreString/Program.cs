using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace ConfuserCoreString
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("= confusertool /experimental version/ =");
            Console.WriteLine("Confuser Core string decrypt tool."); 
            
            var file = string.Join(" ", args);

            if (file.Length == 0)
                file = Path.Combine(Directory.GetCurrentDirectory(), "obfuscated.exe");

            var module = ModuleDefMD.Load(file);
            var assembly = Assembly.LoadFile(file);  
            var assemblyModule = assembly.GetModules().FirstOrDefault();
            
            foreach (var type in module.Types)
            {
                var methods = CollectMethods(type);
                foreach (var method in methods)
                {
                    if (!method.HasBody || !method.Body.HasInstructions)
                        continue;

                    var matches = method.MatchPosMulti(new[]
                    {
                        Code.Ldc_I4,
                        Code.Call
                    });
                    
                    var body = method.Body.Instructions;

                    foreach (var idx in matches)
                    {
                        //poor man signature check
                        var callOperand = body[idx + 1].Operand;

                        if (!(callOperand is MethodSpec spec))
                            continue;

                        if (!(spec.Instantiation is GenericInstMethodSig genericInstMethodSig))
                            continue;
                        
                        var mt = spec.ResolveMethodDef();

                        if (mt == null)
                            continue;
                        
                        if (mt.Parameters.Count == 0 || genericInstMethodSig.GenericArguments.Count == 0)
                            continue;
                        
                        if (mt.Parameters[0].Type.ToString() != "System.Int32")
                            continue;

                        if (genericInstMethodSig.GenericArguments[0].ToString() != "System.String")
                        {
                            //todo handle other type
                            Console.WriteLine(genericInstMethodSig.GenericArguments[0].ToString());
                            continue;
                        }

                        var methodBase = assemblyModule.ResolveMethod(mt.MDToken.ToInt32()) as MethodInfo;
                        var genericSignatureMt = methodBase.MakeGenericMethod(typeof(string));

                        var encryptedKey = Utility.GetLDCi32(body[idx]);
                        
                        var decrypted = genericSignatureMt.Invoke(null, new object[] { encryptedKey }).ToString();

                        body[idx].OpCode = OpCodes.Nop;
                        body[idx].Operand = null;
                        
                        body[idx + 1].OpCode = OpCodes.Ldstr;
                        body[idx + 1].Operand = decrypted;
                        
                        Console.WriteLine($"0x{encryptedKey:X2} -> {decrypted} @ 0x{method.MDToken.ToInt32():X2}");
                    }
                    
                }
            }

            var dstPath = Path.Combine(Path.GetDirectoryName(file),
                Path.GetFileNameWithoutExtension(file) + "_string" + Path.GetExtension(file));
            
            module.Write(dstPath, new ModuleWriterOptions(module)
            {
                MetadataOptions = new MetadataOptions(MetadataFlags.KeepOldMaxStack)
            });
            
            Console.WriteLine("Wrote file to disk!");
            
            Console.ReadLine();
        }

        private static List<MethodDef> CollectMethods(TypeDef type)
        {
            var list = new List<MethodDef>();

            list.AddRange(type.Methods);

            foreach (var nested in type.NestedTypes)
            {
                list.AddRange(CollectMethods(nested));
            }
            
            return list;
        }
    }
}