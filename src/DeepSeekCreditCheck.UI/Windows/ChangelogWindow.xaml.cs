using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class ChangelogWindow : Window
{
    private readonly IChangelogService _changelogService;
    private readonly LocalizationService _loc;

    public ChangelogWindow(IChangelogService? changelogService = null, LocalizationService? loc = null)
    {
        InitializeComponent();

        _changelogService = changelogService ?? ChangelogService.Instance;
        _loc = loc ?? LocalizationService.Instance;

        Title = _loc["changelog_title"];
        CloseButton.Content = _loc["changelog_close_btn"];

        var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (appVersion != null)
        {
            CurrentVersionText.Text = $"v{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}";
        }

        LoadChangelogDocument();
    }

    public void SetUpdateSuccessVersion(string? version)
    {
        if (!string.IsNullOrWhiteSpace(version))
        {
            UpdateBanner.Visibility = Visibility.Visible;
            UpdateBannerMessage.Text = _loc.Format("changelog_update_banner", version);
        }
        else
        {
            UpdateBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadChangelogDocument()
    {
        var rawText = _changelogService.GetChangelogText();
        var doc = new FlowDocument
        {
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            PagePadding = new Thickness(16, 12, 16, 12)
        };

        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        List? currentList = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                currentList = null;
                continue;
            }

            // H1 (# Changelog ...)
            if (trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
            {
                currentList = null;
                // Přeskočíme hlavní H1, máme ho v hlavičce okna
                continue;
            }

            // H2 (## v1.8.0 ...)
            if (trimmed.StartsWith("## "))
            {
                currentList = null;
                var headingText = trimmed.Substring(3).Trim();

                var p = new Paragraph
                {
                    Margin = new Thickness(0, doc.Blocks.Count == 0 ? 0 : 20, 0, 8)
                };

                var run = new Run(headingText)
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132)) // #81C784 zelená
                };
                p.Inlines.Add(run);
                doc.Blocks.Add(p);
                continue;
            }

            // H3 (### ✨ Nové funkce ...)
            if (trimmed.StartsWith("### "))
            {
                currentList = null;
                var subHeadingText = trimmed.Substring(4).Trim();

                var p = new Paragraph
                {
                    Margin = new Thickness(0, 12, 0, 6)
                };

                var run = new Run(subHeadingText)
                {
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 183, 77)) // #FFB74D zlatá/oranžová
                };
                p.Inlines.Add(run);
                doc.Blocks.Add(p);
                continue;
            }

            // Oddělovač (---)
            if (trimmed == "---")
            {
                currentList = null;
                var p = new Paragraph
                {
                    Margin = new Thickness(0, 14, 0, 14)
                };
                var border = new System.Windows.Shapes.Rectangle
                {
                    Height = 1,
                    Fill = new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                p.Inlines.Add(new InlineUIContainer(border));
                doc.Blocks.Add(p);
                continue;
            }

            // Odrážky (* text nebo - text)
            if (trimmed.StartsWith("* ") || trimmed.StartsWith("- "))
            {
                if (currentList == null)
                {
                    currentList = new List
                    {
                        MarkerStyle = TextMarkerStyle.Disc,
                        Margin = new Thickness(10, 2, 0, 8),
                        Padding = new Thickness(0)
                    };
                    doc.Blocks.Add(currentList);
                }

                var itemText = trimmed.Substring(2).Trim();
                var listItem = new ListItem();
                var p = new Paragraph { Margin = new Thickness(0, 2, 0, 4) };

                ParseFormattedInlines(p, itemText);
                listItem.Blocks.Add(p);
                currentList.ListItems.Add(listItem);
                continue;
            }

            // Běžný odstavec
            currentList = null;
            var para = new Paragraph { Margin = new Thickness(0, 2, 0, 6) };
            ParseFormattedInlines(para, trimmed);
            doc.Blocks.Add(para);
        }

        DocumentViewer.Document = doc;
    }

    private static void ParseFormattedInlines(Paragraph paragraph, string text)
    {
        // Parsování bold (**text**) a inline kódu (`text`)
        var pattern = @"(\*\*[^*]+\*\*|`[^`]+`)";
        var parts = Regex.Split(text, pattern);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
            {
                var boldContent = part.Substring(2, part.Length - 4);
                paragraph.Inlines.Add(new Bold(new Run(boldContent))
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240))
                });
            }
            else if (part.StartsWith("`") && part.EndsWith("`") && part.Length > 2)
            {
                var codeContent = part.Substring(1, part.Length - 2);
                var codeSpan = new Span(new Run(codeContent))
                {
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush(Color.FromRgb(79, 195, 247)), // #4FC3F7 modrá
                    Background = new SolidColorBrush(Color.FromRgb(35, 35, 35))
                };
                paragraph.Inlines.Add(codeSpan);
            }
            else
            {
                paragraph.Inlines.Add(new Run(part)
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200))
                });
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
