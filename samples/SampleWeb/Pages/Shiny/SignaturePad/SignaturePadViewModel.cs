using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.SignaturePad;

public partial class SignaturePadViewModel : ObservableObject
{
    [ObservableProperty]
    bool isSignatureOpen;

    [ObservableProperty]
    ImageSource? signatureImage;

    [ObservableProperty]
    string statusMessage = "No signature captured";

    public bool HasSignature => SignatureImage != null;

    [RelayCommand]
    void OpenSignature() => IsSignatureOpen = true;

    [RelayCommand]
    void HandleSigned(Shiny.Maui.Controls.SignaturePad.SignatureImageEventArgs args)
    {
        var ms = new MemoryStream();
        args.ImageStream.CopyTo(ms);
        // A fresh stream per read: the image may load the source again (a recycled handler).
        var png = ms.ToArray();
        SignatureImage = ImageSource.FromStream(() => new MemoryStream(png));
        OnPropertyChanged(nameof(HasSignature));
        StatusMessage = "Signature captured!";
    }

    [RelayCommand]
    void HandleCancelled()
    {
        StatusMessage = "Signature cancelled";
    }

    [RelayCommand]
    void ClearSignature()
    {
        SignatureImage = null;
        OnPropertyChanged(nameof(HasSignature));
        StatusMessage = "Signature cleared";
    }
}
