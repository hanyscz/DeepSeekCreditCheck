using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.UI.Windows
{
    public partial class LoginWindow : Window
    {
        public string? CapturedToken { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            Title = LocalizationService.Instance["platform_login_title"];
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                
                // Filtrovat požadavky na interní API v0 platformy
                webView.CoreWebView2.AddWebResourceRequestedFilter("https://platform.deepseek.com/api/v0/*", CoreWebView2WebResourceContext.All);
                webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při inicializaci WebView2: {ex.Message}\nUjistěte se, že máte nainstalován Microsoft Edge WebView2 Runtime.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (e.Request.Headers.Contains("authorization"))
            {
                var authHeader = e.Request.Headers.GetHeader("authorization");
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    CapturedToken = authHeader;
                    
                    // Zavřít okno na hlavním UI vlákně
                    Dispatcher.BeginInvoke(() =>
                    {
                        DialogResult = true;
                        Close();
                    });
                }
            }
        }
    }
}
