
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
    private Dictionary<string, SequenceObject> SequenceObjects = new Dictionary<string, SequenceObject>();
    private List<string> ActiveArrows = new List<string>();
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

    private List<string> GetActiveArrows(
      Tags fileBaseTags,
      Tags fileObjectTags)
    {
      // For stories, find all the starting nodes that no arrow points to. These are the ones where stories can start.
      // Make a list of all the boxes that are pointed to in the file.
      var result = new List<string>();
      var arePointedTo = new HashSet<string>();
      var nodeList = new List<string>();
      foreach ((var nodeOrArrowName, var nodeOrArrowLabel, var nodeOrArrowValue) in fileBaseTags.All())
      {
        if (nodeOrArrowLabel == "arrow")
        {
          var target = fileBaseTags.LookupFirst(nodeOrArrowValue, null, "target");
          if (!arePointedTo.Contains(target))
          {
            arePointedTo.Add(target);
          }
        }
        else if (nodeOrArrowLabel == "isNode")
        {
          nodeList.Add(nodeOrArrowName);
        }
      }
      foreach (var nodeId in nodeList)
      {
        if (!arePointedTo.Contains(nodeId))
        {
          // When you find a box nothing points to, add to the result any arrows it has which are unconditional and have no reaction text.
          foreach (var arrow in fileBaseTags.LookupAll(nodeId, null, "arrow"))
          {
            if (!SequenceObjects[fileObjectTags.LookupFirst(arrow, null, "text")].ContainsText())
            {
              result.Add(arrow);
            }
          }
        }
      }
      return result;
    }

    private Tags BuildFileObjectTags(
      Tags fileBaseTags,
      string sourceName)
    {
      var result = new Tags();
      foreach ((var boxOrArrowName, var boxOrArrowLabel, var boxOrArrowValue) in fileBaseTags.All())
      {
        if (boxOrArrowLabel != "sourceText")
          continue;

        // Compile the text to an object sequence.
        Log.SetSourceInformation(sourceName, boxOrArrowValue);
        var tokens = Static.SourceTextToTokens(boxOrArrowValue);
        if (tokens == null)
          continue;
        var objectSequence = Static.TokensToObject(tokens);
        if (objectSequence == null)
          continue;

        // Save it as 'text' for later insertion into stories.
        var objectKey = "object~" + (SequenceObjects.Count - 1).ToString();

        // It side effects the SequenceObjects list!
        SequenceObjects[objectKey] = objectSequence;
        result.Add(boxOrArrowName, "text", objectKey);
      }
      return result;
    }

    private void LoadSource()
    {
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

      foreach (var sourcePath in sourcePaths)
      {
        var sourceName = Path.GetFileName(sourcePath);

        bool isMap;
        if (sourceName.StartsWith("map.", StringComparison.CurrentCultureIgnoreCase))
        {
          isMap = true;
        }
        else if (sourceName.StartsWith("story.", StringComparison.CurrentCultureIgnoreCase))
        {
          isMap = false;
        }
        else
          continue;

        string graphml = System.IO.File.ReadAllText(sourcePath);

        // Translate the graphml boxes and arrows to tags.
        var fileBaseTags = Static.GraphmlToTags(graphml, Path.GetFileNameWithoutExtension(sourceName));

        // Compile the directives embedded in the source text of each box and arrow to create a list of object 'text' tags and a SequenceObjects table that relates them to the actual object code.
        var fileObjectTags = BuildFileObjectTags(fileBaseTags, sourceName);

        if (!isMap)
        {
          // Mark story nodes that nothing points to as story start nodes.
          ActiveArrows.AddRange(GetActiveArrows(fileBaseTags, fileObjectTags));
        }

        // Execute the object text for maps.
        var fileNewTags = new Tags();
        foreach ((var boxOrArrowName, var boxOrArrowLabel, var boxOrArrowValue) in fileBaseTags.All())
        {
          if (boxOrArrowLabel != "text")
            continue;

          if (isMap)
          {
            var sequenceObject = SequenceObjects[boxOrArrowValue];

            // First make a pass to set up the internal name reference table for further use.
            sequenceObject.Traverse((@object) =>
            {
              switch (@object)
              {
                case NameObject objectName:
                  // You can mark a box or arrow with [name myReferenceName]. Later, when you use myReferenceName (ex. in [when myReferenceName.isDoor]) we will convert it to the internal name.
                  Log.Add(String.Format("name '{0}' references '{1}'", objectName.Name, boxOrArrowName));
                  InternalNames[objectName.Name] = boxOrArrowName;
                  break;
              }
              return true;
            });

            // Next make all the implicit tag names (ex. [if isLarge]) explicit (ex. [if map_test_n1.isLarge]).
            sequenceObject.Traverse((@object) =>
            {
              switch (@object)
              {
                case SubstitutionObject objectSubstitution:
                  if (objectSubstitution.TagSpec.Name == "")
                  {
                    objectSubstitution.TagSpec.Name = boxOrArrowName;
                  }
                  break;
                case TagObject objectTag:
                  if (objectTag.TagSpec.Name == "")
                  {
                    objectTag.TagSpec.Name = boxOrArrowName;
                  }
                  break;
                case IfObject objectIf:
                  foreach (var tagSpec in objectIf.TagSpecs)
                  {
                    if (tagSpec.Name == "")
                    {
                      tagSpec.Name = boxOrArrowName;
                    }
                  }
                  break;
              }
              return true;
            });

            // Next execute all the tag directives.
            sequenceObject.Traverse((@object) =>
            {
              switch (@object)
              {
                case TagObject objectTag:
                  // [untag name.label] is just a comment. There's nothing in the tags to start with, so everything is untagged.
                  if (!objectTag.Untag)
                  {
                    if (objectTag.TagSpec.Name == "")
                    {
                      Log.Add("expected a name before '.'");
                    }
                    else if (objectTag.TagSpec.Labels.Count != 1)
                    {
                      Log.Add("expected only one label in a map tag specification");
                    }
                    else
                    {
                      fileNewTags.Add(objectTag.TagSpec.Name, objectTag.TagSpec.Labels[0], objectTag.TagSpec.Value);
                    }
                  }
                  break;
              }
              return true;
            });

            // Mark items for what stage they are on.
            foreach ((var boxName, var boxLabel, var boxValue) in Tags.All())
            {
              if (boxLabel == "isStage")
              {
                foreach (var arrowName in Tags.LookupAll(boxName, null, "arrow"))
                {
                  var subordinateBox = Tags.LookupFirst(arrowName, null, "target");
                  if (Tags.LookupFirst(subordinateBox, null, "isStage") == null)
                  {
                    fileNewTags.Add(subordinateBox, "stage", boxName);
                  }
                }
              }
            }
          }
        }
        // Merge all the new tags we generated for this file into the main Tags database.
        Tags.Merge(fileBaseTags);
        Tags.Merge(fileObjectTags);
        Tags.Merge(fileNewTags);
      }
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
      LoadSource();

      // Make a list of all the 'when' starting points that are active.
      // Execute each one until you find one that is true.
      // Then present the first interaction of the true one.
      // Also present the current location.
      // Then wait for the user to pick something.
      foreach (var arrow in ActiveArrows)
      {
        SequenceObjects[Tags.LookupFirst(arrow, null, "text")].Traverse((@object) =>
        {
          switch (@object)
          {
            case WhenObject whenObject:
              //if (ExecuteWhen(whenObject.Nots, whenObject.TagSpecs))

              break;
          }
          return true;
        });
      }

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
