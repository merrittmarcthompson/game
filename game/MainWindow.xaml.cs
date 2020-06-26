#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.IO;
using System;
using System.Linq;

namespace Gamebook
{
   public partial class MainWindow: Window
   {
      private Game Game;

      // ex. change "hello" to “hello”.
      private static string VerticalToMatchingQuotes(
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

      private void CloseCharacterInfoBox()
      {
         if (CharacterInfoBox.Visibility == Visibility.Hidden)
            return;
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Hidden;
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Hidden;
         var firstNameBox = (TextBox)FindName("FirstNameBox");
         var lastNameBox = (TextBox)FindName("LastNameBox");
         Game.SetCharacterName(firstNameBox.Text, lastNameBox.Text);
      }

      private void StoryAreaClicked(object sender, MouseButtonEventArgs e)
      {
         CloseCharacterInfoBox();
      }

      private void SelectAllOnGetFocus(object sender, RoutedEventArgs e)
      {
         var textBox = (TextBox)sender;
         textBox.SelectAll();
      }

      private void UndoItemSelected(object sender, RoutedEventArgs e)
      {
         Game.Undo();
         SetupScreen();
      }

      private void DebugModeItemSelected(object sender, RoutedEventArgs e)
      {
         Game.DebugMode = !Game.DebugMode;
         var debugModeItem = (ListBoxItem)FindName("DebugModeItem");
         debugModeItem.Content = Game.DebugMode ? "Turn off debug mode" : "Turn on debug mode";
      }

      private void CharacterInfoItemSelected(object sender, RoutedEventArgs e)
      {
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Visible;
         var firstNameBox = (TextBox)FindName("FirstNameBox");
         firstNameBox.Focus();

      }

      private void SaveItemSelected(object sender, RoutedEventArgs e)
      {
         var writer = new StreamWriter("save.txt", false);
         Game.Save(writer);
         writer.Close();
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
         var link = hyperlink.CommandParameter as string;
         if (link == null)
            throw new InvalidOperationException(string.Format($"Internal error: hyperlink has no link"));
         CloseCharacterInfoBox();
         Game.MoveToReaction(link);
         SetupScreen();
      }

      private List<Inline> BuildInlines(
         string text)
      {
         // We need to add inlines to both Paragraphs and TextBoxes. They have Inlines properties, but you can't pass properties to functions as ref parameters, so we make a list of inlines and add them outside this function.
         text = VerticalToMatchingQuotes(text);
         // Em dashes
         text = text.Replace("--", "—");
         // Add a termination marker on the end.
         text += '\0';
         int index = 0;
         // Search for the marker we stuck on the end.
         return BuildInlinesTo('\0');

         List<Inline> BuildInlinesTo(
            char terminator)
         {
            var inlines = new List<Inline>();
            var accumulator = "";
            while (true)
            {
               switch (text[index++])
               {
                  case '{':
                     AddAccumulation(inlines, ref accumulator);
                     var hyperlink = new Hyperlink();
                     // No underline.
                     hyperlink.TextDecorations = null;
                     hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(0xc0, 0x00, 0x00));
                     hyperlink.Click += new RoutedEventHandler(HyperlinkClicked);
                     hyperlink.Cursor = Cursors.Hand;
                     var position = text.IndexOfAny(new char[] { '}', '\0' }, index);
                     hyperlink.CommandParameter = text.Substring(index, position - index);
                     hyperlink.Inlines.AddRange(BuildInlinesTo('}'));
                     inlines.Add(hyperlink);
                     break;
                  case '<':
                     AddAccumulation(inlines, ref accumulator);
                     var italic = new Italic();
                     italic.Inlines.AddRange(BuildInlinesTo('>'));
                     inlines.Add(italic);
                     break;
                  case Game.PositiveDebugTextStart:
                  case Game.NegativeDebugTextStart:
                     // Added debug stuff.
                     AddAccumulation(inlines, ref accumulator);
                     var span = new Span();
                     span.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xb0, 0x00));
                     if (text[index - 1] == Game.NegativeDebugTextStart)
                        span.TextDecorations = TextDecorations.Strikethrough;
                     span.Inlines.AddRange(BuildInlinesTo(Game.DebugTextStop));
                     inlines.Add(span);
                     break;
                  case '\0':
                     // Could be that the terminator we're looking for is missing, so keep an eye out for the final terminator (which is always there) and stop on that always. Don't increment the index past this. It must stop all missing terminator searches.
                     --index;
                     return AddAccumulation(inlines, ref accumulator);
                  case var letter:
                     if (letter == terminator)
                        return AddAccumulation(inlines, ref accumulator);
                     accumulator += letter;
                     break;
               }
            }
         }

         List<Inline> AddAccumulation(
            List<Inline> inlines,
            ref string accumulator)
         {
            if (accumulator.Length > 0)
            {
               inlines.Add(new Run(accumulator));
               accumulator = "";
            }
            return inlines;
         }
      }

      private Paragraph BuildBullet(
         string paragraphText)
      {
         var textBlock = new TextBlock();
         textBlock.Inlines.AddRange(BuildInlines(paragraphText));
         textBlock.TextWrapping = TextWrapping.Wrap;
         textBlock.Margin = new Thickness(8, 0, 0, 0);

         var bulletDecorator = new BulletDecorator();
#pragma warning disable CA1303 // Do not pass literals as localized parameters
         bulletDecorator.Bullet = new TextBlock(new Run("○"));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
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

         paragraph.Inlines.AddRange(BuildInlines(paragraphText));

         paragraph.Margin = new Thickness(0, 0, 0, 6);
         paragraph.LineHeight = 18;

         return paragraph;
      }

      private void SetupScreen()
      {
         // It's simple. The Game contains a text version of the screen. This main window code converts that into WPF objects for display.
         var first = true;
         FlowDocument document = new FlowDocument();
         document.FontFamily = new FontFamily("Calibri");
         document.FontSize = 13;
         document.MouseDown += StoryAreaClicked;
         document.TextAlignment = TextAlignment.Left;
         foreach (var paragraphText in Game.ActionText.Split('@'))
         {
            if (first && paragraphText.Length < 1)
               continue;
            first = false;
            document.Blocks.Add(BuildParagraph(paragraphText));
         }
         foreach (var reactionText in Game.ReactionTextsByScore)
            document.Blocks.Add(BuildBullet("{" + reactionText + "}"));
         var storyArea = (FlowDocumentScrollViewer)FindName("StoryArea");
         storyArea.Document = document;
         var undoItem = (ListBoxItem)FindName("UndoItem");
         undoItem.IsEnabled = Game.CanUndo();
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Hidden;
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Hidden;
         var firstNameBox = (TextBox)FindName("FirstNameBox");
         (firstNameBox.Text, LastNameBox.Text) = Game.GetCharacterName();
      }

      public MainWindow()
      {
         InitializeComponent();

         // Get the source directory.
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("usage: gamebook.exe source-directory");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

         // Load the static game world from .graphml files in this directory.
         var world = new World(arguments[1]);

         // If there's a save game state file, load it. Otherwise make a fresh game.
         if (File.Exists("save.txt"))
            using (var reader = new StreamReader("save.txt"))
               Game = new Game(reader, world);
         else
            Game = new Game(world);

         SetupScreen();
      }
   }
}
