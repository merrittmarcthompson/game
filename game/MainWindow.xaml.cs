using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Game
{
   public partial class MainWindow : Window
   {
      private DateTime NextGoodClick = DateTime.Now;
      private int Round = 0;

      private void SetupTextBlock(
        TextBlock block,
        string text,
        bool isListItem)
      {
         block.Inlines.Clear();
         text = Transform.RemoveExtraBlanks(text);
         text = Transform.RemoveBlanksAfterNewLines(text);
         var accumulator = "";
         for (var i = 0; i < text.Length;)
         {
            if (text[i] == '^')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               ++i;
               while (true)
               {
                  if (i >= text.Length || text[i] == '^')
                  {
                     if (accumulator.Length > 0)
                     {
                        var run = new Run(accumulator);
                        block.Inlines.Add(new Bold(run));
                        accumulator = "";
                        ++i;
                        break;
                     }
                  }
                  else
                  {
                     accumulator += text[i];
                     ++i;
                  }
               }
            }
            else if (text[i] == '_')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               ++i;
               while (true)
               {
                  if (i >= text.Length || text[i] == '_')
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
                     accumulator += text[i];
                     ++i;
                  }
               }
            }
            else
            {
               accumulator += text[i];
               ++i;
            }
         }
         if (accumulator.Length > 0)
         {
            block.Inlines.Add(new Run(accumulator));
         }

         block.TextWrapping = TextWrapping.Wrap;
         if (isListItem)
         {
            block.Margin = new Thickness(5, 2.5, 5, 1);
         }
         block.LineHeight = 20;
      }

      private void AddToListBox(
        ListBox box,
        TextBlock block,
        System.Object info)
      {
         DockPanel dockPanel = new DockPanel();
         dockPanel.Children.Add(block);
         DockPanel.SetDock(block, Dock.Left);
         dockPanel.Tag = info;
         box.Items.Add(dockPanel);
      }

      private void ReactionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Get rid of key bounce.
         if (DateTime.Now < NextGoodClick)
            return;
         NextGoodClick = DateTime.Now.AddSeconds(0.5);

         var listBox = sender as ListBox;
         var panel = listBox.SelectedItem as DockPanel;
         if (panel == null)
            return;

         var option = panel.Tag as Description.Option;
         Engine.ShiftContinuationByChoice(option);

         // Show the current stage and stories.
         SetupScreen();
      }

      private void StageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Get rid of key bounce.
         if (DateTime.Now < NextGoodClick)
            return;
         NextGoodClick = DateTime.Now.AddSeconds(0.5);

         var listBox = sender as ListBox;
         var panel = listBox.SelectedItem as DockPanel;
         if (panel == null)
            return;

         // When you click on an item in the stage box, set its isSelected property. That will have an effect on the next shift, possibly producing a new stage or a new story node.
         Engine.SelectItem(panel.Tag as string, true);

         // Show the current stage and stories.
         SetupScreen();

         Engine.SelectItem(panel.Tag as string, false);
      }

      private void SetupScreen()
      {
         Log.Add(String.Format("ROUND {0}", ++Round));

         var description = Engine.UpdateContinuations();

         var title = (TextBlock)FindName("StageListTitleText");
         SetupTextBlock(title, Engine.GetHeroStageDescription(), false);
         var stageListBox = (ListBox)FindName("StageListBox");
         stageListBox.Items.Clear();
         foreach ((var nodeText, var targetName) in Engine.HeroStageContents())
         {
            var block = new TextBlock();
            SetupTextBlock(block, nodeText, true);
            AddToListBox(stageListBox, block, targetName);
         }
         var storyArea = (ItemsControl)FindName("StoryArea");
         var storyBlock = new TextBlock();
         SetupTextBlock(storyBlock, description.Text, false);
         storyArea.Items.Clear();
         storyArea.Items.Add(storyBlock);
         var reactionListBox = (ListBox)FindName("ReactionListBox");
         reactionListBox.Items.Clear();
         foreach (var reaction in description.Options)
         {
            var item = new TextBlock();
            SetupTextBlock(item, reaction.Text, true);
            AddToListBox(reactionListBox, item, reaction);
         }
      }

      public MainWindow()
      {
         /*
           try
           {
         */
         Log.Open("game.log");
         Log.Add("Started");
         InitializeComponent();
         Engine.LoadSource();
         SetupScreen();
         /*
           }
           catch (Exception e)
           {
             MessageBox.Show(String.Format("{0}", e), "Exception caught");
           }
         */
      }
   }
}

