using System;
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
               hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(0xc0, 0x00, 0x00));
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
            block.Margin = new Thickness(14, 2, 5, 2);
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
         // It's simple. The engine builds a text version of the screen. Then this main window code converts that into WPF objects for display.
         var storyArea = (ItemsControl)FindName("StoryArea");
         storyArea.Items.Clear();
         // Add an extra paragraph on the end just to have some white space.
         var text = Engine.BuildNextText() + "@";
         var first = true;
         foreach (var paragraph in text.Split('@'))
         {
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

