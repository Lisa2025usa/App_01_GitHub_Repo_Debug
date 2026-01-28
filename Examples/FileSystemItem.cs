namespace BlazorTOCExample
{
    /// <summary>
    /// Represents a file or folder in the tree structure.
    /// The key to making the TOC work is the IsExpanded property!
    /// </summary>
    public class FileSystemItem
    {
        /// <summary>
        /// Display name of the file or folder
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// True if this is a folder, false if it's a file
        /// </summary>
        public bool IsFolder { get; set; }

        /// <summary>
        /// Child items (only relevant for folders)
        /// </summary>
        public List<FileSystemItem> Children { get; set; } = new();

        /// <summary>
        /// CRITICAL: This property tracks whether the folder is expanded.
        /// Without this, the folder cannot toggle between open/closed states.
        /// This is what ChatGPT and Copilot often forget to include!
        /// </summary>
        public bool IsExpanded { get; set; } = false;

        /// <summary>
        /// Optional: Icon to display for files
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Optional: Path for navigation or file operations
        /// </summary>
        public string? Path { get; set; }
    }
}
