using System.IO;

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

    public static void SetSourceInformation(
      string fileName,
      string sourceText)
    {
      FileName = fileName;
      SourceText = sourceText;
    }
  }
}
