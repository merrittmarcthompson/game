using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Game
{
   public partial class MainWindow : Window
   {
      private int Round = 0;
      private DateTime NextGoodClick = DateTime.Now;
      private string LastSelectedItem = null;
      private int OldQuestCount = 0;

      private void SetupTextBlock(
        TextBlock block,
        string text,
        bool isListItem)
      {
         block.Inlines.Clear();
         text = Transform.RemoveExtraBlanks(text);
         text = Transform.RemoveBlanksAfterNewLines(text);
         text = Transform.VerticalToMatchingQuotes(text);
         // Em dashes
         text = text.Replace("--", "—");
         var accumulator = "";
         for (var i = 0; i < text.Length;)
         {
            if (text[i] == '*')
            {
               if (accumulator.Length > 0)
               {
                  block.Inlines.Add(new Run(accumulator));
                  accumulator = "";
               }
               ++i;
               while (true)
               {
                  if (i >= text.Length || text[i] == '*')
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
            block.Margin = new Thickness(5, 2.5, 5, 2);
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
         var listBox = sender as ListBox;
         var panel = listBox.SelectedItem as DockPanel;
         if (panel == null)
            return;
         listBox.SelectedIndex = -1;

         // Get rid of key bounce.
         if (DateTime.Now < NextGoodClick)
            return;
         NextGoodClick = DateTime.Now.AddSeconds(0.5);

         var option = panel.Tag as Description.Option;
         Engine.ShiftContinuationByChoice(option);

         // Show the current stage and stories.
         SetupScreen(null);
      }

      private void StorageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         StageListBox_SelectionChanged(sender, e);
      }

      private void StageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         var listBox = sender as ListBox;
         var panel = listBox.SelectedItem as DockPanel;
         if (panel == null)
            return;
         listBox.SelectedIndex = -1;

         // Get rid of key bounce.
         if (DateTime.Now < NextGoodClick)
            return;
         NextGoodClick = DateTime.Now.AddSeconds(0.5);

         // When you click on an item in the stage box, set its isSelected property. That will have an effect on the next shift, possibly producing a new stage or a new story node.
         // It stays selected until something else gets selected. That helps stories that are stopped if you lose interest in them and select something else
         var selectedItem = panel.Tag as string;
         Engine.SetTag(selectedItem, "isJustSelected");
         if (selectedItem != LastSelectedItem)
         {
            Engine.ResetTag(LastSelectedItem, "isStillSelected");
            Engine.SetTag(selectedItem, "isStillSelected");
            LastSelectedItem = selectedItem;
         }

         // Show the current stage and stories.
         SetupScreen(selectedItem);

         Engine.ResetTag(selectedItem, "isJustSelected");
      }

      /* There are two issues:
         1. It should only pop over to the storage tab when it first appears.
         2. When you select anything else on the location tab, the storage tab goes away. */
      private void SetupScreen(
         string selectedName)
      {
         Log.Add(String.Format("Round {0}", ++Round), null);

         var description = Engine.UpdateContinuations();

         var stageTitle = (TextBlock)FindName("StageListTitleText");
         SetupTextBlock(stageTitle, Engine.GetHeroStageDescription(), false);

         var stageListBox = (ListBox)FindName("StageListBox");
         stageListBox.Items.Clear();
         foreach ((var nodeText, var targetName) in Engine.HeroStageContents())
         {
            var block = new TextBlock();
            SetupTextBlock(block, nodeText, true);
            AddToListBox(stageListBox, block, targetName);
         }

         var storageTab = (TabItem)FindName("StorageTab");
         var storageTitle = (TextBlock)FindName("StorageListTitleText");
         var (storageDescription, storageName) = Engine.GetHeroSubjectDescription();
         if (storageDescription == null)
         {
            storageTab.Visibility = Visibility.Hidden;
         }
         else
         {
            if (storageName == selectedName)
            {
               // Switch over to the storage tab.
               storageTab.IsSelected = true;
            }
            storageTab.Visibility = Visibility.Visible;
            SetupTextBlock(storageTitle, storageDescription, false);

            var storageListBox = (ListBox)FindName("StorageListBox");
            storageListBox.Items.Clear();
            foreach ((var nodeText, var targetName) in Engine.HeroSubjectContents())
            {
               var block = new TextBlock();
               SetupTextBlock(block, nodeText, true);
               AddToListBox(storageListBox, block, targetName);
            }
         }

         var questListArea = (ItemsControl)FindName("QuestListArea");
         questListArea.Items.Clear();
         var questCount = 0;
         var quests = Engine.HeroQuests();
         if (!quests.Any())
         {
            var block = new TextBlock();
            SetupTextBlock(block, "None", true);
            questListArea.Items.Add(block);
         }
         else
         {
            foreach (var questText in quests)
            {
               ++questCount;
               var block = new TextBlock();
               SetupTextBlock(block, questText, true);
               questListArea.Items.Add(block);
            }
         }
         if (questCount > OldQuestCount)
         {
            var heroTab = (TabItem)FindName("HeroTab");
            heroTab.IsSelected = true;
            OldQuestCount = questCount;
         }

         var storyArea = (ItemsControl)FindName("StoryArea");
         storyArea.Items.Clear();
         foreach (var paragraph in description.Text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
         {
            var storyBlock = new TextBlock();
            SetupTextBlock(storyBlock, paragraph, true);
            storyArea.Items.Add(storyBlock);
         }

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
         SetupScreen(null);
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

