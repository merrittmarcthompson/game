﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Game
{
   public partial class MainWindow : Window
   {
      private DateTime NextGoodClick = DateTime.Now;

      void UndoClicked(object sender, RoutedEventArgs e)
      {
         Engine.Undo();
         SetupScreen();
      }

      private void HyperlinkClicked(object sender, RoutedEventArgs e)
      {
         var hyperlink = (Hyperlink)sender;
         var link = hyperlink.Inlines.FirstInline.ContentStart.GetTextInRun(LogicalDirection.Forward);
         Engine.SelectOption(link);
         SetupScreen();
      }

      private TextBlock TextToWPF(
        string paragraph)
      // Transform the text into a WPF TextBlock. Representing paragraphs with TextBlocks lets us make space after the paragraph, which isn't possible with continuous text. 
      {
         var block = new TextBlock();
         paragraph = Transform.RemoveBlanksAfterNewLines(paragraph);
         paragraph = Transform.VerticalToMatchingQuotes(paragraph);
         // Em dashes
         paragraph = paragraph.Replace("--", "—");
         var accumulator = "";
         var start = 0;
         var isBullet = false;
         if (paragraph.Length > 0 && paragraph[0] == '~')
         {
            // Bullet
            accumulator = "•  ";
            start = 1;
            isBullet = true;
         }

         // TO DO: no way to put italic in a hyperlink or vice versa.

         for (var i = start; i < paragraph.Length; ++i)
         {
            // Hyperlink
            if (paragraph[i] == '{')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               for (++i; i < paragraph.Length && paragraph[i] != '}'; ++i)
               {
                  accumulator += paragraph[i];
               }
               var run = new Run(accumulator);
               var hyperlink = new Hyperlink(run);
               hyperlink.TextDecorations = null;
               hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(0xa0, 0x00, 0x00));
               hyperlink.Click += new RoutedEventHandler(HyperlinkClicked);
               hyperlink.Cursor = Cursors.Hand;
               block.Inlines.Add(hyperlink);
               accumulator = "";
            }
            // Italic
            else if (paragraph[i] == '<')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               for (++i; i < paragraph.Length && paragraph[i] != '>'; ++i)
               {
                  accumulator += paragraph[i];
               }
               var run = new Run(accumulator);
               block.Inlines.Add(new Italic(run));
               accumulator = "";
            }
            else
            {
               accumulator += paragraph[i];
            }
         }
         if (accumulator.Length > 0)
         {
            block.Inlines.Add(new Run(accumulator));
         }

         block.TextWrapping = TextWrapping.Wrap;
         if (isBullet)
         {
            // Indent more and make them closer together.
            block.Margin = new Thickness(14, 0, 5, 3);
         }
         else
         {
            block.Margin = new Thickness(5, 4, 5, 4);
         }
         block.LineHeight = 18;
         
         return block;
         }

      private void SetupScreen()
      {
         // Large test text for screen size measurements.
         //var text = "It was about eleven o’clock in the morning, mid October, with the sun not shining and a look of hard wet rain in the clearness of the foothills. I was wearing my powder-blue suit, with dark blue shirt, tie and display handkerchief, black brogues, black wool socks with dark blue clocks on them. I was neat, clean, shaved and sober, and I didn’t care who knew it. I was everything a well dressed private detective ought to be. I was calling on four million dollars.\r\n   The main hallway of the Sternwood place was two stories high. Over the entrance doors, which would have let in a troop of Indian elephants, there was a broad stained-glass panel showing a knight in dark armour rescuing a lady who was tied to a tree and didn’t have any clothes on but some very long and convenient hair. The knight had pushed the visor of his helmet back to be sociable, and he was fiddling with the knots on the ropes that tied the lady to the tree and not getting anywhere. I stood there and thought that if I lived in the house, I would sooner or later have to climb up there and help him. He didn’t seem to be really trying.\r\n   There were French doors at the back of the hall, beyond them a wide sweep of emerald grass to a white garage, in front of which a slim, dark, young chauffeur in shiny black leggings was dusting a maroon Packard convertible. Beyond the garage were some decorative trees trimmed as carefully as poodle dogs. Beyond them a large greenhouse with a domed roof. Then more trees and beyond everything the solid, uneven, comfortable line of the foothills.";

         // It's simple. The engine builds a text version of the screen. Then this main window code converts that into WPF objects for display.
         var storyArea = (ItemsControl)FindName("StoryArea");
         storyArea.Items.Clear();
         var text = Engine.BuildNextText();

         //text = "@Jane was in her living room, holding the Vault Tec {pamphlet}. @She could hear little Bobby starting to get fussy in the nursery room.@~{Go check on Bobby.}@There was the clicking sound of Tom tapping his razor on the edge of the bathroom sink.@~{Go see Tom.}";

         var first = true;
         foreach (var piece in text.Split('@'))
         {
            var paragraph = Transform.RemoveExtraBlanks(piece);
            if (first && paragraph.Length < 1)
               continue;
            first = false;
            storyArea.Items.Add(TextToWPF(paragraph));
         }
         var undoButton = (Button)FindName("UndoButton");
         undoButton.Visibility = Engine.canUndo()? Visibility.Visible: Visibility.Hidden;
      }

      public MainWindow()
      {
         //try
         //{
         Log.Open("game.log");
         Log.Add("Started");
         InitializeComponent();
         Engine.LoadSource();
         SetupScreen();
         //}
         //catch (Exception e)
         //{
         //  MessageBox.Show(String.Format("{0}", e), "Exception caught");
         //}
      }
   }
}

