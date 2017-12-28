
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
    private Tags MapTags = new Tags();
    private Tags StoryTags = new Tags();
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
      // First make a list of all the boxes that are pointed to in the file.
      var result = new List<string>();
      var arePointedTo = new HashSet<string>();
      var nodeList = new List<string>();
      foreach ((var nodeOrArrowName, var nodeOrArrowValue) in fileBaseTags.LookupAllWithLabel("arrow"))
      {
        var target = fileBaseTags.LookupFirst(nodeOrArrowValue, "target");
        if (!arePointedTo.Contains(target))
        {
          arePointedTo.Add(target);
        }
      }
      // Then find all the boxes that aren't in the list.
      foreach ((var nodeName, var nodeValue) in fileBaseTags.LookupAllWithLabel("isNode"))
      {
        if (!arePointedTo.Contains(nodeName))
        {
          // When you find a box nothing points to, add to the result any arrows it has which are unconditional and have no reaction text.
          foreach (var arrowName in fileBaseTags.LookupAll(nodeName, "arrow"))
          {
            if (!SequenceObjects[fileObjectTags.LookupFirst(arrowName, "text")].ContainsText())
            {
              result.Add(arrowName);
            }
          }
        }
      }
      return result;
    }

    private SequenceObject CompileSourceText(
      string sourceText)
    {
      // Compile the text to an object sequence.
      var tokens = Static.SourceTextToTokens(sourceText);
      if (tokens == null)
        return null;
      return Static.TokensToObjects(tokens);
    }

    private Tags BuildFileObjectTags(
      Tags fileBaseTags)
    {
      var result = new Tags();
      foreach ((var boxOrArrowName, var boxOrArrowValue) in fileBaseTags.LookupAllWithLabel("sourceText"))
      {
        Log.SetSourceText(boxOrArrowValue);
        var sequenceObject = CompileSourceText(boxOrArrowValue);
        Log.SetSourceText(null);
        if (sequenceObject == null)
          continue;

        // Save it as 'text' for later insertion into stories.
        var objectKey = "object~" + (SequenceObjects.Count).ToString();

        // It side effects the SequenceObjects list!
        SequenceObjects[objectKey] = sequenceObject;
        result.Add(boxOrArrowName, "text", objectKey);
      }
      return result;
    }

    private void ProcessStory(
      Tags fileBaseTags,
      Tags fileObjectTags)
    {
      // Find all the arrows that could possibly lead to new stories.
      ActiveArrows.AddRange(GetActiveArrows(fileBaseTags, fileObjectTags));
    }

    private Tags ProcessMap(
      Tags fileBaseTags,
      Tags fileObjectTags)
    {
      // Execute the object text for maps.
      var fileNewTags = new Tags();
      foreach ((var boxOrArrowName, var boxOrArrowValue) in fileObjectTags.LookupAllWithLabel("text"))
      {
        var sequenceObject = SequenceObjects[boxOrArrowValue];

        // First make a pass to set up the internal name reference table for further use.
        sequenceObject.Traverse((@object) =>
        {
          if (!(@object is NameObject nameObject))
            return true;
          // You can mark a box or arrow with [name myReferenceName]. Later, when you use myReferenceName (ex. in [when myReferenceName.isDoor]) we will convert it to the internal name.
          Log.Add(String.Format("name '{0}' references '{1}'", nameObject.Name, boxOrArrowName));
          InternalNames[nameObject.Name] = boxOrArrowName;
          return true;
        });

        // Next make all the implicit tag names (ex. [if isLarge]) explicit (ex. [if map_test_n1.isLarge]).
        sequenceObject.Traverse((@object) =>
        {
          switch (@object)
          {
            case SubstitutionObject substitutionObject:
              if (substitutionObject.Expression.LeftName == "")
              {
                substitutionObject.Expression.LeftName = boxOrArrowName;
              }
              break;
            case TagObject tagObject:
              if (tagObject.Expression.LeftName == "")
              {
                tagObject.Expression.LeftName = boxOrArrowName;
              }
              break;
            case IfObject ifObject:
              foreach (var notExpression in ifObject.NotExpressions)
              {
                if (notExpression.Expression.LeftName == "")
                {
                  notExpression.Expression.LeftName = boxOrArrowName;
                }
                if (notExpression.Expression.RightName == "")
                {
                  notExpression.Expression.RightName = boxOrArrowName;
                }
              }
              break;
          }
          return true;
        });

        // Next execute all the tag directives.
        sequenceObject.Traverse((@object) =>
        {
          if (!(@object is TagObject tagObject))
            return true;
          // [untag name.label] is just a comment in a map file. There's nothing in the tags to start with, so everything is untagged.
          if (tagObject.Untag)
            return true;
          if (tagObject.Expression.LeftLabels.Count != 1)
          {
            Log.Add("expected only one label in a map tag specification");
          }
          else
          {
            fileNewTags.Add(tagObject.Expression.LeftName, tagObject.Expression.LeftLabels[0], tagObject.Expression.RightName);
          }
          return true;
        });

        // Mark items for what stage they are on.
        foreach ((var boxName, var boxValue) in MapTags.LookupAllWithLabel("isStage"))
        {
          foreach (var arrowName in MapTags.LookupAll(boxName, "arrow"))
          {
            var subordinateBox = MapTags.LookupFirst(arrowName, "target");
            if (MapTags.LookupFirst(subordinateBox, "isStage") == null)
            {
              fileNewTags.Add(subordinateBox, "stage", boxName);
            }
          }
        }
      }
      return fileNewTags;
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

      // A chance to do some unit testing on the compiler.
      var sequenceObject = CompileSourceText("[tag isOtherSide]");
      /*
    @"[when
    Door.isDoor,
    not Door.isLockable,
    Destination=Door.arrow,
    Destination.isDoorTarget,
    hero.location.isBank]");
    */

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

        Log.SetSourceName(sourceName);

        string graphml = System.IO.File.ReadAllText(sourcePath);

        // Translate the graphml boxes and arrows to tags.
        var fileBaseTags = Static.GraphmlToTags(graphml, Path.GetFileNameWithoutExtension(sourceName));

        // Compile the directives embedded in the source text of each box and arrow to create a list of object 'text' tags and a SequenceObjects table that relates them to the actual object code.
        var fileObjectTags = BuildFileObjectTags(fileBaseTags);

        Tags fileNewTags;
        if (isMap)
        {
          fileNewTags = ProcessMap(fileBaseTags, fileObjectTags);
          MapTags.Merge(fileBaseTags);
          MapTags.Merge(fileObjectTags);
          MapTags.Merge(fileNewTags);
        }
        else
        {
          ProcessStory(fileBaseTags, fileObjectTags);
          StoryTags.Merge(fileBaseTags);
          StoryTags.Merge(fileObjectTags);
        }
      }
      // Make a list of all the 'when' starting points that are active.
      // Execute each one until you find one that is true.
      // Then present the first interaction of the true one.
      // Also present the current location.
      // Then wait for the user to pick something.
      foreach (var arrow in ActiveArrows)
      {
        SequenceObjects[StoryTags.LookupFirst(arrow, "text")].Traverse((@object) =>
        {
          if (!(@object is WhenObject whenObject))
            return true;
          var story = Static.CastStory(whenObject.NotExpressions, MapTags, InternalNames);
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

