using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Game
{
  public class Location
  {
    // You've got to have get/set property if a member is going to be displayed in the UI.
    public string Node { get; set; }
    public string Description { get; set; }

    public Location(
      string node,
      string description)
    {
      Node = node;
      Description = description;
    }
  }

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
    public List<Location> LocationList { get; set; }
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

      Location location = listBox.SelectedItem as Location;

      if (location == null || location.Node == null)
        return;
      
      SetScreenText(location.Node);

      // Don't leave a selection box on the item after you click.
      listBox.SelectedIndex = -1;
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

          text = Static.RemoveExtraBlanks(text);
          text = Static.RemoveBlanksAfterNewLines(text);


          if (Static.SingleLookup(Tags, node, null, "location") == null)
          {
            paragraph.Inlines.Add(new Run("Here is a story about " + Static.SingleLookup(Tags, node, null, "text") + ". Isn't that interesting? I thought so."));
          }
          else
          {
            LocationList.Clear();
            LocationList.Add(new Location(null, text));
            foreach (var target in Static.MultiLookup(Tags, node, null, "target"))
            {
              var pieces = target.Split('~');
              LocationList.Add(new Location(pieces[0], pieces[1]));
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
      Tags = Static.GraphmlToProperties(graphml);
      Tags.Add(new Tag("~", "p", "\r\n"));
      LocationList = new List<Location>();
      ReactionList = new List<Reaction>();

      SetScreenText("n4::n0");
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
