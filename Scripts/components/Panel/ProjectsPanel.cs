using Godot;
using Godot.Collections;
using GodotSharpExtras;
using System.Linq;

public class ProjectsPanel : Panel
{
#region Node Accessors
    [NodePath("VC/MC/HC/ActionButtons")]
    ActionButtons _actionButtons = null;
    [NodePath("VC/SC/MarginContainer/ProjectList/ListView")]
    VBoxContainer _listView = null;
    [NodePath("VC/SC/MarginContainer/ProjectList/GridView")]
    GridContainer _gridView = null;
    [NodePath("VC/SC/MarginContainer/ProjectList/CategoryView")]
    VBoxContainer _categoryView = null;
    [NodePath("VC/MC/HC/ViewToggleButtons")]
    ViewToggleButtons _viewSelector = null;
#endregion

#region Template Scenes
    PackedScene _ProjectLineEntry = GD.Load<PackedScene>("res://components/ProjectLineEntry.tscn");
    PackedScene _ProjectIconEntry = GD.Load<PackedScene>("res://components/ProjectIconEntry.tscn");
    PackedScene _CategoryList = GD.Load<PackedScene>("res://components/CategoryList.tscn");
#endregion

#region Private Variables
    CategoryList clFavorites = null;
    CategoryList clUncategorized = null;
#endregion

    Array<Container> _views;

    public override void _Ready()
    {
        this.OnReady();

        _views = new Array<Container>();
        _views.Add(_listView);
        _views.Add(_gridView);
        _views.Add(_categoryView);

        _viewSelector.Connect("Clicked", this, "OnViewSelector_Clicked");
        _actionButtons.Connect("clicked", this, "OnActionButtons_Clicked");
        AppDialogs.ImportProject.Connect("update_projects", this, "PopulateListing");

        PopulateListing();
    }


    public ProjectLineEntry NewPLE(ProjectFile pf) {
        ProjectLineEntry ple = _ProjectLineEntry.Instance<ProjectLineEntry>();
        ple.ProjectFile = pf;
        return ple;
    }

    public ProjectIconEntry NewPIE(ProjectFile pf) {
        ProjectIconEntry pie = _ProjectIconEntry.Instance<ProjectIconEntry>();
        pie.ProjectFile = pf;
        return pie;
    }
    
    public CategoryList NewCL(string name) {
        CategoryList clt = _CategoryList.Instance<CategoryList>();
        clt.Toggable = true;
        clt.CategoryName = name;
        return clt;
    }

    public void PopulateListing() {
        ProjectLineEntry ple;
        ProjectIconEntry pie;
        CategoryList clt;

        foreach(Node child in _listView.GetChildren()) {
            child.QueueFree();
        }
        foreach(Node child in _gridView.GetChildren()) {
            child.QueueFree();
        }
        foreach(CategoryList child in _categoryView.GetChildren()) {
            foreach(Node cchild in child.List.GetChildren()) {
                cchild.QueueFree();
            }
            child.QueueFree();
        }

        foreach(Category cat in CentralStore.Categories) {
            clt = NewCL(cat.Name);
            clt.Set("ID",cat.Id);
            _categoryView.AddChild(clt);
        }

        clFavorites = NewCL("Favorites");
        clFavorites.Set("ID", -1);
        _categoryView.AddChild(clFavorites);

        clUncategorized = NewCL("Un-Categorized");
        clUncategorized.Set("ID",-2);
        _categoryView.AddChild(clUncategorized);

        foreach(ProjectFile pf in CentralStore.Projects) {
            ple = NewPLE(pf);
            pie = NewPIE(pf);
            _listView.AddChild(ple);
            ple.Connect("Clicked", this, "OnListEntry_Clicked");
            ple.Connect("DoubleClicked", this, "OnListEntry_DoubleClicked");
            pie.Connect("Clicked", this, "OnIconEntry_Clicked");
            pie.Connect("DoubleClicked", this, "OnIconEntry_DoubleClicked");
            _gridView.AddChild(pie);
            if (pf.CategoryId == -1) {
                clt = clUncategorized;
            } else {
                clt = _categoryView.GetChild<CategoryList>(pf.CategoryId);
            }
            ple = clt.AddProject(pf);
            ple.Connect("Clicked", this, "OnListEntry_Clicked");
            ple.Connect("DoubleClicked", this, "OnListEntry_DoubleClicked");
        }
    }

    private void UpdateListExcept(ProjectLineEntry ple) {
        foreach (ProjectLineEntry cple in _listView.GetChildren()) {
            if (cple != ple)
                cple.SelfModulate = new Color("00ffffff");
        }
    }

