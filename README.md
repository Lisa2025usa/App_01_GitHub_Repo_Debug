# Blazor TOC (Table of Contents) - Why AI Can't Get It Right

This repository demonstrates **why ChatGPT and Copilot struggle to implement a working left panel folder/file tree-of-contents (TOC) in Blazor** and provides complete, working solutions.

## The Problem

When using AI assistants like ChatGPT or Copilot to create a collapsible folder/file tree in Blazor:
- **Folders don't expand or collapse when clicked**
- **Nothing happens when you select a folder**
- **Files remain hidden even after clicking the folder**

This is a common frustration for developers using AI coding assistants.

## Why It Happens

AI tools often generate code that is:
- ✅ **Syntactically correct** (compiles without errors)
- ❌ **Semantically incomplete** (doesn't actually work)

They typically miss one or more of these critical requirements:
1. **State management** - No `IsExpanded` property to track folder state
2. **Conditional rendering** - Using CSS instead of `@if` statements
3. **Event handling** - Missing `StateHasChanged()` or event propagation control
4. **Recursive components** - Hard-coded structures instead of proper recursion

## What's in This Repository

### 📚 Documentation

- **[QUICK_START.md](QUICK_START.md)** - Get a working TOC in 25 minutes
- **[BLAZOR_TOC_SOLUTION.md](BLAZOR_TOC_SOLUTION.md)** - Comprehensive explanation of the problem and solution
- **[BROKEN_VS_WORKING.md](BROKEN_VS_WORKING.md)** - Side-by-side comparison of broken vs working code

### 💾 Working Code Examples

The `Examples/` folder contains complete, tested implementations:

- **FileSystemItem.cs** - Data model with proper state management
- **FolderTreeView.razor** - Recursive component that actually works
- **FolderTreeView.razor.css** - Scoped styling
- **FileBrowserExample.razor** - Example usage with sample data

### 🎯 Key Concepts Demonstrated

All examples show:
- ✅ Proper state management with `IsExpanded` property
- ✅ Conditional rendering using `@if` (not CSS)
- ✅ Event propagation control with `@onclick:stopPropagation`
- ✅ Recursive component design
- ✅ Explicit re-renders with `StateHasChanged()`

## Quick Example

Here's the minimal working implementation:

```razor
@* The key: IsExpanded state + @if conditional rendering *@

<div @onclick="ToggleFolder" @onclick:stopPropagation="true">
    @(Item.IsExpanded ? "📂" : "📁") @Item.Name
</div>

@if (Item.IsExpanded && Item.Children.Any())
{
    <div class="folder-content">
        @foreach (var child in Item.Children)
        {
            <FolderTreeView Item="child" />
        }
    </div>
}

@code {
    [Parameter]
    public FileSystemItem Item { get; set; } = new();
    
    private void ToggleFolder()
    {
        Item.IsExpanded = !Item.IsExpanded;
        StateHasChanged();
    }
}
```

## Getting Started

1. **Start with the Quick Start Guide**: [QUICK_START.md](QUICK_START.md)
2. **Understand the problem**: Read [BLAZOR_TOC_SOLUTION.md](BLAZOR_TOC_SOLUTION.md)
3. **Learn from mistakes**: Study [BROKEN_VS_WORKING.md](BROKEN_VS_WORKING.md)
4. **Copy working code**: Use files from `Examples/` folder

## The Bottom Line

**AI tools generate code that looks right but doesn't work** because they miss the critical connection between:
- State management (`IsExpanded`)
- Conditional rendering (`@if`)
- Event handling (`@onclick`, `StateHasChanged()`)
- Event propagation (`@onclick:stopPropagation`)

**All four elements must work together.** Miss any one, and your TOC won't function.

## Technologies

- **Blazor** - .NET 8 Server or WebAssembly
- **C# 12**
- **Razor Components**

## Use Cases

This pattern works for:
- 📁 File browsers
- 🗂️ Navigation menus
- 📊 Hierarchical data displays
- 🌳 Tree views
- 📑 Nested categories
- 🏗️ Organization charts

## Contributing

Found another common mistake that AI makes? Open an issue or submit a PR!

## License

MIT - Use this code however you want. The goal is to help developers understand why AI-generated code fails and how to fix it.

---

**Remember**: ChatGPT and Copilot are powerful tools, but they can't always understand the subtle state management and component lifecycle requirements in frameworks like Blazor. Human understanding is still essential!
