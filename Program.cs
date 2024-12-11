using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace ToPdf
{
    class Program
    {
        static List<string> excludeList = new List<string>();
        static void Main(string[] args)
        {
            // Process arguments
            var (rootDirectory, extensions) = ProcessArguments(args);

            // Display extensions and excluded files
            Console.WriteLine($"Extensions: {string.Join(", ", extensions)}");
            Console.WriteLine($"Excluded: {string.Join(", ", excludeList)}");

            // Get the name of the directory
            string directoryName = new DirectoryInfo(rootDirectory).Name;
            string outputFileName = $"{directoryName}.pdf";

            // Initialize PDF writer with the directory name as the file name
            var pdfWriter = new PdfWriter(outputFileName);
            var pdf = new PdfDocument(pdfWriter);
            var document = new Document(pdf);

            // Generate and add project tree
            var (treeContent, files) = GenerateProjectTree(rootDirectory, extensions, outputFileName);
            document.Add(new Paragraph("Project Tree:").SetBold().SetFontSize(14));
            document.Add(new Paragraph(treeContent).SetFontSize(10));

            // Add a page break after the project tree
            document.Add(new AreaBreak());

            // Traverse all files with specified extensions in the directory and its subdirectories
            foreach (var file in files)
            {
                // Get the relative path of the file and normalize it
                string relativePath = NormalizePath(Path.GetRelativePath(rootDirectory, file));

                if (!IsExcluded(file))
                {
                    try
                    {
                        // Read the content of the file
                        string content = File.ReadAllText(file);

                        if (content.Length > 0)
                        {
                            // Add the relative path as a chapter title
                            document.Add(new Paragraph(relativePath).SetBold().SetFontSize(12));

                            // Add the content to the PDF
                            document.Add(new Paragraph(content).SetFontSize(10));

                            // Display info about the added file
                            string fileSize = GetHumanReadableFileSize(content.Length);
                            Console.WriteLine($"Added to PDF: {relativePath} ({fileSize})");
                        }
                    }
                    catch (Exception ex)
                    {
                        document.Add(new Paragraph($"Error reading file: {ex.Message}").SetFontSize(10));
                        Console.WriteLine($"Error processing file: {relativePath} - {ex.Message}");
                    }
                }
            }

            // Close the PDF document
            document.Close();

            // Get the size of the created PDF file
            long pdfSize = new FileInfo(outputFileName).Length;
            string readablePdfSize = GetHumanReadableFileSize(pdfSize);

            Console.WriteLine($"PDF '{outputFileName}' created successfully! Size: {readablePdfSize}");
        }

        static (string rootDirectory, string[] extensions) ProcessArguments(string[] args)
        {
            string rootDirectory;
            string[] extensions;

            if (args.Length == 0)
            {
                rootDirectory = Directory.GetCurrentDirectory();
                string jsonConfigPath = Path.Combine(rootDirectory, ".vscode", "topdf.json");
                if (File.Exists(jsonConfigPath))
                {
                    (extensions, excludeList) = ParseJsonConfig(jsonConfigPath);
                }
                else
                {
                    extensions = new[] { "*" };
                }
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
                extensions = new[] { "*" };
            }
            else
            {
                rootDirectory = args[0];
                extensions = args[1].Split(',').Select(ext => ext.Trim() == "*" ? ext : $"*.{ext.Trim()}").ToArray();
            }

            return (rootDirectory, extensions);
        }

        static (string[], List<string>) ParseJsonConfig(string jsonPath)
        {
            string jsonContent = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            string[] extensions = root.GetProperty("extensions").EnumerateArray()
                .Select(e => e.GetString())
                .Where(e => e != null)
                .Select(e => $"*.{e}")
                .ToArray();

            List<string> excludeList = root.GetProperty("exclude").EnumerateArray()
                .Select(e => e.GetString())
                .Where(e => e != null)
                .Select(e => e!) // Use the null-forgiving operator
                .ToList();

            return (extensions, excludeList);
        }

        static bool IsExcluded(string path)
        {
            string[] pathParts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return excludeList.Any(excludedItem => pathParts.Contains(excludedItem, StringComparer.OrdinalIgnoreCase));
        }

        static (string treeContent, List<string> files) GenerateProjectTree(string rootPath, string[] extensions, string outputFileName)
        {
            var tree = new StringBuilder();
            var files = new List<string>();

            // Traverse all files with specified extensions in the directory and its subdirectories
            foreach (var extension in extensions)
            {
                // Skip the output PDF file and files in .git directory
                files.AddRange(Directory.GetFiles(rootPath, extension, SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).Equals(outputFileName, StringComparison.OrdinalIgnoreCase) &&
                                !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) &&
                                !f.Contains(Path.DirectorySeparatorChar + ".vs" + Path.DirectorySeparatorChar) &&
                                !IsExcluded(f)));
            }
            files = files.Distinct().ToList();

            foreach (var file in files.OrderBy(f => f))
            {
                string relativePath = NormalizePath(Path.GetRelativePath(rootPath, file));
                tree.AppendLine($"├── {relativePath}");
            }
            return (tree.ToString(), files);
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        static string GetHumanReadableFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}