using System;
using System.Collections.Generic;
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
         string message,
         Dictionary<string, object> variables)
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
         if (variables != null)
         {
            foreach (var variable in variables)
            {
               result += "\r\n" + variable.Key + "=" + variable.Value.ToString();
            }
         }
         return result;
      } 

      public static void Add(
        string message,
        Dictionary<string, object> variables)
      {
         Writer.WriteLine(message);
         Writer.Flush();
      }

      public static void Add(
        string message)
      {
         Writer.WriteLine(message);
         Writer.Flush();
      }

      public static void Fail(
        string message)
      {
         Add(message);
         MessageBox.Show(BuildFullMessage(message, null), "Game error");
         Environment.Exit(1);
      }

      public static void Fail(
        string message,
        Dictionary<string, object> variables)
      {
         Add(message, variables);
         MessageBox.Show(BuildFullMessage(message, variables), "Game error");
         Environment.Exit(1);
      }

      public static void SetSourceName(
        string fileName)
      {
         FileName = fileName;
      }

      public static string SetSourceText(
        string sourceText)
      {
         var previousSourceText = SourceText;
         SourceText = sourceText;
         return previousSourceText;
      }

      public static object FailWhenNull(
         bool doCheck,
         object @object,
         string sourceName)
      {
         if (doCheck && @object == null)
         {
            Log.Fail(String.Format("{0} must not be null", sourceName));
            return null;
         }
         return @object;
      }
   }
}
