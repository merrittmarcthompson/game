
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
    private Tags Tags = new Tags();
    private Dictionary<string, string> InternalNames = new Dictionary<string, string>();
    private List<ObjectSequence> ObjectSequences = new List<ObjectSequence>();
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
      // This node could be anything selected from the location pane--a stage, an object, a person, etc.
      string node)
    {
      /*
          // One way or another, we're going to put a paragraph on the screen.
          var paragraph = new Paragraph();
          paragraph.FontSize = 13;
          paragraph.LineHeight = 22;

          if (Tags.LookupFirst(node, null, "isStage") == null)
          {
            // If they selected an object, tell a story about it.
            paragraph.Inlines.Add(new Run("Here is a story about " + Tags.LookupFirst(node, null, "text") + ". Isn't that interesting? I thought so."));
          }
          else
          {
            // If they selected a stage, go to the stage. This is only temporary. We will have a story that does this.
            var tokens = Static.SourceTextToTokens(Tags.LookupFirst(node, null, "text"));
            if (tokens != null)
            {
              var sequence = Static.TokensToObject(tokens);
              if (sequence != null)
              {
                var text = "";
                sequence.Traverse(Tags, node, ref text);

                var box = (ListBox)FindName("MapListBox");
                box.Items.Clear();
                AddToListBox(box, text, null);
                foreach (var arrow in Tags.LookupAll(node, null, "arrow"))
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

      // Get the source directory.
      var arguments = Environment.GetCommandLineArgs();
      if (arguments.Length < 2)
      {
        Log.Add("usage: game.exe source-directory");
        return;
      }

      // Get all the graphml files in the source directory.
      var sourcePaths = Directory.GetFiles(arguments[1], "*.graphml");
      if (sourcePaths.Length < 1)
      {
        Log.Add(String.Format("no .graphml files in directory {0}", arguments[1]));
        return;
      }

      // Collect new tags generated by this process in this collection, then merge them into the Tags database at the end. Keeps it from invalidating the Tags foreach loop.
      var newObjectTags = new Tags();
      var newTags = new Tags();
      var newStageTags = new Tags();

      foreach (var sourcePath in sourcePaths)
      {
        var sourceName = Path.GetFileName(sourcePath);

        // Load all the map files right now.
        if (sourceName.StartsWith("map.", StringComparison.CurrentCultureIgnoreCase))
        {
          string graphml = System.IO.File.ReadAllText(sourcePath);

          // First translate the graphml boxes and arrows to tags.
          Tags.Merge(Static.GraphmlToTags(graphml, Path.GetFileNameWithoutExtension(sourceName)));

          // Then process the directives embedded in the text of each box and arrow.
          foreach ((var boxOrArrowName, var boxOrArrowLabel, var boxOrArrowValue) in Tags.All())
          {
            if (boxOrArrowLabel == "sourceText")
            {
              // Compile the text to an object sequence.
              var tokens = Static.SourceTextToTokens(boxOrArrowValue);
              if (tokens == null)
                return; // SourceTextToTokens has already logged.
              var objectSequence = Static.TokensToObject(tokens);
              if (objectSequence == null)
                return;

              // Save it as 'text' for later insertion into stories.
              ObjectSequences.Add(objectSequence);
              newObjectTags.Add(boxOrArrowName, "text", "object~" + (ObjectSequences.Count - 1).ToString());

              // First make a pass to set up the internal name reference table for further use.
              objectSequence.Traverse((@object) =>
              {
                switch (@object)
                {
                  case ObjectName objectName:
                    // You can mark a box or arrow with [name myReferenceName]. Later, when you use myReferenceName (ex. in [when myReferenceName.isDoor]) we will convert it to the internal name.
                    Log.Add(String.Format("name {0} references {1}", boxOrArrowName, objectName.Name));
                    InternalNames[objectName.Name] = boxOrArrowName;
                    break;
                }
                return true;
              });

              // Next make all the implicit tag names (ex. [if isLarge]) explicit (ex. [if map_test_n1.isLarge]).
              objectSequence.Traverse((@object) =>
              {
                switch (@object)
                {
                  case ObjectSubstitution objectSubstitution:
                    if (objectSubstitution.Name == "")
                    {
                      objectSubstitution.Name = boxOrArrowName;
                    }
                    break;
                  case ObjectTag objectTag:
                    if (objectTag.Name == "")
                    {
                      objectTag.Name = boxOrArrowName;
                    }
                    break;
                  case ObjectIf objectIf:
                    if (objectIf.Name == "")
                    {
                      objectIf.Name = boxOrArrowName;
                    }
                    break;
                }
                return true;
              });

              // Next execute all the tag directives.
              objectSequence.Traverse((@object) =>
              {
                switch (@object)
                {
                  case ObjectTag objectTag:
                    // [untag name.label] is just a comment. There's nothing in the tags to start with, so everything is untagged.
                    if (!objectTag.Untag)
                    {
                      newTags.Add(objectTag.Name, objectTag.Label, objectTag.Value);
                    }
                    break;
                }
                return true;
              });
            }
          }
        }
      }

      // Mark items for what stage they are on.
      foreach ((var boxName, var boxLabel, var boxValue) in newTags.All())
      {
        if (boxLabel == "isStage")
        {
          foreach (var arrowName in Tags.LookupAll(boxName, null, "arrow"))
          {
            var subordinateBox = Tags.LookupFirst(arrowName, null, "target");
            if (newTags.LookupFirst(subordinateBox, null, "isStage") == null)
            {
              newStageTags.Add(subordinateBox, "stage", boxName);
            }
          }
        }
      }
      Tags.Merge(newObjectTags);
      Tags.Merge(newTags);
      Tags.Merge(newStageTags);


      Tags.Add("~", "p", "\r\n");
      ReactionList = new List<Reaction>();

      SetScreenText("map.boneyard-simplified_n0");
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
