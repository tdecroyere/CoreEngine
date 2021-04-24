using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreEngine.SourceGenerators
{
    [Generator]
    public class GetComponentHashCodeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            Logger.WriteMessage(receiver.ComponentCandidates.Count.ToString());

            using var hashAlgorithm = new SHA256Managed();

            foreach (var componentCandidate in receiver.ComponentCandidates)
            {
                var semanticModel = context.Compilation.GetSemanticModel(componentCandidate.SyntaxTree);
                
                var typeInfo = semanticModel.GetDeclaredSymbol(componentCandidate);

                if (typeInfo != null)
                {
                    Logger.WriteMessage($"{context.Compilation.AssemblyName} - {typeInfo.ContainingNamespace}");

                    var stringBuilder = new GeneratorStringBuilder();

                    var componentName = typeInfo.Name;
                    var componentNamespace = typeInfo.ContainingNamespace.ToString();

                    Logger.WriteMessage($"Processing Component: {componentName}");

                    stringBuilder.AppendLine($"namespace {componentNamespace}");
                    stringBuilder.AppendLine("{");

                    stringBuilder.AppendLine($"public partial struct {componentName}");
                    stringBuilder.AppendLine("{");

                    stringBuilder.AppendLine($"private static readonly ComponentHash componentHash = new ComponentHash(new byte[] {{ { GetHash(hashAlgorithm, $"{componentNamespace}.{componentName}") } }});");

                    stringBuilder.AppendLine("public ComponentHash GetComponentHash()");
                    stringBuilder.AppendLine("{");

                    stringBuilder.AppendLine($"return componentHash;");
                    
                    stringBuilder.AppendLine("}");
                    stringBuilder.AppendLine("}");
                    stringBuilder.AppendLine("}");

                    var fileName = $"{context.Compilation.AssemblyName}_{componentName}.generated.cs";

                    Utils.AddSource(context, fileName, stringBuilder.ToString());
                }
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            var hashList = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input)).Select(item => item.ToString());
            return string.Join(',', hashList);
        }

        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<StructDeclarationSyntax> ComponentCandidates { get; } = new List<StructDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is StructDeclarationSyntax structNode)
                {
                    if (structNode.BaseList != null && structNode.BaseList.Types.Any(type => type.ToString() == "IComponentData"))
                    {
                        this.ComponentCandidates.Add(structNode);
                    }
                }
            }
        }
    }
}