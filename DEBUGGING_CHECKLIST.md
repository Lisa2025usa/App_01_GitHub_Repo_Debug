# Debugging Checklist: Why Won't My Blazor TOC Work?

Use this checklist to diagnose why your folder/file tree isn't expanding/collapsing.

## Basic Functionality Checks

### ✅ Does the folder have a state variable?

**Check your model:**
```csharp
public class FileSystemItem
{
    public string Name { get; set; } = "";
    public bool IsFolder { get; set; }
    public List<FileSystemItem> Children { get; set; } = new();
    
    // 👇 THIS MUST EXIST
    public bool IsExpanded { get; set; } = false;
}
```

**If missing**: Add the `IsExpanded` property to your model.

---

### ✅ Is there a toggle method?

**Check your component:**
```csharp
@code {
    private void ToggleFolder()
    {
        Item.IsExpanded = !Item.IsExpanded;
        StateHasChanged();
    }
}
```

**If missing**: Add a method that toggles `IsExpanded`.

---

### ✅ Is the toggle method wired to an onclick event?

**Check your markup:**
```razor
<div @onclick="ToggleFolder">
    📁 @Item.Name
</div>
```

**If missing**: Add `@onclick="ToggleFolder"` to your folder element.

---

### ✅ Is there conditional rendering of children?

**Check your markup:**
```razor
@if (Item.IsExpanded && Item.Children.Any())
{
    <div class="folder-content">
        @* Children rendered here *@
    </div>
}
```

**If missing**: Wrap your children rendering in an `@if` block.

---

## Advanced Diagnostics

### 🔍 Add logging to verify toggle is being called

```csharp
private void ToggleFolder()
{
    Console.WriteLine($"Before: {Item.Name} IsExpanded={Item.IsExpanded}");
    Item.IsExpanded = !Item.IsExpanded;
    Console.WriteLine($"After: {Item.Name} IsExpanded={Item.IsExpanded}");
    StateHasChanged();
}
```

**Open browser console (F12)** and click a folder. You should see:
```
Before: Documents IsExpanded=False
After: Documents IsExpanded=True
```

**If you don't see these logs**: The onclick event isn't firing. Check:
- Is `@onclick` on the correct element?
- Are there any JavaScript errors in the console?
- Is the element actually rendered? (inspect with DevTools)

**If logs show state changing but UI doesn't update**: 
- Try adding `StateHasChanged()`
- Check if CSS is hiding the content

---

### 🔍 Verify conditional rendering

Add temporary visible indicators:

```razor
<div>IsExpanded: @Item.IsExpanded</div>
<div>Children Count: @Item.Children.Count</div>

@if (Item.IsExpanded)
{
    <div style="background: yellow; padding: 10px;">
        EXPANDED - You should see this when folder is open
    </div>
}

@if (Item.IsExpanded && Item.Children.Any())
{
    <div class="folder-content">
        @* Your actual children rendering *@
    </div>
}
```

**You should see:**
- `IsExpanded: False` initially
- `IsExpanded: True` after clicking
- Yellow box appears when expanded

**If IsExpanded shows True but yellow box doesn't appear**:
- Problem with the `@if` condition
- Check for syntax errors

---

### 🔍 Check for event propagation issues

Add event handlers to both parent and child:

```razor
<div @onclick="() => Console.WriteLine($\"Parent clicked: {Item.Name}\")">
    <div @onclick="ToggleFolder">
        📁 @Item.Name
    </div>
</div>
```

**If both log messages appear**: Events are bubbling! Fix:
```razor
<div @onclick="() => Console.WriteLine($\"Parent clicked: {Item.Name}\")">
    <div @onclick="ToggleFolder" @onclick:stopPropagation="true">
        📁 @Item.Name
    </div>
</div>
```

---

### 🔍 Verify CSS isn't hiding content

**Temporarily add inline styles:**
```razor
@if (Item.IsExpanded && Item.Children.Any())
{
    <div style="display: block !important; background: lightblue;">
        @foreach (var child in Item.Children)
        {
            <div>Child: @child.Name</div>
        }
    </div>
}
```

**If children appear with this style**:
- Your CSS is overriding visibility
- Check for `display: none` in your stylesheets
- Remove CSS-based hiding logic

---

### 🔍 Test with minimal data

Create the simplest possible structure:

