using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LiveToMoveUI.Views;

public partial class MainWindow : Window
{
    private string _dropBoxLabelText;
    
    public MainWindow()
    {
        InitializeComponent();

        _dropBoxLabelText = this.DropBoxBlock.Text;

        this.ResultBlock.Opacity = 0;
        this.ProcessButton.Click += this.OnProcessClicked;
        
        this.AddHandler(DragDrop.DropEvent, this.OnDrop);
        this.AddHandler(DragDrop.DragOverEvent, this.OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;
    }

    private async void OnProcessClicked(object? sender, RoutedEventArgs e)
    {
        var processText = this.ProcessButton.Content;
        this.IsEnabled = false;

        var cts = new CancellationTokenSource();
        _ = this.AnimateButtonText(this.ProcessButton, "Processing", cts.Token);
        
        try
        {
            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(500, cts.Token);
            }
        }
        finally
        {
            await cts.CancelAsync();

            this.ProcessButton.Content = processText;
            this.IsEnabled = true;
            
            this.ResultBlock.Opacity = 1;
        }
    }
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;

        if (e.Data.Contains(DataFormats.Files))
        {
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file != null)
            {
                this.DropBoxBlock.Text = file.Name;
            }
        }
    }
    
    private async Task AnimateButtonText(Button button, string text, CancellationToken token)
    {
        var baseText = text;
        string[] states = [".", "..", "..."];
        var index = 0;

        while (!token.IsCancellationRequested)
        {
            button.Content = baseText + states[index];
            index = (index + 1) % states.Length;
            
            await Task.Delay(250, token); 
        }
    }
}