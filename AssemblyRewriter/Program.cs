﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ILRepacking;
using Mono.Options;

namespace AssemblyRewriter
{
    internal static class Program
    {
        private static readonly List<string> InputPaths = new List<string>();
        private static readonly List<string> OutputPaths = new List<string>();
        private static bool _help;
        private static bool _verbose;
        private static bool _merge;
        private static string _keyFile;

        private static int Main(string[] args)
        {
            var options = new OptionSet
            {
                {"i|in=", "input {path} for assembly to rewrite. Use multiple flags for multiple input paths", i => InputPaths.Add(i)},
                {"o|out=", "output {path} for rewritten assembly. Use multiple flags for multiple output paths", o => OutputPaths.Add(o)},
                {"m|merge", "merge all output dlls to a single dll using the first output path as target", p => _merge = p != null},
                {"k|keyFile=", "resign merged dll with this keyFile", p => _keyFile = p},
                {"v|verbose", "verbose output", v => _verbose = v != null},
                {"h|?|help", "show this message and exit", h => _help = h != null},
            };

            if (args.Length == 0)
            {
                ShowHelp(options);
                return 1;
            }

            try
            {
                options.Parse(args);
            }
            catch (OptionException o)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(o);
                Console.WriteLine("Try '--help' for more information.");
                Console.ResetColor();
                return 1;
            }

            if (_help)
            {
                ShowHelp(options);
                return 0;
            }

            if (InputPaths.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Must supply at least one input path using -i");
                Console.ResetColor();
                return 1;
            }

            if (OutputPaths.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Must supply at least one output path using -o");
                Console.ResetColor();
                return 1;
            }

            if (InputPaths.Count != OutputPaths.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Number of input paths must equal number of output paths");
                Console.ResetColor();
                return 1;
            }

            try
            {
                var rewriter = new AssemblyRewriter(_verbose);
                rewriter.RewriteNamespaces(InputPaths, OutputPaths);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                return 1;
            }

            if (!_merge) return 0;
            try
            {
                var repackOptions = new ILRepacking.RepackOptions
                {
                    Internalize = true,
                    Closed = true,
                    KeepOtherVersionReferences = false,
                    TargetKind = ILRepack.Kind.SameAsPrimaryAssembly,
                    InputAssemblies = OutputPaths.ToArray(),
                    //LineIndexation = true //https://github.com/gluck/il-repack/blob/96cd4ecf7a0f68e5dadd0a04c774f57e9c46cb56/ILRepack/IKVMLineIndexer.cs
                    OutputFile = OutputPaths.First(),
                    KeyFile = _keyFile,
                    SearchDirectories = OutputPaths.Select(p=> new DirectoryInfo(p).FullName).Distinct()
                };

                var pack = new ILRepacking.ILRepack(repackOptions, new RepackConsoleLogger());
                pack.Repack();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                return 2;
            }
            return 0;
        }

        private static void ShowHelp(OptionSet options)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("AssemblyRewriter");
            Console.WriteLine("----------------");
            Console.WriteLine("Rewrites assemblies and namespaces");
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine("Each input path must have a corresponding output path");
            Console.ResetColor();
        }
    }
}

