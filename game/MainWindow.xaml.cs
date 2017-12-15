using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Game
{
  public partial class MainWindow : Window
  {
    private Location CurrentLocation;
    private Dictionary<string, Location> Locations;
    private Dictionary<string, string> Properties;



    private void SetScreenText()
    {
      // One way or another, we're going to put a paragraph on the screen.
      var paragraph = new Paragraph();
      paragraph.FontSize = 13;
      paragraph.LineHeight = 22;

      (var tokens, var lexicalError) = Static.SourceTextToTokens(CurrentLocation.SourceText);
      if (tokens == null)
      {
        // If there's a source code problem, put an error message on the screen.
        paragraph.Inlines.Add(new Run(lexicalError));
      }
      else
      {
        (var sequence, var syntaxError) = Static.TokensToObjactSequence(tokens);
        if (sequence == null)
        {
          // If there's a source code problem, put an error message on the screen.
          paragraph.Inlines.Add(new Run(syntaxError));
        }
        else
        {
          var text = "";
          var directives = new Dictionary<string, string>();
          sequence.Reduce(Properties, ref text, ref directives);

          text = Static.RemoveExtraBlanks(text);
          text = Static.RemoveBlanksAfterNewLines(text);

          // Break it up by links, then build the links into the text. Every other split piece will be a link.
          bool onLink = text[0] == '{';
          // Split "{}" behavior:
          //   "here's a {link} or {two}" => "here's a " / "link" / " or " / "two" / ""
          //   "{here} is a {link}"       => "here" / " is a " / "link" / ""
          string[] pieces = text.Split("{}".ToCharArray());
          foreach (var piece in pieces)
          {
            if (onLink)
            {
              var hyperlink = new Hyperlink(new Run(piece));
              hyperlink.Click += Hyperlink_Click;
              hyperlink.Tag = piece;
              paragraph.Inlines.Add(hyperlink);
            }
            else
            {
              paragraph.Inlines.Add(new Run(piece));
            }
            onLink = !onLink;
          }
        }
      }
      FlowDocument document = new FlowDocument();
      document.TextAlignment = TextAlignment.Left;
      document.Blocks.Add(paragraph);
      FlowDocumentScrollViewer viewer = (FlowDocumentScrollViewer)FindName("viewer");
      viewer.Document = document;
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
      var hyperlink = sender as Hyperlink;
      var text = hyperlink.Tag as string;
      CurrentLocation = Locations[CurrentLocation.Targets[text]];
      SetScreenText();
    }

    public MainWindow()
    {
      try
      {
        InitializeComponent();

        string graphml = System.IO.File.ReadAllText("map.boneyard-simplified.graphml");
        Locations = Static.GraphmlToLocationDictionary(graphml);
        Properties = new Dictionary<string, string>();
        Properties.Add("p", "\r\n");

        foreach (var location in Locations.Values)
        {
          if (location.SourceText.StartsWith("<< Barclay Hotel room 603"))
          {
            CurrentLocation = location;
            break;
          }
        }

        SetScreenText();
      }
      catch (Exception e)
      {
        MessageBox.Show(String.Format("{0}", e), "Exception caught");
      }
    }
  }
}
