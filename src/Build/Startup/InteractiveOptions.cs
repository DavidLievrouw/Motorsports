using System;
using System.Linq;

namespace Motorsports.Build.Startup {
  public class InteractiveOptions {
    public static Options Prompt(Options options) {
      var possibleActions = new (string Label, Action Invoke)[] {
        ("Quit", o => null),
        ("Restore packages", RestorePackages),
        ("Publish", Publish),
        ("Create IIS application", CreateIISApplication),
        ("Remove IIS application", RemoveIISApplication)
      };

      using (new TemporaryConsoleColor(ConsoleColor.Cyan)) {
        Console.WriteLine("What do you want to do? Possible actions:");
        var possibleActionsOverview = string.Join(
          Environment.NewLine,
          possibleActions.Select((action, i) => $"{i}. {action.Label}"));
        Console.WriteLine(possibleActionsOverview);
      }

      Console.Write($"Action [0-{possibleActions.Length - 1}]: ");
      var chosenAction = ReadChosenAction(possibleActions);
      return chosenAction.Invoke(options.Clone());
    }

    static (string Label, Action Invoke) ReadChosenAction((string Label, Action Invoke)[] possibleActions) {
      var response = Console.ReadLine();
      if (response == null || !int.TryParse(response, out var index) || index < 0 || index >= possibleActions.Length) {
        Console.Error.Write("Invalid response, type a valid action number: ");
        return ReadChosenAction(possibleActions);
      }

      return possibleActions[index];
    }

    static Options RestorePackages(Options options) {
      options.Target = nameof(Tasks.RestorePackages);
      return options;
    }

    static Options Publish(Options options) {
      options.Target = nameof(Tasks.Publish);
      return options;
    }

    static Options CreateIISApplication(Options options) {
      options.Target = nameof(Tasks.CreateIISApplication);
      return options;
    }

    static Options RemoveIISApplication(Options options) {
      options.Target = nameof(Tasks.RemoveIISApplication);
      return options;
    }

    delegate Options Action(Options options);
  }
}