# Unity Namespace Analyzer

This is the roslyn analyzer, that suggests proper namespaces
using the root namespace from a corresponding assembly definition.

It is created to work around this [resaharper-unity issue](https://github.com/JetBrains/resharper-unity/issues/2427).

## How It Works

For every namespace declaration in assembly:
- get the source file path
- get the directory name from the file path
- find the `asmdef`-file in that (or any parent) directory
- read the root namespace from the assembly definition
- get relative path between the asmdef and the source file directories
- combine the root namespace and relative path
- compare the combined value with the declared one 

## How to Install

1. Build the library
2. Install the `dll` following [unity documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/roslyn-analyzers.html). TLDR:
   1. Copy the library to Unity Assets
   2. Make it not imported for any platform (uncheck all checkboxes)
   3. Set the `RoslynAnalyzer` tag
3. Disable the standard resharper namespace suggestions:
```editorconfig
[*.cs]
resharper_check_namespace_highlighting=none
```

## Limitations

- It doesn't support assemblies without assembly definitions (`Assembly-CSharp` and `Assembly-CSharp-Editor`)
- It doesn't have a JSON parser and relies on a standard `asmdef` formatting
- It doesn't handle cases when directory name contains invalid characters for namespaces
- No quick fix action is provided