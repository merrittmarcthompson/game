#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   public class Game
   {
      // Game is the API the UI uses to interact with the UI-independent game engine.

      // VARIABLES
      private Page CurrentPage;

      // Pop back to old pages to implement undo.
      private Stack<Page> UndoStack = new Stack<Page>();

      private PageCreator PageCreator = new PageCreator();

      public const char NegativeDebugTextStart = '′';
      public const char PositiveDebugTextStart = '″';
      public const char DebugTextStop = '‴';

      public bool DebugMode
      {
         get => PageCreator.DebugMode;
         set => PageCreator.DebugMode = value;
      }

      // FUNCTIONS

      // These are the two functions used to show the current page on the screen. GetActionText gets the description on top. GetReactionTextsByScore gets the list of reactions that appear on the bottom.
      public string GetActionText()
      {
         return CurrentPage.ActionText;
      }

      public IEnumerable<string> GetReactionTextsByScore()
      {
         // -1 scores are links.
         return CurrentPage.Reactions.OrderByDescending(pair => pair.Value.Score).Where(pair => pair.Value.Score != -1).Select(pair => pair.Key);
      }

      public Game(
         World world)
      {
         // There are two ways to create the game initially. The first is to create a fresh game from the game world description.
         if (world == null) throw new ArgumentNullException(nameof(world));
         // This constructs the first page right away, ready to go, starting with the first unit in the game. The game is always in a valid state, that is, it contains a completed page, ready to display.
         CurrentPage = PageCreator.BuildFirst(world);
         SetCharacterName("Sarah", "Spaulding");
      }

      public Game(
         TextReader reader,
         World world)
      {
         // The other way to create the game is to read an existing game state from a file and link it up to the game world description.
         if (reader == null) throw new ArgumentNullException(nameof(reader));
         if (world == null) throw new ArgumentNullException(nameof(world));
         // This reads the state of an existing game and links it to the static parts of the game in the dictionaries.
         var page = PageSerializer.TryLoad(reader, world);
         if (page == null)
            throw new InvalidOperationException(string.Format($"Save file is empty."));
         CurrentPage = page;
         while (true)
         {
            var undoPage = PageSerializer.TryLoad(reader, world);
            if (undoPage == null)
               break;
            UndoStack.Push(undoPage);
         }
         // Flip the stack so it goes the right way.
         UndoStack = new Stack<Page>(UndoStack);
      }

      // This is how you save the game to a file.
      public void Save(
         StreamWriter writer)
      {
         if (writer == null) throw new ArgumentNullException(nameof(writer));
         PageSerializer.Save(CurrentPage, writer);
         foreach (var page in UndoStack)
            PageSerializer.Save(page, writer);
      }

      // This is how you go back to an earlier page.
      public void Undo()
      {
         if (CanUndo())
            CurrentPage = UndoStack.Pop();
      }

      // This tells you whether your undo button should be grayed out or not.
      public bool CanUndo()
      {
         return UndoStack.Count != 0;
      }

      // This changes the state of the game by picking one of the reaction texts on the bottom of the screen.
      public void MoveToReaction(
         string reactionText)
      {
         // Before we change the page, save the old page for restoration on undo.
         UndoStack.Push(CurrentPage);

         CurrentPage = PageCreator.BuildNext(CurrentPage, reactionText);
      }

      // These let you set and get game variables, like the name of the player character.
      public void SetCharacterName(
         string jane,
         string smith)
      {
         CurrentPage.Settings["jane"] = new StringSetting(jane);
         CurrentPage.Settings["smith"] = new StringSetting(smith);
      }

      public (string, string) GetCharacterName()
      {
         var janeSetting = (CurrentPage.Settings["jane"] as StringSetting) ??
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("Internal error: 'jane' setting is missing.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
         var smithSetting = (CurrentPage.Settings["smith"] as StringSetting) ??
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("Internal error: 'smith' setting is missing.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
         return (janeSetting.Value, smithSetting.Value);
      }
   }
}
