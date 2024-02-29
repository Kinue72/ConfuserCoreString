using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

// aftery my belove
namespace ConfuserCoreString
{
    public static class Extensions
    {
         public static List<int> MatchPosMulti(this MethodDef method, Code[] code)
        {
            var list = new List<int>();
            if (!method.HasBody || !method.Body.HasInstructions) return list;
            for (var i = 0; i < method.Body.Instructions.Count - code.Length + 1; i++)
            {
                var found = true;
                for (var j = 0; found && j < code.Length; j++)
                    found = code[j] == Code.UNKNOWN1
                            || code[j] == Code.UNKNOWN2
                            && Utility.GetLDCi32(method.Body.Instructions[i + j]) != int.MinValue
                            || code[j] == method.Body.Instructions[i + j].OpCode.Code;
                if (found) list.Add(i);
            }

            return list;
        }
    }
}