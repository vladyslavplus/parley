using AwesomeAssertions;
using Xunit;

namespace Parley.UnitTests.Architecture;

public sealed class SourceLayoutTests
{
    private const string SolutionFileName = "Parley.slnx";
    private const string SourceFolderName = "src";
    private const string GeneratedFolderName = "Migrations";
    private const string GenericFileSuffix = "OfT";

    private static readonly string[] ExcludedFolderNames = ["obj", "bin", GeneratedFolderName];
    private static readonly string[] TypeKeywords = ["class", "struct", "interface", "enum", "record"];

    [Fact]
    public void SourceFiles_DeclareAtMostOneTopLevelType()
    {
        var root = FindSourceRoot();
        var violations = SourceFiles(root)
            .Select(file => (File: file, Types: TopLevelTypes(File.ReadAllText(file))))
            .Where(entry => entry.Types.Count > 1)
            .Select(entry => $"{Path.GetRelativePath(root, entry.File)}: {string.Join(", ", entry.Types.Select(type => type.Name))}")
            .ToArray();

        violations.Should().BeEmpty("each source file holds one top-level type; split the others out");
    }

    [Fact]
    public void SourceFiles_AreNamedAfterTheirTopLevelType()
    {
        var root = FindSourceRoot();
        var violations = SourceFiles(root)
            .Select(file => (File: file, Types: TopLevelTypes(File.ReadAllText(file))))
            .Where(entry => entry.Types.Count == 1)
            .Where(entry => !IsNamedAfterType(Path.GetFileNameWithoutExtension(entry.File), entry.Types[0]))
            .Select(entry => $"{Path.GetRelativePath(root, entry.File)} declares {entry.Types[0].Name}")
            .ToArray();

        violations.Should().BeEmpty("a file takes the name of the type it declares; generic types may use the NameOfT form");
    }

    private static bool IsNamedAfterType(string fileName, TypeDeclaration type)
    {
        return fileName == type.Name
            || (type.IsGeneric && fileName == type.Name + GenericFileSuffix);
    }

