using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClubActivity
{
    internal class CSharp : LangBase
    {
        private RoslynCompiler _compiler;
        static string thisFolder = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName;
        private Type _derivedType;

        public CSharp(Type derive) : base(".cs", "C#")
        {
            _compiler = new RoslynCompiler(
            new string[]
            {
                "System.Linq",
                "System",
                "System.Collections",
                "netstandard",
            },
            new string[]
            {
                Assembly.GetEntryAssembly()!.Location,
                Path.Combine(thisFolder, "DLLS", "MonoGame.Framework.dll"),
            });

            _derivedType = derive;
        }

        public override IFunction? Compile(string source)
        {
            try
            {
                var assembly = _compiler.Compile(source);
                var pTypes = assembly.GetTypes().Where(t => _derivedType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsValueType && !t.IsInterface).ToArray();
                var selected = pTypes.First();

                IFunction result = (IFunction)selected.GetConstructors()[0].Invoke(Array.Empty<object>());
                return result;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Someone failed: {e.Message}");
            }

            return null;
        }
    }

    public class RoslynCompiler
    {
        List<MetadataReference> refs;
        public RoslynCompiler(string[] namespacesToReference, string[] customAssemblyPaths)
        {
            refs = customAssemblyPaths
                        .Select(path => MetadataReference.CreateFromFile(path) as MetadataReference)
                        .ToList();

            // Add namespaces to reference
            refs.AddRange(namespacesToReference
                .Select(namespaceName => MetadataReference.CreateFromFile(GetAssemblyLocation(namespaceName)) as MetadataReference));


            //some default refeerences
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")));
            refs.Add(MetadataReference.CreateFromFile(typeof(Object).Assembly.Location));

            static string GetAssemblyLocation(string namespaceName)
            {
                var assemblyName = new AssemblyName(namespaceName);
                return Assembly.Load(assemblyName).Location;
            }
        }

        public Assembly Compile(string code)
        {
            //generate syntax tree from code and config compilation options
            var syntax = CSharpSyntaxTree.ParseText(code);
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                optimizationLevel: OptimizationLevel.Release);

            CSharpCompilation compilation = CSharpCompilation.Create(Guid.NewGuid().ToString(), new List<SyntaxTree> { syntax }, refs, options);

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    var compilationErrors = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error)
                        .ToList();
                    if (compilationErrors.Any())
                    {
                        var firstError = compilationErrors.First();
                        var errorDescription = firstError.GetMessage();
                        var firstErrorMessage = $"Line {firstError.Location.GetLineSpan().StartLinePosition}: {errorDescription};";
                        var exception = new Exception(firstErrorMessage);
                        compilationErrors.ForEach(e => { if (!exception.Data.Contains(e.Id)) exception.Data.Add(e.Id, e.GetMessage()); });
                        throw exception;
                    }
                }
                ms.Seek(0, SeekOrigin.Begin);

                return AssemblyLoadContext.Default.LoadFromStream(ms);
            }
        }
    }
}
