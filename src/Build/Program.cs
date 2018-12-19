using System;
using System.Collections.Generic;
using Cake.Frosting;
using CommandLine;
using Motorsports.Build.Startup;

namespace Motorsports.Build {
    public class Program {
        public static int Main(string[] args) {
            using (new TemporaryConsoleColor(ConsoleColor.Green)) {
                Console.WriteLine("***************************");
                Console.WriteLine("        MOTORSPORTS        ");
                Console.WriteLine("***************************");
            }

            return Parser.Default.ParseArguments<Options>(args)
                .MapResult(Startup, Shutdown);
        }

      static int Startup(Options options) {
            var localOptions = options.Interactive
                ? InteractiveOptions.Prompt(options)
                : options;

            if (localOptions == null) {
                return 0;
            }

            string[] arguments = {
                $"-target={localOptions.Target}",
                $"-configuration={localOptions.Configuration}",
                $"-environment={localOptions.Environment}",
                $"-verbosity={localOptions.Verbosity}",
                $"-publishDirectory={localOptions.PublishDirectory}"
            };

            var returnCode = new CakeHostBuilder()
                .WithArguments(arguments)
                .UseStartup<FrostingStartup>()
                .Build()
                .Run();

            // If interactive, keep asking 
            if (options.Interactive) {
                Console.WriteLine();
                return Startup(options);
            }
            
            // Otherwise, wait for key press if debugger is attached
            if (System.Diagnostics.Debugger.IsAttached) {
                Console.WriteLine();
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
            }

            return returnCode;
        }

      static int Shutdown(IEnumerable<Error> errors) {
            Console.WriteLine("Shutting down program");
            foreach (var error in errors) {
                Console.WriteLine(error.Tag);
            }
            return 1;
        }
    }
}