    private static IEnumerable<string> SourceFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar)
                .Any(segment => ExcludedFolderNames.Contains(segment, StringComparer.Ordinal)));
    }

    private static string FindSourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
            {
                return Path.Combine(current.FullName, SourceFolderName);
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate {SolutionFileName} above the test output directory.");
    }

    private static List<TypeDeclaration> TopLevelTypes(string source)
    {
        var code = MaskCommentsAndLiterals(source);
        var declarations = new List<TypeDeclaration>();
        var scopes = new Stack<bool>(); // true for a namespace block, false for a type or member body
        var parenDepth = 0;
        var namespaceNext = false;
        TypeDeclaration? open = null;
        var openScopeDepth = 0;
        var openHasBody = false;

        var index = 0;
        while (index < code.Length)
        {
            var character = code[index];
            if (char.IsLetter(character) || character == '_')
            {
                var start = index;
                while (index < code.Length && (char.IsLetterOrDigit(code[index]) || code[index] == '_'))
                {
                    index++;
                }

                var word = code[start..index];
                var atNamespaceLevel = scopes.All(isNamespace => isNamespace);
                if (word == "namespace" && open is null && atNamespaceLevel)
                {
                    namespaceNext = true;
                }
                else if (open is null && atNamespaceLevel && TypeKeywords.Contains(word, StringComparer.Ordinal))
                {
                    var name = NextWord(code, ref index);
                    if (name is "struct" or "class")
                    {
                        name = NextWord(code, ref index);
                    }

                    open = new TypeDeclaration(name, IsGenericHead(code, index));
                    openScopeDepth = scopes.Count;
                    openHasBody = false;
                }

                continue;
            }

            switch (character)
            {
                case '(':
                    parenDepth++;
                    break;
                case ')':
                    parenDepth--;
                    break;
                case '{':
                    if (open is not null && !openHasBody && parenDepth == 0 && scopes.Count == openScopeDepth)
                    {
                        openHasBody = true;
                    }

                    scopes.Push(namespaceNext);
                    namespaceNext = false;
                    break;
                case '}':
                    if (scopes.Count > 0)
                    {
                        scopes.Pop();
                    }

                    if (open is not null && openHasBody && scopes.Count == openScopeDepth)
                    {
                        declarations.Add(open);
                        open = null;
                    }

                    break;
                case ';':
                    if (open is not null && !openHasBody && parenDepth == 0 && scopes.Count == openScopeDepth)
                    {
                        // A record with a primary constructor ends at its semicolon.
                        declarations.Add(open);
                        open = null;
                    }
                    else
                    {
                        // A file-scoped namespace ends at its semicolon and opens no scope.
                        namespaceNext = false;
                    }

                    break;
            }

            index++;
        }

        return declarations;
    }

    private static string NextWord(string code, ref int index)
    {
        while (index < code.Length && char.IsWhiteSpace(code[index]))
        {
            index++;
        }

        var start = index;
        while (index < code.Length && (char.IsLetterOrDigit(code[index]) || code[index] == '_'))
        {
            index++;
        }

        return code[start..index];
    }

    private static bool IsGenericHead(string code, int index)
    {
        while (index < code.Length && char.IsWhiteSpace(code[index]))
        {
            index++;
        }

        return index < code.Length && code[index] == '<';
    }

    /// <summary>
    /// Replaces comments and string or char literals with spaces (newlines are kept),
    /// so brace and keyword scanning never reads inside them.
    /// </summary>
    private static string MaskCommentsAndLiterals(string source)
    {
        var masked = source.ToCharArray();
        var index = 0;
        while (index < source.Length)
        {
            if (StartsAt(source, index, "//"))
            {
                var end = source.IndexOf('\n', index);
                end = end < 0 ? source.Length : end;
                Blank(masked, index, end);
                index = end;
            }
            else if (StartsAt(source, index, "/*"))
            {
                var end = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                end = end < 0 ? source.Length : end + 2;
                Blank(masked, index, end);
                index = end;
            }
            else if (StartsAt(source, index, "\"\"\""))
            {
                var end = source.IndexOf("\"\"\"", index + 3, StringComparison.Ordinal);
                end = end < 0 ? source.Length : end + 3;
                Blank(masked, index, end);
                index = end;
            }
            else if (StartsAt(source, index, "@\""))
            {
                var end = SkipVerbatimString(source, index + 2);
                Blank(masked, index, end);
                index = end;
            }
            else if (StartsAt(source, index, "$\""))
            {
                var end = SkipQuotedString(source, index + 2);
                Blank(masked, index, end);
                index = end;
            }
            else if (source[index] == '"')
            {
                var end = SkipQuotedString(source, index + 1);
                Blank(masked, index, end);
                index = end;
            }
            else if (source[index] == '\'')
            {
                var end = SkipCharLiteral(source, index + 1);
                Blank(masked, index, end);
                index = end;
            }
            else
            {
                index++;
            }
        }

        return new string(masked);
    }

    private static bool StartsAt(string source, int index, string token)
    {
        return string.CompareOrdinal(source, index, token, 0, token.Length) == 0;
    }

    private static void Blank(char[] buffer, int start, int end)
    {
        for (var index = start; index < end && index < buffer.Length; index++)
        {
            if (buffer[index] != '\n')
            {
                buffer[index] = ' ';
            }
        }
    }

    private static int SkipVerbatimString(string source, int index)
    {
        while (index < source.Length)
        {
            if (source[index] == '"')
            {
                if (index + 1 < source.Length && source[index + 1] == '"')
                {
                    index += 2;
                    continue;
                }

                return index + 1;
            }

            index++;
        }

        return source.Length;
    }

    private static int SkipQuotedString(string source, int index)
    {
        while (index < source.Length && source[index] != '"' && source[index] != '\n')
        {
            index += source[index] == '\\' ? 2 : 1;
        }

        return Math.Min(index + 1, source.Length);
    }

    private static int SkipCharLiteral(string source, int index)
    {
        while (index < source.Length && source[index] != '\'' && source[index] != '\n')
        {
            index += source[index] == '\\' ? 2 : 1;
        }

        return Math.Min(index + 1, source.Length);
    }

    private sealed record TypeDeclaration(string Name, bool IsGeneric);
}