```csharp
protected override void OnInitialized()
{
    RootFolder = new FileSystemItem
    {
        Name = "Test Folder",
        IsFolder = true,
        IsExpanded = false,
        Children = new List<FileSystemItem>
        {
            new FileSystemItem { Name = "Test File.txt", IsFolder = false }
        }
    };
}
```

**If this works but your real data doesn't**:
- Problem with your data structure
- Check for null children lists
- Verify `IsFolder` is set correctly

---

## Common Error Messages

### "Object reference not set to an instance of an object"

**Cause**: `Item` or `Item.Children` is null.

**Fix**:
```csharp
[Parameter]
public FileSystemItem Item { get; set; } = new(); // ← Initialize!

// And check before using:
@if (Item?.Children?.Any() == true)
```

---

### "The type or namespace name 'FileSystemItem' could not be found"

**Cause**: Missing namespace or using statement.

**Fix**: Add to your component:
```razor
@using YourApp.Models
```

Or use fully qualified name:
```razor
@code {
    [Parameter]
    public YourApp.Models.FileSystemItem Item { get; set; } = new();
}
```

---

### Nothing happens, no errors

**Most likely causes**:
1. Toggle method is called but doesn't change state
2. State changes but component doesn't re-render
3. Component re-renders but `@if` condition is wrong
4. Condition passes but CSS hides the content

**Systematic debugging**:
1. Add `Console.WriteLine()` in toggle method
2. Display `IsExpanded` value in UI
3. Replace `@if` with `@if (true)` temporarily
4. Add inline styles to override CSS

---

## Quick Verification Test

Create this test component:

```razor
@page "/toc-test"

<h3>TOC Test</h3>

<div>
    <div @onclick="Toggle" style="cursor: pointer; background: #eee; padding: 10px;">
        @(IsExpanded ? "📂" : "📁") Click Me
    </div>
    
    @if (IsExpanded)
    {
        <div style="margin-left: 20px; background: lightblue; padding: 10px;">
            I am visible when expanded!
        </div>
    }
</div>

<div style="margin-top: 20px;">
    <strong>Debug Info:</strong>
    <div>IsExpanded: @IsExpanded</div>
    <div>Click count: @ClickCount</div>
</div>

@code {
    private bool IsExpanded { get; set; } = false;
    private int ClickCount { get; set; } = 0;
    
    private void Toggle()
    {
        ClickCount++;
        IsExpanded = !IsExpanded;
        Console.WriteLine($"Toggle called! IsExpanded={IsExpanded}, ClickCount={ClickCount}");
    }
}
```

**Expected behavior**:
1. Initially shows 📁 and "IsExpanded: False"
2. Click → Icon changes to 📂
3. Blue box appears
4. "IsExpanded: True" is shown
5. Click count increases
6. Click again → Everything reverses

**If this test works but your tree doesn't**:
- Problem is in your recursive component or data structure
- Not a fundamental Blazor issue

**If this test doesn't work**:
- Very basic Blazor issue
- Check Blazor is installed correctly
- Check you're using interactive render mode
- Verify browser console for errors

---

## Still Not Working?

### Final Checklist

- [ ] Using Blazor Server or WebAssembly (not static server-side rendering)
- [ ] Component has `@rendermode InteractiveServer` or `InteractiveWebAssembly`
- [ ] Browser JavaScript is enabled
- [ ] No browser console errors
- [ ] Blazor reconnection circle not showing (Server mode)
- [ ] Component is actually being rendered (check with DevTools)
- [ ] Event handlers are attached (check in DevTools)

### Check Render Mode

For Blazor Server (.NET 8+), your component needs:

```razor
@rendermode InteractiveServer
```

Or in your layout/page:

```razor
<FolderTreeView @rendermode="InteractiveServer" Item="@RootFolder" />
```

**Static SSR (Server-Side Rendering) won't work** because:
- No client-side interactivity
- `@onclick` handlers don't function
- State changes don't trigger re-renders

### Get Help

If you've tried everything:

1. Copy the working example from `Examples/FolderTreeView.razor`
2. Test it with your data
3. If working example works, compare it to your code line-by-line
4. If working example doesn't work, you have an environment issue

---

## Success Criteria

Your TOC works when:

- ✅ Clicking folder icon toggles between 📁 and 📂
- ✅ Children appear when folder is expanded
- ✅ Children disappear when folder is collapsed
- ✅ Nested folders work independently
- ✅ No console errors
- ✅ UI updates immediately on click

If all these work, congratulations! Your TOC is functioning correctly. 🎉
