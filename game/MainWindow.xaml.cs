using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Game
{
   public partial class MainWindow : Window
   {
      private DateTime NextGoodClick = DateTime.Now;

      private void HyperlinkClicked(object sender, RoutedEventArgs e)
      {
         var hyperlink = (Hyperlink)sender;
         var link = hyperlink.Inlines.FirstInline.ContentStart.GetTextInRun(LogicalDirection.Forward);
         Engine.SelectLink(link);
         SetupScreen();
      }

      private TextBlock TextToWPF(
        string paragraph)
      // Transform the text into a WPF TextBlock. The TextBlock represents a paragraph. This allows us to have spaces between paragraphs (though I don't use that feature at the moment).
      {
         var block = new TextBlock();
         paragraph = Transform.RemoveBlanksAfterNewLines(paragraph);
         paragraph = Transform.VerticalToMatchingQuotes(paragraph);
         // Em dashes
         paragraph = paragraph.Replace("--", "—");
         // Indent the top of the paragraph.
         var accumulator = "    ";
         for (var i = 0; i < paragraph.Length;)
         {
            if (paragraph[i] == '*')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               ++i;
               while (true)
               {
                  if (i >= paragraph.Length || paragraph[i] == '*')
                  {
                     if (accumulator.Length > 0)
                     {
                        var run = new Run(accumulator);
                        var hyperlink = new Hyperlink(run);
                        hyperlink.TextDecorations = null;
                        hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(0xdd, 0x41, 0x36));
                        hyperlink.Click += new RoutedEventHandler(HyperlinkClicked);
                        block.Inlines.Add(new Bold(hyperlink));
                        accumulator = "";
                        ++i;
                        break;
                     }
                  }
                  else
                  {
                     accumulator += paragraph[i];
                     ++i;
                  }
               }
            }
            else if (paragraph[i] == '_')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               ++i;
               while (true)
               {
                  if (i >= paragraph.Length || paragraph[i] == '_')
                  {
                     if (accumulator.Length > 0)
                     {
                        var run = new Run(accumulator);
                        block.Inlines.Add(new Italic(run));
                        accumulator = "";
                        ++i;
                        break;
                     }
                  }
                  else
                  {
                     accumulator += paragraph[i];
                     ++i;
                  }
               }
            }
            else
            {
               accumulator += paragraph[i];
               ++i;
            }
         }
         if (accumulator.Length > 0)
         {
            block.Inlines.Add(new Run(accumulator));
         }

         block.TextWrapping = TextWrapping.Wrap;
         block.Margin = new Thickness(5, 0, 5, 0);
         block.LineHeight = 20;

         return block;
      }

      private void SetupScreen()
      {
         //var description = "It was about eleven o’clock in the morning, mid October, with the sun not shining and a look of hard wet rain in the clearness of the foothills. I was wearing my powder-blue suit, with dark blue shirt, tie and display handkerchief, black brogues, black wool socks with dark blue clocks on them. I was neat, clean, shaved and sober, and I didn’t care who knew it. I was everything a well dressed private detective ought to be. I was calling on four million dollars.\r\n   The main hallway of the Sternwood place was two stories high. Over the entrance doors, which would have let in a troop of Indian elephants, there was a broad stained-glass panel showing a knight in dark armour rescuing a lady who was tied to a tree and didn’t have any clothes on but some very long and convenient hair. The knight had pushed the visor of his helmet back to be sociable, and he was fiddling with the knots on the ropes that tied the lady to the tree and not getting anywhere. I stood there and thought that if I lived in the house, I would sooner or later have to climb up there and help him. He didn’t seem to be really trying.\r\n   There were French doors at the back of the hall, beyond them a wide sweep of emerald grass to a white garage, in front of which a slim, dark, young chauffeur in shiny black leggings was dusting a maroon Packard convertible. Beyond the garage were some decorative trees trimmed as carefully as poodle dogs. Beyond them a large greenhouse with a domed roof. Then more trees and beyond everything the solid, uneven, comfortable line of the foothills.";
         //var description = "Jane woke and opened her eyes. She was lying in bed in a small, windowless room that looked like it had once been a walk-in closet. There was just room for the bed, a chair, and a cart with a dented *bedpan* on it. A light hung from the ceiling. The door was metal. It was obviously scavenged. It had GYMNASIUM stenciled on it in faded yellow letters. The paint was mostly flaked from the walls, but it had a clean, antiseptic scent. None of it was familiar to her.\r\nThe door bolt scraped open. A man came in. He was older, balding, and wore a long, patched white coat and a stethoscope. He sat down in the chair. He glanced over her body under the sheets. Then he examined her face and forehead. As he did so, he said, \"I had a feeling you might wake up today. I'm Doc Mitchell.\" He took her wrist and checked her pulse. \"You're pretty lucky to be alive.\"\r\n\r\n*Where am I?*";

         // Why are you keeping me locked up in here? He gave her a rueful smile. "It always pays to be careful. You never know what you're going to get, even if you're a doctor."
         // "You're in my office. It's in Goodsprings, in the Mojave Desert outside of New Vegas." He gave her a look. "Does that ring any bells for you?"

         // It's simple. The engine builds a text version of the screen. Then this main window code converts that into WPF objects for display.
         var storyArea = (ItemsControl)FindName("StoryArea");
         storyArea.Items.Clear();
         var text = Engine.BuildNextText();
         var first = true;
         foreach (var piece in text.Split('@'))
         {
            var paragraph = Transform.RemoveExtraBlanks(piece);
            if (first && paragraph.Length < 1)
               continue;
            first = false;
            storyArea.Items.Add(TextToWPF(paragraph));
         }
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

