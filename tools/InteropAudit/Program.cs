using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text.Json;

if (args.Length < 2 || args.Length > 3) throw new ArgumentException("Usage: InteropAudit runtime.dll host-directory");
var runtimePath = Path.GetFullPath(args[0]);
var interop = Path.GetFullPath(Path.Combine(args[1], "BepInEx", "interop"));
var resolver = new DefaultAssemblyResolver();
foreach (var path in new[] { interop, Path.Combine(args[1], "BepInEx", "core"), Path.GetDirectoryName(runtimePath), Path.GetDirectoryName(typeof(object).Assembly.Location) }) resolver.AddSearchDirectory(path);
using var module = ModuleDefinition.ReadModule(runtimePath, new ReaderParameters { AssemblyResolver = resolver });
if (args.Length == 3)
{
    foreach (var reference in module.AssemblyReferences)
    {
        var type = resolver.Resolve(reference).MainModule.GetType(args[2]);
        if (type == null) continue;
        Console.WriteLine(JsonSerializer.Serialize(new { type = type.FullName, fields = type.Fields.Select(f => f.FullName).ToArray(), properties = type.Properties.Select(p => p.FullName).ToArray(), methods = type.Methods.Select(m => m.FullName).ToArray() }, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }
    throw new ArgumentException("Type not found: " + args[2]);
}IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots) => roots.SelectMany(t => new[] { t }.Concat(Types(t.NestedTypes)));
var queue = new Queue<(MethodReference method, string path, int depth)>();
foreach (var method in Types(module.Types).SelectMany(t => t.Methods).Where(m => m.HasBody))
    foreach (var instruction in method.Body.Instructions)
        if (instruction.Operand is MethodReference called && (instruction.OpCode.Code == Code.Call || instruction.OpCode.Code == Code.Callvirt || instruction.OpCode.Code == Code.Newobj))
            queue.Enqueue((called, method.FullName, 0));
var visited = new HashSet<string>();
var issues = new List<object>();
var unresolved = new HashSet<string>();
while (queue.TryDequeue(out var item))
{
    var key = item.method.DeclaringType.Scope + ":" + item.method.FullName;
    if (!visited.Add(key)) continue;
    if (item.method.DeclaringType.FullName.StartsWith("Il2CppSystem.Span`1", StringComparison.Ordinal) ||
        item.method.DeclaringType.FullName.StartsWith("Il2CppSystem.ReadOnlySpan`1", StringComparison.Ordinal))
    {
        issues.Add(new { caller = item.path, unsafeSpan = item.method.FullName, depth = item.depth });
        continue;
    }
    MethodDefinition method;
    try { method = item.method.Resolve(); }
    catch { unresolved.Add(item.method.DeclaringType.Scope + ":" + item.method.FullName); continue; }
    if (method == null || !method.HasBody) continue;
    var inInterop = Path.GetFullPath(method.Module.FileName).StartsWith(interop + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    if (!inInterop) continue;
    if (method.Body.Instructions.Any(i => i.OpCode.Code == Code.Ldstr && i.Operand is string text && text.Contains("Method unstripping failed", StringComparison.Ordinal)))
    {
        issues.Add(new { caller = item.path, stub = method.FullName, depth = item.depth });
        continue;
    }
    if (item.depth >= 6) continue;
    foreach (var instruction in method.Body.Instructions)
        if (instruction.Operand is MethodReference called && (instruction.OpCode.Code == Code.Call || instruction.OpCode.Code == Code.Callvirt || instruction.OpCode.Code == Code.Newobj))
            queue.Enqueue((called, item.path + " -> " + method.FullName, item.depth + 1));
}
var nativeBase = Path.Combine(args[1], "NeuroMita_Data", "Plugins", "x86_64");
var imports = Types(module.Types).SelectMany(t => t.Methods).Where(m => m.IsPInvokeImpl).Select(m => new { library = m.PInvokeInfo.Module.Name, entry = m.PInvokeInfo.EntryPoint }).Distinct().ToArray();
var missingExports = new List<string>();
foreach (var group in imports.GroupBy(i => i.library))
{
    string file = Path.Combine(nativeBase, group.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? group.Key : group.Key + ".dll");
    if (!File.Exists(file)) { missingExports.Add("Library absent: " + file); continue; }
    using var stream = File.OpenRead(file);
    using var pe = new System.Reflection.PortableExecutable.PEReader(stream);
    int exportRva = pe.PEHeaders.PEHeader.ExportTableDirectory.RelativeVirtualAddress;
    var header = pe.GetSectionData(exportRva).GetReader();
    header.Offset = 24;
    int count = header.ReadInt32();
    header.ReadInt32();
    int namesRva = header.ReadInt32();
    var names = pe.GetSectionData(namesRva).GetReader();
    var exports = new HashSet<string>();
    for (int i = 0; i < count; i++)
    {
        int nameRva = names.ReadInt32();
        var bytes = pe.GetSectionData(nameRva).GetReader();
        var name = new System.Text.StringBuilder();
        byte next;
        while ((next = bytes.ReadByte()) != 0) name.Append((char)next);
        exports.Add(name.ToString());
    }
    foreach (var import in group) if (!exports.Contains(import.entry)) missingExports.Add(import.library + ":" + import.entry);
}Console.WriteLine(JsonSerializer.Serialize(new { visited = visited.Count, unresolved = unresolved.OrderBy(x => x).ToArray(), issues, nativeImports = imports.Length, missingExports }, new JsonSerializerOptions { WriteIndented = true }));
Environment.ExitCode = issues.Count > 0 || unresolved.Count > 0 || missingExports.Count > 0 ? 1 : 0;