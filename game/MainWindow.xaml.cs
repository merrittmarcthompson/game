using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

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
    private HashSet<Tag> Tags;
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

      SetScreenText(panel.Tag as string);

      // Don't leave a selection box on the item after you click.
      listBox.SelectedIndex = -1;
    }

    private void AddToListBox(
      ListBox box,
      string text,
      string node)
    {
      text = Static.RemoveExtraBlanks(text);
      text = Static.RemoveBlanksAfterNewLines(text);

      TextBlock block = new TextBlock();
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
      block.Tag = node;

      DockPanel dockPanel = new DockPanel();
      dockPanel.Children.Add(block);
      DockPanel.SetDock(block, Dock.Left);
      dockPanel.Tag = node;

      box.Items.Add(dockPanel);
    }

    private void SetScreenText(
      string node)
    {
      // One way or another, we're going to put a paragraph on the screen.
      var paragraph = new Paragraph();
      paragraph.FontSize = 13;
      paragraph.LineHeight = 22;

      (var tokens, var lexicalError) = Static.SourceTextToTokens(Static.SingleLookup(Tags, node, null, "text"));

      if (tokens == null)
      {
        // If there's a source code problem, put the error message on the screen.
        paragraph.Inlines.Add(new Run(lexicalError));
      }
      else
      {
        (var sequence, var syntaxError) = Static.TokensToObjactSequence(tokens);
        if (sequence == null)
        {
          // If there's a source code problem, put the error message on the screen.
          paragraph.Inlines.Add(new Run(syntaxError));
        }
        else
        {
          var text = "";
          sequence.Reduce(Tags, node, ref text);

          if (Static.SingleLookup(Tags, node, null, "location") == null)
          {
             paragraph.Inlines.Add(new Run("Here is a story about " + Static.SingleLookup(Tags, node, null, "text") + ". Isn't that interesting? I thought so."));
          }
          else
          {
            var box = (ListBox)FindName("MapListBox");
            box.Items.Clear();
            AddToListBox(box, text, null);
            foreach (var target in Static.MultiLookup(Tags, node, null, "target"))
            {
              var pieces = target.Split('~');
              AddToListBox(box, pieces[1], pieces[0]);
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
    }

    public MainWindow()
    {
      /*
        try
        {
      */
      InitializeComponent();

      string graphml = System.IO.File.ReadAllText("map.boneyard-simplified.graphml");
      Tags = Static.GraphmlToTags(graphml, "map.boneyard-simplified");
//      graphml = System.IO.File.ReadAllText("story.mitchell-simplified.graphml");
//      Tags.UnionWith(Static.GraphmlToTags(graphml, "story.mitchell-simplified"));
      Tags.Add(new Tag("~", "p", "\r\n"));
      ReactionList = new List<Reaction>();

      SetScreenText("map.boneyard-simplified:n0");
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
