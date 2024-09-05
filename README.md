# ToPdf

ToPdf is a C# console application that generates a PDF document containing the content of specified files within a directory and its subdirectories. It also includes a project tree structure at the beginning of the PDF for easy navigation.

## Features

- Generate a PDF containing the content of specified file types
- Include a project tree structure in the PDF
- Support for wildcard (*) to include all file types
- Specify multiple file extensions
- Traverse subdirectories
- Error handling for file access issues

## Prerequisites

- .NET 8.0 SDK or later
- iText 7 library for PDF generation

## Installation

1. Clone the repository or download the source code.
2. Ensure you have the .NET 8.0 SDK installed.
3. Restore the NuGet packages by running:

```
dotnet restore
```

4. Build the project:

```
dotnet build --configuration Release
```

## Usage

Run the compiled executable with the following syntax:

```
ToPdf.exe [directory_path] [file_extensions]
```

- `[directory_path]` (optional): The path to the directory you want to process. If not specified, the current directory will be used.
- `[file_extensions]` (optional): A comma-separated list of file extensions to include. Use "*" to include all file types. If not specified, all file types will be included.

### Examples

1. Process all files in the current directory:
```
ToPdf.exe *
```

2. Process specific file types in the current directory:
```
ToPdf.exe py,md,txt,toml
```

3. Process all files in a specific directory:
```
ToPdf.exe C:\your\directory\path *
```

4. Process specific file types in a given directory:
```
ToPdf.exe C:\your\directory\path py,md,txt,toml
```

## Output

The program will generate a PDF file named after the processed directory. For example, if you process the "MyProject" directory, the output will be "MyProject.pdf".

The PDF will contain:
1. A project tree structure showing all included files and directories.
2. The content of each included file, with the file path as a heading.

## Limitations

- The program may struggle with very large files or a large number of files due to memory constraints.
- Binary files are not handled specially and may produce unreadable content in the PDF.
- The project tree and file contents are basic text representations. Advanced formatting is not preserved.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the [MIT License](LICENSE).

## Acknowledgements

- This project uses the [iText 7](https://itextpdf.com/en/products/itext-7/itext-7-core) library for PDF generation.
