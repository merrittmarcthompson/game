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
      private int Turn = 0;

      private void SetupTextBlock(
        TextBlock block,
        string text,
        bool isListItem)
      {
         block.Inlines.Clear();
         text = Static.RemoveExtraBlanks(text);
         text = Static.RemoveBlanksAfterNewLines(text);
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
            block.Margin = new Thickness(5, 10, 5, -5);
         }
         block.LineHeight = 20;
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

         (var targetNode, var story) = (ValueTuple<string, Continuation>)panel.Tag;

         story.CurrentActionNodeName = targetNode;
         SetupScreen();
      }

      private void MapListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Get rid of key bounce.
         if (DateTime.Now < NextGoodClick)
            return;
         NextGoodClick = DateTime.Now.AddSeconds(0.5);

         var listBox = sender as ListBox;
         var panel = listBox.SelectedItem as DockPanel;
         if (panel == null)
            return;

         // When you click on an item in the stage box, set its isSelected property, then see what stories are active based on that.
         Engine.SelectMapNode(panel.Tag as string);
         //Engine.ActivateStories();

         // Show the current stage and stories.
         SetupScreen();
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
      /* We want to have "continuation points". This is a point where a story can be continued.
         To start out, we make a continuation point for every starting node of every story.
         Whenever we move to a different story node, we destroy the old continuation point and add a new one for the different node (unless it's a starting node, in which case we keep the old continuation point).
         Whenever we set up the screen, we scan all the continuation points and present all the ones which can be cast.
         A continuation point can be cast when at least one of its arrows can be cast.
      */
      private void SetupScreen()
      {
         Log.Add(String.Format("TURN {0}", ++Turn));
         // Display the current stage.
         var title = (TextBlock)FindName("StageListTitleText");
         SetupTextBlock(title, Engine.EvaluateItemText(Engine.CurrentStageNodeName, null), false);

         var stageListBox = (ListBox)FindName("StageListBox");
         stageListBox.Items.Clear();
         foreach (var arrowName in Engine.MapArrowsFor(Engine.CurrentStageNodeName))
         {
            var item = new TextBlock();
            SetupTextBlock(item, Engine.EvaluateItemText(arrowName, null), true);
            string targetNode = Engine.GetMapTagValue(arrowName, "target");
            AddToListBox(stageListBox, item, targetNode);
         }

         // Display the active stories.
         var stories = Engine.GetActiveStories();
         foreach (var story in stories)
         {
            var storyBlock = (TextBlock)FindName("StoryBlock");
            SetupTextBlock(storyBlock, Engine.EvaluateItemText(story.CurrentActionNodeName, story.Variables), false);
            // Display the reactions.
            var reactionListBox = (ListBox)FindName("ReactionListBox");
            reactionListBox.Items.Clear();
            foreach (var arrowName in Engine.StoryArrowsFor(story.CurrentActionNodeName))
            {
               if (Engine.ReactionIsActive(arrowName))
               {
                  var item = new TextBlock();
                  SetupTextBlock(item, Engine.EvaluateItemText(arrowName, story.Variables), true);
                  string targetAction = Engine.GetStoryTagValue(arrowName, "target");
                  AddToListBox(reactionListBox, item, (targetAction, story));
               }
            }
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

