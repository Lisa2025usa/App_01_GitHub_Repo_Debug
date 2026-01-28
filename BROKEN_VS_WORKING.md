# Common Mistakes: Broken vs Working Implementations

This document shows side-by-side comparisons of what ChatGPT/Copilot often generate (BROKEN) versus the correct implementation (WORKING).

## Mistake #1: Missing State Variable

### ❌ BROKEN (What AI Generates)

```razor
@* FolderItem.razor - BROKEN *@

<div @onclick="ToggleFolder">
    📁 @FolderName
</div>

<div class="folder-content">
    @ChildContent
</div>

@code {
    [Parameter]
    public string FolderName { get; set; } = "";
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    private void ToggleFolder()
    {
        // What should this do? There's no state to change!
        Console.WriteLine("Clicked!"); // This runs, but UI doesn't change
    }
}
```

**Why it's broken:**
- No boolean variable to track expanded/collapsed state
- The `ToggleFolder` method has nothing to toggle
- Child content is always rendered (never hidden)

### ✅ WORKING (Correct Implementation)

```razor
@* FolderItem.razor - WORKING *@

<div @onclick="ToggleFolder">
    @(IsExpanded ? "📂" : "📁") @FolderName
</div>

@if (IsExpanded)
{
    <div class="folder-content">
        @ChildContent
    </div>
}

@code {
    [Parameter]
    public string FolderName { get; set; } = "";
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    // CRITICAL: State variable
    private bool IsExpanded { get; set; } = false;
    
    private void ToggleFolder()
    {
        IsExpanded = !IsExpanded; // Toggle the state
    }
}
```

**Why it works:**
- `IsExpanded` tracks the folder's state
- `ToggleFolder` changes the state
- `@if (IsExpanded)` conditionally renders content
- Icon changes based on state

---

## Mistake #2: Using CSS Instead of Conditional Rendering

### ❌ BROKEN (What AI Generates)

```razor
@* BROKEN: Using CSS classes *@

<div @onclick="ToggleFolder" class="folder-header">
    📁 @FolderName
</div>

<div class="@GetContentClass()">
    @ChildContent
</div>

@code {
    private bool IsExpanded { get; set; } = false;
    
    private void ToggleFolder()
    {
        IsExpanded = !IsExpanded;
    }
    
    private string GetContentClass()
    {
        return IsExpanded ? "folder-content visible" : "folder-content hidden";
    }
}
```

```css
/* BROKEN CSS */
.folder-content.hidden {
    display: none; /* This might work, but it's not idiomatic Blazor */
}

.folder-content.visible {
    display: block;
}
```

**Why it's problematic:**
- CSS-based hiding is not the Blazor way
- DOM elements are still rendered (just hidden)
- More complex state management
- Can have CSS specificity issues

### ✅ WORKING (Correct Implementation)

```razor
@* WORKING: Using conditional rendering *@

<div @onclick="ToggleFolder" class="folder-header">
    @(IsExpanded ? "📂" : "📁") @FolderName
</div>

@if (IsExpanded)
{
    <div class="folder-content">
        @ChildContent
    </div>
}

@code {
    private bool IsExpanded { get; set; } = false;
    
    private void ToggleFolder()
    {
        IsExpanded = !IsExpanded;
    }
}
```

```css
/* WORKING CSS - No display:none tricks needed */
.folder-content {
    margin-left: 20px;
    /* That's it! No visibility logic in CSS */
}
```

**Why it works:**
- DOM elements are only created when needed
- Cleaner, more idiomatic Blazor code
- Better performance (fewer DOM nodes)
- No CSS tricks required

---

## Mistake #3: Missing Event Propagation Control

### ❌ BROKEN (What AI Generates)

```razor
@* BROKEN: Events bubble up *@

<div class="tree-node" @onclick="SelectNode">
    <div @onclick="ToggleFolder">
        📁 @FolderName
    </div>
    
    @if (IsExpanded)
    {
        <div class="children">
            @ChildContent
        </div>
    }
</div>

@code {
    private bool IsExpanded { get; set; } = false;
    
    private void ToggleFolder()
    {
        IsExpanded = !IsExpanded;
    }
    
    private void SelectNode()
    {
        // This fires when clicking the folder!
        // Both ToggleFolder AND SelectNode execute
        Console.WriteLine("Node selected");
    }
}
```

**Why it's broken:**
- Click events bubble from child to parent
- Both `ToggleFolder` and `SelectNode` fire
- Unpredictable behavior
- May cause multiple state changes

### ✅ WORKING (Correct Implementation)

```razor
@* WORKING: Stop propagation *@

<div class="tree-node" @onclick="SelectNode">
    <div @onclick="ToggleFolder" @onclick:stopPropagation="true">
        @(IsExpanded ? "📂" : "📁") @FolderName
    </div>
    
    @if (IsExpanded)
    {
        <div class="children">
            @ChildContent
        </div>
    }
</div>

@code {
    private bool IsExpanded { get; set; } = false;
    
    private void ToggleFolder()
    {
        IsExpanded = !IsExpanded;
        // Only this executes when clicking the folder icon
    }
    
    private void SelectNode()
    {
        // This only fires when clicking outside the folder icon
        Console.WriteLine("Node selected");
    }
}
```

**Why it works:**
- `@onclick:stopPropagation="true"` prevents event bubbling
- Each click handler executes independently
- Predictable, controlled behavior

---

## Mistake #4: Not Using Recursive Components

### ❌ BROKEN (What AI Generates)

