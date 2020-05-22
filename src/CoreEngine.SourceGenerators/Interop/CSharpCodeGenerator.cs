using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MyGenerator
{
    [Generator]
    public class CSharpCodeGenerator : ISourceGenerator
    {
        public void Initialize(InitializationContext context)
        {
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new HostInterfacesReceiver());
        }

        public void Execute(SourceGeneratorContext context)
        {
            var syntaxReceiver = (HostInterfacesReceiver?)context.SyntaxReceiver;

            if (syntaxReceiver != null)
            {
                foreach (var test in syntaxReceiver.HostInterfaces)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TEST", test.Identifier.ValueText, test.Identifier.ValueText, "Test", DiagnosticSeverity.Warning, true), Location.Create("test", new TextSpan(), new LinePositionSpan())));
                }
            }

            context.AddSource("test.cs", SourceText.From("namespace CoreEngine { public class pouet {} }", Encoding.UTF8));
            // var output = GenerateCode()
        }

        private static string GenerateCode(InterfaceDeclarationSyntax interfaceNode)
        {
            var nullableTypes = new List<string>();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("using System.Numerics;");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("namespace CoreEngine.HostServices.Interop");
            stringBuilder.AppendLine("{");

            var delegateNameList = new List<string>();

            // Generate delegate declarations
            foreach (var member in interfaceNode.Members)
            {
                if (member.Kind() == SyntaxKind.MethodDeclaration)
                {
                    var method = (MethodDeclarationSyntax)member;
                    var parameters = method.ParameterList.Parameters;
                    var delegateTypeName = $"{interfaceNode.Identifier.ToString().Substring(1)}_{method.Identifier}Delegate";

                    var currentIndex = 0;
                    var delegateTypeNameOriginal = delegateTypeName;

                    while (delegateNameList.Contains(delegateTypeName))
                    {
                        delegateTypeName = delegateTypeNameOriginal + $"_{++currentIndex}";
                    }

                    delegateNameList.Add(delegateTypeName);

                    var delegateVariableName = char.ToLowerInvariant(delegateTypeName[0]) + delegateTypeName.Substring(1);
                    var returnType = method.ReturnType.ToString();

                    // Generate delegate
                    IndentCode(stringBuilder, 1);
                    stringBuilder.Append("internal unsafe delegate ");

                    if (returnType.Last() == '?')
                    {
                        nullableTypes.Add(returnType[0..^1]);
                        returnType = $"Nullable{returnType[0..^1]}";
                    }

                    stringBuilder.Append(returnType);
                    stringBuilder.Append($" {delegateTypeName}(IntPtr context");

                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var parameter = parameters[i];

                        stringBuilder.Append(", ");

                        if (parameter.Type!.ToString().Contains("ReadOnlySpan<"))
                        {
                            var index = parameter.Type!.ToString().IndexOf("<");
                            var parameterType = parameter.Type!.ToString().Substring(index).Replace("<", string.Empty).Replace(">", string.Empty);
                            
                            stringBuilder.Append($"{parameterType}* {parameter.Identifier}, int {parameter.Identifier}Length");
                        }

                        else
                        {
                            stringBuilder.Append($"{parameter.Type} {parameter.Identifier}");
                        }
                    }

                    stringBuilder.AppendLine(");");
                }
            }

            // Generate struct
            stringBuilder.AppendLine();

            IndentCode(stringBuilder, 1);
            stringBuilder.AppendLine($"public struct {interfaceNode.Identifier.Text.Substring(1)} : {interfaceNode.Identifier}");
            
            IndentCode(stringBuilder, 1);
            stringBuilder.AppendLine("{");

            IndentCode(stringBuilder, 2);
            stringBuilder.AppendLine("private IntPtr context { get; }");
        
            delegateNameList = new List<string>();

            foreach (var member in interfaceNode.Members)
            {
                if (member.Kind() == SyntaxKind.MethodDeclaration)
                {
                    var method = (MethodDeclarationSyntax)member;
                    var parameters = method.ParameterList.Parameters;
                    var delegateTypeName = $"{interfaceNode.Identifier.ToString().Substring(1)}_{method.Identifier}Delegate";

                    var delegateTypeNameOriginal = delegateTypeName;
                    var currentIndex = 0;

                    while (delegateNameList.Contains(delegateTypeName))
                    {
                        delegateTypeName = delegateTypeNameOriginal + $"_{++currentIndex}";
                    }

                    var delegateVariableName = char.ToLowerInvariant(delegateTypeName[0]) + delegateTypeName.Substring(1);

                    // Generate struct field
                    stringBuilder.AppendLine();

                    IndentCode(stringBuilder, 2);
                    stringBuilder.AppendLine($"private {delegateTypeName} {delegateVariableName} {{ get; }}");

                    // Generate struct method
                    IndentCode(stringBuilder, 2);
                    stringBuilder.Append($"public unsafe {method.ReturnType} {method.Identifier}(");

                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var parameter = parameters[i];

                        if (i > 0)
                        {
                            stringBuilder.Append(", ");
                        }

                        stringBuilder.Append($"{parameter.Type} {parameter.Identifier}");
                    }

                    stringBuilder.AppendLine(")");

                    IndentCode(stringBuilder, 2);
                    stringBuilder.AppendLine("{");

                    var argumentList = new List<string>()
                    {
                        "this.context"
                    };
                    
                    var currentParameterIndex = 1;

                    foreach (var parameter in parameters)
                    {
                        if (parameter.Type!.ToString().Contains("ReadOnlySpan<"))
                        {
                            argumentList.Add($"{parameter.Identifier.Text}Pinned");
                            argumentList.Insert(++currentParameterIndex, $"{parameter.Identifier.Text}.Length");
                        }

                        else
                        {
                            argumentList.Add(parameter.Identifier.Text);
                        }

                        currentParameterIndex++;
                    }

                    var generatedArgumentList = string.Join(", ", argumentList.ToArray());

                    var currentIndentationLevel = 3;

                    IndentCode(stringBuilder, currentIndentationLevel++);
                    stringBuilder.AppendLine($"if (this.context != null && this.{delegateVariableName} != null)");

                    var variablesToPin = parameters.Where(item => item.Type!.ToString().Contains("ReadOnlySpan<"));

                    foreach (var variableToPin in variablesToPin)
                    {
                        var index = variableToPin.Type!.ToString().IndexOf("<");
                        var variableType = variableToPin.Type!.ToString().Substring(index).Replace("<", string.Empty).Replace(">", string.Empty);

                        IndentCode(stringBuilder, currentIndentationLevel++);
                        stringBuilder.AppendLine($"fixed ({variableType}* {variableToPin.Identifier.Text}Pinned = {variableToPin.Identifier.Text})");
                    }

                    if (method.ReturnType.ToString() != "void")
                    {
                        if (nullableTypes.Contains(method.ReturnType.ToString()[0..^1]))
                        {
                            IndentCode(stringBuilder, currentIndentationLevel - 1);
                            stringBuilder.AppendLine("{");

                            IndentCode(stringBuilder, currentIndentationLevel);
                            stringBuilder.Append($"var returnedValue = ");
                        }

                        else
                        {
                            IndentCode(stringBuilder, currentIndentationLevel);
                            stringBuilder.Append("return ");
                        }
                    }

                    else
                    {
                        IndentCode(stringBuilder, currentIndentationLevel);
                    }

                    stringBuilder.Append($"this.{delegateVariableName}({generatedArgumentList})");

                    if (method.ReturnType.ToString() != "void" && nullableTypes.Contains(method.ReturnType.ToString()[0..^1]))
                    {
                        stringBuilder.AppendLine(";");

                        IndentCode(stringBuilder, currentIndentationLevel);
                        stringBuilder.AppendLine("if (returnedValue.HasValue) return returnedValue.Value;");

                        IndentCode(stringBuilder, currentIndentationLevel - 1);
                        stringBuilder.AppendLine("}");
                    }

                    else
                    {
                        stringBuilder.AppendLine(";");
                    }

                    if (method.ReturnType.ToString() != "void")
                    {
                        stringBuilder.AppendLine();

                        if (method.ReturnType.ToString() == "string")
                        {
                            IndentCode(stringBuilder, 3);
                            stringBuilder.AppendLine($"return string.Empty;");
                        }

                        else
                        {
                            IndentCode(stringBuilder, 3);
                            stringBuilder.AppendLine($"return default({method.ReturnType});");
                        }
                    }

                    IndentCode(stringBuilder, 2);
                    stringBuilder.AppendLine("}");
                }
            }

            IndentCode(stringBuilder, 1);
            stringBuilder.AppendLine("}");

            foreach (var nullableType in nullableTypes)
            {
                stringBuilder.AppendLine();

                IndentCode(stringBuilder, 1);
                stringBuilder.AppendLine($"public struct Nullable{nullableType}");
                
                IndentCode(stringBuilder, 1);
                stringBuilder.AppendLine("{");

                IndentCode(stringBuilder, 2);
                stringBuilder.AppendLine("public bool HasValue { get; }");

                IndentCode(stringBuilder, 2);
                stringBuilder.AppendLine($"public {nullableType} Value {{ get; }}");

                IndentCode(stringBuilder, 1);
                stringBuilder.AppendLine("}");
            }

            stringBuilder.AppendLine("}");
            return stringBuilder.ToString();
        }

        private static void IndentCode(StringBuilder stringBuilder, int level)
        {
            for (var i = 0; i < level; i++)
            {
                stringBuilder.Append("    ");
            }
        }
    }

    class HostInterfacesReceiver : ISyntaxReceiver
    {
        public IList<InterfaceDeclarationSyntax> HostInterfaces { get; } = new List<InterfaceDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InterfaceDeclarationSyntax interfaceNode)
            {
                this.HostInterfaces.Add(interfaceNode);
            }
        }
    }
}