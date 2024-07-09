using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace PyToPdf
{
    class Program
    {
        static void Main(string[] args)
        {
            string rootDirectory;
            string[] extensions;

            if (args.Length == 0)
            {
                rootDirectory = Directory.GetCurrentDirectory();
                extensions = new[] { "*.py" };
            }
            else if (args.Length == 1 && args[0] == "*")
            {
                rootDirectory = Directory.GetCurrentDirectory();
                extensions = new[] { "*" };
            }
            else if (args.Length == 1 && args[0].Contains(','))
            {
                rootDirectory = Directory.GetCurrentDirectory();
                extensions = args[0].Split(',').Select(ext => ext.Trim() == "*" ? ext : $"*.{ext.Trim()}").ToArray();
            }
            else if (args.Length == 1)
            {
                rootDirectory = args[0];
                extensions = new[] { "*.py" };
            }
            else
            {
                rootDirectory = args[0];
                extensions = args[1].Split(',').Select(ext => ext.Trim() == "*" ? ext : $"*.{ext.Trim()}").ToArray();
            }

            // Verify that the directory exists
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"Error: Directory '{rootDirectory}' does not exist.");
                return;
            }

            // Get the name of the directory
            string directoryName = new DirectoryInfo(rootDirectory).Name;
            string outputFileName = $"{directoryName}.pdf";

            // Initialize PDF writer with the directory name as the file name
            var pdfWriter = new PdfWriter(outputFileName);
            var pdf = new PdfDocument(pdfWriter);
            var document = new Document(pdf);

            // Generate and add project tree
            document.Add(new Paragraph("Project Tree:").SetBold().SetFontSize(14));
            string projectTree = GenerateProjectTree(rootDirectory, extensions, outputFileName);
            document.Add(new Paragraph(projectTree).SetFontSize(10));

            // Add a page break after the project tree
            document.Add(new AreaBreak());

            // Traverse all files with specified extensions in the directory and its subdirectories
            foreach (var extension in extensions)
            {
                foreach (var file in Directory.GetFiles(rootDirectory, extension, SearchOption.AllDirectories))
                {
                    // Skip the output PDF file
                    if (Path.GetFileName(file).Equals(outputFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get the relative path of the file
                    string relativePath = Path.GetRelativePath(rootDirectory, file);

                    // Add the relative path as a chapter title
                    document.Add(new Paragraph(relativePath).SetBold().SetFontSize(12));

                    try
                    {
                        // Read the content of the file
                        string content = File.ReadAllText(file);

                        // Add the content to the PDF
                        document.Add(new Paragraph(content).SetFontSize(10));
                    }
                    catch (Exception ex)
                    {
                        document.Add(new Paragraph($"Error reading file: {ex.Message}").SetFontSize(10));
                    }
                }
            }

            // Close the PDF document
            document.Close();

            Console.WriteLine($"PDF '{outputFileName}' created successfully!");
        }

        static string GenerateProjectTree(string rootPath, string[] extensions, string outputFileName)
        {
            var tree = new StringBuilder();
            var dirInfo = new DirectoryInfo(rootPath);
            tree.AppendLine(dirInfo.Name);
            GenerateProjectTreeRecursive(dirInfo, "", tree, extensions, outputFileName);
            return tree.ToString();
        }

        static void GenerateProjectTreeRecursive(DirectoryInfo dir, string indent, StringBuilder tree, string[] extensions, string outputFileName)
        {
            var files = dir.GetFiles()
                .Where(f => (extensions.Contains("*") || extensions.Any(ext => f.Name.EndsWith(ext.TrimStart('*'), StringComparison.OrdinalIgnoreCase)))
                            && !f.Name.Equals(outputFileName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Name);
            var subDirs = dir.GetDirectories().OrderBy(d => d.Name);

            foreach (var file in files)
            {
                tree.AppendLine($"{indent}├── {file.Name}");
            }

            foreach (var subDir in subDirs)
            {
                tree.AppendLine($"{indent}├── {subDir.Name}/");
                GenerateProjectTreeRecursive(subDir, indent + "│   ", tree, extensions, outputFileName);
            }
        }
    }
}