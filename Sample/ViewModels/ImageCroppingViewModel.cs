using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ.Avalonia.Control;

namespace Sample.ViewModels;

public partial class ImageCroppingViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial Bitmap? SourceImage { get; set; }

    public double AspectRatio => SelectedItem.Value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AspectRatio))]
    public partial AspectRatioMap SelectedItem { get; set; } = AspectRatioMaps[0];

    public Bitmap? CroppedImage
    {
        get;
        set
        {
            OnPropertyChanged();
            field = value;
        }
    }

    public static AspectRatioMap[] AspectRatioMaps { get; set; } =
        [new("1:1", 1.0), new("4:3", 4.0 / 3.0), new("16:9", 16.0 / 9.0), new("自由比例", 0.0)];

    [RelayCommand]
    private async Task OpenImageButtonClick()
    {
        var topLevel = App.TopLevel;
        if (topLevel == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择图片",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("图片文件")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"],
                    },
                ],
            }
        );

        if (files.Count <= 0)
            return;

        try
        {
            var file = files[0];
            await using var stream = await file.OpenReadAsync();
            var bitmap = new Bitmap(stream);
            SourceImage = bitmap;
        }
        catch (Exception ex)
        {
            // ignored
        }
    }

    [RelayCommand]
    private static async Task SaveImageButtonClick(ImageCropperControl? imageCropper)
    {
        var croppedImage = imageCropper?.GetCroppedImage();
        if (croppedImage == null)
            return;

        var topLevel = App.TopLevel;
        if (topLevel == null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "保存裁剪图片",
                SuggestedFileName = new DateTimeOffset(DateTime.UtcNow)
                    .ToUnixTimeSeconds()
                    .ToString(),
                DefaultExtension = "png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG图片") { Patterns = ["*.png"] },
                    new FilePickerFileType("JPEG图片") { Patterns = ["*.jpg", "*.jpeg"] },
                ],
            }
        );

        if (file == null)
            return;
        try
        {
            await using var stream = await file.OpenWriteAsync();
            croppedImage.Save(stream);
        }
        catch (Exception ex)
        {
            // ignored
        }
    }
}

public record AspectRatioMap(string Key, double Value);