    void OnListEntry_Clicked(ProjectLineEntry ple) {
        UpdateListExcept(ple);
    }

    void OnListEntry_DoubleClicked(ProjectLineEntry ple) {
        GodotVersion gv = CentralStore.Instance.FindVersion(ple.GodotVersion);
        if (gv == null)
            return;
        GD.Print($"OS.Execute: {gv.GetExecutablePath()} --path \"{ple.Location.GetBaseDir()}\" -e");
        OS.Execute(gv.GetExecutablePath().GetOSDir(), new string[] { "--path", ple.Location.GetBaseDir(), "-e" }, false);
    }

    private void UpdateIconsExcept(ProjectIconEntry pie) {
        foreach(ProjectIconEntry cpie in _gridView.GetChildren()) {
            if (cpie != pie)
                cpie.SelfModulate = new Color("00FFFFFF");
        }
    }

    private void OnIconEntry_Clicked(ProjectIconEntry pie) {
        UpdateIconsExcept(pie);
    }

    private void OnIconEntry_DoubleClicked(ProjectIconEntry pie) {
        GodotVersion gv = CentralStore.Instance.FindVersion(pie.GodotVersion);
        if (gv == null)
            return;
        GD.Print($"OS.Execute: {gv.GetExecutablePath()} --path \"{pie.Location.GetBaseDir()}\" -e");
        OS.Execute(gv.GetExecutablePath().GetOSDir(), new string[] { "--path", pie.Location.GetBaseDir(), "-e" }, false);
    }

    void OnActionButtons_Clicked(int index) {
        switch (index) {
            case 0: // New Project File
                AppDialogs.CreateProject.ShowDialog();
                break;
            case 1: // Import Project File
                AppDialogs.ImportProject.ShowDialog();
                break;
            case 2: // Scan Project Folder
                break;
            case 3: // Remove Project (May be removed completely)
                break;
        }
    }

    void OnViewSelector_Clicked(int page) {
        for (int i = 0; i < _views.Count; i++) {
            if (i == page)
                _views[i].Show();
            else
                _views[i].Hide();
        }
    }

    public void AddTestProjects() {
        ProjectFile pf = ProjectFile.ReadFromFile(@"E:\Projects\Godot\godot-manager-mono\project.godot");
        CentralStore.Projects.Add(pf);
        pf = ProjectFile.ReadFromFile(@"E:\Projects\Godot\3D Platformer Demo\project.godot");
        CentralStore.Projects.Add(pf);
        pf = ProjectFile.ReadFromFile(@"E:\Projects\Godot\EditorPlugins\project.godot");
        CentralStore.Projects.Add(pf);
        pf = ProjectFile.ReadFromFile(@"E:\Projects\Godot\Godot-3D-Space-Shooter-main\project.godot");
        CentralStore.Projects.Add(pf);
        pf = ProjectFile.ReadFromFile(@"E:\Projects\Godot\Third Person Shooter Demo\project.godot");
        CentralStore.Projects.Add(pf);
        pf = ProjectFile.ReadFromFile(@"E:\Projects\src\mad-productivity\project.godot");
        CentralStore.Projects.Add(pf);
        CentralStore.Instance.SaveDatabase();
        PopulateListing();
    }

    public Array<ProjectFile> TestSortListing() {
        Array<ProjectFile> projectFiles = new Array<ProjectFile>();
        var pfolder = CentralStore.Projects.OrderByDescending(pf => pf.LastAccessed);
        foreach (ProjectFile pf in pfolder)
            projectFiles.Add(pf);
        return projectFiles;
    }

    public Array<ProjectFile> TestFavSortListing() {
        Array<ProjectFile> projectFiles = new Array<ProjectFile>();
        var fav = CentralStore.Projects.Where(pf => pf.Favorite == true).OrderByDescending(pf => pf.LastAccessed);
        var non_fav = CentralStore.Projects.Where(pf => pf.Favorite != true).OrderByDescending(pf => pf.LastAccessed);

        foreach(ProjectFile pf in fav)
            projectFiles.Add(pf);
        
        foreach(ProjectFile pf in non_fav)
            projectFiles.Add(pf);
        
        return projectFiles;
    }

    void OnButton_Pressed() {
        //AddTestProjects();
        
        Array<ProjectFile> projectFiles = TestFavSortListing(); //TestSortListing();

        foreach(ProjectFile pf in projectFiles) {
            GD.Print($"Project: {pf.Name}, Last Accessed: {pf.LastAccessed}");
        }
        
    }
}   