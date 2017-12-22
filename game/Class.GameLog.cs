using System.IO;

namespace Game
{
  // Have a global log that can be written to as a side effect from anywhere without access to any class object.
  public static class GameLog
  {
    private static StreamWriter Writer;

    public static void Open(
      string filename)
    {
      Writer = new StreamWriter(filename, false);
    }
      
    public static void Add(
      string message)
    {
      Writer.WriteLine(message);
      Writer.Flush();
    }
  }
}
