using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace ToPdf
{
    class Program
    {
        static List<string> ignoredPatterns = new List<string>();
        static List<string> excludeList = new List<string>();

        static void Main(string[] args)
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

            // Display extensions and excluded files
            Console.WriteLine($"Extensions: {string.Join(", ", extensions)}");
            Console.WriteLine($"Excluded: {string.Join(", ", excludeList)}");

            // Parse .gitignore if it exists
            string gitignorePath = Path.Combine(rootDirectory, ".gitignore");
            if (File.Exists(gitignorePath))
            {
                ParseGitignore(gitignorePath);
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
            string treeContent = GenerateProjectTree(rootDirectory, extensions, outputFileName);
            document.Add(new Paragraph(treeContent).SetFontSize(10));

            // Add a page break after the project tree
            document.Add(new AreaBreak());

            // Traverse all files with specified extensions in the directory and its subdirectories
            foreach (var extension in extensions)
            {
                foreach (var file in Directory.GetFiles(rootDirectory, extension, SearchOption.AllDirectories)
                    .Where(f => !IsIgnored(f, rootDirectory) && IsTextFile(f)))
                {
                    // Skip the output PDF file and files in .git directory
                    if (Path.GetFileName(file).Equals(outputFileName, StringComparison.OrdinalIgnoreCase) ||
                        file.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) ||
                        file.Contains(Path.DirectorySeparatorChar + ".vs" + Path.DirectorySeparatorChar))
                    {
                        continue;
                    }

                    // Get the relative path of the file and normalize it
                    string relativePath = NormalizePath(Path.GetRelativePath(rootDirectory, file));

                    // Add the relative path as a chapter title
                    document.Add(new Paragraph(relativePath).SetBold().SetFontSize(12));

                    try
                    {
                        if (!IsExcluded(file))
                        {
                            // Read the content of the file
                            string content = File.ReadAllText(file);

                            // Add the content to the PDF
                            document.Add(new Paragraph(content).SetFontSize(10));

                            // Display info about the added file
                            string fileSize = GetHumanReadableFileSize(content.Length);
                            Console.WriteLine($"Added to PDF: {relativePath} ({fileSize})");
                        }
                        else
                        {
                            // Add a placeholder for excluded files
                            document.Add(new Paragraph("[content not included]").SetFontSize(10));

                            // Display info about the excluded file
                            Console.WriteLine($"Excluded from PDF: {relativePath}");
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

        static void ParseGitignore(string gitignorePath)
        {
            foreach (var line in File.ReadAllLines(gitignorePath))
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#"))
                {
                    ignoredPatterns.Add(trimmedLine);
                }
            }
        }

        static bool IsIgnored(string filePath, string rootPath)
        {
            string relativePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
            bool isDirectory = Directory.Exists(filePath);

            foreach (var pattern in ignoredPatterns)
            {
                if (IsMatch(relativePath, pattern, isDirectory))
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsExcluded(string path)
        {
            string[] pathParts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return excludeList.Any(excludedItem => pathParts.Contains(excludedItem, StringComparer.OrdinalIgnoreCase));
        }

        static bool IsMatch(string path, string pattern, bool isDirectory)
        {
            // Convert .gitignore pattern to regex
            string regex = "^";
            bool isExactMatch = !pattern.EndsWith("/");

            // Handle directory-only patterns
            if (pattern.EndsWith("/") && !isDirectory)
            {
                return false;
            }

            // Split pattern into segments
            var segments = pattern.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];

                if (i > 0)
                {
                    regex += "\\/";
                }

                if (segment == "**")
                {
                    regex += ".*";
                }
                else
                {
                    regex += string.Join("",
                        segment.Select(c => c switch
                        {
                            '*' => "[^/]*",
                            '?' => "[^/]",
                            '.' => "\\.",
                            _ => Regex.Escape(c.ToString())
                        })
                    );
                }
            }

            // Handle exact matches
            if (isExactMatch && !isDirectory)
            {
                regex += "$";
            }
            else if (!isExactMatch)
            {
                regex += "(/.+)?$";
            }

            return Regex.IsMatch(path, regex, RegexOptions.IgnoreCase);
        }

        static string GenerateProjectTree(string rootPath, string[] extensions, string outputFileName)
        {
            var tree = new StringBuilder();
            var files = GetAllFiles(rootPath, extensions, outputFileName);
            foreach (var file in files.OrderBy(f => f))
            {
                string relativePath = NormalizePath(Path.GetRelativePath(rootPath, file));
                tree.AppendLine($"├── {relativePath}");
            }
            return tree.ToString();
        }

        static List<string> GetAllFiles(string rootPath, string[] extensions, string outputFileName)
        {
            var files = new List<string>();
            foreach (var extension in extensions)
            {
                files.AddRange(Directory.GetFiles(rootPath, extension, SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).Equals(outputFileName, StringComparison.OrdinalIgnoreCase) &&
                                !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) &&
                                !f.Contains(Path.DirectorySeparatorChar + ".vs" + Path.DirectorySeparatorChar) &&
                                !IsIgnored(f, rootPath) &&
                                IsTextFile(f)));
            }
            return files.Distinct().ToList();
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        static bool IsTextFile(string filePath)
        {
            // List of known binary file extensions
            string[] binaryExtensions = { ".exe", ".dll", ".obj", ".cache", ".bin", ".dat", ".iso", ".zip", ".rar", ".7z", ".gz", ".tar" };

            // Check if the file has a known binary extension
            if (binaryExtensions.Contains(Path.GetExtension(filePath).ToLower()))
            {
                return false;
            }

            const int charsToCheck = 8000;
            const double asciiThreshold = 0.9;

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    char[] buffer = new char[charsToCheck];
                    int bytesRead = streamReader.Read(buffer, 0, charsToCheck);

                    if (bytesRead == 0)
                    {
                        return true; // Empty file, consider it as text
                    }

                    int asciiCount = buffer.Take(bytesRead).Count(c => c <= 127);
                    return (double)asciiCount / bytesRead >= asciiThreshold;
                }
            }
            catch
            {
                return false; // If we can't read the file, assume it's not text
            }
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