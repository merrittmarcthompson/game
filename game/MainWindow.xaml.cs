using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace Gamebook
{
   public partial class MainWindow : Window
   {
      // ex. change "hello" to “hello”.
      private string VerticalToMatchingQuotes(
        string text)
      {
         var result = "";
         var testText = " " + text;
         for (int i = 1; i < testText.Length; ++i)
         {
            var letter = testText[i];
            if (letter == '"')
            {
               if (testText[i - 1] == ' ')
               {
                  result += '“';
               }
               else
               {
                  result += '”';
               }
            }
            else
            {
               result += letter;
            }
         }
         return result;
      }

      private void FinishTemporaryItems()
      {
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Hidden;
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Hidden;
         var firstNameBox = (TextBox)FindName("FirstNameBox");
         Engine.Set("jane", firstNameBox.Text);
         var lastNameBox = (TextBox)FindName("LastNameBox");
         Engine.Set("smith", lastNameBox.Text);
         SetupScreen(null);
      }

      private void StoryAreaClicked(object sender, MouseButtonEventArgs e)
      {
         FinishTemporaryItems();
      }

      private void UndoItemSelected(object sender, RoutedEventArgs e)
      {
         Engine.Undo();
         SetupScreen(null);
      }

      private void DebugModeItemSelected(object sender, RoutedEventArgs e)
      {
         Engine.DebugMode = !Engine.DebugMode;
         var debugModeItem = (ListBoxItem)FindName("DebugModeItem");
         debugModeItem.Content = Engine.DebugMode? "Turn off debug mode": "Turn on debug mode";
         SetupScreen(null);
      }

      private void CharacterInfoItemSelected(object sender, RoutedEventArgs e)
      {
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Visible;
      }

      void HamburgerClicked(object sender, RoutedEventArgs e)
      {
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Visible;
      }

      private void HamburgerMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Hidden;
         hamburgerMenu.SelectedIndex = -1;
      }

      private void HyperlinkClicked(object sender, RoutedEventArgs e)
      {
         var hyperlink = (Hyperlink)sender;
         var link = hyperlink.Inlines.FirstInline.ContentStart.GetTextInRun(LogicalDirection.Forward);
         FinishTemporaryItems();
         SetupScreen(link);
      }

      private List<Inline> BuildInlines(
         string text)
      {
         // We need to add inlines to both Paragraphs and TextBoxes. They have Inlines properties, but you can't pass properties to functions as ref parameters, so we make a list of inlines and add them outside this function.

         // TO DO: there's no way to put italic in a hyperlink or vice versa.

         var inlines = new List<Inline>();

         text = VerticalToMatchingQuotes(text);
         // Em dashes
         text = text.Replace("--", "—");
         var accumulator = "";
         for (var i = 0; i < text.Length; ++i)
         {
            // Hyperlink
            if (text[i] == '{')
            {
               if (accumulator.Length > 0)
               {
                  inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               for (++i; i < text.Length && text[i] != '}'; ++i)
                  accumulator += text[i];
               var run = new Run(accumulator);
               var hyperlink = new Hyperlink(run);
               hyperlink.TextDecorations = null;
               hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(0xc0, 0x00, 0x00));
               hyperlink.Click += new RoutedEventHandler(HyperlinkClicked);
               hyperlink.Cursor = Cursors.Hand;
               inlines.Add(hyperlink);
               accumulator = "";
            }
            // Italic
            else if (text[i] == '<')
            {
               if (accumulator.Length > 0)
               {
                  inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               for (++i; i < text.Length && text[i] != '>'; ++i)
                  accumulator += text[i];
               var run = new Run(accumulator);
               inlines.Add(new Italic(run));
               accumulator = "";
            }
            else if (text[i] == '`')
            {
               if (accumulator.Length > 0)
               {
                  inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               for (++i; i < text.Length && text[i] != '`'; ++i)
                  accumulator += text[i];
               var run = new Run(accumulator);
               run.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xb0, 0x00));
               inlines.Add(run);
               accumulator = "";
            }
            else
               accumulator += text[i];
         }
         if (accumulator.Length > 0)
            inlines.Add(new Run(accumulator));

         return inlines;
      }

      private Paragraph BuildBullet(
         string paragraphText)
      {
         var textBlock = new TextBlock();
         foreach (var inline in BuildInlines(paragraphText))
            textBlock.Inlines.Add(inline);
         textBlock.TextWrapping = TextWrapping.Wrap;
         textBlock.Margin = new Thickness(8, 0, 0, 0);

         var bulletDecorator = new BulletDecorator();
         bulletDecorator.Bullet = new TextBlock(new Run("○"));
         bulletDecorator.Child = textBlock;
         
         var paragraph = new Paragraph();
         paragraph.Inlines.Add(bulletDecorator);
         paragraph.Margin = new Thickness(0, 0, 0, 6);
         paragraph.LineHeight = 18;

         return paragraph;
      }

      private Paragraph BuildParagraph(
        string paragraphText)
      {
         var paragraph = new Paragraph();

         foreach (var inline in BuildInlines(paragraphText))
            paragraph.Inlines.Add(inline);

         paragraph.Margin = new Thickness(0, 0, 0, 6);
         paragraph.LineHeight = 18;

         return paragraph;
      }

      private void SetupScreen(
         string selectedReactionText)
      {
         // It's simple. The engine builds a text version of the screen. Then this main window code converts that into WPF objects for display.
         var storyArea = (FlowDocumentScrollViewer)FindName("StoryArea");
         var (actionText, reactionTexts) = Engine.BuildRoundTextForReaction(selectedReactionText);
         var first = true;
         FlowDocument document = new FlowDocument();
         document.FontFamily = new FontFamily("Segoe UI");
         document.FontSize = 12;
         document.MouseDown += StoryAreaClicked;
         document.TextAlignment = TextAlignment.Left;
         foreach (var paragraphText in actionText.Split('@'))
         {
            if (first && paragraphText.Length < 1)
               continue;
            first = false;
            document.Blocks.Add(BuildParagraph(paragraphText));
         }
         foreach (var reactionText in reactionTexts)
         {
            document.Blocks.Add(BuildBullet(reactionText));
         }
         storyArea.Document = document;
         var undoItem = (ListBoxItem)FindName("UndoItem");
         undoItem.IsEnabled = Engine.canUndo();
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Hidden;
      }

      public MainWindow()
      {
         //try
         //{
         Log.Open("gamebook.log");
         Log.Add("Started");
         InitializeComponent();
         Engine.Start();
         SetupScreen(null);
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Hidden;
         var firstNameBox = (TextBox)FindName("FirstNameBox");
         firstNameBox.Text = Engine.Get("jane");
         var lastNameBox = (TextBox)FindName("LastNameBox");
         lastNameBox.Text = Engine.Get("smith");
         //}
         //catch (Exception e)
         //{
         //  MessageBox.Show(String.Format("{0}", e), "Exception caught");
         //}
      }
   }
}

