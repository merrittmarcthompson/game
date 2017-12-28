using System.IO;
using System.Windows;

namespace Game
{
  // Have a global log that can be written to as a side effect from anywhere without access to any class object.
  public static class Log
  {
    private static StreamWriter Writer;
    private static string SourceText;
    private static string FileName;

    public static void Open(
      string filename)
    {
      Writer = new StreamWriter(filename, false);
    }
      
    public static void Add(
      string message)
    {
      if (FileName != null)
      {
        Writer.Write(FileName + ": ");
      }
      Writer.WriteLine(message);
      if (SourceText != null)
      {
        Writer.WriteLine(SourceText);
      }
      Writer.Flush();
    }

    public static void Fail(
      string message)
    {
      Add(message);
      MessageBox.Show(message, "Game error");
      Application.Current.Shutdown();
    }

    public static void SetSourceName(
      string fileName)
    {
      FileName = fileName;
    }

    public static void SetSourceText(
      string sourceText)
    {
      SourceText = sourceText;
    }
  }
}
