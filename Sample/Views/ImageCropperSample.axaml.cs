using System;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Sample;

public partial class ImageCropperSample : UserControl
{
    public ImageCropperSample()
    {
        InitializeComponent();

        OpenImageButton.Click += OpenImageButton_Click;
        SaveImageButton.Click += SaveImageButton_Click;
        AspectRatioComboBox.SelectionChanged += AspectRatioComboBox_SelectionChanged;
    }

    private async void OpenImageButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e
    )
    {
        var topLevel = TopLevel.GetTopLevel(this);
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

        if (files.Count > 0)
        {
            try
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
                ImageCropper.SourceImage = bitmap;
                SaveImageButton.IsEnabled = true;
            }
            catch (Exception ex) { }
        }
    }

    private async void SaveImageButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e
    )
    {
        var croppedImage = ImageCropper.GetCroppedImage();
        if (croppedImage == null)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "保存裁剪图片",
                DefaultExtension = "png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG图片") { Patterns = ["*.png"] },
                    new FilePickerFileType("JPEG图片") { Patterns = ["*.jpg", "*.jpeg"] },
                ],
            }
        );

        if (file != null)
        {
            try
            {
                await using var stream = await file.OpenWriteAsync();
                croppedImage.Save(stream);
            }
            catch (Exception ex) { }
        }
    }

    private void AspectRatioComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AspectRatioComboBox.SelectedItem is ComboBoxItem item)
        {
            switch (item.Content?.ToString())
            {
                case "1:1":
                    ImageCropper.AspectRatio = 1.0;
                    break;
                case "4:3":
                    ImageCropper.AspectRatio = 4.0 / 3.0;
                    break;
                case "16:9":
                    ImageCropper.AspectRatio = 16.0 / 9.0;
                    break;
                case "自由比例":
                    ImageCropper.AspectRatio = 0; // 0表示自由比例
                    break;
            }
        }
    }
}