```razor
@* BROKEN: Flat structure, no nesting support *@

<div class="folder">
    <div @onclick="ToggleFolder1">📁 Folder 1</div>
    @if (IsExpanded1)
    {
        <div class="file">📄 File 1.1</div>
        <div class="file">📄 File 1.2</div>
    }
</div>

<div class="folder">
    <div @onclick="ToggleFolder2">📁 Folder 2</div>
    @if (IsExpanded2)
    {
        <div class="file">📄 File 2.1</div>
        <div class="file">📄 File 2.2</div>
    }
</div>

@code {
    private bool IsExpanded1 { get; set; } = false;
    private bool IsExpanded2 { get; set; } = false;
    
    private void ToggleFolder1() => IsExpanded1 = !IsExpanded1;
    private void ToggleFolder2() => IsExpanded2 = !IsExpanded2;
}
```

**Why it's broken:**
- Hard-coded structure
- Can't handle dynamic data
- No support for nested folders
- Doesn't scale

### ✅ WORKING (Correct Implementation)

```razor
@* TreeNode.razor - WORKING: Recursive component *@

<div class="tree-node">
    @if (Item.IsFolder)
    {
        <div @onclick="ToggleFolder" @onclick:stopPropagation="true">
            @(Item.IsExpanded ? "📂" : "📁") @Item.Name
        </div>
        
        @if (Item.IsExpanded)
        {
            <div class="children">
                @foreach (var child in Item.Children)
                {
                    @* Recursive: Component renders itself *@
                    <TreeNode Item="child" />
                }
            </div>
        }
    }
    else
    {
        <div class="file">📄 @Item.Name</div>
    }
</div>

@code {
    [Parameter]
    public TreeItem Item { get; set; } = new();
    
    private void ToggleFolder()
    {
        Item.IsExpanded = !Item.IsExpanded;
    }
}
```

**Why it works:**
- Component renders itself for children
- Handles arbitrary nesting depth
- Works with dynamic data
- Each folder has its own state
- Fully scalable

---

## Mistake #5: Forgetting StateHasChanged()

### ❌ BROKEN (What AI Generates)

```razor
@code {
    private bool IsExpanded { get; set; } = false;
    
    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(1000);
        IsExpanded = true; // Might not trigger re-render!
    }
    
    private async Task LoadDataAndExpand()
    {
        await LoadSomeDataAsync();
        IsExpanded = true; // Might not trigger re-render!
    }
}
```

**Why it's broken:**
- Async methods may not trigger automatic re-renders
- State changes in event callbacks might be missed
- UI can get out of sync with state

### ✅ WORKING (Correct Implementation)

```razor
@code {
    private bool IsExpanded { get; set; } = false;
    
    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(1000);
        IsExpanded = true;
        StateHasChanged(); // Explicit re-render
    }
    
    private async Task LoadDataAndExpand()
    {
        await LoadSomeDataAsync();
        IsExpanded = true;
        StateHasChanged(); // Explicit re-render
    }
    
    // For synchronous event handlers, usually not needed:
    private void ToggleFolder()
    {
        IsExpanded = !IsExpanded;
        // StateHasChanged() is automatic here, but doesn't hurt
    }
}
```

**Why it works:**
- Explicit `StateHasChanged()` ensures UI updates
- Defensive programming prevents state sync issues
- Works reliably in all scenarios

---

## Mistake #6: Wrong Model Structure

### ❌ BROKEN (What AI Generates)

```csharp
// BROKEN: State not in the model
public class FolderItem
{
    public string Name { get; set; } = "";
    public List<FolderItem> Children { get; set; } = new();
    // Missing: IsExpanded property!
}
```

```razor
@code {
    private FolderItem RootFolder { get; set; } = new();
    
    // Separate dictionary to track state - messy!
    private Dictionary<FolderItem, bool> ExpandedState { get; set; } = new();
    
    private void ToggleFolder(FolderItem item)
    {
        if (ExpandedState.ContainsKey(item))
            ExpandedState[item] = !ExpandedState[item];
        else
            ExpandedState[item] = true;
    }
}
```

**Why it's broken:**
- State is separate from the model
- Complex state management
- Hard to serialize/deserialize
- Prone to bugs

### ✅ WORKING (Correct Implementation)

```csharp
// WORKING: State in the model
public class FolderItem
{
    public string Name { get; set; } = "";
    public List<FolderItem> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = false; // State here!
}
```

```razor
@code {
    private FolderItem RootFolder { get; set; } = new();
    
    private void ToggleFolder(FolderItem item)
    {
        item.IsExpanded = !item.IsExpanded; // Simple and clean!
    }
}
```

**Why it works:**
- State is part of the model
- Simple, intuitive code
- Easy to serialize
- Each item owns its state

---

## Complete Working Example Summary

Here's the minimal complete implementation:

### Model
```csharp
public class FileSystemItem
{
    public string Name { get; set; } = "";
    public bool IsFolder { get; set; }
    public List<FileSystemItem> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = false; // ← KEY!
}
```

### Component
```razor
<div class="tree-item">
    @if (Item.IsFolder)
    {
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
    }
    else
    {
        <div>📄 @Item.Name</div>
    }
</div>

@code {
    [Parameter]
    public FileSystemItem Item { get; set; } = new();
    
    private void ToggleFolder()
    {
        if (Item.IsFolder)
        {
            Item.IsExpanded = !Item.IsExpanded;
            StateHasChanged();
        }
    }
}
```

### Key Takeaways

1. ✅ **State in the model** (`IsExpanded` property)
2. ✅ **Conditional rendering** (`@if`, not CSS)
3. ✅ **Event propagation control** (`@onclick:stopPropagation`)
4. ✅ **Recursive design** (component renders itself)
5. ✅ **Explicit re-renders** (`StateHasChanged()`)
6. ✅ **Icons reflect state** (`IsExpanded ? "📂" : "📁"`)

Miss any one of these, and the TOC won't work properly!
