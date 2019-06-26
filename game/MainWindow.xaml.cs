﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Serialization;
using System;

namespace Gamebook
{
   public partial class MainWindow: Window
   {
      private Game Game;

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
         Game.Set("jane", firstNameBox.Text);
         var lastNameBox = (TextBox)FindName("LastNameBox");
         Game.Set("smith", lastNameBox.Text);
         SetupScreen(null);
      }

      private void StoryAreaClicked(object sender, MouseButtonEventArgs e)
      {
         FinishTemporaryItems();
      }

      private void UndoItemSelected(object sender, RoutedEventArgs e)
      {
         Game.Undo();
         SetupScreen(null);
      }

      private void DebugModeItemSelected(object sender, RoutedEventArgs e)
      {
         Game.DebugMode = !Game.DebugMode;
         var debugModeItem = (ListBoxItem)FindName("DebugModeItem");
         debugModeItem.Content = Game.DebugMode ? "Turn off debug mode" : "Turn on debug mode";
         SetupScreen(null);
      }

      private void CharacterInfoItemSelected(object sender, RoutedEventArgs e)
      {
         var characterInfoBox = (Border)FindName("CharacterInfoBox");
         characterInfoBox.Visibility = Visibility.Visible;
      }

      private void SaveItemSelected(object sender, RoutedEventArgs e)
      {
         string json = JsonConvert.SerializeObject(Game, Formatting.Indented);
         var writer = new StreamWriter("save.json", false);
         writer.WriteLine(json);
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
         FinishTemporaryItems();
         SetupScreen(link);
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
                  case '`':
                     // Added debug stuff.
                     AddAccumulation(inlines, ref accumulator);
                     var span = new Span();
                     span.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xb0, 0x00));
                     span.Inlines.AddRange(BuildInlinesTo('~'));
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

         paragraph.Inlines.AddRange(BuildInlines(paragraphText));

         paragraph.Margin = new Thickness(0, 0, 0, 6);
         paragraph.LineHeight = 18;

         return paragraph;
      }

      private void SetupScreen(
         string selectedReactionText)
      {
         // It's simple. The engine builds a text version of the screen. Then this main window code converts that into WPF objects for display.
         var storyArea = (FlowDocumentScrollViewer)FindName("StoryArea");
         var (actionText, reactionTexts) = Game.BuildUnitTextForReaction(selectedReactionText);
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
         undoItem.IsEnabled = Game.CanUndo();
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

         // Get the source directory.
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
            Log.Fail("usage: gamebook.exe source-directory");

         // If there's a save game, deserialize it. Otherwise make a fresh game.
         if (File.Exists("save.json"))
         {
            Game = JsonConvert.DeserializeObject<Game>(File.ReadAllText("save.json"), Unit.LoadConverter(arguments[1]));
            Game.FixAfterDeserialization();
         }
         else
            Game = new Game(Unit.LoadFirst(arguments[1]));

         SetupScreen(null);
         var hamburgerMenu = (ListBox)FindName("HamburgerMenu");
         hamburgerMenu.Visibility = Visibility.Hidden;
         var firstNameBox = (TextBox)FindName("FirstNameBox");
         firstNameBox.Text = Game.Get("jane");
         var lastNameBox = (TextBox)FindName("LastNameBox");
         lastNameBox.Text = Game.Get("smith");
         //}
         //catch (Exception e)
         //{
         //  MessageBox.Show(String.Format("{0}", e), "Exception caught");
         //}
      }
   }
}

