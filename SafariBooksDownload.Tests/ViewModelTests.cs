namespace SafariBooksDownload.Tests;

public class MainViewModelTests
{
    private static List<string> Capture(MainViewModel vm)
    {
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);
        return changed;
    }

    [Fact]
    public void NewViewModel_StartsEmpty()
    {
        var vm = new MainViewModel();

        Assert.Empty(vm.Books);
        Assert.NotNull(vm.DownloadProgress);
        Assert.Equal("", vm.LastFileDownloadPath);
        Assert.False(vm.SahreButtonVisible);
        Assert.False(vm.RetainFolder);
        Assert.False(vm.searchInProgress);
    }

    [Fact]
    public void SettingLastFileDownloadPath_RaisesBothPathAndShareVisibility()
    {
        var vm = new MainViewModel();
        var changed = Capture(vm);

        vm.LastFileDownloadPath = "/tmp/a.epub";

        Assert.Equal([nameof(MainViewModel.LastFileDownloadPath), nameof(MainViewModel.SahreButtonVisible)], changed);
        Assert.True(vm.SahreButtonVisible);
    }

    [Fact]
    public void SettingTheSameValue_DoesNotRaise()
    {
        var vm = new MainViewModel { LastFileDownloadPath = "a", LastFileDownloadName = "n", RetainFolder = true, searchInProgress = true };
        var changed = Capture(vm);

        vm.LastFileDownloadPath = "a";
        vm.LastFileDownloadName = "n";
        vm.RetainFolder = true;
        vm.searchInProgress = true;

        Assert.Empty(changed);
    }

    [Fact]
    public void OtherProperties_RaiseOnChange()
    {
        var vm = new MainViewModel();
        var changed = Capture(vm);

        vm.LastFileDownloadName = "n";
        vm.RetainFolder = true;
        vm.searchInProgress = true;

        Assert.Equal(
            [nameof(MainViewModel.LastFileDownloadName), nameof(MainViewModel.RetainFolder), nameof(MainViewModel.searchInProgress)],
            changed);
        Assert.Equal("n", vm.LastFileDownloadName);
    }

    [Fact]
    public void ClearingThePath_HidesTheShareButton()
    {
        var vm = new MainViewModel { LastFileDownloadPath = "x" };

        vm.LastFileDownloadPath = "";

        Assert.False(vm.SahreButtonVisible);
    }
}

public class DownloadViewModelTests
{
    [Fact]
    public void EachProperty_RaisesItsOwnChange()
    {
        var vm = new DownloadViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.DownloadLabel = "d";
        vm.ProgressLabel = "p";
        vm.ProgressBarValue = 0.5;

        Assert.Equal(["DownloadLabel", "ProgressLabel", "ProgressBarValue"], changed);
        Assert.Equal(("d", "p", 0.5), (vm.DownloadLabel, vm.ProgressLabel, vm.ProgressBarValue));
    }

    [Fact]
    public void SettingWithoutSubscribers_DoesNotThrow()
    {
        var vm = new DownloadViewModel { ProgressBarValue = 1 };

        Assert.Equal(1, vm.ProgressBarValue);
    }
}
