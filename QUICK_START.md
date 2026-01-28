# Quick Start Guide: Implementing a Working Folder/File TOC in Blazor

This guide shows you how to implement a working collapsible folder/file tree-of-contents (TOC) in Blazor in just a few steps.

## TL;DR - The 3 Essential Requirements

For a folder/file TOC to work in Blazor, you MUST have:

1. **A state variable** (`bool IsExpanded`) for each folder
2. **Conditional rendering** (`@if (IsExpanded)`) to show/hide content
3. **A toggle method** that changes the state and triggers re-render

If you're missing ANY of these, it won't work!

## Step-by-Step Implementation

### Step 1: Create Your Data Model (5 minutes)

Create a class to represent files and folders:

```csharp
// Models/FileSystemItem.cs
namespace YourApp.Models
{
    public class FileSystemItem
    {
        public string Name { get; set; } = "";
        public bool IsFolder { get; set; }
        public List<FileSystemItem> Children { get; set; } = new();
        
        // ⚠️ CRITICAL: This property is what makes it work!
        public bool IsExpanded { get; set; } = false;
    }
}
```

### Step 2: Create the TreeView Component (10 minutes)

Create a new Razor component:

```razor
@* Components/FolderTreeView.razor *@

<div class="tree-item">
    @if (Item.IsFolder)
    {
        @* Folder: Clickable and expandable *@
        <div class="folder-header" 
             @onclick="ToggleFolder" 
             @onclick:stopPropagation="true">
            <span>@(Item.IsExpanded ? "📂" : "📁")</span>
            <span>@Item.Name</span>
        </div>

        @* ⚠️ CRITICAL: Conditional rendering based on IsExpanded *@
        @if (Item.IsExpanded && Item.Children.Any())
        {
            <div class="folder-content">
                @foreach (var child in Item.Children)
                {
                    @* Recursive: Render children with same component *@
                    <FolderTreeView Item="child" />
                }
            </div>
        }
    }
    else
    {
        @* File: Just display it *@
        <div class="file-item">
            <span>📄</span>
            <span>@Item.Name</span>
        </div>
    }
</div>

@code {
    [Parameter]
    public FileSystemItem Item { get; set; } = new();

    // ⚠️ CRITICAL: This method toggles the state
    private void ToggleFolder()
    {
        if (Item.IsFolder)
        {
            Item.IsExpanded = !Item.IsExpanded;
            StateHasChanged(); // Ensure UI updates
        }
    }
}
```

### Step 3: Add Basic Styling (5 minutes)

Create a scoped CSS file:

```css
/* Components/FolderTreeView.razor.css */

.folder-header {
    display: flex;
    align-items: center;
    padding: 4px 8px;
    cursor: pointer;
    user-select: none;
}

.folder-header:hover {
    background-color: #f0f0f0;
}

.folder-content {
    margin-left: 20px;
}

.file-item {
    display: flex;
    align-items: center;
    padding: 4px 8px;
}

.folder-header span:first-child,
.file-item span:first-child {
    margin-right: 8px;
}
```

### Step 4: Use It in Your Page (5 minutes)

```razor
@page "/files"

<h3>My Files</h3>
<FolderTreeView Item="@RootFolder" />

@code {
    private FileSystemItem RootFolder { get; set; } = new();

    protected override void OnInitialized()
    {
        // Create sample data
        RootFolder = new FileSystemItem
        {
            Name = "Root",
            IsFolder = true,
            IsExpanded = true, // Start expanded
            Children = new List<FileSystemItem>
            {
                new FileSystemItem
                {
                    Name = "Documents",
                    IsFolder = true,
                    Children = new List<FileSystemItem>
                    {
                        new FileSystemItem { Name = "Report.pdf", IsFolder = false },
                        new FileSystemItem { Name = "Notes.txt", IsFolder = false }
                    }
                },
                new FileSystemItem
                {
                    Name = "Pictures",
                    IsFolder = true,
                    Children = new List<FileSystemItem>
                    {
                        new FileSystemItem { Name = "Photo1.jpg", IsFolder = false },
                        new FileSystemItem { Name = "Photo2.jpg", IsFolder = false }
                    }
                },
                new FileSystemItem { Name = "README.md", IsFolder = false }
            }
        };
    }
}
```

### Step 5: Test It! (2 minutes)

1. Run your Blazor app
2. Navigate to `/files` (or wherever you put the component)
3. Click folders - they should expand/collapse
4. Icons should change (📁 to 📂)
5. Nested folders should work independently

## Troubleshooting Checklist

If it doesn't work, check these:

### ❌ Folder doesn't expand/collapse
- [ ] Do you have `IsExpanded` property in your model?
- [ ] Is `ToggleFolder()` actually changing `IsExpanded`?
- [ ] Did you add `@onclick="ToggleFolder"`?

### ❌ Content doesn't show/hide
- [ ] Do you have `@if (Item.IsExpanded)`?
- [ ] Is the condition checking the right property?
- [ ] Are you using CSS `display:none` instead? (Don't!)

### ❌ Icons don't change
- [ ] Do you have `@(Item.IsExpanded ? "📂" : "📁")`?
- [ ] Is it using the correct state variable?

### ❌ Both parent and child folders toggle
- [ ] Did you add `@onclick:stopPropagation="true"`?

### ❌ Nothing happens at all
- [ ] Check browser console for errors
- [ ] Is the component rendered at all?
- [ ] Try adding `Console.WriteLine()` in `ToggleFolder()`

## Common Mistakes to Avoid

### ❌ DON'T: Use CSS to hide content
```css
/* WRONG - Don't do this */
.folder-content {
    display: none;
}
```

### ✅ DO: Use conditional rendering
```razor
@if (Item.IsExpanded)
{
    <div class="folder-content">...</div>
}
```

---

### ❌ DON'T: Forget the state property
```csharp
// WRONG - Missing IsExpanded
public class FileSystemItem
{
    public string Name { get; set; }
    public bool IsFolder { get; set; }
}
```

### ✅ DO: Include state in your model
```csharp
// CORRECT
public class FileSystemItem
{
    public string Name { get; set; }
    public bool IsFolder { get; set; }
    public bool IsExpanded { get; set; } // ← Essential!
}
```

---

### ❌ DON'T: Hard-code the structure
```razor
<div>📁 Folder 1</div>
<div>📁 Folder 2</div>
```

### ✅ DO: Use recursive components
```razor
@foreach (var child in Item.Children)
{
    <FolderTreeView Item="child" />
}
```

## Next Steps

Once you have the basic version working, you can enhance it:

1. **Add file selection**: Track which file is selected
2. **Add icons**: Use different icons for different file types
3. **Add context menus**: Right-click for options
4. **Add drag & drop**: Rearrange files
5. **Add search**: Filter the tree
6. **Persist state**: Save expanded/collapsed state
7. **Lazy loading**: Load children on demand

## Need More Help?

- See `BLAZOR_TOC_SOLUTION.md` for detailed explanation
- See `BROKEN_VS_WORKING.md` for common mistake examples
- See `Examples/` folder for complete working code

## Summary

The key to making a folder/file TOC work in Blazor:

1. **State**: `bool IsExpanded { get; set; }`
2. **Conditional Rendering**: `@if (IsExpanded)`
3. **Toggle Method**: `Item.IsExpanded = !Item.IsExpanded;`

That's it! Those three elements are what ChatGPT and Copilot often miss or implement incorrectly.
