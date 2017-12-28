
/* saved substitution code:

    // The defaultName is the context that the Reduce is being run in, i.e. we reducing the text for a location node or story node. The name is the location or story ID. This is used when there is no explicit name specified.
    string value = tags.LookupFirst(SpecifiedName, defaultName, Label);
      if (value == null)
      {
        // Ex. "[{Lucy}? {}?:{hero's first name}]"
        text += "[{" + SpecifiedName + "}? {" + defaultName + "}?:{" + Label + "}]";
      }
      else
      {
        text += value;
      }
*/

/* saved tag/untag code:

      // Get rid of any existing tags for the name and label.
      tags.Remove(SpecifiedName, defaultName, Label);

      // Create a new tag in the list. We're assuming there must be a defaultName.
      string name = SpecifiedName;
      if (name == null || name == "")
      {
        name = defaultName;
      }
      tags.Add(name, Label, Value);
*/

/* saved text code:

      text += Text;

*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Game
{
  public class Reaction
  {
    // You've got to have get/set property if a member is going to be displayed in the UI.
    public string Name { get; set; }
    public string Description { get; set; }

    public Reaction(
      string name,
      string description)
    {
      Name = name;
      Description = description;
    }
  }

  public partial class MainWindow : Window
  {
    public List<Reaction> ReactionList { get; set; }
    private DateTime NextGoodClick = DateTime.Now;

    private void ReactionListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // Get rid of key bounce.
      if (DateTime.Now < NextGoodClick)
        return;
      NextGoodClick = DateTime.Now.AddSeconds(0.5);

      /*
      Reaction reaction = ((sender as ListBox).SelectedItem as Reaction);

      if (reaction == null)
        return;

      Position.MoveTo(reaction);
      SetUpScreen();
      this.DataContext = Position.CurrentInteraction;
      */
    }

    private void MapListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // Get rid of key bounce.
      if (DateTime.Now < NextGoodClick)
        return;
      NextGoodClick = DateTime.Now.AddSeconds(0.5);

      var listBox = sender as ListBox;

      // The top one is a constant title.
      if (listBox.SelectedIndex == 0)
      {
        listBox.SelectedIndex = -1;
        return;
      }

      var panel = listBox.SelectedItem as DockPanel;
      if (panel == null)
        return;

      SetupScreen(/*panel.Tag as string*/);

      // Don't leave a selection box on the item after you click.
      listBox.SelectedIndex = -1;
    }

    private void SetupTextBlock(
      TextBlock block,
      string text)
    {
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
      block.Margin = new Thickness(10, 2, 10, 0);
      block.LineHeight = 20;
      //result.Tag = node;
    }

    private void AddToListBox(
      ListBox box,
      TextBlock block,
      string node)
    {
      DockPanel dockPanel = new DockPanel();
      dockPanel.Children.Add(block);
      DockPanel.SetDock(block, Dock.Left);
      dockPanel.Tag = node;

      box.Items.Add(dockPanel);
    }

    private void SetupScreen()
    {
      // Display the current stage and active stories.
      var box = (ListBox)FindName("StageListBox");
      box.Items.Clear();
      var title = (TextBlock)FindName("StageListTitleText");
      SetupTextBlock(title, Engine.EvaluateItemText(Engine.CurrentStage));
      foreach (var arrowName in Engine.MapArrowsFor(Engine.CurrentStage))
      {
        var item = new TextBlock();
        SetupTextBlock(item, Engine.EvaluateItemText(arrowName));
        AddToListBox(box, item, null);
      }
/*
      // One way or another, we're going to put a paragraph on the screen.
      var paragraph = new Paragraph();
          paragraph.FontSize = 13;
          paragraph.LineHeight = 22;

          if (Tags.LookupFirst(stage, null, "isStage") == null)
          {
            // If they selected an object, tell a story about it.
            paragraph.Inlines.Add(new Run("Here is a story about " + Tags.LookupFirst(stage, null, "text") + ". Isn't that interesting? I thought so."));
          }
          else
          {
            // If they selected a stage, go to the stage. This is only temporary. We will have a story that does this.
            var tokens = Static.SourceTextToTokens(Tags.LookupFirst(stage, null, "text"));
            if (tokens != null)
            {
              var sequence = Static.TokensToObject(tokens);
              if (sequence != null)
              {
                var text = "";
                sequence.Traverse(Tags, stage, ref text);

                var box = (ListBox)FindName("MapListBox");
                box.Items.Clear();
                AddToListBox(box, text, null);
                foreach (var arrow in Tags.LookupAll(stage, null, "arrow"))
                {
                  var arrowText = Tags.LookupFirst(arrow, null, "text");
                  var target = Tags.LookupFirst(arrow, null, "target");
                  AddToListBox(box, arrowText, target);
                }
              }
              // This lets the UI get to the WPF data. Yes, you've got to set it to null first, otherwise it won't redisplay anything.
              DataContext = null;
              DataContext = this;
            }
          }

          FlowDocument document = new FlowDocument();
          document.TextAlignment = TextAlignment.Left;
          document.Blocks.Add(paragraph);
          FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)FindName("StoryViewer");
          viewer.Document = document;

          ReactionList.Clear();
          ReactionList.Add(new Reaction("It works!", "Here is a big one. Now is the time for all good men to come to the aid of their country"));
          ReactionList.Add(new Reaction("Here's another one", "Hurray!"));
        */
    }
    
    public MainWindow()
    {
      /*
        try
        {
      */
      Log.Open("game.log");
      Log.Add("START");
      InitializeComponent();

      Engine.LoadSource();
      // Make a list of all the 'when' starting points that are active.
      // Execute each one until you find one that is true.
      // Then present the first interaction of the true one.
      // Also present the current location.
      // Then wait for the user to pick something.

      ReactionList = new List<Reaction>();

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

