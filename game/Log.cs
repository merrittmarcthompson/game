using System;
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

      private static string BuildFullMessage(
         string message)
      {
         string result = "";
         if (FileName != null)
         {
            result += FileName + ": ";
         }
         result += message;
         if (SourceText != null)
         {
            result += ":\r\n" + SourceText;
         }
         return result;
      } 

      public static void Add(
        string message)
      {
         Writer.WriteLine(BuildFullMessage(message));
         Writer.Flush();
      }

      public static void Fail(
        string message)
      {
         Add(message);
         MessageBox.Show(BuildFullMessage(message), "Game error");
         Environment.Exit(1);
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
