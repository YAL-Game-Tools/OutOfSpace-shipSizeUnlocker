using System;
using System.Linq;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

/// @author YellowAfterlife
namespace OutOfSpace_shipSizeUnlocker {
	class Program {
        static FieldDefinition GetField(ModuleDefinition module, string type, string name) {
            return module.GetType(type).Fields.First(m => m.Name == name);
        }
        static MethodDefinition GetMethod(ModuleDefinition module, string type, string name) {
            return module.GetType(type).Methods.First(m => m.Name == name);
        }
        static void Patch_InitializeCarrousel(ModuleDefinition module) {
            var method = GetMethod(module, "PlayerSelectUI", "InitializeCarrousel");
            var proccessor = method.Body.GetILProcessor();
            var instructions = method.Body.Instructions;

            // find "ANY" in ship size list initializer
            var strAnyAt = -1;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Ldstr) continue;
                var str = instruction.Operand as string;
                if (str != "ANY") continue;
                strAnyAt = i;
                break;
            }
            if (strAnyAt < 0) throw new Exception("Couldn't find where to add 'GIANT' ship type");
            var addCall = instructions[strAnyAt + 1];
            var funcAdd = addCall.Operand as MethodReference;

            // add "GIANT" before it
            var insertBefore = instructions[strAnyAt - 1];
            proccessor.InsertBefore(insertBefore, proccessor.Create(OpCodes.Ldloc, 0));
            proccessor.InsertBefore(insertBefore, proccessor.Create(OpCodes.Ldstr, "GIANT"));
            proccessor.InsertBefore(insertBefore, proccessor.Create(OpCodes.Callvirt, funcAdd));
        }
        static void Patch_SetWeeklyChallenge(ModuleDefinition module) {
            var method = GetMethod(module, "PlayerSelectUI", "SetWeeklyChallenge");
            var proccessor = method.Body.GetILProcessor();
            var instructions = method.Body.Instructions;

            // replace "No" with ""
            var found = false;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Ldstr) continue;
                var str = instruction.Operand as string;
                if (str != "No") continue;
                instruction.Operand = "";
                found = true;
                break;
            }
            if (!found) throw new Exception("Couldn't find 'No' in SetWeeklyChallenge - is the game already patched?");

            // replace jump-if-false by jump-if-true
            found = false;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Brfalse) continue;
                instructions[i] = proccessor.Create(OpCodes.Brtrue, instruction.Operand as Instruction);
                found = true;
                break;
            }
            if (!found) throw new Exception("Couldn't find jump instruction in SetWeeklyChallenge");

            // remove the [now-unnecessary] call to InitializeCarrousel
            var InitializeCarrousel = GetMethod(module, "PlayerSelectUI", "InitializeCarrousel");
            found = false;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Call) continue;
                var instrFunc = instruction.Operand as MethodReference;
                if (instrFunc != InitializeCarrousel) continue;
                instructions[i - 1] = proccessor.Create(OpCodes.Nop);
                instructions[i] = proccessor.Create(OpCodes.Nop);
                found = true;
                break;
            }
            if (!found) throw new Exception("Couldn't find InitializeCarrousel call in SetWeeklyChallenge");
        }
        static void Patch_StartGame(ModuleDefinition module) {
            var method = GetMethod(module, "PlayerSelectUI", "StartGame");
            var proccessor = method.Body.GetILProcessor();
            var instructions = method.Body.Instructions;

            // I regret to inform that this function creates an enumerator
            var found = false;
            do {
                var newobj = instructions[0];
                if (newobj.OpCode.Code != Code.Newobj) continue;
                var coroutineConstructor = newobj.Operand as MethodReference;
                var coroutineClass = coroutineConstructor.DeclaringType.Resolve();
                method = coroutineClass.Methods.First(m => m.Name == "MoveNext");
                proccessor = method.Body.GetILProcessor();
                instructions = method.Body.Instructions;
                found = true;
            } while (false);
            if (!found) throw new Exception("Couldn't find coroutine construction in StartGame");

            // strip all `local = "LARGE"` to allow other ship sizes
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Ldstr) continue;
                var str = instruction.Operand as string;
                if (str != "LARGE") continue;
                var next = instructions[i + 1];
                if (!next.OpCode.Code.ToString().ToLower().Contains("stloc")) continue;
                instructions[i + 1] = proccessor.Create(OpCodes.Pop);
            }
        }
        const string dir = @"Out of Space_Data\Managed";
        const string path = dir + @"\Assembly-CSharp.dll";
        const string backupDir = dir + @"\shipSizeUnlocker backup";
        const string backup = backupDir + @"\Assembly-CSharp.dll";
        const string next = dir + @"\Assembly-CSharp-New.dll";
        static void Main_1() {
            Console.WriteLine("Making a backup...");
            //
            Console.WriteLine("Patching...");
            //
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(dir);
            var readerParams = new ReaderParameters();
            readerParams.AssemblyResolver = resolver;
            //
            using (var module = ModuleDefinition.ReadModule(path, readerParams)) {
                Patch_SetWeeklyChallenge(module);
                Patch_InitializeCarrousel(module);
                Patch_StartGame(module);
                //
                Console.WriteLine("Saving...");
                module.Write(next);
                //
                module.Dispose();
            }
#if !DEBUG
            if (!Directory.Exists(backupDir)) {
                Directory.CreateDirectory(backupDir);
            } else {
                if (File.Exists(backup)) File.Delete(backup);
            }
            File.Copy(path, backup);
            File.Delete(path);
            File.Move(next, path);
#endif
            //
            Console.WriteLine("All good! You can run the game now.");
            Console.WriteLine("(and get rid of the patcher's files)");
        }
        static void Main(string[] args) {
            if (!File.Exists(path)) {
                Console.WriteLine("Please extract into the game's directory before running.");
            } else {
#if DEBUG
                Main_1();
#else
                try {
                    Main_1();
                } catch (Exception e) {
                    Console.WriteLine("An error occurred: " + e);
                }
#endif
            }
            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }
	}
}
